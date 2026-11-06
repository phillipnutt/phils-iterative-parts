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
        public const string StandInSuffix = "-stock";

        #region Which chassis a model uses (DESIGN.md section 17)

        // A model's chassis: the preferred one when its part is installed, else its
        // stock stand-in ("<name>-stock"), else null (nothing to make the part from).
        public static string Resolve(string chassis)
        {
            if (string.IsNullOrEmpty(chassis) || PartLoader.Instance == null) return null;
            if (PartLoader.getPartInfoByName(chassis) != null) return chassis;
            string stock = chassis + StandInSuffix;
            return PartLoader.getPartInfoByName(stock) != null ? stock : null;
        }

        public static AvailablePart Info(string name) => string.IsNullOrEmpty(name) || PartLoader.Instance == null ? null : PartLoader.getPartInfoByName(name);

        public static string ChassisFor(PartType t, string[] model) => t.Valid(model) ? Resolve(t.ChassisPart(model)) : null;

        public static bool Made(PartType t, string[] model) => ChassisFor(t, model) != null;

        // What to install for a model whose chassis can't be made ("Near Future
        // Propulsion"), from the PIP_NEEDS notes the chassis generators write.
        static Dictionary<string, string> needs;
        public static string Needs(PartType t, string[] model)
        {
            if (needs == null)
            {
                needs = new Dictionary<string, string>();
                if (GameDatabase.Instance != null)
                    foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("PIP_NEEDS"))
                        if (n.HasValue("chassis") && n.HasValue("needs")) needs[n.GetValue("chassis")] = n.GetValue("needs");
            }
            return needs.TryGetValue(t.ChassisPart(model), out string what) ? what : "a part that isn't installed";
        }

        // The installed part a type's standard model is copied from: its standard look.
        // That's the type's preferred part when installed, else the stock stand-in's part.
        public static string StandardSource(PartType t, string[] model)
        {
            string preferred = t.DefaultLook(model);
            if (!string.IsNullOrEmpty(preferred) && PartLoader.getPartInfoByName(preferred) != null) return preferred;
            Part prefab = Info(ChassisFor(t, model))?.partPrefab;
            string source = prefab?.FindModuleImplementing<ModulePipEngine>()?.chassisSource;
            if (string.IsNullOrEmpty(source)) source = prefab?.FindModuleImplementing<ModulePipPart>()?.chassisSource;
            return string.IsNullOrEmpty(source) ? preferred : source;
        }

        // One line for the log at the main menu: how every model of every type will be
        // made here (preferred part, stock stand-in, or padlocked for a missing mod).
        public static string ModelReport()
        {
            int preferred = 0;
            var standIns = new List<string>();
            var locked = new List<string>();
            foreach (PartType t in PartTypes.All)
                foreach (string[] m in AllModels(t))
                {
                    if (!t.Valid(m)) continue;
                    string c = ChassisFor(t, m);
                    if (c == null) locked.Add($"{t.ChassisPart(m)} (needs {Needs(t, m)})");
                    else if (c.EndsWith(StandInSuffix, StringComparison.Ordinal)) standIns.Add($"{t.ChassisPart(m)}: {StandardSource(t, m)}");
                    else preferred++;
                }
            return $"[PIP] Models: {preferred} preferred, {standIns.Count} stock stand-ins, {locked.Count} need a mod" +
                   (locked.Count > 0 ? "\n[PIP] Need a mod: " + string.Join(", ", locked) : "") +
                   (standIns.Count > 0 ? "\n[PIP] Stand-ins: " + string.Join(", ", standIns) : "");
        }

        static IEnumerable<string[]> AllModels(PartType t)
        {
            IEnumerable<string[]> all = new[] { new string[0] };
            foreach (Choice c in t.Choices)
            {
                Choice choice = c;
                all = all.SelectMany(a => choice.Options.Select(o => a.Concat(new[] { o.Code }).ToArray())).ToList();
            }
            return all;
        }

        // Why a program has no part right now, or null when it has one.
        public static string Problem(Program p)
        {
            if (p == null || made.ContainsKey(p.PartName) || made.ContainsKey(p.SidePartName)) return null;
            return Made(p.Type, p.Model) ? null : $"Its model needs {Needs(p.Type, p.Model)}, which isn't installed, so it has no part right now.";
        }

        #endregion

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

        // The size a part is: its program's own; for programs from before sizes were
        // fixed, the side-mount part's first size (the in-line part's is picked on it).
        public static string ListVariant(Program p, string partName) =>
            p.FixedSize ? p.Variant : IsSidePart(partName) ? p.Type.DefaultSideVariant(p.Model) : null;

        // Makes (or refreshes) the parts for a program. Safe to call repeatedly.
        // A program with its own size makes one part, in-line or side-mount
        // (DESIGN.md section 14); older programs make both, sized on the part.
        public static AvailablePart Ensure(Program p)
        {
            if (p == null || PartLoader.Instance == null) return null;
            if (p.FixedSize && p.IsSide)
            {
                string sideOnly = SideChassis(p);
                if (sideOnly != null) return Make(p, p.SidePartName, sideOnly);
                Debug.LogError($"[PIP] No side-mount chassis for program {p.Name}");
                return null;
            }
            if (p.FixedSize && p.IsAdapter)
            {
                string adapter = AdapterChassis(p);
                if (adapter != null) return Make(p, p.PartName, adapter);
                Debug.LogError($"[PIP] No adapter of {p.Name}'s ratio is installed");
                return null;
            }
            AvailablePart ap = Make(p, p.PartName, InlineChassis(p));
            if (!p.FixedSize && p.Type.HasSideMount && p.Type.HasLooks)
            {
                string side = SideChassis(p);
                if (side != null) Make(p, p.SidePartName, side);
            }
            return ap;
        }

        // A program without a side-mount look gets the default one, kept from then on.
        static string SideChassis(Program p)
        {
            if (!p.Type.HasSideMount || !p.Type.HasLooks) return null;
            if (string.IsNullOrEmpty(p.SideLook)) p.SideLook = Looks.DefaultSide(p.Type, p.Model) ?? "";
            return Looks.Chassis(p.Type, true, p.SideLook, p.Model);
        }

        // An adapter program's look (DESIGN.md 20.2), kept in SideLook. If the mod it
        // came from is gone, the ratio's default stands in until it's back.
        static string AdapterChassis(Program p)
        {
            if (!p.Type.HasAdapters || !p.Type.HasLooks) return null;
            if (string.IsNullOrEmpty(p.SideLook)) p.SideLook = Looks.DefaultAdapter(p.Type, p.Model, p.Variant) ?? "";
            return Looks.Chassis(p.Type, "A", p.SideLook, p.Model, p.Variant)
                   ?? Looks.Chassis(p.Type, "A", Looks.DefaultAdapter(p.Type, p.Model, p.Variant), p.Model, p.Variant);
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
            string standard = ChassisFor(p.Type, p.Model) ?? p.ChassisPart;
            if (string.IsNullOrEmpty(p.Look)) return standard;
            string look = Looks.Chassis(p.Type, false, p.Look, p.Model);
            if (look != null) return look;
            Debug.LogWarning($"[PIP] {p.Name}'s look {p.Look} isn't installed; it uses PIP's own model");
            return standard;
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
            ap.title = p.DisplayName(mk) + (side ? " (side-mount)" : p.FixedSize && p.IsAdapter ? " (adapter)" : "");
            ap.cost = (float)Math.Round(s.BuildPrice);
            string chassis = chassisOf.TryGetValue(ap.name, out string c) ? c : ChassisFor(p.Type, p.Model) ?? p.ChassisPart;
            ap.moduleInfos = PartInfo.ModuleInfos(PartLoader.getPartInfoByName(chassis), p, s, side);
            ap.description = $"{p.Label}{(side && !p.FixedSize ? ", side-mount" : "")}. Program {p.Title}, now at {Progress.MkName(mk)}.\n" + Describe(p.Type, p.Model, s);
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
