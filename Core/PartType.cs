using System;
using System.Collections.Generic;
using System.Linq;

namespace PIP
{
    // One option of a free choice, e.g. Fuel = Hydrolox, and the tech node that
    // opens it (null = open with the type's first programs).
    public sealed class Option
    {
        public readonly string Code, Label, Tech;
        // Every node it needs. Most options need one (Tech); an adapter tank's size
        // needs Fuel Systems and its size's tooling (DESIGN.md 20.2).
        public readonly string[] Techs;
        public Option(string code, string label, string tech = null)
        {
            Code = code; Label = label; Tech = tech;
            Techs = string.IsNullOrEmpty(tech) ? new string[0] : new[] { tech };
        }
        Option(string code, string label, string[] techs) { Code = code; Label = label; Techs = techs; Tech = techs.LastOrDefault(); }
        public static Option Needing(string code, string label, params string[] techs) =>
            new Option(code, label, techs.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToArray());
    }

    // A free choice made when a program starts (fuel, size, nozzle...).
    public sealed class Choice
    {
        public readonly string Name;
        public readonly Option[] Options;
        public Choice(string name, params Option[] options) { Name = name; Options = options; }
        public Option Find(string code) => Options.FirstOrDefault(o => o.Code == code);
    }

    // A points category (Thrust, Efficiency...).
    public sealed class Category
    {
        public readonly string Label;
        public readonly int DefaultPoints;
        public Category(string label, int defaultPoints = 25) { Label = label; DefaultPoints = defaultPoints; }
    }

    // Stat keys shared between the rules and the part modules.
    public static class K
    {
        public const string ThrustVac = "thrustVac", ThrustSL = "thrustSL", IspVac = "ispVac", IspSL = "ispSL";
        public const string Mass = "mass", Gimbal = "gimbal", Fail = "fail", Price = "price";
        public const string EcRate = "ecRate", Radiation = "radiation", Speed = "speed", TopMach = "topMach";
        public const string ThrustDry = "thrustDry", IspDry = "ispDry";
        public const string RocketThrust = "rocketThrust", RocketIspVac = "rocketIspVac", RocketIspSL = "rocketIspSL";
        public const string Propellant = "propellant", Burn = "burn";
    }

    // A program's numbers at one Mk, by key.
    public sealed class Stats
    {
        readonly Dictionary<string, double> values = new Dictionary<string, double>();
        public double this[string key]
        {
            get => values.TryGetValue(key, out double v) ? v : 0;
            set => values[key] = value;
        }
        public bool Has(string key) => values.ContainsKey(key);
        public double Mass => this[K.Mass];
        public double BuildPrice => this[K.Price];
        public double FailureChance => this[K.Fail];
    }

    // A line in the program window: a category's main number (Category >= 0,
    // with -/+ buttons) or an indented detail line (Category = -1).
    public sealed class Row
    {
        public readonly string Label, Key;
        public readonly int Category;
        public readonly Func<double, string> Format;
        public Row(string label, string key, int category, Func<double, string> format)
        {
            Label = label; Key = key; Category = category; Format = format;
        }
    }

    // A kind of part PIP makes programs for: its free choices, its categories,
    // its tech nodes and its numbers. Models are arrays of option codes, one per
    // choice, written "K-125-S" in saves and chassis names.
    public abstract class PartType
    {
        public abstract string Id { get; }            // "liquid"
        public abstract string Title { get; }         // "Liquid engines"
        public abstract string Noun { get; }          // "engine"
        public virtual string ChassisCode => Id.ToUpperInvariant();
        public abstract Choice[] Choices { get; }

        // The type's own sliders, then Build cost, which every type has (DESIGN.md
        // section 18.3): it brings the build price down as the program matures.
        protected abstract Category[] OwnCategories { get; }
        public Category[] Categories => OwnCategories.Concat(new[] { BuildCost }).ToArray();
        public int CostIndex => OwnCategories.Length;
        static readonly Category BuildCost = new Category("Build cost");

        // [first programs (caps 25), caps 50, caps 75, caps 100]
        public abstract string[] CapTechs { get; }
        // A brand-new program with every slider at 100 costs this (section 18.5).
        public abstract double CostCeiling { get; }
        public virtual bool IsEngine => true;

        protected abstract Row[] OwnRows(string[] model);
        public Row[] Rows(string[] model) =>
            OwnRows(model).Concat(new[] { new Row("Build price, each", K.Price, CostIndex, v => "√ " + v.ToString("N0")) }).ToArray();

        public abstract Stats Compute(string[] model, int[] points, int mk, FailureLevel level, string variant = null);

        // A program's numbers: Compute, for a version that starts from the points
        // of the one before it (null: a first version, from half its points), with
        // its gimbal pick (degrees; ignored by types without one).
        public Stats Compute(string[] model, int[] points, int mk, FailureLevel level, string variant, int[] startPoints, int gimbal)
        {
            int[] was = start;
            start = startPoints;
            try
            {
                Stats s = Compute(model, points, mk, level, variant);
                if (HasGimbalPick) s[K.Gimbal] = CanGimbal(model) ? gimbal : 0;
                return s;
            }
            finally { start = was; }
        }

        // The starting points of the version being computed (see E). Set only
        // around one Compute call, on KSP's single thread.
        [ThreadStatic] static int[] start;

        // Gimbal is a free pick, not a slider (section 18.4): 0, 5, 10 or 20
        // degrees, opened by research. Fixed for a program family's life.
        public virtual bool HasGimbalPick => false;
        public virtual bool CanGimbal(string[] model) => HasGimbalPick;
        public static readonly Option[] GimbalOptions =
            { new Option("0", "0°"), new Option("5", "5°"), new Option("10", "10°", "generalRocketry"), new Option("20", "20°", "advRocketry") };
        public static int GimbalDegrees(string code) => int.TryParse(code, out int d) ? d : 0;

        // Sizes of a part type that isn't an engine (B9PartSwitch on the chassis).
        // Each program picks one for life, in-line or side-mount (DESIGN.md
        // section 14, changed 2026-11-05). Null for engines: their size is a choice.
        public virtual Option[] Variants(string[] model) => null;
        public virtual string VariantName => "Size";
        public virtual string DefaultVariant(string[] model)
        {
            Option[] v = Variants(model);
            return v == null || v.Length == 0 ? null : v[Math.Min(1, v.Length - 1)].Code;
        }

        // Side-mount parts (DESIGN.md section 15): the same program also makes a
        // part that attaches to the side, with these sizes. Their codes start "R".
        public virtual bool HasSideMount => false;
        public virtual Option[] SideVariants(string[] model) => null;
        public string DefaultSideVariant(string[] model) => SideVariants(model)?.FirstOrDefault()?.Code;
        public static bool IsSide(string variant) => variant != null && variant.StartsWith("R", StringComparison.Ordinal);
        public Option[] VariantsFor(string[] model, bool side) => side ? SideVariants(model) : Variants(model);

        // Adapter tanks (DESIGN.md 20.2): a program can be an adapter between two
        // sizes, picked for its life. Their codes start "A-". An adapter's taper is
        // built into its model, so each size pair needs an installed adapter of the
        // same ratio: AdapterKind names the ratio, the look kind it needs.
        public virtual bool HasAdapters => false;
        public virtual Option[] AdapterVariants(string[] model) => null;
        public static bool IsAdapter(string variant) => variant != null && variant.StartsWith("A-", StringComparison.Ordinal);
        public virtual string AdapterKind(string variant) => null;
        // The looks to try first for a ratio, and what to install when none is.
        public virtual string[] AdapterLookPreference(string kind) => new string[0];
        public virtual string AdapterNeeds(string kind) => null;
        // An adapter look is stretched to this height (m) for a size.
        public virtual double NominalHeight(string variant) => 0;

        // Where a size's part goes: "" in-line, "R" side-mount, "A" adapter. Side-mount
        // and adapter parts are always made from a look (Looks.cs).
        public static string MountOf(string variant) => IsSide(variant) ? "R" : IsAdapter(variant) ? "A" : "";
        public Option[] VariantsForMount(string[] model, string mount) =>
            mount == "R" ? SideVariants(model) : mount == "A" ? AdapterVariants(model) : Variants(model);

        // A size code, in-line, side-mount or adapter, or null when this model has no such size.
        public Option VariantOption(string[] model, string code) =>
            string.IsNullOrEmpty(code) ? null
            : (Variants(model) ?? new Option[0]).Concat(SideVariants(model) ?? new Option[0]).Concat(AdapterVariants(model) ?? new Option[0])
                .FirstOrDefault(o => o.Code == code);

        // "1.25 m long", "Small", "Side-mount", "Side-mount, small", "Adapter, 2.5 to 1.25 m long".
        public string VariantLabel(string[] model, string code)
        {
            Option o = VariantOption(model, code);
            if (o == null) return code ?? "";
            if (IsAdapter(code)) return "Adapter, " + o.Label;
            if (!IsSide(code)) return o.Label;
            return o.Label == "Side-mount" ? o.Label : "Side-mount, " + o.Label.ToLowerInvariant();
        }

        // Looks (DESIGN.md section 16): the program wears an installed part's model.
        // The part PIP uses when no look is picked, and the side-mount looks to
        // try first. Part names as KSP lists them ("_" becomes ".").
        public virtual bool HasLooks => false;
        public virtual string DefaultLook(string[] model) => null;
        public virtual string[] SideLookPreference(string[] model) => new string[0];

        // How a look's model is scaled to each size on the part: by diameter, by
        // mass (side-mount batteries, RTGs, dishes), by output (solar panels), by
        // diameter and length (tanks), by volume (radial tanks), or not at all (one
        // size). kind: what the
        // installed part is ("F"/"D" panel, "O"/"D" antenna, tank contents).
        public virtual LookScale ScaleBasis(bool side, string kind) => LookScale.Diameter;
        public virtual double NominalMass(string variant) => 0;
        public virtual double NominalOutput(string variant) => 0;
        public virtual double NominalVolume(string variant) => 0;

        // Some types need a separate look part per program choice, because the
        // choice changes the part's config: a tank's propellants, an RCS block's.
        // LookKey: which one a program uses. LookKeys: the ones an installed part
        // of this kind is made into.
        public virtual string LookKey(string[] model) => "";
        public virtual string[] LookKeys(string kind) => new[] { "" };
        // Whether a look of this kind suits the program (a fixed panel can't be a
        // deployable program's look; a 4-nozzle block can't be a 5-way's).
        public virtual bool LookFits(string[] model, string kind) => true;
        // The model whose sizes a look of this kind and key is switched between.
        public virtual string[] LookModel(string kind, string key) => DefaultModel;

        // A category that does nothing for this model (an aerospike's gimbal).
        public virtual bool CategoryUsed(string[] model, int category) => true;
        public virtual bool Valid(string[] model) =>
            model != null && model.Length == Choices.Length && Choices.Select((c, i) => c.Find(model[i]) != null).All(ok => ok);

        public virtual string Label(string[] model) =>
            string.Join(", ", model.Select((code, i) => Choices[i].Find(code)?.Label ?? code));
        // "PIP-SOL-125-M"; a type with no choices has one chassis, "PIP-BAT".
        public virtual string ChassisPart(string[] model) =>
            "PIP-" + ChassisCode + (model == null || model.Length == 0 ? "" : "-" + Code(model));

        // One line for the new-program window: what the choices mean in play.
        public virtual string Character(string[] model) => "";
        // Mature (Mk. 10) failure chance, for the summary line; engines only.
        public virtual double FailureAt(string[] model, int reliabilityPoints, int mk, FailureLevel level) => 0;
        public virtual int ReliabilityCategory => -1;

        public int MaxTotal => Categories.Length * Pricing.MaxPoints;
        public string Code(string[] model) => string.Join("-", model);
        public string[] DefaultModel => Choices.Select(c => c.Options[0].Code).ToArray();
        public int[] DefaultPoints => Categories.Select(c => c.DefaultPoints).ToArray();

        public string[] ParseModel(string code)
        {
            if (Choices.Length == 0) return new string[0];
            if (string.IsNullOrEmpty(code)) return null;
            string[] parts = code.Split('-');
            return Valid(parts) ? parts : null;
        }

        // Every node that must be researched to start a program of this model
        // (and size, for types that have sizes).
        public IEnumerable<string> Techs(string[] model, string variant = null)
        {
            yield return CapTechs[0];
            for (int i = 0; i < Choices.Length && i < model.Length; i++)
            {
                string tech = Choices[i].Find(model[i])?.Tech;
                if (!string.IsNullOrEmpty(tech)) yield return tech;
            }
            foreach (string sizeTech in VariantOption(model, variant)?.Techs ?? new string[0]) yield return sizeTech;
        }

        protected static int P(int[] points, int i) => points != null && i < points.Length ? Pricing.Clamp(points[i]) : 0;
        // Points delivered at a Mk.: from the version's starting points to its own.
        protected static double E(int[] points, int i, int mk) =>
            start != null && i < start.Length ? Progress.Effective(P(points, i), Math.Min(P(start, i), P(points, i)), mk)
                                              : Progress.Effective(P(points, i), mk);
        // Points a part is built to at a Mk. (what its weight and price follow): all of
        // them for a first version; a later version's grow from where the one before
        // ended, so its Mk. 1 weighs and costs what the old Mk. 5 did.
        protected static double D(int[] points, int i, int mk) =>
            start != null && i < start.Length ? Progress.Effective(P(points, i), Math.Min(P(start, i), P(points, i)), mk) : P(points, i);
        protected static double Lerp(double a, double b, double points) => a + (b - a) * points / 100.0;
        protected static int Total(int[] points) => points?.Sum(p => Pricing.Clamp(p)) ?? 0;
        // Ambition from every slider but Build cost, which then brings the price down.
        protected double BuildPrice(double basePrice, int[] points, int mk)
        {
            double built = 0;
            for (int i = 0; i < CostIndex; i++) built += D(points, i, mk);
            return basePrice * Pricing.Ambition(built, MaxTotal - Pricing.MaxPoints) * Pricing.CostFactor(E(points, CostIndex, mk));
        }

        // The share of its points a design spends, over the sliders it uses (section 18.5).
        public double PointsShare(string[] model, int[] points)
        {
            int used = 0, total = 0;
            for (int i = 0; i < Categories.Length; i++)
                if (model == null || CategoryUsed(model, i)) { used++; total += P(points, i); }
            return used == 0 ? 0 : total / (used * (double)Pricing.MaxPoints);
        }

    }

    public enum LookScale { Diameter, Mass, Output, Tank, Volume, Adapter, None }

    public static class PartTypes
    {
        public static readonly PartType Liquid = new LiquidEngines();
        public static readonly PartType Solid = new SolidMotors();
        public static readonly PartType Monoprop = new MonopropEngines();
        public static readonly PartType Nuclear = new NuclearEngines();
        public static readonly PartType Electric = new ElectricEngines();
        public static readonly PartType Jet = new JetEngines();

        public static readonly PartType Rcs = new RcsThrusters();
        public static readonly PartType Battery = new Batteries();
        public static readonly PartType Wheel = new ReactionWheels();
        public static readonly PartType Shield = new HeatShields();
        public static readonly PartType Solar = new SolarPanels();
        public static readonly PartType Antenna = new Antennas();
        public static readonly PartType Probe = new ProbeCores();
        public static readonly PartType Tank = new FuelTanks();
        public static readonly PartType Rtg = new Rtgs();
        public static readonly PartType Intake = new AirIntakes();

        public static readonly PartType[] Engines = { Liquid, Solid, Monoprop, Nuclear, Electric, Jet };
        public static readonly PartType[] Parts = { Rcs, Battery, Wheel, Shield, Solar, Antenna, Probe, Tank, Rtg, Intake };
        public static readonly PartType[] All = Engines.Concat(Parts).ToArray();

        public static PartType Get(string id) =>
            string.IsNullOrEmpty(id) ? Liquid : All.FirstOrDefault(t => t.Id == id);
    }
}
