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
        static readonly HashSet<string> RocketFuels = new HashSet<string> { "LiquidFuel", "LqdHydrogen", "LqdMethane" };
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
            // Round stack tanks only: plane fuselages, radial and odd shapes stay.
            if (ap.category == PartCategories.FuelTank && resources.Count > 0 && resources.All(TankResources.Contains) &&
                prefab.attachNodes.Count >= 2)
            {
                string[] profiles = (ap.bulkheadProfiles ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                if (profiles.Count(RoundSizes.Contains) == 1 && profiles.All(x => RoundSizes.Contains(x) || x == "srf")) return PartTypes.Tank;
            }
            return null;
        }

        static PartType ClassifyEngine(AvailablePart ap)
        {
            // Near Future and CryoEngines file their engines under Propulsion, stock under Engine.
            if (ap.category != PartCategories.Engine && ap.category != PartCategories.Propulsion) return null;
            List<ModuleEngines> modes = ap.partPrefab.FindModulesImplementing<ModuleEngines>();
            if (modes == null || modes.Count == 0) return null;
            List<HashSet<string>> props = modes.Select(e => new HashSet<string>(e.propellants.Select(p => p.name))).ToList();

            if (props.Any(p => p.Contains("IntakeAir"))) return PartTypes.Jet;

            // ElectricCharge is allowed alongside (electric-pump engines like the Goldfish).
            if (props.All(p => p.Contains("Oxidizer") && p.Overlaps(RocketFuels) &&
                               p.All(n => n == "Oxidizer" || n == "ElectricCharge" || RocketFuels.Contains(n))))
                return PartTypes.Liquid;

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
