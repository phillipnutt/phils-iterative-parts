using System;

namespace PIP
{
    // The engine families beyond liquids. Numbers from DESIGN.md section 13.

    // Solid boosters: size and length; the length sets the propellant load.
    public sealed class SolidMotors : PartType
    {
        public override string Id => "solid";
        public override string Title => "Solid boosters";
        public override string Noun => "booster";
        public override string ChassisCode => "SOL";
        public override double PriceFactor => 0.5;
        public override int ReliabilityCategory => 3;

        static readonly Choice[] choices =
        {
            new Choice("Size",
                new Option("0625", "0.625 m"), new Option("125", "1.25 m"),
                new Option("1875", "1.875 m", Sizes.Tooling("1875")), new Option("25", "2.5 m", Sizes.Tooling("25")),
                new Option("375", "3.75 m", Sizes.Tooling("375"))),
            new Choice("Length", new Option("S", "Short"), new Option("M", "Medium"), new Option("L", "Long", "generalRocketry")),
        };

        static readonly Category[] categories =
        {
            new Category("Thrust"), new Category("Efficiency"), new Category("Weight"), new Category("Reliability"), new Category("Gimbal", 0),
        };

        public override Choice[] Choices => choices;
        public override Category[] Categories => categories;
        public override string[] CapTechs => new[] { "start", "generalRocketry", "heavierRocketry", "experimentalRocketry" };

        // SolidFuel units, [size][length]; 1 unit = 7.5 kg. The chassis carry the same amounts.
        public static readonly double[,] Units =
        {
            { 40, 90, 325 }, { 375, 820, 2600 }, { 1265.625, 2767.5, 5800 }, { 3000, 8000, 16400 }, { 10125, 27000, 55350 },
        };
        public const double UnitMass = 0.0075;

        // Vacuum kN at medium length, { 0 points, 100 points }; short x0.6, long x2.
        static readonly double[,] MediumThrust = { { 12, 50 }, { 120, 480 }, { 300, 1100 }, { 650, 2500 }, { 2000, 7500 } };
        static readonly double[] LengthThrust = { 0.6, 1, 2 };

        static int Size(string[] m) => Math.Max(0, Array.FindIndex(choices[0].Options, o => o.Code == m[0]));
        static int Length(string[] m) => Math.Max(0, Array.FindIndex(choices[1].Options, o => o.Code == m[1]));

        public static double PropellantUnits(string[] m) => Units[Size(m), Length(m)];

        // Looks: a booster at the program's size and length; kind "125-M". Its
        // length is whichever of the size's three loads its own fuel is nearest.
        static readonly string[][] Standard =
        {
            new[] { "Mite", "Shrimp", "MassiveBooster" }, new[] { "solidBooster.v2", "solidBooster1-1", "MassiveBooster" },
            new[] { "solidBooster.v2", "solidBooster1-1", "Pollux" }, new[] { "solidBooster.v2", "Thoroughbred", "Clydesdale" },
            new[] { "solidBooster.v2", "Thoroughbred", "Clydesdale" },
        };
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Standard[Size(m)][Length(m)];
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public override bool LookFits(string[] m, string kind) => kind == m[0] + "-" + m[1];
        public static string LengthOf(string size, double units)
        {
            int s = Array.FindIndex(choices[0].Options, o => o.Code == size);
            if (s < 0 || units <= 0) return null;
            int best = 0;
            for (int l = 1; l < 3; l++)
                if (Math.Abs(Math.Log(units / Units[s, l])) < Math.Abs(Math.Log(units / Units[s, best]))) best = l;
            return choices[1].Options[best].Code;
        }
        public static double UnitsFor(string size, string length)
        {
            int s = Array.FindIndex(choices[0].Options, o => o.Code == size), l = Array.FindIndex(choices[1].Options, o => o.Code == length);
            return s < 0 || l < 0 ? 0 : Units[s, l];
        }

        public override double FailureAt(string[] m, int rel, int mk, FailureLevel level) => Failure.Chance(0.01, 0.0005, rel, mk, level);

        static double GimbalAt(double p) => p <= 0 ? 0 : p <= 10 ? 0.2 * p : 2 + (p - 10) * 6.0 / 90.0;

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            int si = Size(m);
            double lengthK = LengthThrust[Length(m)];
            s[K.ThrustVac] = Lerp(MediumThrust[si, 0], MediumThrust[si, 1], E(pts, 0, mk)) * lengthK;
            s[K.IspSL] = Lerp(150, 240, E(pts, 1, mk));
            s[K.IspVac] = Lerp(170, 265, E(pts, 1, mk));
            s[K.ThrustSL] = s[K.ThrustVac] * s[K.IspSL] / s[K.IspVac];
            s[K.Propellant] = PropellantUnits(m) * UnitMass;
            s[K.Mass] = s[K.Propellant] * Lerp(0.27, 0.13, E(pts, 2, mk));
            s[K.Burn] = s[K.Propellant] * s[K.IspVac] * Engines.G0 / Math.Max(0.001, s[K.ThrustVac]);
            s[K.Fail] = FailureAt(m, P(pts, 3), mk, level);
            s[K.Gimbal] = GimbalAt(E(pts, 4, mk));
            // About 1 fund per unit of propellant, fuel included (as stock prices are).
            s[K.Price] = BuildPrice(PropellantUnits(m), pts, mk);
            return s;
        }

        public override Row[] Rows(string[] m) => new[]
        {
            new Row("Thrust, vacuum", K.ThrustVac, 0, Fmt.Kn),
            new Row("  at sea level", K.ThrustSL, -1, Fmt.Kn),
            new Row("  burn at full thrust", K.Burn, -1, Fmt.Sec),
            new Row("Isp, vacuum", K.IspVac, 1, Fmt.Sec),
            new Row("  Isp at sea level", K.IspSL, -1, Fmt.Sec),
            new Row("Weight (casing)", K.Mass, 2, Fmt.Tons),
            new Row("  propellant", K.Propellant, -1, Fmt.Tons),
            new Row("Fails to light", K.Fail, 3, Fmt.Pct),
            new Row("Gimbal", K.Gimbal, 4, Fmt.Deg),
        };

        public override string Label(string[] m) => $"{Sizes.Label(m[0])} solid, {choices[1].Find(m[1])?.Label.ToLowerInvariant()}";
        public override string Character(string[] m) => "Thrust is never wasted: the thrust limiter trades it for burn time.";
    }

    // Monopropellant engines: one vacuum nozzle, size only.
    public sealed class MonopropEngines : PartType
    {
        public override string Id => "monoprop";
        public override string Title => "Monopropellant engines";
        public override string Noun => "engine";
        public override string ChassisCode => "MON";
        public override double PriceFactor => 0.5;
        public override int ReliabilityCategory => 3;

        static readonly Choice[] choices =
        {
            new Choice("Size", new Option("0625", "0.625 m"), new Option("125", "1.25 m"),
                new Option("25", "2.5 m", Sizes.Tooling("25")), new Option("375", "3.75 m", Sizes.Tooling("375"))),
        };

        static readonly Category[] categories =
        {
            new Category("Thrust"), new Category("Efficiency"), new Category("Weight"), new Category("Reliability"), new Category("Gimbal", 10),
        };

        public override Choice[] Choices => choices;
        public override Category[] Categories => categories;
        public override string[] CapTechs => new[] { "propulsionSystems", "precisionPropulsion", "heavyLanding", "experimentalRocketry" };

        // Looks: a monopropellant engine at the program's size; kind "125".
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => "orbital-engine-" + m[0];
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public override bool LookFits(string[] m, string kind) => kind == m[0];

        static readonly double[,] ThrustW = { { 4, 20 }, { 25, 170 }, { 60, 350 }, { 150, 700 } };
        static readonly double[] BasePrice = { 400, 1000, 2500, 6000 };

        static int Size(string[] m) => Math.Max(0, Array.FindIndex(choices[0].Options, o => o.Code == m[0]));

        public override double FailureAt(string[] m, int rel, int mk, FailureLevel level) => Failure.Chance(0.01, 0.0005, rel, mk, level);

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            int si = Size(m);
            s[K.ThrustVac] = Lerp(ThrustW[si, 0], ThrustW[si, 1], E(pts, 0, mk));
            s[K.IspSL] = Lerp(150, 215, E(pts, 1, mk));
            s[K.IspVac] = Lerp(260, 345, E(pts, 1, mk));
            s[K.ThrustSL] = s[K.ThrustVac] * s[K.IspSL] / s[K.IspVac];
            double light = Lerp(ThrustW[si, 0], ThrustW[si, 1], P(pts, 0)) / (25 * Engines.G0);
            s[K.Mass] = light * (2 - E(pts, 2, mk) / 100.0);
            s[K.Fail] = FailureAt(m, P(pts, 3), mk, level);
            s[K.Gimbal] = GimbalCurve(E(pts, 4, mk));
            s[K.Price] = BuildPrice(BasePrice[si], pts, mk);
            return s;
        }

        public override Row[] Rows(string[] m) => new[]
        {
            new Row("Thrust, vacuum", K.ThrustVac, 0, Fmt.Kn),
            new Row("  at sea level", K.ThrustSL, -1, Fmt.Kn),
            new Row("Isp, vacuum", K.IspVac, 1, Fmt.Sec),
            new Row("  Isp at sea level", K.IspSL, -1, Fmt.Sec),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
            new Row("Fails to light", K.Fail, 3, Fmt.Pct),
            new Row("Gimbal", K.Gimbal, 4, Fmt.Deg),
        };

        public override string Label(string[] m) => $"{Sizes.Label(m[0])} monopropellant";
        public override string Character(string[] m) => "An orbital maneuvering engine: made for vacuum.";
    }

    // Nuclear thermal: solid or gas core, hydrogen only. The fifth category is
    // Shielding when Kerbalism is installed (it makes these engines radioactive),
    // Gimbal otherwise.
    public sealed class NuclearEngines : PartType
    {
        public static bool Kerbalism;

        public override string Id => "nuclear";
        public override string Title => "Nuclear engines";
        public override string Noun => "engine";
        public override string ChassisCode => "NUC";
        public override double PriceFactor => 2;
        public override int ReliabilityCategory => 3;

        static readonly Choice[] choices =
        {
            new Choice("Core", new Option("S", "Solid core"), new Option("G", "Gas core", "expNuclearPropulsion")),
            new Choice("Size",
                new Option("0625", "0.625 m", "improvedNuclearPropulsion"), new Option("125", "1.25 m"),
                new Option("1875", "1.875 m", Sizes.Tooling("1875")), new Option("25", "2.5 m", Sizes.Tooling("25")),
                new Option("375", "3.75 m", Sizes.Tooling("375"))),
        };

        public override Choice[] Choices => choices;
        public override Category[] Categories => new[]
        {
            new Category("Thrust"), new Category("Efficiency"), new Category("Weight"), new Category("Reliability"),
            Kerbalism ? new Category("Shielding", 0) : new Category("Gimbal", 10),
        };
        public override string[] CapTechs => new[] { "nuclearPropulsion", "improvedNuclearPropulsion", "advNuclearPropulsion", "expNuclearPropulsion" };

        // Looks: any nuclear engine at the program's size, either core; kind "125".
        // A look keeps one hydrogen mode (no oxygen boost, no liquid-fuel mode).
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m)
        {
            if (Gas(m)) return "ntr-gc-25-1";
            switch (m[1])
            {
                case "0625": return "ntr-sc-0625-1";
                case "125": case "1875": return "nuclearEngine";
                default: return "restock-engine-cherenkov";
            }
        }
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public override bool LookFits(string[] m, string kind) => kind == m[1];

        public override bool Valid(string[] m) => base.Valid(m) && !(m[0] == "G" && m[1] == "0625");

        // kN { 0, 100 } by size: 0.625, 1.25, 1.875, 2.5, 3.75 (gas core has no 0.625).
        static readonly double[,] SolidThrust = { { 6, 25 }, { 30, 120 }, { 60, 250 }, { 120, 500 }, { 400, 1500 } };
        static readonly double[,] GasThrust = { { 0, 0 }, { 100, 400 }, { 200, 800 }, { 400, 1600 }, { 1000, 4000 } };
        static readonly double[] Radiation = { 0.05, 0.25, 0.35, 0.5, 1.0 };   // rad/h at 0 points
        static readonly double[] BasePrice = { 8000, 12000, 20000, 40000, 100000 };

        static int Size(string[] m) => Math.Max(0, Array.FindIndex(choices[1].Options, o => o.Code == m[1]));
        static bool Gas(string[] m) => m[0] == "G";

        public override double FailureAt(string[] m, int rel, int mk, FailureLevel level) => Failure.Chance(0.03, 0.002, rel, mk, level);

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            int si = Size(m);
            bool gas = Gas(m);
            double[,] w = gas ? GasThrust : SolidThrust;
            s[K.ThrustVac] = Lerp(w[si, 0], w[si, 1], E(pts, 0, mk));
            s[K.IspSL] = gas ? Lerp(500, 1200, E(pts, 1, mk)) : Lerp(150, 400, E(pts, 1, mk));
            s[K.IspVac] = gas ? Lerp(1500, 3000, E(pts, 1, mk)) : Lerp(750, 1000, E(pts, 1, mk));
            s[K.ThrustSL] = s[K.ThrustVac] * s[K.IspSL] / s[K.IspVac];
            double light = Lerp(w[si, 0], w[si, 1], P(pts, 0)) / ((gas ? 12 : 5.5) * Engines.G0);
            s[K.Mass] = light * (2 - E(pts, 2, mk) / 100.0);
            s[K.Fail] = FailureAt(m, P(pts, 3), mk, level);
            if (Kerbalism)
            {
                s[K.Radiation] = Radiation[si] * (gas ? 2 : 1) * Math.Pow(0.1, E(pts, 4, mk) / 100.0);
                s[K.Gimbal] = 3;
            }
            else s[K.Gimbal] = GimbalCurve(E(pts, 4, mk));
            s[K.Price] = BuildPrice(BasePrice[si] * (gas ? 2.5 : 1), pts, mk);
            return s;
        }

        public override Row[] Rows(string[] m) => new[]
        {
            new Row("Thrust, vacuum", K.ThrustVac, 0, Fmt.Kn),
            new Row("  at sea level", K.ThrustSL, -1, Fmt.Kn),
            new Row("Isp, vacuum", K.IspVac, 1, Fmt.Sec),
            new Row("  Isp at sea level", K.IspSL, -1, Fmt.Sec),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
            new Row("Fails to light", K.Fail, 3, Fmt.Pct),
            Kerbalism ? new Row("Radiation given off", K.Radiation, 4, Fmt.Rad) : new Row("Gimbal", K.Gimbal, 4, Fmt.Deg),
        };

        public override string Label(string[] m) => $"{Sizes.Label(m[1])} {(Gas(m) ? "gas-core" : "solid-core")} nuclear";
        public override string Character(string[] m) =>
            Gas(m) ? "Enormous efficiency, heavy and hot." : "Twice a chemical engine's efficiency, at a fraction of its thrust.";
    }

    // Electric: gridded ion (xenon), Hall (argon) or MPD (lithium). Power points
    // set how much electricity each kN and each second of Isp costs.
    public sealed class ElectricEngines : PartType
    {
        public override string Id => "electric";
        public override string Title => "Electric engines";
        public override string Noun => "thruster";
        public override string ChassisCode => "ELE";
        public override double PriceFactor => 1.5;
        public override int ReliabilityCategory => 3;

        static readonly Choice[] choices =
        {
            new Choice("Type", new Option("I", "Gridded ion"), new Option("H", "Hall"), new Option("M", "MPD", "plasmaPropulsion")),
            new Choice("Size", new Option("0625", "0.625 m"), new Option("125", "1.25 m", "advIonPropulsion"), new Option("25", "2.5 m", "advEMSystems")),
        };

        static readonly Category[] categories =
        {
            new Category("Thrust"), new Category("Efficiency"), new Category("Weight"), new Category("Reliability"), new Category("Power"),
        };

        public override Choice[] Choices => choices;
        public override Category[] Categories => categories;
        public override string[] CapTechs => new[] { "ionPropulsion", "advIonPropulsion", "advGriddedThrusters", "advEMSystems" };

        // Looks: a thruster at the program's size burning the type's gas (xenon
        // gridded ion, argon Hall, lithium MPD); kind "I-0625". One-mode thrusters only.
        static readonly string[][] Standard =
        {
            new[] { "ionXenon-0625", "ionXenon-0625-2", "ionXenon-0625-3" },
            new[] { "ionArgon-0625", "ionArgon-0625-3", "ionArgon-0625-3" },
            new[] { "mpdt-0625", "mpdt-125", "mpdt-25" },
        };
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m) => Standard[TypeIndex(m)][Size(m)];
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public override bool LookFits(string[] m, string kind) => kind == m[0] + "-" + m[1];
        public static string TypeForGas(string gas)
        {
            int i = Array.IndexOf(Gas, gas);
            return i < 0 ? null : choices[0].Options[i].Code;
        }

        // [type][size] = { 0, 100 } kN
        static readonly double[][,] ThrustW =
        {
            new double[,] { { 0.5, 5 }, { 2, 20 }, { 8, 80 } },
            new double[,] { { 1, 10 }, { 4, 40 }, { 15, 150 } },
            new double[,] { { 15, 60 }, { 35, 130 }, { 90, 320 } },
        };
        static readonly double[,] IspW = { { 3000, 12000 }, { 1800, 3800 }, { 2200, 4200 } };
        static readonly double[] Twr = { 2.2, 3.6, 18 };          // at 100 weight points
        static readonly double[] TypePrice = { 1, 0.8, 3 };
        static readonly double[] SizePrice = { 2500, 6000, 15000 };
        public static readonly string[] Gas = { "XenonGas", "ArgonGas", "Lithium" };

        public static int TypeIndex(string[] m) => Math.Max(0, Array.FindIndex(choices[0].Options, o => o.Code == m[0]));
        static int Size(string[] m) => Math.Max(0, Array.FindIndex(choices[1].Options, o => o.Code == m[1]));

        public override double FailureAt(string[] m, int rel, int mk, FailureLevel level) => Failure.Chance(0.02, 0.001, rel, mk, level);

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            int t = TypeIndex(m), si = Size(m);
            double[,] w = ThrustW[t];
            s[K.ThrustVac] = Lerp(w[si, 0], w[si, 1], E(pts, 0, mk));
            s[K.IspVac] = Lerp(IspW[t, 0], IspW[t, 1], E(pts, 1, mk));
            s[K.IspSL] = s[K.IspVac] / 100;
            s[K.ThrustSL] = s[K.ThrustVac] / 100;
            double light = Lerp(w[si, 0], w[si, 1], P(pts, 0)) / (Twr[t] * Engines.G0);
            s[K.Mass] = light * (2 - E(pts, 2, mk) / 100.0);
            s[K.Fail] = FailureAt(m, P(pts, 3), mk, level);
            // EC/s at full thrust = kN x Isp/1000 x factor; points move the factor 6 -> 2.5.
            s[K.EcRate] = s[K.ThrustVac] * s[K.IspVac] / 1000 * Lerp(6, 2.5, E(pts, 4, mk));
            s[K.Price] = BuildPrice(SizePrice[si] * TypePrice[t], pts, mk);
            return s;
        }

        public override Row[] Rows(string[] m) => new[]
        {
            new Row("Thrust, vacuum", K.ThrustVac, 0, Fmt.Kn),
            new Row("Isp, vacuum", K.IspVac, 1, Fmt.Sec),
            new Row("Weight", K.Mass, 2, Fmt.Tons),
            new Row("Fails to light", K.Fail, 3, Fmt.Pct),
            new Row("Power at full thrust", K.EcRate, 4, Fmt.Ec),
        };

        public override string Label(string[] m) => $"{Sizes.Label(m[1])} {choices[0].Find(m[0])?.Label.ToLowerInvariant()}";
        public override string Character(string[] m)
        {
            switch (m[0])
            {
                case "I": return "Xenon. The highest Isp and the gentlest push.";
                case "H": return "Argon. More push than ion, less Isp.";
                default: return "Lithium. Real thrust, and it needs a reactor's worth of power.";
            }
        }
    }

    // Jets: turbofan, afterburning turbojet, ramjet or combined cycle. No
    // reliability (no failures); Speed stretches the type's speed range.
    public sealed class JetEngines : PartType
    {
        public override string Id => "jet";
        public override string Title => "Jet engines";
        public override string Noun => "jet";
        public override string ChassisCode => "JET";

        static readonly Choice[] choices =
        {
            new Choice("Type",
                new Option("F", "Turbofan"), new Option("J", "Turbojet", "supersonicFlight"),
                new Option("R", "Ramjet", "hypersonicFlight"), new Option("C", "Combined cycle", "aerospaceTech")),
            new Choice("Size",
                new Option("0625", "0.625 m"), new Option("125", "1.25 m"),
                new Option("1875", "1.875 m", "highAltitudeFlight"), new Option("25", "2.5 m", "highAltitudeFlight")),
        };

        static readonly Category[] categories =
        {
            new Category("Thrust"), new Category("Efficiency"), new Category("Weight"), new Category("Speed"),
        };

        public override Choice[] Choices => choices;
        public override Category[] Categories => categories;
        public override string[] CapTechs => new[] { "aviation", "supersonicFlight", "highAltitudeFlight", "aerospaceTech" };

        // Looks: a jet at the program's size with the same modes: one air-breathing
        // mode (turbofan, ramjet) "125-1", dry and afterburner (turbojet) "125-2",
        // air-breathing and rocket (combined cycle) "125-C".
        static readonly string[][] Standard =
        {
            new[] { "miniJetEngine", "JetEngine", "JetEngine", "turboFanSize2" },
        };
        public override bool HasLooks => true;
        public override string DefaultLook(string[] m)
        {
            switch (m[0])
            {
                case "J": return "turboJet";
                case "R": return "turboFanEngine";
                case "C": return "RAPIER";
                default: return Standard[0][Size(m)];
            }
        }
        public override LookScale ScaleBasis(bool side, string kind) => LookScale.None;
        public static string Modes(string[] m) => m[0] == "J" ? "2" : m[0] == "C" ? "C" : "1";
        public override bool LookFits(string[] m, string kind) => kind == m[1] + "-" + Modes(m);

        static readonly double[,] ThrustW = { { 10, 40 }, { 60, 200 }, { 120, 360 }, { 200, 600 } };
        // Isp { 0, 100 }: turbofan, turbojet (afterburner), ramjet, combined (air).
        static readonly double[,] IspW = { { 5000, 15000 }, { 2500, 4500 }, { 2500, 5000 }, { 2500, 4000 } };
        static readonly double[] BaseTopMach = { 2.2, 3.3, 5.5, 6.0 };
        static readonly double[] TypePrice = { 1, 1.3, 1.5, 3 };
        static readonly double[] SizePrice = { 500, 1500, 2200, 3000 };

        public static int TypeIndex(string[] m) => Math.Max(0, Array.FindIndex(choices[0].Options, o => o.Code == m[0]));
        static int Size(string[] m) => Math.Max(0, Array.FindIndex(choices[1].Options, o => o.Code == m[1]));

        public override Stats Compute(string[] m, int[] pts, int mk, FailureLevel level, string variant = null)
        {
            var s = new Stats();
            int t = TypeIndex(m), si = Size(m);
            double eff = E(pts, 1, mk);
            s[K.ThrustVac] = Lerp(ThrustW[si, 0], ThrustW[si, 1], E(pts, 0, mk));
            s[K.IspVac] = Lerp(IspW[t, 0], IspW[t, 1], eff);
            s[K.IspSL] = s[K.IspVac];
            s[K.ThrustSL] = s[K.ThrustVac];
            if (t == 1)
            {
                s[K.ThrustDry] = s[K.ThrustVac] * 2 / 3;
                s[K.IspDry] = Lerp(5000, 10000, eff);
            }
            if (t == 3)
            {
                s[K.RocketThrust] = s[K.ThrustVac] * 1.7;
                s[K.RocketIspSL] = Lerp(240, 300, eff);
                s[K.RocketIspVac] = Lerp(290, 340, eff);
            }
            double light = Lerp(ThrustW[si, 0], ThrustW[si, 1], P(pts, 0)) / (12 * Engines.G0);
            s[K.Mass] = light * (2 - E(pts, 2, mk) / 100.0);
            s[K.Speed] = Lerp(0.8, 1.2, E(pts, 3, mk));
            s[K.TopMach] = BaseTopMach[t] * s[K.Speed];
            s[K.Price] = BuildPrice(SizePrice[si] * TypePrice[t], pts, mk);
            return s;
        }

        public override Row[] Rows(string[] m)
        {
            int t = TypeIndex(m);
            var rows = new System.Collections.Generic.List<Row>
            {
                new Row(t == 1 ? "Thrust, afterburner" : t == 3 ? "Thrust, air-breathing" : "Thrust", K.ThrustVac, 0, Fmt.Kn),
            };
            if (t == 1) rows.Add(new Row("  dry thrust", K.ThrustDry, -1, Fmt.Kn));
            if (t == 3) rows.Add(new Row("  rocket thrust", K.RocketThrust, -1, Fmt.Kn));
            rows.Add(new Row(t == 1 ? "Isp, afterburner" : "Isp", K.IspVac, 1, Fmt.Sec));
            if (t == 1) rows.Add(new Row("  dry Isp", K.IspDry, -1, Fmt.Sec));
            if (t == 3) rows.Add(new Row("  rocket Isp, vacuum", K.RocketIspVac, -1, Fmt.Sec));
            rows.Add(new Row("Weight", K.Mass, 2, Fmt.Tons));
            rows.Add(new Row("Top speed", K.TopMach, 3, Fmt.Mach));
            return rows.ToArray();
        }

        public override string Label(string[] m) => $"{Sizes.Label(m[1])} {choices[0].Find(m[0])?.Label.ToLowerInvariant()}";
        public override string Character(string[] m)
        {
            switch (m[0])
            {
                case "F": return "Efficient and subsonic.";
                case "J": return "Dry for cruising, afterburner for speed.";
                case "R": return "Weak off the runway, strong around Mach 3.";
                default: return "Breathes air to Mach 6, then becomes a rocket.";
            }
        }
    }
}
