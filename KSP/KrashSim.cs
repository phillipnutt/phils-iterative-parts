using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PIP
{
    // While KRASH says a sim is running, every MechJeb feature and Kerbal Engineer's flight
    // readouts are on, whatever you've researched. When the sim ends, you're back to your real
    // research (Phil, 2026-11-15; DESIGN.md 30). Buildings stay as they are: a MechJeb feature
    // that needs a Tracking Station upgrade still needs it in a sim.
    //
    // Nothing is written anywhere. MechJeb checks its features against research every frame in
    // a career, and KER asks every frame whether to show its readouts, so the answer changes on
    // the frame the sim starts or ends.
    //
    // Each mod is found by name. If one is missing, or the members used here have changed, its
    // patch isn't applied and PIP logs one line: a failure always means your real research.
    static class KrashSim
    {
        static object api;
        static MethodInfo active;
        static bool? installed;
        static int frame = -1;
        static bool running;

        // KRASH's public API (README4Modders.txt): APIManager.ApiInstance.simAPI.simulationActive().
        // PIP calls it by reflection rather than copying KRASH's wrapper, which is GPL.
        public static bool Installed
        {
            get
            {
                if (installed.HasValue) return installed.Value;
                try
                {
                    Type manager = AccessTools.TypeByName("KRASH.APIManager");
                    object instance = manager?.GetProperty("ApiInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
                    api = instance == null ? null : manager.GetField("simAPI", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
                    active = api?.GetType().GetMethod("simulationActive", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    installed = active != null && active.ReturnType == typeof(bool);
                    if (!installed.Value)
                        Debug.Log(manager == null ? "[PIP] KRASH not found" : "[PIP] KRASH has changed: sims keep your research");
                }
                catch (Exception e)
                {
                    installed = false;
                    Debug.Log("[PIP] KRASH has changed: sims keep your research (" + e.Message + ")");
                }
                return installed.Value;
            }
        }

        // Asked once a frame, and only in flight: KRASH's answer is meaningless anywhere else.
        public static bool Running
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight || !Installed) return false;
                if (Time.frameCount != frame)
                {
                    frame = Time.frameCount;
                    try { running = (bool)active.Invoke(api, null); }
                    // KRASH's flight menu isn't there yet in a flight's first frame.
                    catch (Exception) { running = false; }
                }
                return running;
            }
        }
    }

    // MechJeb (2.15.4): ComputerModule.UnlockCheck locks a feature whose research or building
    // is missing; a window's own override calls it and hides the window when it locks. In a sim,
    // a feature whose building is there skips the check, so research can't lock it. A missing
    // building still runs MechJeb's own check, which locks it as usual.
    [HarmonyPatch]
    static class MechJebSimPatch
    {
        static MethodInfo target, building;
        static bool? ready;

        static bool Prepare()
        {
            if (ready.HasValue) return ready.Value;
            Type module = AccessTools.TypeByName("MuMech.ComputerModule");
            if (module == null) return (ready = false).Value; // MechJebLimits logs "MechJeb not found"
            target = AccessTools.Method(module, "UnlockCheck", Type.EmptyTypes);
            building = AccessTools.Method(module, "IsSpaceCenterUpgradeUnlocked", Type.EmptyTypes);
            bool members = target != null && building?.ReturnType == typeof(bool);
            ready = members && KrashSim.Installed;
            if (!members) Debug.Log("[PIP] MechJeb has changed: KRASH sims keep your research for MechJeb");
            else if (ready.Value) Debug.Log("[PIP] KRASH found: sims get every MechJeb feature your buildings allow");
            return ready.Value;
        }

        static MethodBase TargetMethod() => target;

        static bool Prefix(object __instance)
        {
            if (!KrashSim.Running) return true;
            try { return !(bool)building.Invoke(__instance, null); }
            catch (Exception) { return true; }
        }
    }

    // Kerbal Engineer (1.1.9.5): FlightEngineerCore.IsDisplayable says whether its flight
    // readouts show. In a sim they do, unless the UI is hidden (F2), which KER's own answer
    // already covers.
    [HarmonyPatch]
    static class KerSimPatch
    {
        static MethodInfo target;
        static bool? ready;

        static bool Prepare()
        {
            if (ready.HasValue) return ready.Value;
            Type core = AccessTools.TypeByName("KerbalEngineer.Flight.FlightEngineerCore");
            if (core == null) return (ready = false).Value;
            target = AccessTools.PropertyGetter(core, "IsDisplayable");
            bool members = target != null && target.IsStatic && target.ReturnType == typeof(bool);
            ready = members && KrashSim.Installed;
            if (!members) Debug.Log("[PIP] Kerbal Engineer has changed: KRASH sims keep your research for it");
            else if (ready.Value) Debug.Log("[PIP] KRASH found: sims show Kerbal Engineer's flight readouts");
            return ready.Value;
        }

        static MethodBase TargetMethod() => target;

        static void Postfix(ref bool __result)
        {
            if (__result || !KrashSim.Running) return;
            if (MainCanvasUtil.MainCanvas != null && !MainCanvasUtil.MainCanvas.enabled) return;
            __result = true;
        }
    }
}
