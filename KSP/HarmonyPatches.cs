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

    // R&D's four "can this part be used" questions: purchased and tech available,
    // each as a public static and a private instance version (the VAB parts list
    // filters through the instance ones). All four get Availability's answer.
    [HarmonyPatch]
    static class AvailabilityPatch
    {
        static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (string name in new[] { nameof(ResearchAndDevelopment.PartModelPurchased), nameof(ResearchAndDevelopment.PartTechAvailable),
                                            "partModelPurchased", "partTechAvailable" })
                yield return AccessTools.Method(typeof(ResearchAndDevelopment), name, new[] { typeof(AvailablePart) });
        }

        static bool Prefix(AvailablePart ap, ref bool __result)
        {
            if (!Availability.Decide(ap, out bool v)) return true;
            __result = v;
            return false;
        }
    }
}

namespace PIP
{
    // A craft loaded in the VAB is built new (DESIGN.md 18.1 and 26.3): PipApi.ToCurrent,
    // the same step a Space Center build takes, makes each PIP part the family's newest
    // version (an older version's part is renamed) at its current Mk before KSP builds
    // the ship, except built hardware another mod brought in (PipApi.IsHardware, 26.4).
    // Loading, merging, subassemblies and undo all come through here. Parts keep their
    // place and settings. Every LoadShip overload (they may call each other; doing it
    // twice is harmless).
    [HarmonyLib.HarmonyPatch]
    static class NewestVersionOnLoadPatch
    {
        static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
            typeof(ShipConstruct).GetMethods().Where(m => m.Name == nameof(ShipConstruct.LoadShip) && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(ConfigNode));

        static void Prefix(ConfigNode root)
        {
            try
            {
                if (root == null || !HighLogic.LoadedSceneIsEditor) return;
                PipApi.ToCurrent(root, out int swapped, true);
                if (swapped > 0)
                {
                    UnityEngine.Debug.Log($"[PIP] Craft loaded with {swapped} parts moved to their program's newest version");
                    ScreenMessages.PostScreenMessage($"{swapped} part{(swapped == 1 ? "" : "s")} updated to the newest program version.", 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("[PIP] Updating a craft to the newest versions failed: " + e);
            }
        }
    }
}

namespace PIP
{
    // A look keeps its installed part's paint variants (ModulePartVariants), and only
    // their paint (Looks.Build drops their node moves). But KSP makes each variant as
    // a copy of the part's base variant, which carries the look's own unscaled stack
    // nodes, and applying one puts the nodes back there: changing a held part's
    // variant in the VAB left a shrunk tank's top and bottom nodes at the full-size
    // model's spacing (DESIGN.md 27.1). PIP's size switch owns a look's nodes, so a
    // variant applied to a look moves none.
    [HarmonyPatch(typeof(ModulePartVariants), nameof(ModulePartVariants.ApplyVariant),
        new[] { typeof(Part), typeof(Transform), typeof(PartVariant), typeof(Material[]), typeof(bool), typeof(int) })]
    static class LookVariantNodesPatch
    {
        static readonly System.Reflection.FieldInfo SrfNode = AccessTools.Field(typeof(PartVariant), "srfAttachNode");

        static void Prefix(Part part, PartVariant variant)
        {
            try
            {
                if (part == null || variant == null) return;
                ModulePipPart pip = part.FindModuleImplementing<ModulePipPart>();
                if (pip == null || !pip.look) return;
                variant.AttachNodes?.Clear();
                SrfNode?.SetValue(variant, null);
            }
            catch (Exception e) { Debug.LogError("[PIP] Keeping a look's nodes through a variant change failed: " + e); }
        }
    }
}
