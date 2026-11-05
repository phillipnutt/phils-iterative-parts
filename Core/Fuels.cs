using System;
using System.Collections.Generic;
using System.Linq;

namespace PIP
{
    // One fuel a liquid engine program can burn (DESIGN.md section 19). The table
    // starts as kerolox, hydrolox and methalox; in game, GameData/PIP/Fuels.cfg
    // (PIP_FUEL nodes) replaces it, so a fuel is data, not code.
    public sealed class FuelDef
    {
        public string Code = "K", Title = "Kerolox", Tech = "";
        public string Note = "";                        // one line for the new-program window
        public string[] Resources = { "LiquidFuel", "Oxidizer" };
        public double[] Ratios = { 0.9, 1.1 };          // engine propellant ratios, as KSP writes them
        // Isp windows by nozzle (S, B, V, A): sea level at 0 and 100 points, vacuum at 0 and 100.
        public double[][] Isp =
        {
            new double[] { 220, 310, 245, 340 }, new double[] { 190, 285, 270, 360 },
            new double[] { 50, 100, 300, 385 }, new double[] { 240, 305, 280, 350 },
        };
        public double Thrust = 1;        // thrust ceiling against kerolox
        public double Twr = 24.5;        // engine thrust-to-weight at 100 weight points
        public double Price = 1;         // build price against kerolox
        public double FailHi = 0.02, FailLo = 0.001;   // mature fail-to-light at 0 and 100 reliability points

        public FuelDef Clone() => (FuelDef)MemberwiseClone();
    }

    // A fuel's Waterfall plume templates (DESIGN.md 19.4), from its PLUME node in
    // Fuels.cfg. Each role lists templates best first; the first installed is used.
    public sealed class PlumeSet
    {
        public string[] Match = new string[0];      // words in a template's name that mean "already this fuel"
        public Dictionary<string, string[]> Roles = new Dictionary<string, string[]>();
    }

    // An engine look or chassis burning a different fuel than its model was made
    // for gets that fuel's plume: each fuel-specific Waterfall template on it is
    // swapped for the fuel's template of the same role, at the same place and
    // size. PIP only names templates other mods install; it ships none.
    public static class Plumes
    {
        public const string Lower = "lower", Upper = "upper", Core = "core", Generator = "generator";
        public static readonly Dictionary<string, PlumeSet> ByFuel = new Dictionary<string, PlumeSet>();

        // Words that mark a template as belonging to one fuel. Others (lights,
        // generic glows) are never touched.
        static readonly string[] FuelWords = { "kerolox", "hydrolox", "methalox", "alcolox", "ethalox", "hypergolic", "aerozine", "keroxide", "hydyne", "nitric", "udmh" };

        // What a template does on the engine: the glow inside the bell, the gas
        // generator's exhaust, or the plume itself (vacuum-style or sea-level-style).
        public static string Role(string template)
        {
            string t = (template ?? "").ToLowerInvariant();
            if (t.Contains("core")) return Core;
            if (t.Contains("generator") || t.Contains("exhaust")) return Generator;
            if (t.Contains("upper") || t.Contains("vac")) return Upper;
            return Lower;
        }

        // Templates draw at very different sizes (a vernier template is a fraction of a
        // main plume's), so a swapped template is rescaled: its scale times the new
        // template's size over the old one's. A template's size is the scale mods give
        // it per meter of engine (measured at load from every engine that uses it,
        // FuelLoader), else a typical one for its role, from those measurements.
        public static readonly Dictionary<string, double> SizePerMeter = new Dictionary<string, double>();
        static readonly Dictionary<string, double> TypicalSize = new Dictionary<string, double>
            { { Lower, 0.55 }, { Upper, 0.7 }, { Core, 0.3 }, { Generator, 0.35 } };
        const double TypicalVernier = 2.4;

        public static double Size(string template)
        {
            if (template != null && SizePerMeter.TryGetValue(template, out double s) && s > 0) return s;
            string t = (template ?? "").ToLowerInvariant();
            return t.Contains("vernier") ? TypicalVernier : TypicalSize[Role(t)];
        }

        // What to multiply a swapped template's scale by.
        public static double Rescale(string from, string to) => Size(to) / Size(from);

        // Median of a template's measured scales per meter, from (scale, engine diameter) pairs.
        public static double Median(List<double> perMeter)
        {
            if (perMeter == null || perMeter.Count == 0) return 0;
            var v = perMeter.OrderBy(x => x).ToList();
            return v.Count % 2 == 1 ? v[v.Count / 2] : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2;
        }

        // For one of an engine's templates, when it burns this fuel: the template to
        // use instead, "" to drop it (a glow in the wrong color looks worse than none),
        // or null to keep it.
        public static string Swap(string template, string fuel, Func<string, bool> installed)
        {
            string t = (template ?? "").ToLowerInvariant();
            if (t.Length == 0 || !FuelWords.Any(t.Contains) || t.Contains("spike")) return null;   // aerospikes keep theirs
            if (fuel == null || !ByFuel.TryGetValue(fuel, out PlumeSet set)) return null;
            if (set.Match.Any(w => w.Length > 0 && t.Contains(w.ToLowerInvariant()))) return null;
            string role = Role(t);
            if (set.Roles.TryGetValue(role, out string[] options))
                foreach (string o in options)
                    if (!string.IsNullOrEmpty(o) && (installed == null || installed(o))) return o;
            return role == Core ? "" : null;
        }
    }

    public static class FuelTable
    {
        public static List<FuelDef> All { get; private set; } = Defaults();

        public static List<FuelDef> Defaults() => new List<FuelDef>
        {
            new FuelDef(),
            new FuelDef
            {
                Code = "H", Title = "Hydrolox", Tech = "advFuelSystems",
                Resources = new[] { "LqdHydrogen", "Oxidizer" }, Ratios = new[] { 1.5, 0.1 },
                Isp = new[]
                {
                    new double[] { 300, 380, 340, 425 }, new double[] { 270, 355, 370, 450 },
                    new double[] { 60, 120, 400, 475 }, new double[] { 310, 370, 360, 440 },
                },
                Thrust = 0.9, Twr = 22, Price = 1.3,
            },
            new FuelDef
            {
                Code = "M", Title = "Methalox", Tech = "highPerformanceFuelSystems",
                Resources = new[] { "LqdMethane", "Oxidizer" }, Ratios = new[] { 3, 1.0 },
                Isp = new[]
                {
                    new double[] { 280, 340, 300, 362 }, new double[] { 255, 320, 320, 378 },
                    new double[] { 50, 110, 340, 395 }, new double[] { 290, 332, 315, 370 },
                },
                Thrust = 1.15, Twr = 28, Price = 1.2, FailHi = 0.015, FailLo = 0.0005,
            },
        };

        // Replaces the table (from Fuels.cfg). Codes must be unique and can't contain "-".
        public static void Set(IEnumerable<FuelDef> fuels)
        {
            var list = new List<FuelDef>();
            foreach (FuelDef f in fuels ?? Enumerable.Empty<FuelDef>())
            {
                if (f == null || string.IsNullOrEmpty(f.Code) || f.Code.Contains("-") || list.Any(x => x.Code == f.Code)) continue;
                if (f.Isp == null || f.Isp.Length < 4 || f.Isp.Any(n => n == null || n.Length < 4)) continue;
                if (f.Resources == null || f.Ratios == null || f.Resources.Length == 0 || f.Resources.Length != f.Ratios.Length) continue;
                list.Add(f);
            }
            if (list.Count == 0) return;
            All = list;
            allResources = null;
            Changed?.Invoke();
        }

        public static void Reset() => Set(Defaults());

        public static event Action Changed;

        public static FuelDef Find(string code) => All.FirstOrDefault(f => f.Code == code);
        public static int IndexOf(string code) => Math.Max(0, All.FindIndex(f => f.Code == code));

        // The fuel whose propellants are exactly these resources (electric charge,
        // for pumps, aside), or null.
        public static FuelDef ForResources(IEnumerable<string> resources)
        {
            var set = new HashSet<string>(resources ?? Enumerable.Empty<string>());
            set.Remove("ElectricCharge");
            return All.FirstOrDefault(f => set.SetEquals(f.Resources));
        }

        // Every resource some fuel burns (a tank holding only these is a fuel tank).
        static HashSet<string> allResources;
        public static HashSet<string> AllResources => allResources ?? (allResources = new HashSet<string>(All.SelectMany(f => f.Resources)));

        // A liquid rocket engine burns an oxidizer and a rocket fuel (stock's rule,
        // whatever the mix), or exactly the propellants of a fuel in the table.
        static readonly HashSet<string> StockFuels = new HashSet<string> { "LiquidFuel", "LqdHydrogen", "LqdMethane" };
        public static bool IsLiquidMix(IEnumerable<string> propellants)
        {
            var p = new HashSet<string>(propellants ?? Enumerable.Empty<string>());
            p.Remove("ElectricCharge");
            if (p.Contains("Oxidizer") && p.Overlaps(StockFuels) && p.All(x => x == "Oxidizer" || StockFuels.Contains(x))) return true;
            return p.Count > 1 && ForResources(p) != null;
        }
    }
}
