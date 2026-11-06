using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace PIP
{
    // "Replace stock": takes every non-PIP part of a replaced type off the parts
    // list and out of R&D. Which type a part is comes from what its engines
    // burn. Sepratrons, escape towers, RCS and launch-pad props are never touched.
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorHider : MonoBehaviour
    {
        const string FilterName = "PIP-HideReplaced";
        static readonly Dictionary<string, PartType> cache = new Dictionary<string, PartType>();
        static readonly HashSet<string> ElectricGases = new HashSet<string> { "XenonGas", "ArgonGas", "Lithium" };
        static readonly HashSet<string> NuclearFuels = new HashSet<string> { "LqdHydrogen", "LiquidFuel", "EnrichedUranium" };
        EditorPartListFilter<AvailablePart> filter;

        void Start()
        {
            if (EditorPartList.Instance == null) return;
            // Also hides program parts that don't belong to this game (made for
            // another save earlier in the session) or are retired. Career R&D does
            // this through HarmonyPatches; sandbox has no R&D filter, so it's done here.
            filter = new EditorPartListFilter<AvailablePart>(FilterName,
                ap => !IsReplaced(ap) && !(PartFactory.IsPip(ap) && !(Availability.Decide(ap, out bool ok) && ok)), "");
            EditorPartList.Instance.ExcludeFilters.AddFilter(filter);
            EditorPartList.Instance.Refresh();
        }

        void OnDestroy()
        {
            if (filter != null && EditorPartList.Instance != null) EditorPartList.Instance.ExcludeFilters.RemoveFilter(filter);
        }

        public static bool IsReplaced(AvailablePart ap) => PipSettings.Replacing(TypeOf(ap));

        // R&D: hidden parts are marked TechHidden, which R&D respects. Each part's
        // own setting is remembered so another game that doesn't replace it gets it back.
        static readonly Dictionary<AvailablePart, bool> originalTechHidden = new Dictionary<AvailablePart, bool>();

        public static void ApplyToResearch()
        {
            if (PartLoader.Instance == null) return;
            var counts = new Dictionary<string, int>();
            foreach (AvailablePart ap in PartLoader.LoadedPartsList)
            {
                PartType t = TypeOf(ap);
                if (t == null) continue;
                if (!originalTechHidden.ContainsKey(ap)) originalTechHidden[ap] = ap.TechHidden;
                bool hide = PipSettings.Replacing(t);
                ap.TechHidden = originalTechHidden[ap] || hide;
                if (hide) counts[t.Id] = (counts.TryGetValue(t.Id, out int c) ? c : 0) + 1;
            }
            Debug.Log("[PIP] Hidden in R&D: " + (counts.Count == 0 ? "nothing" : string.Join(", ", counts.Select(kv => $"{kv.Value} {kv.Key}"))));
            if (!tanksListed && PipSettings.Replacing(PartTypes.Tank)) ListTanks();
        }

        // Once a session: which fuel tanks Replace hides and which it leaves, by name.
        // Tanks are the type where "is this a plain tank?" is a judgment call.
        static bool tanksListed;
        static void ListTanks()
        {
            tanksListed = true;
            var hidden = new List<string>();
            var left = new List<string>();
            foreach (AvailablePart ap in PartLoader.LoadedPartsList)
            {
                if (ap == null || PartFactory.IsPip(ap)) continue;
                PartType t = TypeOf(ap);
                if ((t == PartTypes.Tank || t == PartTypes.Plane) && PipSettings.Replacing(t)) hidden.Add(ap.name);
                else if (ap.category == PartCategories.FuelTank && !ap.TechHidden) left.Add(ap.name);
            }
            hidden.Sort(System.StringComparer.OrdinalIgnoreCase);
            left.Sort(System.StringComparer.OrdinalIgnoreCase);
            Debug.Log($"[PIP] Replace hides {hidden.Count} tanks: {string.Join(", ", hidden)}");
            Debug.Log($"[PIP] Fuel tanks Replace leaves ({left.Count}): {string.Join(", ", left)}");
        }

        // The PIP part type a non-PIP part would be replaced by, or null.
        public static PartType TypeOf(AvailablePart ap)
        {
            if (ap?.partPrefab == null || PartFactory.IsPip(ap)) return null;
            if (cache.TryGetValue(ap.name, out PartType t)) return t;
            t = Classify(ap);
            cache[ap.name] = t;
            return t;
        }

        static readonly HashSet<string> TankResources = new HashSet<string>
            { "LiquidFuel", "Oxidizer", "LqdHydrogen", "LqdMethane", "MonoPropellant", "XenonGas", "ArgonGas", "Lithium" };
        static readonly HashSet<string> RoundSizes = new HashSet<string> { "size0", "size1", "size1p5", "size2", "size3", "size4" };

        // A part that can't be read is left alone, never allowed to stop the sorting:
        // one failure here used to break R&D hiding and the whole editor parts list
        // (on plain KSP the EVA kerbals have no resource list at all).
        static PartType Classify(AvailablePart ap)
        {
            try { return ClassifyUnsafe(ap); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PIP] Left {ap.name} alone: couldn't sort it ({e.GetType().Name})");
                return null;
            }
        }

        static PartType ClassifyUnsafe(AvailablePart ap)
        {
            Part prefab = ap.partPrefab;
            // EVA kerbals and the flag are never replaced.
            if (prefab.GetComponent<KerbalEVA>() != null || ap.name == "flag") return null;
            var modules = new HashSet<string>();
            foreach (PartModule m in prefab.Modules) modules.Add(m.moduleName);
            bool engine = prefab.FindModuleImplementing<ModuleEngines>() != null;
            bool command = modules.Contains("ModuleCommand");
            return engine ? ClassifyEngine(ap) : ClassifyPart(ap, prefab, modules, command);
        }

        // Parts that aren't engines (DESIGN.md section 14).
        static PartType ClassifyPart(AvailablePart ap, Part prefab, HashSet<string> modules, bool command)
        {
            var resources = new HashSet<string>();
            if (prefab.Resources != null && prefab.Resources.Count > 0)
                foreach (PartResource r in prefab.Resources) resources.Add(r.resourceName);

            if (command)
                return prefab.CrewCapacity == 0 && ap.category == PartCategories.Pods ? PartTypes.Probe : null;

            ModuleRCS rcs = prefab.FindModuleImplementing<ModuleRCS>();
            if (rcs != null)
            {
                var props = new HashSet<string>(rcs.propellants.Select(p => p.name));
                if (props.Count == 0 && !string.IsNullOrEmpty(rcs.resourceName)) props.Add(rcs.resourceName);
                bool chemical = props.Count > 0 && props.All(n => n == "MonoPropellant" || n == "LiquidFuel" || n == "Oxidizer");
                return chemical ? PartTypes.Rcs : null;
            }
            // Decouplers (DESIGN.md 21.1): plain stack decouplers and separators of PIP's
            // sizes, and radial decouplers. Engine plates, docking ports, hardpoints
            // and the 7.5 m ones stay.
            if (ap.category == PartCategories.Coupling && (modules.Contains("ModuleDecouple") || modules.Contains("ModuleAnchoredDecoupler")))
            {
                if (modules.Overlaps(Looks.DecouplerExtras)) return null;
                string[] shape = (ap.bulkheadProfiles ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                if (modules.Contains("ModuleAnchoredDecoupler"))
                    return prefab.attachNodes.Count == 0 && prefab.attachRules.srfAttach ? PartTypes.Decoupler : null;
                bool onAxis = prefab.attachNodes.All(n => Mathf.Abs(n.originalPosition.x) < 1e-3f && Mathf.Abs(n.originalPosition.z) < 1e-3f);
                return prefab.attachNodes.Count >= 2 && onAxis && shape.Count(RoundSizes.Contains) == 1 && shape.All(x => RoundSizes.Contains(x) || x == "srf")
                    ? PartTypes.Decoupler : null;
            }
            if (modules.Contains("ModuleDeployableSolarPanel") || modules.Contains("ModuleCurvedSolarPanel")) return PartTypes.Solar;
            // Service bays with an RTG inside (Near Future's utility pods) aren't RTGs.
            if (modules.Contains("ModuleRadioisotopeGenerator")) return ap.category == PartCategories.Electrical ? PartTypes.Rtg : null;
            if (modules.Contains("ModuleAblator")) return PartTypes.Shield;
            if (modules.Contains("ModuleReactionWheel")) return PartTypes.Wheel;
            if ((modules.Contains("ModuleRTAntenna") || modules.Contains("ModuleDataTransmitter")) && ap.category == PartCategories.Communication)
                return PartTypes.Antenna;
            // Breaking Ground's turboshafts take in air too, but stay stock (14.10).
            if (modules.Contains("ModuleResourceIntake") && resources.All(n => n == "IntakeAir") && !modules.Contains("ModuleRoboticServoRotor"))
                return PartTypes.Intake;
            if (ap.category == PartCategories.Electrical && resources.Count == 1 && resources.Contains("ElectricCharge") &&
                !modules.Overlaps(new[] { "ModuleGenerator", "ModuleResourceConverter", "ModuleDeployableSolarPanel" }))
                return PartTypes.Battery;
            // Round stack tanks, radial tanks and adapters: plane fuselages and odd
            // shapes stay.
            // CryoTanks gives stock tanks a B9PartSwitch fuel switch instead of
            // resources, and B9 only adds the resources to parts in the VAB, so the
            // prefab is empty: the switch itself marks them as tanks (as for looks).
            bool switched = resources.Count == 0 && HasFuelSwitch(ap);
            string[] profiles = (ap.bulkheadProfiles ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            // The stock 3.75 m tanks are filed under Propulsion (this method never sees engines).
            if ((ap.category == PartCategories.FuelTank || ap.category == PartCategories.Propulsion) &&
                (switched || resources.Count > 0 && resources.All(n => TankResources.Contains(n) || FuelTable.AllResources.Contains(n))))
            {
                // Cluster tanks, nosecones and the like stay, whatever their shape.
                if (FuelTanks.KeptTank(ap.name, ap.title)) return null;
                // Mk2 and Mk3 fuel fuselages are plane fuselages (DESIGN.md 21.2); parts joining
                // two shapes are plane adapters.
                string[] sections = profiles.Where(Looks.IsPlaneSection).Distinct().ToArray();
                if (sections.Length > 0)
                    return sections.Length == 1 && profiles.All(x => x == sections[0] || x == "srf") && prefab.attachNodes.Count >= 2 &&
                           prefab.attachNodes.All(n => Mathf.Abs(n.originalPosition.x) < 1e-3f && Mathf.Abs(n.originalPosition.z) < 1e-3f)
                        ? PartTypes.Plane : null;
                if (prefab.attachNodes.Count >= 2 && profiles.Count(RoundSizes.Contains) == 1 && profiles.All(x => RoundSizes.Contains(x) || x == "srf")) return PartTypes.Tank;
                // Radial tanks (DESIGN.md 20.1): no stack nodes, stick to a surface.
                if (prefab.attachNodes.Count == 0 && prefab.attachRules.srfAttach && profiles.All(x => x == "srf")) return PartTypes.Tank;
                // Fuel adapters (DESIGN.md 20.2).
                if (IsPipAdapter(ap, prefab, profiles)) return PartTypes.Tank;
            }
            // Plain structural adapters: an empty PIP adapter stands in (DESIGN.md 20.2).
            if (ap.category == PartCategories.Structural && resources.Count == 0 && modules.All(Looks.PlainStructuralModules.Contains) &&
                IsPipAdapter(ap, prefab, profiles))
                return PartTypes.Tank;
            return null;
        }

        static readonly Dictionary<string, string> SizeCodes = new Dictionary<string, string>
            { { "size0", "0625" }, { "size1", "125" }, { "size1p5", "1875" }, { "size2", "25" }, { "size3", "375" }, { "size4", "5" } };

        // An adapter between two sizes PIP makes as a pair, every stack node on the
        // axis (slanted and multi-adapters have theirs off it), and not one of the
        // parts kept on purpose (skeletal, truss, cluster tanks, nosecones).
        static bool IsPipAdapter(AvailablePart ap, Part prefab, string[] profiles)
        {
            if (profiles.Any(x => !SizeCodes.ContainsKey(x) && x != "srf")) return false;
            string[] ends = profiles.Where(SizeCodes.ContainsKey).Select(x => SizeCodes[x]).Distinct().ToArray();
            if (ends.Length != 2 || prefab.attachNodes.Count < 2) return false;
            if (prefab.attachNodes.Any(n => Mathf.Abs(n.originalPosition.x) > 1e-3f || Mathf.Abs(n.originalPosition.z) > 1e-3f)) return false;
            if (FuelTanks.KeptTank(ap.name, ap.title)) return false;
            string big = ends.OrderByDescending(Sizes.IndexOf).First(), small = ends.OrderBy(Sizes.IndexOf).First();
            return FuelTanks.AdapterPairs.Any(p => p[0] == big && p[1] == small);
        }

        static bool HasFuelSwitch(AvailablePart ap) =>
            Looks.FuelSwitchOf(ap.partConfig) != null;

        static PartType ClassifyEngine(AvailablePart ap)
        {
            // Near Future and CryoEngines file their engines under Propulsion, stock under Engine.
            if (ap.category != PartCategories.Engine && ap.category != PartCategories.Propulsion) return null;
            List<ModuleEngines> modes = ap.partPrefab.FindModulesImplementing<ModuleEngines>();
            if (modes == null || modes.Count == 0) return null;
            List<HashSet<string>> props = modes.Select(e => new HashSet<string>(e.propellants.Select(p => p.name))).ToList();

            if (props.Any(p => p.Contains("IntakeAir"))) return PartTypes.Jet;

            // Stock's mixes, or any fuel in the fuel table; ElectricCharge is allowed
            // alongside (electric-pump engines like the Goldfish).
            if (props.All(FuelTable.IsLiquidMix)) return PartTypes.Liquid;

            // Sepratrons have no stack node; boosters do.
            if (props.All(p => p.Count == 1 && p.Contains("SolidFuel")))
                return ap.partPrefab.attachNodes.Count > 0 ? PartTypes.Solid : null;

            if (props.All(p => p.Count == 1 && p.Contains("MonoPropellant"))) return PartTypes.Monoprop;

            if (props.Any(p => p.Contains("ElectricCharge") && p.Overlaps(ElectricGases))) return PartTypes.Electric;

            // A mode burning hydrogen or liquid fuel alone, with no oxidizer: a nuclear thermal rocket.
            if (props.Any(p => p.Count > 0 && p.All(n => NuclearFuels.Contains(n)) && !p.Contains("Oxidizer")))
                return PartTypes.Nuclear;

            return null;
        }
    }
}
