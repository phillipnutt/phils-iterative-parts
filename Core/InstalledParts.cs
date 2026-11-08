using System;
using System.Collections.Generic;
using System.Linq;

namespace PIP
{
    // A stack node's position on a part.
    public struct NodeAt
    {
        public readonly float X, Y, Z;
        public NodeAt(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    // An installed part (not PIP's), as the sorting below needs it. Replace
    // (EditorHider) reads it off the compiled part, looks (Looks.cs) off the
    // config, before KSP compiles it.
    public sealed class PartFacts
    {
        public string Name = "", Title = "";
        public string Category = "";                       // as configs write it: "FuelTank", "Engine"...
        public readonly HashSet<string> Modules = new HashSet<string>();
        public readonly HashSet<string> Resources = new HashSet<string>();
        public readonly List<HashSet<string>> EngineModes = new List<HashSet<string>>();   // each engine module's propellants
        public HashSet<string> RcsPropellants;             // null: no RCS module
        public int Crew;
        public bool FuelSwitch;                            // a B9PartSwitch propellant switch (InstalledParts.IsFuelTank)
        public string[] Profiles = new string[0];          // bulkhead profiles: "size1", "mk2", "srf"...
        public readonly List<NodeAt> StackNodes = new List<NodeAt>();
        public bool SurfaceAttach;

        public static string[] ProfilesOf(string bulkheadProfiles) =>
            (bulkheadProfiles ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
    }

    // What an installed part is: the one set of rules for Replace and for looks
    // (DESIGN.md 14, 16, 20, 21). TypeOf says which PIP type a part is by what it
    // does; ReplacedBy adds the shapes Replace hides. Looks take TypeOf and check
    // shapes of their own (Looks.Classify): they also come from 6.25 and 7.5 m
    // parts, and from adapters of any size whose taper PIP makes.
    public static class InstalledParts
    {
        static readonly HashSet<string> TankResources = new HashSet<string>
            { "LiquidFuel", "Oxidizer", "LqdHydrogen", "LqdMethane", "MonoPropellant", "XenonGas", "ArgonGas", "Lithium" };
        public static readonly HashSet<string> ElectricGases = new HashSet<string> { "XenonGas", "ArgonGas", "Lithium" };
        static readonly HashSet<string> NuclearFuels = new HashSet<string> { "LqdHydrogen", "LiquidFuel", "EnrichedUranium" };
        static readonly HashSet<string> RcsPropellants = new HashSet<string> { "MonoPropellant", "LiquidFuel", "Oxidizer" };
        static readonly HashSet<string> Generators = new HashSet<string>
            { "ModuleGenerator", "ModuleResourceConverter", "ModuleDeployableSolarPanel", "ModuleCurvedSolarPanel" };

        // Parts that decouple but are more than a decoupler: engine plates (their
        // shroud and node mounts), docking ports, cargo bays and structural tubes, the
        // Klaw. They're never decouplers to PIP (DESIGN.md 21.1).
        static readonly HashSet<string> DecouplerExtras = new HashSet<string>
            { "ModuleDynamicNodes", "ModuleJettison", "ModuleDockingNode", "ModuleGrappleNode", "ModuleCargoBay", "ModuleCommand", "ModuleAblator" };

        // Plain structural adapters (FL-A10, Rockomax Brand Adapter) count as adapter
        // tanks. Only these modules may be on one: anything else (a habitat, a
        // docking port, a switch) makes it more than a plain adapter.
        static readonly HashSet<string> PlainStructuralModules = new HashSet<string>
        {
            "ModulePartVariants", "ModuleCargoPart", "TweakScale", "ModuleFreeIva", "SCANRPMStorage",
            "FundsKeeper", "StealBackMyFunds", "AttachedOnEditor", "ModuleToggleCrossfeed",
        };

        // The round sizes PIP makes, by bulkhead profile.
        static readonly Dictionary<string, string> PipSizes = new Dictionary<string, string>
            { { "size0", "0625" }, { "size1", "125" }, { "size1p5", "1875" }, { "size2", "25" }, { "size3", "375" }, { "size4", "5" } };

        public static bool IsTankResource(string r) => TankResources.Contains(r) || FuelTable.AllResources.Contains(r);
        public static bool IsPlaneSection(string profile) => profile == "mk2" || profile == "mk3";
        public static bool OnAxis(IEnumerable<NodeAt> nodes) => nodes.All(n => Math.Abs(n.X) < 1e-3f && Math.Abs(n.Z) < 1e-3f);

        // Whether a part's lowest end splits in two (two stack nodes at the bottom): the bicoupler.
        public static bool TwinEnd(List<NodeAt> stack)
        {
            if (stack.Count < 3) return false;
            float bottom = stack.Min(v => v.Y);
            return stack.Count(v => Math.Abs(v.Y - bottom) < 0.01f) >= 2;
        }

        // Rocket propellant tanks: by contents, or (empty until the VAB) by a fuel
        // switch. The stock 3.75 m tanks are filed under Propulsion.
        public static bool IsFuelTank(PartFacts f) =>
            (f.Category == "FuelTank" || f.Category == "Propulsion") &&
            (f.Resources.Count > 0 ? f.Resources.All(IsTankResource) : f.FuelSwitch);

        public static bool IsPlainStructural(PartFacts f) =>
            f.Category == "Structural" && f.Resources.Count == 0 && f.Modules.All(PlainStructuralModules.Contains);

        // The PIP type a part is, by what it does, or null. A tank can be a fuel tank
        // or a plain structural part (IsPlainStructural); its shape decides the rest.
        public static PartType TypeOf(PartFacts f)
        {
            if (f.EngineModes.Count > 0) return EngineType(f);
            HashSet<string> m = f.Modules;
            if (m.Contains("ModuleCommand")) return f.Crew == 0 && f.Category == "Pods" ? PartTypes.Probe : null;
            if (f.RcsPropellants != null)
                return f.RcsPropellants.Count > 0 && f.RcsPropellants.All(RcsPropellants.Contains) ? PartTypes.Rcs : null;
            if (f.Category == "Coupling" && (m.Contains("ModuleDecouple") || m.Contains("ModuleAnchoredDecoupler")))
                return m.Overlaps(DecouplerExtras) ? null : PartTypes.Decoupler;
            if (m.Contains("ModuleDeployableSolarPanel") || m.Contains("ModuleCurvedSolarPanel")) return PartTypes.Solar;
            // Service bays with an RTG inside (Near Future's utility pods) aren't RTGs.
            if (m.Contains("ModuleRadioisotopeGenerator")) return f.Category == "Electrical" ? PartTypes.Rtg : null;
            if (m.Contains("ModuleAblator")) return PartTypes.Shield;
            if (m.Contains("ModuleReactionWheel")) return PartTypes.Wheel;
            if ((m.Contains("ModuleRTAntenna") || m.Contains("ModuleDataTransmitter")) && f.Category == "Communication") return PartTypes.Antenna;
            // Breaking Ground's turboshafts take in air too, but stay stock (14.10).
            if (m.Contains("ModuleResourceIntake") && f.Resources.All(r => r == "IntakeAir") && !m.Contains("ModuleRoboticServoRotor"))
                return PartTypes.Intake;
            if (f.Category == "Electrical" && f.Resources.Count == 1 && f.Resources.Contains("ElectricCharge") && !m.Overlaps(Generators))
                return PartTypes.Battery;
            return IsFuelTank(f) || IsPlainStructural(f) ? PartTypes.Tank : null;
        }

        // Engines, by what they burn. Near Future and CryoEngines file theirs under
        // Propulsion, stock under Engine.
        static PartType EngineType(PartFacts f)
        {
            if (f.Category != "Engine" && f.Category != "Propulsion") return null;
            List<HashSet<string>> props = f.EngineModes;
            if (props.Any(p => p.Contains("IntakeAir"))) return PartTypes.Jet;
            // Stock's mixes, or any fuel in the fuel table; ElectricCharge is allowed
            // alongside (electric-pump engines like the Goldfish).
            if (props.All(FuelTable.IsLiquidMix)) return PartTypes.Liquid;
            // Sepratrons have no stack node; boosters do.
            if (props.All(p => p.Count == 1 && p.Contains("SolidFuel"))) return f.StackNodes.Count > 0 ? PartTypes.Solid : null;
            if (props.All(p => p.Count == 1 && p.Contains("MonoPropellant"))) return PartTypes.Monoprop;
            if (props.Any(p => p.Contains("ElectricCharge") && p.Overlaps(ElectricGases))) return PartTypes.Electric;
            // A mode burning hydrogen or liquid fuel alone, with no oxidizer: a nuclear thermal rocket.
            if (props.Any(p => p.Count > 0 && p.All(NuclearFuels.Contains) && !p.Contains("Oxidizer"))) return PartTypes.Nuclear;
            return null;
        }

        // Replace (DESIGN.md 14): the PIP type that takes this part's place, or null
        // when the part stays. Sepratrons, escape towers, RCS that isn't chemical,
        // engine plates, 7.5 m parts and odd shapes are never hidden.
        public static PartType ReplacedBy(PartFacts f)
        {
            PartType t = TypeOf(f);
            if (t == PartTypes.Decoupler) return ReplacedDecoupler(f) ? t : null;
            if (t == PartTypes.Tank) return ReplacedTank(f);
            return t;
        }

        // Plain stack decouplers and separators of PIP's sizes, and radial decouplers.
        static bool ReplacedDecoupler(PartFacts f)
        {
            if (f.Modules.Contains("ModuleAnchoredDecoupler")) return f.StackNodes.Count == 0 && f.SurfaceAttach;
            return f.StackNodes.Count >= 2 && OnAxis(f.StackNodes) && OneRoundSize(f);
        }

        // One of PIP's round sizes, and nothing but that and surface attachment.
        static bool OneRoundSize(PartFacts f) =>
            f.Profiles.Count(PipSizes.ContainsKey) == 1 && f.Profiles.All(x => PipSizes.ContainsKey(x) || x == "srf");

        // Round stack tanks, radial tanks, adapters, plane fuselages and plane
        // adapters. Cluster tanks, nosecones and the like stay, whatever their shape.
        static PartType ReplacedTank(PartFacts f)
        {
            // Plain structural adapters: an empty PIP adapter stands in (DESIGN.md 20.2).
            if (!IsFuelTank(f)) return IsPipAdapter(f) ? PartTypes.Tank : null;
            if (FuelTanks.KeptTank(f.Name, f.Title)) return null;
            // Mk2 and Mk3 fuel fuselages are plane fuselages (DESIGN.md 21.2); parts joining
            // two shapes are plane adapters.
            string[] sections = f.Profiles.Where(IsPlaneSection).Distinct().ToArray();
            if (sections.Length > 0)
            {
                if (FuelTanks.PlaneAdapterKindOf(f.Profiles, TwinEnd(f.StackNodes)) != null) return PartTypes.Tank;
                return sections.Length == 1 && f.Profiles.All(x => x == sections[0] || x == "srf") && f.StackNodes.Count >= 2 && OnAxis(f.StackNodes)
                    ? PartTypes.Plane : null;
            }
            if (f.StackNodes.Count >= 2 && OneRoundSize(f)) return PartTypes.Tank;
            // Radial tanks (DESIGN.md 20.1): no stack nodes, stick to a surface.
            if (f.StackNodes.Count == 0 && f.SurfaceAttach && f.Profiles.All(x => x == "srf")) return PartTypes.Tank;
            // Fuel adapters (DESIGN.md 20.2).
            return IsPipAdapter(f) ? PartTypes.Tank : null;
        }

        // An adapter between two sizes PIP makes as a pair, every stack node on the
        // axis (slanted and multi-adapters have theirs off it), and not one of the
        // parts kept on purpose (skeletal, truss, cluster tanks, nosecones).
        static bool IsPipAdapter(PartFacts f)
        {
            if (f.Profiles.Any(x => !PipSizes.ContainsKey(x) && x != "srf")) return false;
            string[] ends = f.Profiles.Where(PipSizes.ContainsKey).Select(x => PipSizes[x]).Distinct().ToArray();
            if (ends.Length != 2 || f.StackNodes.Count < 2 || !OnAxis(f.StackNodes)) return false;
            if (FuelTanks.KeptTank(f.Name, f.Title)) return false;
            string big = ends.OrderByDescending(Sizes.IndexOf).First(), small = ends.OrderBy(Sizes.IndexOf).First();
            return FuelTanks.AdapterPairs.Any(p => p[0] == big && p[1] == small);
        }
    }
}
