using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PIP
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class PipHarmony : MonoBehaviour
    {
        void Awake()
        {
            try
            {
                new Harmony("PhilsIterativeParts").PatchAll(typeof(PipHarmony).Assembly);
                Debug.Log("[PIP] Harmony patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[PIP] Harmony patching failed: " + e);
            }
        }
    }

    // Optional mods change what some categories do. Checked once at the main
    // menu, when every mod's assemblies are loaded.
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class PipModCheck : MonoBehaviour
    {
        void Start()
        {
            // Kerbalism's radiation comes from its Emitter part module.
            NuclearEngines.Kerbalism = AssemblyLoader.GetClassByName(typeof(PartModule), "Emitter") != null;
            Debug.Log($"[PIP] Kerbalism {(NuclearEngines.Kerbalism ? "found: nuclear engines get Shielding" : "not found: nuclear engines get Gimbal")}");
        }
    }

    // The load dialog checks a save's vessels for missing parts as soon as it
    // reads the file, before the Game exists, so program parts are registered here
    // too. Without this, a fresh session offers to drop every vessel carrying one.
    [HarmonyPatch(typeof(GamePersistence), nameof(GamePersistence.LoadSFSFile))]
    static class LoadSfsPatch
    {
        static void Postfix(ConfigNode __result)
        {
            try { PartFactory.RegisterFromSave(__result?.GetNode("GAME") ?? __result); }
            catch (Exception e) { Debug.LogError("[PIP] Registering program parts from the save file failed: " + e); }
        }
    }

    // The load dialog's own check ("Vessel X is missing part Y") reads the save
    // file by itself, before anything else; register that save's program parts first.
    [HarmonyPatch]
    static class LoadDialogCheckPatch
    {
        static readonly System.Collections.Generic.Dictionary<string, long> seen = new System.Collections.Generic.Dictionary<string, long>();

        static System.Reflection.MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("LoadGameDialog+PlayerProfileInfo"), "AreVesselsInFlightCompatible");

        static void Prefix(string filename, string saveFolder)
        {
            try
            {
                string name = filename.EndsWith(".sfs", StringComparison.OrdinalIgnoreCase) ? filename : filename + ".sfs";
                string path = System.IO.Path.Combine(System.IO.Path.Combine(KSPUtil.ApplicationRootPath, "saves"), System.IO.Path.Combine(saveFolder, name));
                if (!System.IO.File.Exists(path)) return;
                // Each save file is read once per version of it.
                long stamp = System.IO.File.GetLastWriteTimeUtc(path).Ticks;
                if (seen.TryGetValue(path, out long done) && done == stamp) return;
                seen[path] = stamp;
                ConfigNode root = ConfigNode.Load(path);
                PartFactory.RegisterFromSave(root?.GetNode("GAME") ?? root);
            }
            catch (Exception e) { Debug.LogError("[PIP] Registering program parts for the load dialog failed: " + e); }
        }
    }

    // Register program parts before a save's vessels look for them.
    [HarmonyPatch(typeof(Game), MethodType.Constructor, new Type[] { typeof(ConfigNode) })]
    static class GameConstructorPatch
    {
        static void Prefix(ConfigNode root)
        {
            try { PartFactory.RegisterFromSave(root); }
            catch (Exception e) { Debug.LogError("[PIP] Registering program parts failed: " + e); }
        }
    }

    // Program parts are available when their program exists in this game and
    // isn't retired; chassis parts never are.
    static class Availability
    {
        public static bool Decide(AvailablePart ap, out bool available)
        {
            available = false;
            if (PartFactory.IsChassis(ap)) return true;
            if (!PartFactory.IsProgramPart(ap)) return false;
            Program p = PipScenario.Instance?.Find(PartFactory.ProgramIdOf(ap.name));
            available = p != null && !p.Retired && PipSettings.On(p.Type);
            return true;
        }
    }

    [HarmonyPatch(typeof(ResearchAndDevelopment), nameof(ResearchAndDevelopment.PartModelPurchased))]
    static class PurchasedPatch
    {
        static bool Prefix(AvailablePart ap, ref bool __result)
        {
            if (!Availability.Decide(ap, out bool v)) return true;
            __result = v;
            return false;
        }
    }

    [HarmonyPatch(typeof(ResearchAndDevelopment), nameof(ResearchAndDevelopment.PartTechAvailable))]
    static class TechAvailablePatch
    {
        static bool Prefix(AvailablePart ap, ref bool __result)
        {
            if (!Availability.Decide(ap, out bool v)) return true;
            __result = v;
            return false;
        }
    }

    // The VAB parts list filters through these private instance versions.
    [HarmonyPatch(typeof(ResearchAndDevelopment), "partModelPurchased")]
    static class PurchasedInstancePatch
    {
        static bool Prefix(AvailablePart ap, ref bool __result)
        {
            if (!Availability.Decide(ap, out bool v)) return true;
            __result = v;
            return false;
        }
    }

    [HarmonyPatch(typeof(ResearchAndDevelopment), "partTechAvailable")]
    static class TechAvailableInstancePatch
    {
        static bool Prefix(AvailablePart ap, ref bool __result)
        {
            if (!Availability.Decide(ap, out bool v)) return true;
            __result = v;
            return false;
        }
    }
}
