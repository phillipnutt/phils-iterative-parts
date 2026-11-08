using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace PIP
{
    // "Replace stock": takes every non-PIP part of a replaced type off the parts
    // list and out of R&D. Which type a part is, and which shapes are hidden, is
    // InstalledParts.ReplacedBy (Core), the same rules looks use.
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorHider : MonoBehaviour
    {
        const string FilterName = "PIP-HideReplaced";
        static readonly Dictionary<string, PartType> cache = new Dictionary<string, PartType>();
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
                if (t is TankKind && PipSettings.Replacing(t)) hidden.Add(ap.name);
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

        // The rules are InstalledParts' (shared with looks); this only reads the part.
        static PartType ClassifyUnsafe(AvailablePart ap)
        {
            Part prefab = ap.partPrefab;
            // EVA kerbals and the flag are never replaced.
            if (prefab.GetComponent<KerbalEVA>() != null || ap.name == "flag") return null;
            return InstalledParts.ReplacedBy(FactsOf(ap));
        }

        static PartFacts FactsOf(AvailablePart ap)
        {
            Part prefab = ap.partPrefab;
            var f = new PartFacts
            {
                Name = ap.name, Title = ap.title, Category = ap.category.ToString(), Crew = prefab.CrewCapacity,
                Profiles = PartFacts.ProfilesOf(ap.bulkheadProfiles), SurfaceAttach = prefab.attachRules.srfAttach,
                // CryoTanks gives stock tanks a B9PartSwitch fuel switch instead of
                // resources, and B9 only adds the resources to parts in the VAB, so the
                // prefab is empty: the switch itself marks them as tanks.
                FuelSwitch = Looks.FuelSwitchOf(ap.partConfig) != null,
            };
            foreach (PartModule m in prefab.Modules) f.Modules.Add(m.moduleName);
            if (prefab.Resources != null)
                foreach (PartResource r in prefab.Resources) f.Resources.Add(r.resourceName);
            foreach (ModuleEngines e in prefab.FindModulesImplementing<ModuleEngines>())
                f.EngineModes.Add(new HashSet<string>(e.propellants?.Select(p => p.name) ?? new string[0]));
            ModuleRCS rcs = prefab.FindModuleImplementing<ModuleRCS>();
            if (rcs != null)
            {
                f.RcsPropellants = new HashSet<string>(rcs.propellants?.Select(p => p.name) ?? new string[0]);
                if (f.RcsPropellants.Count == 0 && !string.IsNullOrEmpty(rcs.resourceName)) f.RcsPropellants.Add(rcs.resourceName);
            }
            foreach (AttachNode n in prefab.attachNodes) f.StackNodes.Add(new NodeAt(n.originalPosition.x, n.originalPosition.y, n.originalPosition.z));
            return f;
        }
    }
}
