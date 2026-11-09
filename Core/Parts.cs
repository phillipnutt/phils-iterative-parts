using System;
using System.Collections.Generic;
using System.Linq;

namespace PIP
{
    // Part types that aren't engines (DESIGN.md section 14). Each program picks one
    // size for life (Variants, in-line or side-mount). No failures.
    public abstract class PartKind : PartType
    {
        public override bool IsEngine => false;

        protected static double Log(double a, double b, double points) => a * Math.Pow(b / a, points / 100.0);

        // A factor on today's part as plain words: 1.2 -> "20% more", 0.6 -> "40% less".
        public static string Percent(double factor, string more, string less) =>
            Math.Abs(factor - 1) < 0.005 ? "as stock" : factor > 1 ? $"{(factor - 1) * 100:0}% {more}" : $"{(1 - factor) * 100:0}% {less}";

        protected static int Index(Option[] options, string code) => Math.Max(0, Array.FindIndex(options, o => o.Code == code));

        // Sizes on the part: 0.625-5 m, larger ones opened by the shared rocket tooling.
        protected static Option Size(string code, string tech = null) => new Option(code, Sizes.Label(code), tech ?? Sizes.Tooling(code));
    }

    // RCS thrusters: propellant and layout. Thrust is per nozzle.
    public sealed class RcsThrusters : PartKind
    {
        public override string Id => "rcs";
        public override string Title => "RCS thrusters";
        public override string Noun => "thruster";
        public override string ChassisCode => "RCS";

        static readonly Choice[] choices =
        {
            new Choice("Propellant", new Option("M", "Monopropellant"), new Option("B", "Bipropellant", "specializedControl")),
            new Choice("Layout", new Option("1", "Single"), new Option("4", "4-way"), new Option("5", "5-way")),
        };
        static readonly Category[] categories = { new Category("Thrust"), new Category("Efficiency"), new Category("Weight") };

        public override Choice[] Choices => choices;
        protected override Category[] OwnCategories => categories;
        public override string[] CapTechs => new[] { "flightControl", "advFlightControl", "specializedControl", "experimentalRocketry" };
        public override double CostCeiling => 2.5e5;

        // Looks (16): one look part per propellant, because the thrusters' fuel is
        // in the config. The look's number of nozzles (its kind) must match the layout.
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) =>
            m[1] == "1" ? (Biprop(m) ? "nflv-rcs-heavy-1x-2" : "linearRcs")
            : m[1] == "4" ? (Biprop(m) ? "nflv-rcs-heavy-4x-2" : "RCSBlock.v2")
            : "rcsblock-orbital-5way-1";
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public override string LookKey(string[] m) => m[0];
        public override string[] LookKeys(string kind) => new[] { "M", "B" };
        public override bool LookFits(string[] m, string kind) => kind == m[1];
        public override string[] LookModel(string kind, string key) => new[] { key, kind };

        public static int Nozzles(string[] m) => m[1] == "1" ? 1 : m[1] == "4" ? 4 : 5;
        static bool Biprop(string[] m) => m[0] == "B";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            bool bi = Biprop(m);
            int n = Nozzles(m);
            s[K.ThrustVac] = bi ? Lerp(2, 12, E(pts, 0, mk)) : Lerp(0.25, 3, E(pts, 0, mk));
            s[K.IspVac] = bi ? Lerp(240, 300, E(pts, 1, mk)) : Lerp(220, 300, E(pts, 1, mk));
            s[K.IspSL] = bi ? Lerp(100, 160, E(pts, 1, mk)) : Lerp(70, 120, E(pts, 1, mk));
            double designed = n * (bi ? Lerp(2, 12, D(pts, 0, mk)) : Lerp(0.25, 3, D(pts, 0, mk)));
            s[K.Mass] = designed / ((bi ? 25 : 12.5) * Engines.G0) * (2 - E(pts, 2, mk) / 100.0);
            s[K.Price] = BuildPrice(n * (bi ? 150 : 50), pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Thrust per nozzle, vacuum", K.ThrustVac, 0, Fmt.Kn),
            new Row("Isp, vacuum", K.IspVac, 1, Fmt.Sec),
            new Row("  Isp at sea level", K.IspSL, -1, Fmt.Sec),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
        };

        public override string Label(string[] m) => $"{choices[1].Find(m[1])?.Label} {(Biprop(m) ? "bipropellant" : "monopropellant")} RCS";
        public override string Character(string[] m) => Biprop(m) ? "Heavy thrusters on kerolox, for big ships." : "Classic monopropellant attitude control.";
    }

    // Batteries: one category. Every battery in the install holds 20,000 EC per
    // ton; points move that from 12,000 to 30,000. Each size keeps today's mass.
    public sealed class Batteries : PartKind
    {
        public override string Id => "battery";
        public override string Title => "Batteries";
        public override string Noun => "battery";
        public override string ChassisCode => "BAT";

        static readonly Option[] variants = { Size("0625", ""), Size("125", ""), Size("1875"), Size("25"), Size("375") };
        static readonly double[] MassBySize = { 0.01, 0.05, 0.125, 0.2, 0.5 };
        static readonly double[] PriceBySize = { 360, 880, 2800, 4500, 11250 };
        // Side-mount: Z-100, Z-400 and Near Future's B-800 sizes.
        static readonly Option[] sideVariants = { new Option("R-S", "Small", ""), new Option("R-M", "Medium", ""), new Option("R-L", "Large", "") };
        static readonly double[] SideMass = { 0.005, 0.02, 0.04 };
        static readonly double[] SidePrice = { 80, 550, 1100 };

        public override Choice[] Choices => new Choice[0];
        protected override Category[] OwnCategories => new[] { new Category("Capacity") };
        public override string[] CapTechs => new[] { "start", "electrics", "largeElectrics", "experimentalElectrics" };
        public override double CostCeiling => 1e5;
        public override Option[] Variants(string[] m) => variants;
        public override bool HasSideMount => true;
        public override Option[] SideVariants(string[] m) => sideVariants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "batteryBank";
        public override string[] SideLookPreference(string[] m) => new[] { "batteryPack", "ksp.r.largeBatteryPack" };
        public override LookScale ScaleBasis(bool side, string kind) => side ? LookScale.Mass : LookScale.Diameter;
        public override double NominalMass(string variant) =>
            IsSide(variant) ? SideMass[Index(sideVariants, variant)] : MassBySize[Index(variants, variant)];

        public const string Capacity = "capacity", Ec = "ec";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            variant = variant ?? DefaultVariant(m);
            bool side = IsSide(variant);
            int v = Index(side ? sideVariants : variants, variant);
            var s = new Stats();
            s[Capacity] = Lerp(12000, 30000, E(pts, 0, mk));
            s[K.Mass] = side ? SideMass[v] : MassBySize[v];
            s[Ec] = s[K.Mass] * s[Capacity];
            s[K.Price] = BuildPrice(side ? SidePrice[v] : PriceBySize[v], pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Capacity", Capacity, 0, v => $"{v:N0} EC/t"),
            new Row("  holds", Ec, -1, v => $"{v:N0} EC"),
            new Row("  weight", K.Mass, -1, Fmt.Tons),
        };

        public override string Label(string[] m) => "Batteries";
        public override string Character(string[] m) => "Stores electric charge.";
    }

    // Reaction wheels: torque, power and weight; size on the part.
    public sealed class ReactionWheels : PartKind
    {
        public override string Id => "wheel";
        public override string Title => "Reaction wheels";
        public override string Noun => "wheel";
        public override string ChassisCode => "RWL";

        static readonly Option[] variants = { Size("0625", ""), Size("125", ""), Size("1875"), Size("25"), Size("375") };
        static readonly double[,] Torque = { { 2, 8 }, { 6, 25 }, { 10, 35 }, { 15, 50 }, { 30, 100 } };
        static readonly double[] PriceBySize = { 500, 1100, 1800, 2500, 5000 };
        // Side-mount: one size, the radial gyroscopes' (14.3's radial row).
        static readonly Option[] sideVariants = { new Option("R", "Side-mount", "") };
        const double SideTorqueLo = 0.5, SideTorqueHi = 3, SidePrice = 500;

        public override Choice[] Choices => new Choice[0];
        protected override Category[] OwnCategories => new[] { new Category("Torque"), new Category("Power draw"), new Category("Weight") };
        public override string[] CapTechs => new[] { "stability", "flightControl", "advFlightControl", "specializedControl" };
        public override double CostCeiling => 2.5e5;
        public override Option[] Variants(string[] m) => variants;
        public override bool HasSideMount => true;
        public override Option[] SideVariants(string[] m) => sideVariants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "advSasModule";
        public override string[] SideLookPreference(string[] m) => new[] { "restock-reactionwheel-radial-1", "nfex-reaction-wheel-mini-1" };
        public override LookScale ScaleBasis(bool side, string kind) => side ? LookScale.None : LookScale.Diameter;

        public const string TorqueKey = "torque";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            variant = variant ?? DefaultVariant(m);
            bool side = IsSide(variant);
            int v = Index(variants, variant);
            double lo = side ? SideTorqueLo : Torque[v, 0], hi = side ? SideTorqueHi : Torque[v, 1];
            var s = new Stats();
            s[TorqueKey] = Lerp(lo, hi, E(pts, 0, mk));
            s[K.EcRate] = s[TorqueKey] * Lerp(0.08, 0.015, E(pts, 1, mk));
            s[K.Mass] = Lerp(lo, hi, D(pts, 0, mk)) / Lerp(60, 180, E(pts, 2, mk));
            s[K.Price] = BuildPrice(side ? SidePrice : PriceBySize[v], pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Torque", TorqueKey, 0, v => $"{v:0.#} kNm"),
            new Row("Power draw", K.EcRate, 1, Fmt.Ec),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
        };

        public override string Label(string[] m) => "Reaction wheels";
    }

    // Heat shields: ablator efficiency, weight and heat tolerance; size on the part.
    public sealed class HeatShields : PartKind
    {
        public override string Id => "shield";
        public override string Title => "Heat shields";
        public override string Noun => "heat shield";
        public override string ChassisCode => "HSH";

        static readonly Option[] variants = { Size("0625", ""), Size("125", ""), Size("1875"), Size("25"), Size("375"), Size("5") };
        public static readonly double[] Ablator = { 50, 200, 500, 800, 1800, 3200 };
        static readonly double[] ShieldMass = { 0.025, 0.1, 0.3, 0.5, 1.0, 1.8 };
        static readonly double[] PriceBySize = { 150, 300, 600, 1000, 2000, 3500 };

        public override Choice[] Choices => new Choice[0];
        protected override Category[] OwnCategories => new[] { new Category("Ablator"), new Category("Weight"), new Category("Heat tolerance") };
        public override string[] CapTechs => new[] { "survivability", "enhancedSurvivability", "heatManagementSystems", "advHeatManagement" };
        public override double CostCeiling => 1e5;
        public override Option[] Variants(string[] m) => variants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "HeatShield1";

        public const string AblatorFactor = "ablatorFactor", AblatorAmount = "ablator", MaxTemp = "maxTemp";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            int v = Index(variants, variant ?? DefaultVariant(m));
            var s = new Stats();
            s[AblatorFactor] = Lerp(0.6, 1.5, E(pts, 0, mk));
            s[AblatorAmount] = Ablator[v];
            s[K.Mass] = ShieldMass[v] * Lerp(1.6, 0.8, E(pts, 1, mk));
            s[MaxTemp] = Lerp(2400, 3600, E(pts, 2, mk));
            s[K.Price] = BuildPrice(PriceBySize[v], pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Heat soaked per unit of ablator", AblatorFactor, 0, v => Percent(v, "more", "less")),
            new Row("  ablator", AblatorAmount, -1, v => $"{v:N0}"),
            new Row("Weight (the shield)", K.Mass, 1, Fmt.Tons),
            new Row("Heat tolerance", MaxTemp, 2, v => $"{v:N0} K"),
        };

        public override string Label(string[] m) => "Heat shields";
    }

    // Solar panels: fixed or deployable; output, weight, durability; size on the part.
    public sealed class SolarPanels : PartKind
    {
        public override string Id => "solar";
        public override string Title => "Solar panels";
        public override string Noun => "panel";
        public override string ChassisCode => "PV";

        static readonly Choice[] choices = { new Choice("Kind", new Option("F", "Fixed"), new Option("D", "Deployable", "electrics")) };
        static readonly Option[] fixedSizes = { new Option("S", "Small", ""), new Option("M", "Medium", "electrics") };
        static readonly Option[] deployableSizes =
            { new Option("S", "Small", ""), new Option("M", "Medium", ""), new Option("L", "Large", "largeElectrics"), new Option("H", "Huge", "cuttingEdgeSolarTech") };
        static readonly double[,] Output = { { 0.5, 3 }, { 3, 15 }, { 15, 60 }, { 60, 300 } };
        static readonly double[] PriceBySize = { 300, 1500, 3000, 12000 };

        public override Choice[] Choices => choices;
        protected override Category[] OwnCategories => new[] { new Category("Output"), new Category("Weight"), new Category("Durability", 0) };
        public override string[] CapTechs => new[] { "survivability", "electrics", "advSolarTech", "cuttingEdgeSolarTech" };
        public override double CostCeiling => 2.5e5;
        public override Option[] Variants(string[] m) => Deployable(m) ? deployableSizes : fixedSizes;
        public override string VariantName => "Size class";

        // Looks: fixed panels for fixed programs, deployable for deployable. A look
        // is scaled so its panels' area suits the middle of each size class's output.
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Deployable(m) ? "largeSolarPanel" : "LgRadialSolarPanel";
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.Output;
        public override bool LookFits(string[] m, string kind) => kind == (Deployable(m) ? "D" : "F");
        public override string[] LookModel(string kind, string key) => new[] { kind };
        public override double NominalOutput(string variant)
        {
            int v = Index(deployableSizes, variant);
            return Math.Sqrt(Output[v, 0] * Output[v, 1]);
        }

        public static bool Deployable(string[] m) => m != null && m.Length > 0 && m[0] == "D";
        public override bool CategoryUsed(string[] m, int category) => category != 2 || Deployable(m);

        public const string Retractable = "retractable", Toughness = "toughness";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            int v = Index(deployableSizes, variant ?? DefaultVariant(m));
            var s = new Stats();
            s[K.EcRate] = Lerp(Output[v, 0], Output[v, 1], E(pts, 0, mk));
            s[K.Mass] = Lerp(Output[v, 0], Output[v, 1], D(pts, 0, mk)) / Lerp(40, 150, E(pts, 1, mk));
            // Durability: how much air pressure and g-force it takes. From 50 points (as designed) it folds back up.
            s[Toughness] = Deployable(m) ? Lerp(0.5, 3, E(pts, 2, mk)) : 1;
            s[Retractable] = Deployable(m) && P(pts, 2) >= 50 ? 1 : 0;
            s[K.Price] = BuildPrice(PriceBySize[v], pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Output in Kerbin's sunlight", K.EcRate, 0, Fmt.Ec),
            new Row("Weight", K.Mass, 1, Fmt.Tons),
            new Row("Durability", Toughness, 2, v => Percent(v, "tougher", "weaker")),
            new Row("  folds back up", Retractable, -1, v => v > 0 ? "yes" : "no"),
        };

        public override string Label(string[] m) => Deployable(m) ? "Deployable solar panels" : "Fixed solar panels";
        public override string Character(string[] m) => Deployable(m) ? "Tracks the sun. Careful in the air." : "Always out, never tracks, can't break.";
    }

    // Antennas (RemoteTech when installed): omni or dish; range on a log scale.
    public sealed class Antennas : PartKind
    {
        public override string Id => "antenna";
        public override string Title => "Antennas";
        public override string Noun => "antenna";
        public override string ChassisCode => "ANT";

        static readonly Choice[] choices = { new Choice("Kind", new Option("O", "Omni"), new Option("D", "Dish", "basicScience")) };
        static readonly Option[] omni = { new Option("O", "Omni", "") };
        static readonly Option[] dishes =
            { new Option("S", "Short-range", ""), new Option("I", "Interplanetary", "precisionEngineering"), new Option("D", "Deep-space", "unmannedTech") };

        // [omni, short, interplanetary, deep]: range m, power EC/s, weight t, cone degrees
        static readonly double[,] Range = { { 0.5e6, 6e6 }, { 5e6, 100e6 }, { 5e9, 80e9 }, { 50e9, 500e9 } };
        static readonly double[,] Power = { { 0.3, 0.02 }, { 1.0, 0.1 }, { 1.5, 0.3 }, { 3, 0.6 } };
        static readonly double[,] Weight = { { 0.03, 0.005 }, { 0.1, 0.01 }, { 0.3, 0.05 }, { 1.0, 0.15 } };
        static readonly double[] Cone = { 360, 25, 0.1, 0.01 };
        static readonly double[] PriceBy = { 300, 800, 2500, 8000 };

        public override Choice[] Choices => choices;
        protected override Category[] OwnCategories => new[] { new Category("Range"), new Category("Power draw"), new Category("Weight") };
        public override string[] CapTechs => new[] { "start", "precisionEngineering", "unmannedTech", "largeUnmanned" };
        public override double CostCeiling => 2.5e5;
        public override Option[] Variants(string[] m) => Dish(m) ? dishes : omni;
        public override string VariantName => "Class";
        public override string DefaultVariant(string[] m) => Dish(m) ? "S" : "O";

        // Looks: omni with omni, dish with dish. Dishes are scaled by weight to
        // each class; an omni keeps its own size.
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Dish(m) ? "HighGainAntenna" : "longAntenna";
        public override LookScale ScaleBasis(bool side, string kind) => kind == "D" ? LookScale.Mass : LookScale.None;
        public override bool LookFits(string[] m, string kind) => kind == (Dish(m) ? "D" : "O");
        public override string[] LookModel(string kind, string key) => new[] { kind };
        public override double NominalMass(string variant)
        {
            int k = 1 + Index(dishes, variant);
            return Math.Sqrt(Weight[k, 0] * Weight[k, 1]);
        }

        public static bool Dish(string[] m) => m != null && m.Length > 0 && m[0] == "D";
        public const string RangeKey = "range", ConeKey = "cone";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            int k = Dish(m) ? 1 + Index(dishes, variant ?? DefaultVariant(m)) : 0;
            var s = new Stats();
            s[RangeKey] = Log(Range[k, 0], Range[k, 1], E(pts, 0, mk));
            s[K.EcRate] = Lerp(Power[k, 0], Power[k, 1], E(pts, 1, mk));
            s[K.Mass] = Lerp(Weight[k, 0], Weight[k, 1], E(pts, 2, mk));
            s[ConeKey] = Cone[k];
            s[K.Price] = BuildPrice(PriceBy[k], pts);
            return s;
        }

        public static string Distance(double m) => m >= 1e9 ? $"{m / 1e9:0.#} Gm" : m >= 1e6 ? $"{m / 1e6:0.#} Mm" : $"{m / 1e3:0} km";

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Range", RangeKey, 0, Distance),
            new Row("Power draw", K.EcRate, 1, Fmt.Ec),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
            new Row("  cone", ConeKey, -1, v => v >= 360 ? "all round" : $"{v:0.##}°"),
        };

        public override string Label(string[] m) => Dish(m) ? "Dish antennas" : "Omni antennas";
    }

    // Probe cores: autopilot in steps, power, weight, torque; size on the part.
    public sealed class ProbeCores : PartKind
    {
        public override string Id => "probe";
        public override string Title => "Probe cores";
        public override string Noun => "probe core";
        public override string ChassisCode => "PRB";

        static readonly Option[] variants =
            { Size("0625", ""), new Option("125", "1.25 m", "basicScience"), new Option("1875", "1.875 m", "unmannedTech"), new Option("25", "2.5 m", "largeUnmanned") };
        // The last entry is the Mk2 shape: the Mk2 Drone Core's 800 EC and 15 kNm, 0.2 t at 75 points.
        static readonly double[,] Weight = { { 0.065, 0.03 }, { 0.2, 0.1 }, { 0.3, 0.15 }, { 0.6, 0.3 }, { 0.4, 0.13 } };
        static readonly double[] Torque = { 1, 5, 10, 20, 15 };
        public static readonly double[] Battery = { 50, 200, 400, 1000, 800 };
        static readonly double[] PriceBySize = { 450, 1000, 2000, 4000, 2700 };

        // A Mk2-shaped probe core (DESIGN.md 21.2): made from an installed Mk2 probe
        // core (the Mk2 Drone Core), unscaled, the way a tank adapter is made from an
        // adapter. It goes by the adapter mount, named "Mk2" here.
        static readonly Option[] shaped = { new Option("A-MK2", "drone core", "automation") };
        public override bool HasAdapters => true;
        public override Option[] AdapterVariants(string[] m) => shaped;
        public override string AdapterKind(string variant) => variant == "A-MK2" ? "mk2" : null;
        public override string[] AdapterLookPreference(string variant) => new[] { "mk2DroneCore" };
        public override string MountName(string mount) => mount == "A" ? "Mk2" : base.MountName(mount);
        public override LookScale ScaleBasis(bool side, string kind) => kind == "mk2" ? LookScale.None : LookScale.Diameter;
        static int SizeIndex(string variant) => variant == "A-MK2" ? 4 : Index(variants, variant);

        public override Choice[] Choices => new Choice[0];
        protected override Category[] OwnCategories =>
            new[] { new Category("Autopilot"), new Category("Power draw"), new Category("Weight"), new Category("Torque", 10) };
        public override string[] CapTechs => new[] { "start", "precisionEngineering", "advUnmanned", "largeUnmanned" };
        public override double CostCeiling => 6e5;
        public override Option[] Variants(string[] m) => variants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "probeCoreOcto2.v2";

        public const string Sas = "sas", TorqueKey = "torque", BatteryKey = "battery";

        // -1 none, 0 hold, 1 + prograde/retrograde, 2 + normal/radial/target, 3 + maneuver node.
        public static int SasLevel(double effectivePoints) =>
            effectivePoints >= 75 ? 3 : effectivePoints >= 50 ? 2 : effectivePoints >= 25 ? 1 : effectivePoints >= 10 ? 0 : -1;

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            int v = SizeIndex(variant ?? DefaultVariant(m));
            var s = new Stats();
            s[Sas] = SasLevel(E(pts, 0, mk));
            s[K.EcRate] = Lerp(0.1, 0.015, E(pts, 1, mk));
            s[K.Mass] = Lerp(Weight[v, 0], Weight[v, 1], E(pts, 2, mk));
            s[TorqueKey] = Lerp(0, Torque[v], E(pts, 3, mk));
            s[BatteryKey] = Battery[v];
            s[K.Price] = BuildPrice(PriceBySize[v], pts);
            return s;
        }

        static string SasName(double level)
        {
            switch ((int)Math.Round(level))
            {
                case 0: return "hold attitude";
                case 1: return "+ pro/retrograde";
                case 2: return "+ target, normal";
                case 3: return "+ maneuver node";
                default: return "none";
            }
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Autopilot (SAS)", Sas, 0, SasName),
            new Row("Power draw", K.EcRate, 1, v => $"{v:0.000} EC/s"),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
            new Row("Torque", TorqueKey, 3, v => $"{v:0.#} kNm"),
            new Row("  battery", BatteryKey, -1, v => $"{v:N0} EC"),
        };

        public override string Label(string[] m) => "Probe cores";
        public override string Character(string[] m) => "Autopilot comes in steps: 10, 25, 50 and 75 points.";
    }

    // What fuel tanks and plane fuselages share: the tank's kind (what it holds), the
    // Weight / Strength / Insulation sliders, and numbers by propellant volume
    // (an FL-T400 holds 400), so a fuselage and a round tank of the same volume
    // weigh and cost the same. The statics live on FuelTanks.
    public abstract class TankKind : PartKind
    {
        public static readonly Option[] KindOptions =
        {
            new Option("S", "Standard"), new Option("C", "Cryogenic", "largeVolumeContainment"),
            new Option("M", "Monopropellant", "flightControl"), new Option("E", "Electric propellant", "ionPropulsion"),
        };

        // Propellant volume for a size.
        public abstract double TankVolume(string variant);

        protected override Category[] OwnCategories => new[] { new Category("Weight"), new Category("Strength"), new Category("Insulation", 0) };
        public override double CostCeiling => 1e5;
        public override bool CategoryUsed(string[] m, int category) => category != 2 || FuelTanks.Cryogenic(m);

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            s[FuelTanks.WeightFactor] = Lerp(1.6, 0.8, E(pts, 0, mk));
            s[FuelTanks.Crash] = Lerp(4, 20, E(pts, 1, mk));
            s[FuelTanks.MaxTemp] = Lerp(1400, 2600, E(pts, 1, mk));
            s[FuelTanks.Boiloff] = FuelTanks.Cryogenic(m) ? Lerp(2, 0.2, E(pts, 2, mk)) : FuelTanks.Uninsulated;
            s[FuelTanks.BoiloffRate] = FuelTanks.HydrogenBoiloff * s[FuelTanks.Boiloff];
            s[FuelTanks.VolumeKey] = TankVolume(variant ?? DefaultVariant(m));
            s[FuelTanks.UnitsKey] = s[FuelTanks.VolumeKey] * FuelTanks.UnitsPerVolume(m);
            s[K.Mass] = FuelTanks.ChassisMass + s[FuelTanks.VolumeKey] * FuelTanks.MassPerVolume(m) * s[FuelTanks.WeightFactor];
            // Dry price: about 0.8 a unit of volume (an FL-T400 is ~500 with its fuel).
            s[K.Price] = BuildPrice(0.8 * s[FuelTanks.VolumeKey], pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Empty weight", K.Mass, 0, Fmt.Tons),
            new Row("Survives impacts to", FuelTanks.Crash, 1, v => $"{v:0} m/s"),
            new Row("  and heat to", FuelTanks.MaxTemp, -1, v => $"{v:N0} K"),
            FuelTanks.Cryogenic(m) ? new Row("Hydrogen boil-off", FuelTanks.BoiloffRate, 2, v => $"{v:0.###}% an hour")
                : new Row("Hydrogen boil-off, uninsulated", FuelTanks.BoiloffRate, m != null && m.Length > 0 && m[0] == "S" ? -1 : 2, v => $"{v:0.###}% an hour"),
            new Row($"  holds ({FuelTanks.Contents(m)})", FuelTanks.UnitsKey, -1, v => $"{v:N0} units"),
        };

        public override string Character(string[] m)
        {
            switch (m[0])
            {
                case "S": return "Any fuel's mix, picked on the part. No insulation: hydrogen and other cryogenic fuels boil off fast.";
                case "C": return "Hydrolox, methalox or another cryogenic mix, picked on the part. Boils off unless cooled.";
                case "M": return "Monopropellant.";
                default: return "Xenon, argon or lithium, picked on the part.";
            }
        }
    }

    // Fuel tanks: kind, size and length per program; propellant on the part. Weight is a
    // factor on today's tanks. Standard tanks hold any fuel's mix but have no
    // insulation or cooling, so cryogenic fuels boil off fast in them; cryogenic tanks
    // (later in the tree) have Insulation and cooling (DESIGN.md 19.7).
    public sealed class FuelTanks : TankKind
    {
        public override string Id => "tank";
        public override string Title => "Fuel tanks";
        public override string Noun => "tank";
        public override string ChassisCode => "TNK";

        static readonly Choice[] choices =
        {
            new Choice("Kind", KindOptions),
        };
        public static readonly string[] SizeCodes = { "0625", "125", "1875", "25", "375", "5" };
        public static readonly string[] LengthCodes = { "S", "M", "L", "X" };
        public static readonly string[] LengthLabels = { "short", "medium", "long", "extra long" };
        public static readonly double[] LengthFactor = { 0.25, 0.5, 1, 2 };   // FL-T100, T200, T400, T800

        static readonly Option[] variants = SizeCodes.SelectMany(s => LengthCodes.Select((l, i) =>
            new Option(s + "-" + l, Sizes.Label(s) + " " + LengthLabels[i], Sizes.Tooling(s) ?? ""))).ToArray();

        // Side-mount (radial) tanks, by volume (DESIGN.md 20.1): the Stratus-V Mini,
        // Dumpling, Baguette, HR-1 and HR-18 are about these sizes.
        public static readonly string[] SideCodes = { "R-T", "R-S", "R-M", "R-L", "R-H" };
        public static readonly double[] SideVolume = { 5, 25, 50, 200, 1600 };
        static readonly Option[] sideVariants =
        {
            new Option("R-T", "Tiny", "propulsionSystems"), new Option("R-S", "Small", "propulsionSystems"),
            new Option("R-M", "Medium", "propulsionSystems"), new Option("R-L", "Large", Sizes.Tooling("1875")),
            new Option("R-H", "Huge", Sizes.Tooling("25")),
        };

        // Adapter tanks (DESIGN.md 20.2): a size pair and a length, for the program's
        // life. An adapter's taper is built into its model, so each ratio has its own
        // model: an installed adapter of that ratio (its look kind). Each ratio holds
        // what its stock adapter holds, scaled by size cubed, and is as tall for its
        // size (the C7, the ADTP-2-3, the FL-A215, the FL-A150). Short: half of both.
        // One fill for every ratio was the plan, but the C7 holds about a third less
        // for its shape than the other three, so each matches its own (measured 2026-11-06).
        sealed class Ratio
        {
            public readonly string Kind, Needs;
            public readonly double Big, Volume, Height, Value;
            public readonly string[] Looks;
            public Ratio(string kind, double value, double big, double volume, double height, string needs, params string[] looks)
            { Kind = kind; Value = value; Big = big; Volume = volume; Height = height; Needs = needs; Looks = looks; }
        }
        static readonly Ratio[] ratios =
        {
            new Ratio("A2", 2, 2.5, 800, 2.5, null, "adapterSize2-Size1"),
            new Ratio("A32", 1.5, 3.75, 3000, 2.25017, null, "Size3To2Adapter.v2"),
            new Ratio("A43", 4 / 3.0, 2.5, 1200, 1.875, "Making History, ReStock+ or Near Future Launch Vehicles",
                "Size1p5.Size2.Adapter.01", "restock-fueltank-adapter-25-1875-1", "nflv-fueltank-adapter-5-375-2", "Size3.Size4.Adapter.01"),
            new Ratio("A3", 3, 1.875, 160, 0.75, "Making History or ReStock+", "Size1p5.Size0.Adapter.01", "restock-fueltank-adapter-1875-0625-1"),
        };
        // Big end, small end.
        public static readonly string[][] AdapterPairs =
        {
            new[] { "125", "0625" }, new[] { "1875", "0625" }, new[] { "1875", "125" }, new[] { "25", "125" }, new[] { "25", "1875" },
            new[] { "375", "1875" }, new[] { "375", "25" }, new[] { "5", "25" }, new[] { "5", "375" },
        };
        public static readonly string[] AdapterLengthCodes = { "S", "L" };
        static readonly string[] adapterLengthLabels = { "short", "long" };
        // Plane adapters (DESIGN.md 21.2): each joins a plane section to another shape as
        // one stock part does, holds what it holds and is as long; it wears that part
        // (or another with the same two ends), stretched only along its length. Their
        // kinds start "AP" and name the two ends ("x2": the narrow end splits in two).
        sealed class PlaneAdapter
        {
            public readonly string Code, Kind, Label;
            public readonly double Volume, Height;
            public readonly string[] Techs, Looks;
            public PlaneAdapter(string code, string kind, string label, double volume, double height, string[] techs, params string[] looks)
            { Code = code; Kind = kind; Label = label; Volume = volume; Height = height; Techs = techs; Looks = looks; }
        }
        static readonly PlaneAdapter[] planeAdapters =
        {
            new PlaneAdapter("A-MK3-MK2-L", "APmk2-mk3", "Mk3 to Mk2 long", 2000, 5, new[] { "heavyAerodynamics" }, "adapterMk3-Mk2"),
            new PlaneAdapter("A-MK3-25-L", "APmk3-size2", "Mk3 to 2.5 m long", 2500, 3.75, new[] { "heavyAerodynamics", Sizes.Tooling("25") }, "adapterMk3-Size2"),
            new PlaneAdapter("A-MK3-375-L", "APmk3-size3", "Mk3 to 3.75 m long", 2500, 1.875, new[] { "experimentalAerodynamics", Sizes.Tooling("375") }, "adapterSize3-Mk3"),
            new PlaneAdapter("A-25-MK2-L", "APmk2-size2", "2.5 m to Mk2 long", 800, 3.75, new[] { "highAltitudeFlight", Sizes.Tooling("25") }, "adapterSize2-Mk2"),
            new PlaneAdapter("A-MK2-125-S", "APmk2-size1", "Mk2 to 1.25 m short", 400, 1.875, new[] { "supersonicFlight" }, "mk2SpacePlaneAdapter", "mk2.1m.AdapterLong"),
            new PlaneAdapter("A-MK2-125-L", "APmk2-size1", "Mk2 to 1.25 m long", 800, 3.75, new[] { "highAltitudeFlight" }, "mk2.1m.AdapterLong", "mk2SpacePlaneAdapter"),
            new PlaneAdapter("A-MK2-2X125-L", "APmk2-size1x2", "Mk2 to two 1.25 m long", 400, 1.875, new[] { "supersonicFlight" }, "mk2.1m.Bicoupler"),
        };
        static PlaneAdapter PlaneAdapterOf(string variant) => planeAdapters.FirstOrDefault(p => p.Code == variant);

        // The plane adapter kind for a part's ends ("mk2", "size1"...) and whether its
        // narrow end splits in two, or null when PIP makes no such pair.
        public static string PlaneAdapterKindOf(IEnumerable<string> profiles, bool twin)
        {
            string[] ends = profiles.Where(x => x != "srf").Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
            if (ends.Length != 2 || !ends.Any(x => x == "mk2" || x == "mk3")) return null;
            string kind = "AP" + ends[0] + "-" + ends[1] + (twin ? "x2" : "");
            return planeAdapters.Any(p => p.Kind == kind) ? kind : null;
        }
        public static bool IsPlaneAdapterKind(string kind) => kind != null && kind.StartsWith("AP", StringComparison.Ordinal);

        static readonly Option[] adapterVariants = AdapterPairs.SelectMany(p => AdapterLengthCodes.Select((l, i) =>
            Option.Needing($"A-{p[0]}-{p[1]}-{l}", $"{Sizes.Label(p[0]).Replace(" m", "")} to {Sizes.Label(p[1])} {adapterLengthLabels[i]}",
                "fuelSystems", Sizes.Tooling(p[1]), Sizes.Tooling(p[0]))))
            .Concat(planeAdapters.Select(p => Option.Needing(p.Code, p.Label, new[] { "fuelSystems" }.Concat(p.Techs).ToArray()))).ToArray();

        // The ratio kind of an adapter with these ends ("A2" for 2.5 to 1.25 m), or null.
        public static string AdapterKindOf(double big, double small)
        {
            if (small <= 0) return null;
            double r = big / small;
            return ratios.FirstOrDefault(x => Math.Abs(r - x.Value) < 0.02)?.Kind;
        }

        // A long adapter's height (m) for a ratio at this big end.
        public static double AdapterHeight(string kind, double big)
        {
            Ratio r = ratios.FirstOrDefault(x => x.Kind == kind);
            return r == null ? 0 : r.Height * big / r.Big;
        }

        // Tank-shaped parts that stay what they are (DESIGN.md 20.2, decision 13), for
        // Replace and looks alike: skeletal and truss adapters, cluster tanks,
        // nosecones and slanted adapters. Slanted and multi-adapters are also told
        // apart by their off-axis nodes.
        static readonly string[] keptWords = { "skeletal", "skl", "truss", "girder", "cluster", "nose", "slant" };
        public static bool KeptTank(string name, string title) =>
            keptWords.Any(w => (name ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               (title ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);

        static Ratio RatioOf(string variant, out double big, out double length)
        {
            big = 0; length = 1;
            if (!IsAdapter(variant)) return null;
            string[] p = variant.Split('-');
            int b = p.Length > 3 ? Sizes.IndexOf(p[1]) : -1, s = p.Length > 3 ? Sizes.IndexOf(p[2]) : -1;
            if (b < 0 || s < 0) return null;
            big = Sizes.Diameters[b];
            length = p[3] == "S" ? 0.5 : 1;
            string kind = AdapterKindOf(big, Sizes.Diameters[s]);
            return ratios.FirstOrDefault(r => r.Kind == kind);
        }

        public override bool HasAdapters => true;
        public override Option[] AdapterVariants(string[] m) => adapterVariants;
        public override string AdapterKind(string variant) => PlaneAdapterOf(variant)?.Kind ?? RatioOf(variant, out _, out _)?.Kind;
        public override string[] AdapterLookPreference(string variant) =>
            PlaneAdapterOf(variant)?.Looks ?? ratios.FirstOrDefault(r => r.Kind == AdapterKind(variant))?.Looks ?? new string[0];
        public override string AdapterNeeds(string kind) => ratios.FirstOrDefault(r => r.Kind == kind)?.Needs;
        public override double NominalHeight(string variant)
        {
            Ratio r = RatioOf(variant, out double big, out double length);
            return r == null ? 0 : r.Height * big / r.Big * length;
        }
        // A plane adapter is stretched to its stock part's length, never widened.
        public override double NominalLength(string variant) => PlaneAdapterOf(variant)?.Height ?? 0;

        public override Choice[] Choices => choices;
        public override string[] CapTechs => new[] { "basicRocketry", "fuelSystems", "largeVolumeContainment", "specializedFuelStorage" };
        public override Option[] Variants(string[] m) => variants;
        public override string VariantName => "Size and length";
        public override string DefaultVariant(string[] m) => "125-L";

        // Looks: one look part per kind, since each kind has its own propellants.
        // A tank's kind is what it holds: fuel tanks serve standard and cryogenic
        // programs, monopropellant (M) and electric-propellant (E) tanks their own.
        public override bool HasSideMount => true;
        public override Option[] SideVariants(string[] m) => sideVariants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "fuelTank";
        // Stock radial tanks, always installed: the Baguette and Dumpling, the
        // Stratus-V monopropellant tanks, the PB-X50R xenon container.
        public override string[] SideLookPreference(string[] m)
        {
            switch (m != null && m.Length > 0 ? m[0] : "S")
            {
                case "M": return new[] { "rcsTankRadialLong", "radialRCSTank" };
                case "E": return new[] { "xenonTankRadial" };
                default: return new[] { "externalTankCapsule", "externalTankRound" };
            }
        }
        // In-line looks are stretched to the size and length; radial ones scaled by
        // volume; adapters scaled to the big end and stretched to the height.
        public override LookScale ScaleBasis(bool side, string kind) =>
            side ? LookScale.Volume : IsPlaneAdapterKind(kind) ? LookScale.Length : IsAdapterKind(kind) ? LookScale.Adapter : LookScale.Tank;
        public override double NominalVolume(string variant) => Volume(variant);
        public override string LookKey(string[] m) => m[0];
        // An adapter look (kind "A2" ...) serves every kind of tank: stock has no
        // monopropellant or xenon adapters to dress those as.
        public override string[] LookKeys(string kind) =>
            kind == "M" ? new[] { "M" } : kind == "E" ? new[] { "E" } : IsAdapterKind(kind) ? new[] { "S", "C", "M", "E" } : new[] { "S", "C" };
        public static bool IsAdapterKind(string kind) => kind != null && kind.Length > 1 && kind[0] == 'A';
        public override string[] LookModel(string kind, string key) => new[] { key };
        // FL-T400 proportions: a "long" tank is this many times as tall as it is wide.
        public const double LongAspect = 1.894225 / 1.25;

        public static bool Cryogenic(string[] m) => m != null && m.Length > 0 && m[0] == "C";

        // Propellant volume in KSP units (an FL-T400 holds 400). Tanks scale like
        // stock ones: a long 2.5 m tank is an FL-T400 twice as big every way (X200-32).
        public static double Volume(string variant)
        {
            int side = Array.IndexOf(SideCodes, variant);
            if (side >= 0) return SideVolume[side];
            if (IsAdapter(variant))
            {
                if (PlaneAdapterOf(variant) is PlaneAdapter pa) return pa.Volume;
                Ratio r = RatioOf(variant, out double big, out double length);
                if (r == null) return 0;
                double k = big / r.Big;
                return r.Volume * k * k * k * length;
            }
            string[] p = (variant ?? "125-L").Split('-');
            int s = Math.Max(0, Array.IndexOf(SizeCodes, p[0]));
            int l = p.Length > 1 ? Math.Max(0, Array.IndexOf(LengthCodes, p[1])) : 2;
            double d = Sizes.Diameters[s] / 1.25;
            return 400 * d * d * d * LengthFactor[l];
        }

        public const string WeightFactor = "weightFactor", Crash = "crash", MaxTemp = "maxTemp", Boiloff = "boiloff", VolumeKey = "volume";
        public const string BoiloffRate = "boiloffRate";

        // Today's empty tank, per unit of volume, with each kind's standard contents
        // (the B9PartSwitch tank types: LF/Ox, LH2/Ox, monopropellant, xenon), plus
        // the chassis's own 0.001 t. On the part, the contents actually picked count.
        public const double ChassisMass = 0.001;
        public static double MassPerVolume(string[] m)
        {
            switch (m != null && m.Length > 0 ? m[0] : "S")
            {
                case "C": return 0.000278904;
                case "M": return 0.0004;
                case "E": return 0.00123;
                default: return 0.000625;
            }
        }

        // What a tank of each kind holds as it comes (its first propellant on the
        // part), in resource units per unit of volume: LF 0.45 + Ox 0.55; LH2 4.995 +
        // Ox 0.333; monopropellant 1.5; xenon 15.75 (the B9PartSwitch tank types).
        public static double UnitsPerVolume(string[] m)
        {
            switch (m != null && m.Length > 0 ? m[0] : "S")
            {
                case "C": return 5.328;
                case "M": return 1.5;
                case "E": return 15.75;
                default: return 1;
            }
        }

        public static string Contents(string[] m)
        {
            switch (m != null && m.Length > 0 ? m[0] : "S")
            {
                case "C": return "LH2/Ox";
                case "M": return "monoprop";
                case "E": return "xenon";
                default: return "LF/Ox";
            }
        }

        // Units a tank of this kind and size holds as it comes.
        public static double Units(string[] m, string variant) => Volume(variant) * UnitsPerVolume(m);
        public const string UnitsKey = "units";
        // Liquid hydrogen boil-off, % of the fuel per hour, before Insulation (Fuels.cfg; CryoTanks' rate).
        public const double HydrogenBoiloff = 0.05;
        // A standard tank's boil-off against the base rate: no insulation, no cooling
        // (a cryogenic tank with no Insulation points is 2).
        public const double Uninsulated = 4;

        public override double TankVolume(string variant) => Volume(variant ?? "125-L");

        public override string Label(string[] m) => choices[0].Find(m[0])?.Label + " tanks";
    }

    // Plane fuselages (DESIGN.md 21.2): Mk2 and Mk3 fuel tanks, a type of their own in
    // PIP's window but tanks in every other way: the same kinds, sliders and numbers
    // by volume. Mk2 short and long (1.875 and 3.75 m, holding 400 and 800, as
    // stock's); Mk3 short, medium and long (2.5, 5 and 10 m: 2,500, 5,000, 10,000).
    // In the VAB they're under Fuel Tanks. Mk2 fuselages keep their body lift.
    public sealed class PlaneFuselages : TankKind
    {
        public override string Id => "plane";
        public override string Title => "Plane fuselages";
        public override string Noun => "fuselage";
        public override string ChassisCode => "PLN";

        static readonly Choice[] choices =
        {
            new Choice("Kind", KindOptions),
            new Choice("Section", new Option("2", "Mk2"), new Option("3", "Mk3", "heavyAerodynamics")),
        };
        static readonly Option[] mk2 = { new Option("2-S", "Short"), new Option("2-L", "Long", "highAltitudeFlight") };
        static readonly Option[] mk3 = { new Option("3-S", "Short"), new Option("3-M", "Medium"), new Option("3-L", "Long", "experimentalAerodynamics") };
        static readonly Dictionary<string, double[]> lengthAndVolume = new Dictionary<string, double[]>
        {
            { "2-S", new[] { 1.875, 400.0 } }, { "2-L", new[] { 3.75, 800.0 } },
            { "3-S", new[] { 2.5, 2500.0 } }, { "3-M", new[] { 5.0, 5000.0 } }, { "3-L", new[] { 10.0, 10000.0 } },
        };
        // Stock Mk2 fuselages' body lift, per meter of length (0.7 on the long one).
        public const double Mk2LiftPerMeter = 0.7 / 3.75;

        public override Choice[] Choices => choices;
        public override string[] CapTechs => new[] { "supersonicFlight", "highAltitudeFlight", "heavyAerodynamics", "experimentalAerodynamics" };
        public override Option[] Variants(string[] m) => Mk3(m) ? mk3 : mk2;
        public override string VariantName => "Length";
        public override string DefaultVariant(string[] m) => Mk3(m) ? "3-M" : "2-L";
        public static bool Mk3(string[] m) => m != null && m.Length > 1 && m[1] == "3";

        public static double Length(string variant) => lengthAndVolume.TryGetValue(variant ?? "", out double[] lv) ? lv[0] : 0;
        public static double Volume(string variant) => lengthAndVolume.TryGetValue(variant ?? "", out double[] lv) ? lv[1] : 0;
        public override double TankVolume(string variant) => Volume(variant ?? "2-L");
        public static double Units(string[] m, string variant) => Volume(variant) * FuelTanks.UnitsPerVolume(m);

        // Looks: any Mk2 or Mk3 fuel fuselage of the program's section (its kind, "2" or
        // "3"), stretched to the length; one serves every kind, as adapters do.
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Mk3(m) ? "mk3FuselageLFO.50" : "mk2FuselageLongLFO";
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.Length;
        public override double NominalLength(string variant) => Length(variant);
        public override string LookKey(string[] m) => m[0];
        public override string[] LookKeys(string kind) => new[] { "S", "C", "M", "E" };
        public override bool LookFits(string[] m, string kind) => kind == (Mk3(m) ? "3" : "2");
        public override string[] LookModel(string kind, string key) => new[] { key, kind };

        public override string Label(string[] m) => $"Mk{m[1]} {Fmt.Lower(KindOptions.First(o => o.Code == m[0]).Label)} fuselages";
    }

    // Decouplers (DESIGN.md 21.1): a stack decoupler or a stack separator, picked
    // for the program's life; a decoupler can also be side-mount, a radial
    // decoupler. Today's parts set the middle of each range: mass by diameter
    // squared (the TD-12's 0.04 t at 1.25 m), ejection force by diameter^0.6 (its
    // 100 kN; 50 at 0.625 m, 200 at 3.75 m); separators weigh a quarter more.
    public sealed class Decouplers : PartKind
    {
        public override string Id => "decoupler";
        public override string Title => "Decouplers";
        public override string Noun => "decoupler";
        public override string ChassisCode => "DEC";

        static readonly Choice[] choices =
        {
            new Choice("Kind", new Option("D", "Decoupler"), new Option("S", "Separator", "generalConstruction")),
        };
        static readonly Option[] variants = { Size("0625", ""), Size("125", ""), Size("1875"), Size("25"), Size("375"), Size("5") };
        // Radial: the TT-14, the TT-38K and TT-70, the hydraulic manifold and TTR-6R.
        static readonly Option[] sideVariants =
            { new Option("R-S", "Small", "stability"), new Option("R-M", "Medium", "stability"), new Option("R-L", "Large", "specializedConstruction") };
        static readonly double[] SideMass = { 0.0125, 0.04, 0.4 };
        static readonly double[] SideEject = { 70, 250, 600 };
        static readonly double[] SidePrice = { 250, 600, 770 };

        public override Choice[] Choices => choices;
        protected override Category[] OwnCategories => new[] { new Category("Ejection force"), new Category("Weight"), new Category("Strength") };
        public override string[] CapTechs => new[] { "engineering101", "generalConstruction", "advConstruction", "specializedConstruction" };
        public override double CostCeiling => 5e4;
        public override Option[] Variants(string[] m) => variants;
        public override string DefaultVariant(string[] m) => "125";
        // Only a decoupler goes on a side: separators are stack parts.
        public override bool HasSideMount => true;
        public override Option[] SideVariants(string[] m) => Separator(m) ? null : sideVariants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Separator(m) ? "Separator.1" : "Decoupler.1";
        public override string[] SideLookPreference(string[] m) => new[] { "radialDecoupler", "radialDecoupler2" };
        public override LookScale ScaleBasis(bool side, string kind) => side ? LookScale.Mass : LookScale.Diameter;
        public override double NominalMass(string variant) => IsSide(variant) ? SideMass[Index(sideVariants, variant)] : 0;
        // A look is only the model: any stack decoupler or separator dresses
        // either kind (its look chassis is set to the program's kind); radial
        // decouplers (kind "R") dress decouplers.
        public override string LookKey(string[] m) => m[0];
        public override string[] LookKeys(string kind) => kind == "R" ? new[] { "D" } : new[] { "D", "S" };
        public override string[] LookModel(string kind, string key) => new[] { key };

        public const string Eject = "eject", Crash = "crash", MaxTemp = "maxTemp";
        public static bool Separator(string[] m) => m != null && m.Length > 0 && m[0] == "S";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            variant = variant ?? DefaultVariant(m);
            bool side = IsSide(variant);
            int v = Index(sideVariants, variant);
            int si = Sizes.IndexOf(variant);
            double d = (si < 0 ? 1.25 : Sizes.Diameters[si]) / 1.25;
            double sep = Separator(m) ? 1.25 : 1;
            var s = new Stats();
            s[Eject] = (side ? SideEject[v] : 100 * Math.Pow(d, 0.6)) * Lerp(0.5, 2.5, E(pts, 0, mk));
            s[K.Mass] = (side ? SideMass[v] : 0.04 * d * d * sep) * Lerp(1.6, 0.8, E(pts, 1, mk));
            s[Crash] = Lerp(4, 20, E(pts, 2, mk));
            s[MaxTemp] = Lerp(1400, 2600, E(pts, 2, mk));
            s[K.Price] = BuildPrice(side ? SidePrice[v] : 200 * Math.Pow(d, 0.6) * (Separator(m) ? 1.3 : 1), pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Ejection force", Eject, 0, v => $"{v:N0} kN"),
            new Row("Weight", K.Mass, 1, Fmt.Tons),
            new Row("Survives impacts to", Crash, 2, v => $"{v:0} m/s"),
            new Row("  and heat to", MaxTemp, -1, v => $"{v:N0} K"),
        };

        public override string Label(string[] m) => Separator(m) ? "Separators" : "Decouplers";
        public override string Character(string[] m) => Separator(m)
            ? "Lets go of both sides when it fires and falls away: a clean break."
            : "Stays with one side when it fires. Can be side-mount, for radial boosters.";
    }

    // RTGs (Near Future): output per ton and half-life; size on the part.
    public sealed class Rtgs : PartKind
    {
        public override string Id => "rtg";
        public override string Title => "RTGs";
        public override string Noun => "RTG";
        public override string ChassisCode => "RTG";

        static readonly Option[] variants = { new Option("S", "Small", ""), new Option("L", "Large", "highTechElectricalSystems") };
        static readonly double[] MassBySize = { 0.03, 0.3 };
        static readonly double[] PriceBySize = { 4000, 20000 };

        public override Choice[] Choices => new Choice[0];
        protected override Category[] OwnCategories => new[] { new Category("Output"), new Category("Lifetime") };
        public override string[] CapTechs => new[] { "specializedElectrics", "experimentalElectrics", "highTechElectricalSystems", "highPowerElectricalSystems" };
        public override double CostCeiling => 6e5;
        public override Option[] Variants(string[] m) => variants;
        public override string VariantName => "Size class";
        public override string DefaultVariant(string[] m) => "S";
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "rtg";
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.Mass;
        public override double NominalMass(string variant) => MassBySize[Index(variants, variant)];

        public const string PerTon = "perTon", HalfLife = "halfLife";

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            int v = Index(variants, variant ?? DefaultVariant(m));
            var s = new Stats();
            s[PerTon] = Lerp(6, 15, E(pts, 0, mk));
            s[K.Mass] = MassBySize[v];
            s[K.EcRate] = s[PerTon] * s[K.Mass];
            s[HalfLife] = Lerp(4, 20, E(pts, 1, mk));
            s[K.Price] = BuildPrice(PriceBySize[v], pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Output", PerTon, 0, v => $"{v:0.0} EC/s per ton"),
            new Row("  this size makes", K.EcRate, -1, Fmt.Ec),
            new Row("Lifetime (half-life)", HalfLife, 1, v => $"{v:0.#} years"),
        };

        public override string Label(string[] m) => "RTGs";
    }

    // Air intakes: speed type matched to the jets; airflow, top speed, weight; size on the part.
    public sealed class AirIntakes : PartKind
    {
        public override string Id => "intake";
        public override string Title => "Air intakes";
        public override string Noun => "intake";
        public override string ChassisCode => "INT";

        static readonly Choice[] choices =
        {
            new Choice("Speed type", new Option("S", "Subsonic"), new Option("P", "Supersonic", "highAltitudeFlight"), new Option("H", "Hypersonic", "hypersonicFlight")),
        };
        static readonly Option[] variants =
            { Size("0625", ""), Size("125", ""), new Option("1875", "1.875 m", "highAltitudeFlight"), new Option("25", "2.5 m", "highAltitudeFlight") };
        static readonly Option[] sideVariants = { new Option("R", "Side-mount", "") };
        static readonly double[] BaseTopMach = { 2.5, 5, 8 };
        // Today's intake of each speed type at 1.25 m (circular, ramp, shock cone)
        // and side-mount (XM-G50; the radial ramp for the faster two): area, tons.
        // These, not the look's own, are what the Airflow and Weight factors scale.
        static readonly double[] BaseArea = { 0.006, 0.0074, 0.0075 }, BaseMass = { 0.04, 0.06, 0.12 };
        static readonly double[] SideArea = { 0.0031, 0.001, 0.001 }, SideMass = { 0.02, 0.01, 0.01 };
        static readonly string[] Default = { "CircularIntake", "ramAirIntake", "shockConeIntake" };

        public override Choice[] Choices => choices;
        protected override Category[] OwnCategories => new[] { new Category("Airflow"), new Category("Top speed"), new Category("Weight") };
        public override string[] CapTechs => new[] { "aviation", "aerodynamicSystems", "highAltitudeFlight", "hypersonicFlight" };
        public override double CostCeiling => 1e5;
        public override Option[] Variants(string[] m) => variants;
        public override bool HasSideMount => true;
        public override Option[] SideVariants(string[] m) => sideVariants;
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Default[SpeedIndex(m)];
        public override string[] SideLookPreference(string[] m) =>
            SpeedIndex(m) == 0 ? new[] { "airScoop", "IntakeRadialLong" } : new[] { "IntakeRadialLong", "airScoop" };
        public override LookScale ScaleBasis(bool side, string kind) => side ? LookScale.None : LookScale.Diameter;

        public const string AreaFactor = "areaFactor", MassFactor = "massFactor", Area = "area";

        static int SpeedIndex(string[] m) => m == null || m.Length == 0 ? 0 : Index(choices[0].Options, m[0]);

        public static double SizeScale(string variant)
        {
            if (IsSide(variant)) return 1;
            int i = Sizes.IndexOf(variant ?? "125");
            return i < 0 ? 1 : Sizes.Diameters[i] / 1.25;
        }

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            int t = SpeedIndex(m);
            variant = variant ?? DefaultVariant(m);
            bool side = IsSide(variant);
            double sc = SizeScale(variant);
            var s = new Stats();
            s[AreaFactor] = Lerp(0.6, 1.5, E(pts, 0, mk));
            s[K.Speed] = Lerp(0.8, 1.2, E(pts, 1, mk));
            s[K.TopMach] = BaseTopMach[t] * s[K.Speed];
            s[MassFactor] = Lerp(1.6, 0.8, E(pts, 2, mk));
            s[Area] = (side ? SideArea[t] : BaseArea[t] * sc * sc) * s[AreaFactor];
            s[K.Mass] = (side ? SideMass[t] : BaseMass[t] * sc * sc) * s[MassFactor];
            s[K.Price] = BuildPrice(side ? 250 : 300 * sc * sc, pts);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Intake area", Area, 0, v => $"{v:0.0000} m²"),
            new Row("Top speed", K.TopMach, 1, Fmt.Mach),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
        };

        public override string Label(string[] m) => choices[0].Find(m[0])?.Label + " intakes";
    }
}
