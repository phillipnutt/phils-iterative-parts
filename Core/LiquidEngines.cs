using System;
using System.Linq;

namespace PIP
{
    // Liquid rocket engines: fuel, size and nozzle; thrust, efficiency, weight,
    // reliability and gimbal. Numbers from DESIGN.md sections 4 and 8.
    public sealed class LiquidEngines : PartType
    {
        public override string Id => "liquid";
        public override string Title => "Liquid engines";
        public override string Noun => "engine";

        // Chassis names predate other part types: PIP-K-125-S.
        public override string ChassisPart(string[] model) => "PIP-" + Code(model);

        // The fuels come from the fuel table (Core/Fuels.cs, GameData/PIP/Fuels.cfg).
        static Choice[] choices = Build();
        static LiquidEngines() { FuelTable.Changed += () => choices = Build(); }

        static Choice[] Build() => new[]
        {
            new Choice("Fuel", FuelTable.All.Select(f => new Option(f.Code, f.Title, string.IsNullOrEmpty(f.Tech) ? null : f.Tech)).ToArray()),
            new Choice("Size",
                new Option("0625", "0.625 m", "propulsionSystems"),
                new Option("125", "1.25 m"),
                new Option("1875", "1.875 m", Sizes.Tooling("1875")),
                new Option("25", "2.5 m", Sizes.Tooling("25")),
                new Option("375", "3.75 m", Sizes.Tooling("375")),
                new Option("5", "5 m", Sizes.Tooling("5"))),
            new Choice("Nozzle",
                new Option("S", "Sea level"),
                new Option("B", "Balanced", "precisionPropulsion"),
                new Option("V", "Vacuum", "generalRocketry"),
                new Option("A", "Aerospike", "experimentalRocketry")),
        };

        static readonly Category[] categories =
        {
            new Category("Thrust"), new Category("Efficiency"), new Category("Weight"),
            new Category("Reliability"),
        };

        public override Choice[] Choices => choices;
        protected override Category[] OwnCategories => categories;
        public override string[] CapTechs => new[] { "basicRocketry", "advRocketry", "heavierRocketry", "experimentalRocketry" };
        public override double CostCeiling => 1.5e6;
        public override int ReliabilityCategory => 3;
        // Gimbal is a free pick (DESIGN.md 18.4); an aerospike can't gimbal.
        public override bool HasGimbalPick => true;
        public override bool CanGimbal(string[] m) => !IsAerospike(m);

        public const int Thrust = 0, Efficiency = 1, Weight = 2, Reliability = 3;

        // Looks (16): an engine at the program's own size, never scaled; aerospike
        // looks for aerospike programs only. One look part per fuel, since the
        // propellants are in the config. Its kind: "125", or "125-A" for an aerospike.
        static readonly string[][] Standard =
        {
            new[] { "nflv-engine-rutherford-1", "liquidEngine2.v2", "LiquidEngineLV-TX87", "liquidEngineMainsail.v2", "nflv-engine-rd701-1", "nflv-engine-rd701-1" },
            new[] { "liquidEngineMini.v2", "LiquidEngineRE-I2", "nflv-engine-rd704-1", "engineLargeSkipper.v2", "cryoengine-etna-1", "cryoengine-etna-1" },
            new[] { "nflv-engine-rutherford-vac-1", "liquidEngine3.v2", "cryoengine-pavonis-1", "liquidEngine2-2.v2", "Size3AdvancedEngine", "Size3AdvancedEngine" },
        };
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) =>
            IsAerospike(m) ? "toroidalAerospike" : Standard[Math.Max(0, Array.FindIndex(choices[2].Options, o => o.Code == m[2]))][Math.Max(0, Sizes.IndexOf(m[1]))];
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public override string LookKey(string[] m) => m[0];
        public override string[] LookKeys(string kind) => FuelTable.All.Select(f => f.Code).ToArray();
        public override bool LookFits(string[] m, string kind) => kind == m[1] + (IsAerospike(m) ? "-A" : "");

        // kN, [size] = { 0 points, 100 points }
        static readonly double[,] LiftThrust = { { 15, 110 }, { 90, 550 }, { 250, 1500 }, { 500, 2200 }, { 1500, 5000 }, { 3000, 9000 } };
        static readonly double[,] VacuumThrust = { { 4, 30 }, { 20, 150 }, { 60, 400 }, { 110, 600 }, { 350, 1400 }, { 700, 2500 } };

        static readonly double[] BasePrice = { 300, 1200, 2500, 6000, 20000, 40000 };

        // The fuel's numbers (Isp windows by nozzle, thrust, weight, price, failures)
        // come from the fuel table.
        static FuelDef Fuel(string[] m) => FuelTable.Find(m[0]) ?? FuelTable.All[0];
        static int SizeIndex(string[] m) => Math.Max(0, Sizes.IndexOf(m[1]));
        static int NozzleIndex(string[] m) => Math.Max(0, Array.FindIndex(choices[2].Options, o => o.Code == m[2]));
        public static bool IsAerospike(string[] m) => m != null && m.Length > 2 && m[2] == "A";
        static bool IsVacuum(string[] m) => m[2] == "V";

        public static double ThrustAt(string[] m, double points)
        {
            double[,] w = IsVacuum(m) ? VacuumThrust : LiftThrust;
            int s = SizeIndex(m);
            return Lerp(w[s, 0], w[s, 1] * Fuel(m).Thrust, points);
        }

        public static double IspSLAt(string[] m, double points)
        {
            double[] w = Fuel(m).Isp[NozzleIndex(m)];
            return Lerp(w[0], w[1], points);
        }

        public static double IspVacAt(string[] m, double points)
        {
            double[] w = Fuel(m).Isp[NozzleIndex(m)];
            return Lerp(w[2], w[3], points);
        }

        // Weight follows the thrust the program was designed for (its full thrust
        // points): 100 points = thrust ÷ (thrust-to-weight × g), 0 points = twice that.
        public static double MassAt(string[] m, double thrustPoints, double weightPoints)
        {
            double light = ThrustAt(m, thrustPoints) / (Fuel(m).Twr * Engines.G0);
            return 2 * light - light * weightPoints / 100.0;
        }

        public override double FailureAt(string[] m, int reliabilityPoints, int mk, FailureLevel level)
        {
            FuelDef f = Fuel(m);
            return Failure.Chance(f.FailHi, f.FailLo, reliabilityPoints, mk, level);
        }

        public double BuildPriceAt(string[] m, int[] points) =>
            BuildPrice(BasePrice[SizeIndex(m)] * Fuel(m).Price * (IsAerospike(m) ? 2 : 1), points);


        public override Stats Compute(string[] m, int[] points, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            s[K.ThrustVac] = ThrustAt(m, E(points, Thrust, mk));
            s[K.IspVac] = IspVacAt(m, E(points, Efficiency, mk));
            s[K.IspSL] = IspSLAt(m, E(points, Efficiency, mk));
            s[K.ThrustSL] = s[K.IspVac] > 0 ? s[K.ThrustVac] * s[K.IspSL] / s[K.IspVac] : 0;
            s[K.Mass] = MassAt(m, D(points, Thrust, mk), E(points, Weight, mk));
            s[K.Fail] = FailureAt(m, P(points, Reliability), mk, level);
            s[K.Price] = BuildPriceAt(m, points);
            return s;
        }

        protected override Row[] OwnRows(string[] m) => new[]
        {
            new Row("Thrust, vacuum", K.ThrustVac, Thrust, Fmt.Kn),
            new Row("  at sea level", K.ThrustSL, -1, Fmt.Kn),
            new Row("Isp, vacuum", K.IspVac, Efficiency, Fmt.Sec),
            new Row("  Isp at sea level", K.IspSL, -1, Fmt.Sec),
            new Row("Weight", K.Mass, Weight, Fmt.Tons),
            new Row("Fails to light", K.Fail, Reliability, Fmt.Pct),
            new Row("  gimbal", K.Gimbal, -1, v => IsAerospike(m) ? "none" : Fmt.Deg(v)),
        };

        public override string Label(string[] m) =>
            Sizes.Label(m[1]) + " " + Fmt.Lower(choices[0].Find(m[0])?.Label) + ", " +
            (IsAerospike(m) ? "aerospike" : choices[2].Find(m[2])?.Label.ToLowerInvariant() + " nozzle");

        public override string Character(string[] m)
        {
            switch (m[2])
            {
                case "S": return "Built for the pad.";
                case "B": return "A compromise: decent low and high.";
                case "V": return "An upper-stage engine, nearly useless at sea level.";
                default: return "Good at any altitude, best at none, and it can't gimbal.";
            }
        }
    }

    public static class Engines
    {
        public const double G0 = 9.80665;
    }
}
