using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PIP
{
    // MechJeb on a PIP probe unlocks feature by feature with research, exactly like on any
    // other probe (Phil, 2026-11-13; DESIGN.md 29.5).
    //
    // MechJeb (2.15.4) finds a part's research limits by part name in a list it builds while
    // the game loads. PIP's program parts are made later, so MechJeb found none and turned
    // every feature on. It also saves the limits into craft files and vessels, so the ones
    // saved since then carry "no limits". Right after MechJeb loads on a PIP part, this sets
    // each feature's limits from the part's config, which is the chassis's as ModuleManager
    // patched it: the same values a stock probe gets. MechJeb checks them every frame in a
    // career, so a feature locks or unlocks on the next frame, and its next save stores them.
    //
    // MechJeb has no API for this, so the patch uses its public members: GetComputerModules
    // and each feature's unlockTechs and unlockParts. If MechJeb is missing, or any of those
    // has changed, nothing is patched and PIP probes keep MechJeb's own behaviour.
    [HarmonyPatch]
    static class MechJebLimitsPatch
    {
        const string CoreType = "MuMech.MechJebCore", ModuleType = "MuMech.ComputerModule";
        static MethodInfo target, modules;
        static FieldInfo techs, parts;
        static bool? ready;
        static bool failed;

        static bool Prepare(MethodBase original)
        {
            if (ready.HasValue) return ready.Value;
            Type core = AccessTools.TypeByName(CoreType);
            if (core == null)
            {
                Debug.Log("[PIP] MechJeb not found");
                return (ready = false).Value;
            }
            Type module = AccessTools.TypeByName(ModuleType);
            target = AccessTools.Method(core, "OnLoad", new[] { typeof(ConfigNode) });
            MethodInfo list = core.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetComputerModules" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            techs = module?.GetField("unlockTechs", BindingFlags.Public | BindingFlags.Instance);
            parts = module?.GetField("unlockParts", BindingFlags.Public | BindingFlags.Instance);
            try { modules = module == null ? null : list?.MakeGenericMethod(module); }
            catch (Exception) { modules = null; }
            ready = target != null && modules != null && typeof(IEnumerable).IsAssignableFrom(modules.ReturnType)
                && techs?.FieldType == typeof(string) && parts?.FieldType == typeof(string);
            Debug.Log(ready.Value
                ? "[PIP] MechJeb found: PIP probes get their chassis's MechJeb research limits"
                : "[PIP] MechJeb has changed: PIP probes keep MechJeb's own behaviour");
            return ready.Value;
        }

        static MethodBase TargetMethod() => target;

        static void Postfix(PartModule __instance)
        {
            if (failed) return;
            try
            {
                Part part = __instance.part;
                if (part == null || part.partInfo == null || part.GetComponent<ModulePipPart>() == null) return;
                ConfigNode limits = part.partInfo.partConfig?.GetNodes("MODULE")
                    .FirstOrDefault(n => n.GetValue("name") == "MechJebCore")?.GetNode("MechJebLocalSettings");
                if (limits == null) return;
                // A feature with no entry has no limits, as on a stock probe.
                foreach (object feature in (IEnumerable)modules.Invoke(__instance, null))
                {
                    ConfigNode n = limits.GetNode(feature.GetType().Name);
                    techs.SetValue(feature, n?.GetValue("unlockTechs") ?? "");
                    parts.SetValue(feature, n?.GetValue("unlockParts") ?? "");
                }
            }
            catch (Exception e)
            {
                failed = true;
                Debug.LogError("[PIP] Setting MechJeb's research limits on a PIP part failed; PIP probes keep MechJeb's own behaviour: " + e);
            }
        }
    }
}
