using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KSP.Localization;
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

        public static bool IsProgramPart(AvailablePart ap) => IsProgramPart(ap?.name);
        public static bool IsProgramPart(string partName) => partName != null && partName.StartsWith(ProgramPrefix, StringComparison.Ordinal);
        public static bool IsChassis(AvailablePart ap) => ap?.name != null && ap.name.StartsWith(ChassisPrefix, StringComparison.Ordinal);
        public static bool IsPip(AvailablePart ap) => IsProgramPart(ap) || IsChassis(ap);
        public static bool IsSidePart(string partName) => IsProgramPart(partName) && partName.EndsWith(SideSuffix, StringComparison.Ordinal);

        // "PIP.<id>" or "PIP.<id>.side" -> "<id>".
        public static string ProgramIdOf(string partName)
        {
            if (!IsProgramPart(partName)) return null;
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
                    ap.partPrefab = MasterCopy(chassis.partPrefab, ap) ?? chassis.partPrefab;
                    ap.category = CategoryOf(p.Type);
                    // Filed under the type's first-programs node and hidden from R&D.
                    // The launch check reads that node's purchased list directly, so
                    // EnsurePurchased adds it there; the editor list asks HarmonyPatches.
                    ap.TechHidden = true;
                    ap.TechRequired = p.Type.CapTechs[0];
                    ap.manufacturer = "Phil's Iterative Parts";
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
                case "tank": case "plane": return PartCategories.FuelTank;
                case "intake": return PartCategories.Aero;
                case "decoupler": return PartCategories.Coupling;
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

        // A program part's title (DESIGN.md 26.2): "Augur I Mk. 1", with the Mk only
        // when it's a part's own (mk 0: none). The parts list's title is shared by every
        // part built from it, whatever Mk each was built at, so it has no Mk.
        // programTitle: the version the part was built as, if not p's own title.
        public static string TitleOf(Program p, string partName, string programTitle, int mk)
        {
            string title = programTitle ?? p?.Title ?? "PIP part";
            if (mk >= Progress.MinMk) title += " " + Progress.MkName(mk);
            if (IsSidePart(partName)) return title + " (side-mount)";
            if (p != null && p.FixedSize && p.IsAdapter) title += $" ({(p.Type == PartTypes.Tank ? "adapter" : p.Type.MountName("A"))})";
            return title;
        }

        // A built part's own title, from its PIP module; null for a part that isn't PIP's.
        public static string OwnTitle(Part part)
        {
            if (part?.partInfo == null) return null;
            foreach (PartModule m in part.Modules)
            {
                if (m is ModulePipPart pp && pp.Assigned) return OwnTitle(part, pp.programId, pp.programName, pp.mk);
                if (m is ModulePipEngine pe && pe.Assigned) return OwnTitle(part, pe.programId, pe.programName, pe.mk);
            }
            return null;
        }

        static string OwnTitle(Part part, string programId, string programName, int mk) =>
            TitleOf(PipScenario.Instance?.Find(programId), part.partInfo.name, programName, mk);

        static void Refresh(AvailablePart ap, Program p)
        {
            int mk = p.Mk;
            bool side = IsSidePart(ap.name);
            string variant = ListVariant(p, ap.name);
            Stats s = p.Stats(mk, PipSettings.Failures, variant);
            ap.title = TitleOf(p, ap.name, null, 0);
            ap.cost = (float)Math.Round(s.BuildPrice);
            // VAB search: the name follows a rename.
            ap.tags = $"pip program {p.Type.Noun} {p.Type.Id} {p.Name.ToLowerInvariant()}" + (side ? " side radial" : "");
            string chassis = chassisOf.TryGetValue(ap.name, out string c) ? c : ChassisFor(p.Type, p.Model) ?? p.ChassisPart;
            AvailablePart from = PartLoader.getPartInfoByName(chassis);
            ap.moduleInfos = PartInfo.ModuleInfos(from, p, s, side);
            ap.resourceInfos = ResourceInfos(from?.resourceInfos ?? ap.resourceInfos, p, s);
            ap.description = $"{p.Label}{(side && !p.FixedSize ? ", side-mount" : "")}. Program {p.Title}, now at {Progress.MkName(mk)}.\n" + Describe(p.Type, p.Model, s);
            SetUpMasterCopy(ap, p, variant);
        }

        // Each program's part has its own master copy (KSP's partPrefab, DESIGN.md 29.3),
        // not its chassis's. What reads a part's master copy instead of the part (Kerbalism,
        // for a ship out of range: antenna speed, RTG power) then gets this program's numbers,
        // at its current Mk. Made the way KSP makes every placed part, then woken once so each
        // module knows its part and has its field lists and resources (a copy that never woke
        // has neither, and parts built from it lost their resources), and put back to sleep.
        static readonly HashSet<string> ownCopies = new HashSet<string>();
        static bool copyFailed;

        static Part MasterCopy(Part chassis, AvailablePart ap)
        {
            if (chassis == null || copyFailed) return null;
            try
            {
                Part copy = UnityEngine.Object.Instantiate(chassis);
                copy.gameObject.SetActive(true);
                copy.gameObject.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(copy.gameObject);
                copy.gameObject.name = ap.name;
                copy.partInfo = ap;
                ownCopies.Add(ap.name);
                return copy;
            }
            catch (Exception e)
            {
                // Without its own copy a program shares its chassis's, as before 29.3.
                copyFailed = true;
                Debug.LogError($"[PIP] Making {ap.name}'s own master copy failed; programs share their chassis's this session: {e}");
                return null;
            }
        }

        // The program's numbers at its current Mk on the copy's modules. PIP's own module on
        // it stays blank, so a part placed from the copy is set up from its program as always.
        static void SetUpMasterCopy(AvailablePart ap, Program p, string variant)
        {
            if (!ownCopies.Contains(ap.name) || ap.partPrefab == null) return;
            try
            {
                foreach (PartModule m in ap.partPrefab.Modules)
                {
                    if (m is ModulePipPart pp) pp.SetUpMasterCopy(p, variant);
                    else if (m is ModulePipEngine pe) pe.SetUpMasterCopy(p);
                }
            }
            catch (Exception e) { Debug.LogError($"[PIP] Setting {ap.name}'s master copy to {p.Title} {Progress.MkName(p.Mk)} failed: {e}"); }
        }

        public static bool HasOwnMasterCopy(string partName) => ownCopies.Contains(partName);

        // The VAB tooltip's resource lines come from the chassis (a Z-100's 100
        // charge), but the part holds what its program says, so those lines are
        // rewritten, in KSP's own words (PartLoader.CompilePartInfo). The chassis's
        // list is shared with it, so it's copied, never changed. A tank's contents are
        // picked on the part: an adapter look's placeholder propellant
        // (Looks.AddFirstPropellant) would show as "1.0" units, so tanks list none.
        static List<AvailablePart.ResourceInfo> ResourceInfos(List<AvailablePart.ResourceInfo> chassis, Program p, Stats s)
        {
            if (p.Type is TankKind || chassis == null) return new List<AvailablePart.ResourceInfo>();
            if (!ModulePipPart.Held(p.Type.Id, s, out string held, out double amount)) return chassis;
            var list = new List<AvailablePart.ResourceInfo>();
            foreach (AvailablePart.ResourceInfo r in chassis)
            {
                if (r.resourceName != held) list.Add(r);
                else if (amount > 0) list.Add(ResourceInfo(r, amount));
            }
            return list;
        }

        static AvailablePart.ResourceInfo ResourceInfo(AvailablePart.ResourceInfo was, double amount)
        {
            PartResourceDefinition def = PartResourceLibrary.Instance?.GetDefinition(was.resourceName);
            double density = def?.density ?? 0, unitCost = def?.unitCost ?? 0;
            return new AvailablePart.ResourceInfo
            {
                resourceName = was.resourceName,
                displayName = was.displayName,
                info = Localizer.Format("#autoLOC_166269", amount.ToString("F1"))
                       + Localizer.Format("#autoLOC_166270", (amount * density).ToString("F2"))
                       + Localizer.Format("#autoLOC_7001407")
                       + (unitCost > 0 ? Localizer.Format("#autoLOC_166271", (amount * unitCost).ToString("F2")) : ""),
                primaryInfo = $"<b>{was.displayName}: </b>{KSPUtil.LocalizeNumber(amount, "F1")}",
            };
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
