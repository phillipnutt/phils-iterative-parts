using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PIP
{
    // Each program is its own part in the parts list ("Kestrel Mk. 4"). The part
    // is a copy of a hidden chassis part, made at runtime and named PIP.<program id>:
    // PIP's own chassis for its model, or the chassis of the look it picked
    // (Looks.cs). Types with a side-mount part also get PIP.<program id>.side.
    // Parts are registered before a save's vessels load (see HarmonyPatches), so
    // ships carrying them always find them.
    public static class PartFactory
    {
        public const string ProgramPrefix = "PIP.";
        public const string ChassisPrefix = "PIP-";
        public const string SideSuffix = ".side";

        static readonly Dictionary<string, AvailablePart> made = new Dictionary<string, AvailablePart>();
        static readonly Dictionary<string, string> chassisOf = new Dictionary<string, string>();
        static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo FinderByName = typeof(PartLoader).GetField("APFinderByName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool IsProgramPart(AvailablePart ap) => ap?.name != null && ap.name.StartsWith(ProgramPrefix, StringComparison.Ordinal);
        public static bool IsChassis(AvailablePart ap) => ap?.name != null && ap.name.StartsWith(ChassisPrefix, StringComparison.Ordinal);
        public static bool IsPip(AvailablePart ap) => IsProgramPart(ap) || IsChassis(ap);
        public static bool IsSidePart(string partName) => partName != null && IsProgramName(partName) && partName.EndsWith(SideSuffix, StringComparison.Ordinal);
        static bool IsProgramName(string n) => n.StartsWith(ProgramPrefix, StringComparison.Ordinal);

        // "PIP.<id>" or "PIP.<id>.side" -> "<id>".
        public static string ProgramIdOf(string partName)
        {
            if (partName == null || !IsProgramName(partName)) return null;
            string rest = partName.Substring(ProgramPrefix.Length);
            int dot = rest.IndexOf('.');
            return dot < 0 ? rest : rest.Substring(0, dot);
        }

        // The variant a part shows in the parts list: the side-mount part's own sizes.
        public static string ListVariant(Program p, string partName) =>
            IsSidePart(partName) ? p.Type.DefaultSideVariant(p.Model) : null;

        // Makes (or refreshes) the parts for a program. Safe to call repeatedly.
        public static AvailablePart Ensure(Program p)
        {
            if (p == null || PartLoader.Instance == null) return null;
            AvailablePart ap = Make(p, p.PartName, InlineChassis(p));
            if (p.Type.HasSideMount && p.Type.HasLooks)
            {
                // A program without a side-mount look gets the default one, kept from then on.
                if (string.IsNullOrEmpty(p.SideLook)) p.SideLook = Looks.DefaultSide(p.Type, p.Model) ?? "";
                string side = Looks.Chassis(p.Type, true, p.SideLook, p.Model);
                if (side != null) Make(p, p.SidePartName, side);
            }
            return ap;
        }

        // Every part a program has made (one, or two with a side-mount part).
        public static IEnumerable<AvailablePart> PartsOf(Program p)
        {
            if (p == null) yield break;
            if (made.TryGetValue(p.PartName, out AvailablePart a)) yield return a;
            if (made.TryGetValue(p.SidePartName, out AvailablePart b)) yield return b;
        }

        static string InlineChassis(Program p)
        {
            if (string.IsNullOrEmpty(p.Look)) return p.ChassisPart;
            string look = Looks.Chassis(p.Type, false, p.Look, p.Model);
            if (look != null) return look;
            Debug.LogWarning($"[PIP] {p.Name}'s look {p.Look} isn't installed; it uses PIP's own model");
            return p.ChassisPart;
        }

        static AvailablePart Make(Program p, string partName, string chassisName)
        {
            if (!made.TryGetValue(partName, out AvailablePart ap))
            {
                ap = PartLoader.getPartInfoByName(partName);
                if (ap == null)
                {
                    AvailablePart chassis = PartLoader.getPartInfoByName(chassisName);
                    if (chassis == null)
                    {
                        Debug.LogError($"[PIP] No chassis part {chassisName} for program {p.Name}");
                        return null;
                    }
                    ap = (AvailablePart)Clone.Invoke(chassis, null);
                    ap.name = partName;
                    ap.category = CategoryOf(p.Type);
                    // Filed under the type's first-programs node and hidden from R&D.
                    // The launch check reads that node's purchased list directly, so
                    // EnsurePurchased adds it there; the editor list asks HarmonyPatches.
                    ap.TechHidden = true;
                    ap.TechRequired = p.Type.CapTechs[0];
                    ap.manufacturer = "Phil's Iterative Parts";
                    ap.tags = $"pip program {p.Type.Noun} {p.Type.Id} {p.Name.ToLowerInvariant()}" + (IsSidePart(partName) ? " side radial" : "");
                    PartLoader.LoadedPartsList.Add(ap);
                    // getPartInfoByName reads this dictionary, not the list.
                    if (FinderByName?.GetValue(PartLoader.Instance) is Dictionary<string, AvailablePart> finder) finder[ap.name] = ap;
                    Debug.Log($"[PIP] Made part {ap.name} for {p.Name} from {chassis.name}");
                }
                made[partName] = ap;
                chassisOf[partName] = chassisName;
            }
            Refresh(ap, p);
            return ap;
        }

        // The VAB tab a program's part shows under.
        public static PartCategories CategoryOf(PartType t)
        {
            switch (t.Id)
            {
                case "rcs": case "wheel": return PartCategories.Control;
                case "battery": case "solar": case "rtg": return PartCategories.Electrical;
                case "shield": return PartCategories.Thermal;
                case "antenna": return PartCategories.Communication;
                case "probe": return PartCategories.Pods;
                case "tank": return PartCategories.FuelTank;
                case "intake": return PartCategories.Aero;
                default: return PartCategories.Engine;
            }
        }

        // Career and science games: mark program parts as purchased in their tech
        // node, which is what KSP's pre-launch check looks at.
        public static void EnsurePurchased(IEnumerable<Program> programs)
        {
            foreach (Program p in programs)
            {
                Ensure(p);
                // Sandbox has no R&D: the parts just need to exist.
                if (ResearchAndDevelopment.Instance == null) continue;
                foreach (AvailablePart ap in PartsOf(p))
                {
                    ProtoTechNode node = ResearchAndDevelopment.Instance.GetTechState(ap.TechRequired);
                    if (node == null) continue;
                    if (node.partsPurchased == null) node.partsPurchased = new List<AvailablePart>();
                    if (!node.partsPurchased.Contains(ap)) node.partsPurchased.Add(ap);
                }
            }
        }

        public static void Refresh(Program p)
        {
            foreach (AvailablePart ap in PartsOf(p).ToList()) Refresh(ap, p);
        }

        static void Refresh(AvailablePart ap, Program p)
        {
            int mk = p.Mk;
            bool side = IsSidePart(ap.name);
            string variant = ListVariant(p, ap.name);
            Stats s = p.Stats(mk, PipSettings.Failures, variant);
            ap.title = p.DisplayName(mk) + (side ? " (side-mount)" : "");
            ap.cost = (float)Math.Round(s.BuildPrice);
            string chassis = chassisOf.TryGetValue(ap.name, out string c) ? c : p.ChassisPart;
            ap.moduleInfos = PartInfo.ModuleInfos(PartLoader.getPartInfoByName(chassis), p, s, side);
            ap.description = $"{p.Label}{(side ? ", side-mount" : "")}. Program {p.Name}, now at {Progress.MkName(mk)}.\n" + Describe(p.Type, p.Model, s);
        }

        // "Thrust, vacuum 205 kN. Isp, vacuum 300 s. ..." from the type's main rows.
        public static string Describe(PartType t, string[] model, Stats s) =>
            string.Join(" ", t.Rows(model).Where(r => r.Category >= 0 && t.CategoryUsed(model, r.Category))
                .Select(r => $"{r.Label.Trim()}: {r.Format(s[r.Key])}."));

        // Called when a save loads, before its vessels: parts for every program in it.
        public static void RegisterFromSave(ConfigNode root)
        {
            if (root == null) return;
            int count = 0;
            foreach (ConfigNode scenario in root.GetNodes("SCENARIO"))
            {
                if (scenario.GetValue("name") != nameof(PipScenario)) continue;
                foreach (ConfigNode n in scenario.GetNodes("PROGRAM"))
                {
                    Program p = Program.FromValues(n.GetValue);
                    if (p != null && Ensure(p) != null) count++;
                }
            }
            if (count > 0) Debug.Log($"[PIP] Registered {count} program parts before loading the save");
        }
    }
}
