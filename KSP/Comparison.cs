using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PIP
{
    // The "compared with" and summary lines in the new-program window. Each
    // model is compared with a familiar engine of the same fuel, size and nozzle
    // when the install has one; its numbers are read from that engine itself.
    public static class Comparison
    {
        static readonly Dictionary<string, string> Refs = new Dictionary<string, string>
        {
            { "K-0625-S", "liquidEngineMini_v2" }, { "K-125-S", "liquidEngine_v2" }, { "K-1875-S", "LiquidEngineLV-TX87" },
            { "K-25-S", "liquidEngineMainsail_v2" }, { "K-375-S", "Size3EngineCluster" },
            { "K-0625-B", "liquidEngineMini_v2" }, { "K-125-B", "liquidEngine2_v2" }, { "K-1875-B", "nflv-engine-rd704-1" },
            { "K-25-B", "engineLargeSkipper_v2" }, { "K-375-B", "nflv-engine-rd701-1" },
            { "K-0625-V", "nflv-engine-rutherford-vac-1" }, { "K-125-V", "liquidEngine3_v2" }, { "K-25-V", "liquidEngine2-2_v2" },
            { "K-375-V", "Size3AdvancedEngine" }, { "K-125-A", "toroidalAerospike" },
            { "H-0625-S", "cryoengine-stromboli-1" }, { "H-125-S", "cryoengine-vesuvius-1" }, { "H-1875-S", "cryoengine-erebus-1" },
            { "H-25-S", "cryoengine-fuji-1" }, { "H-375-S", "cryoengine-etna-1" },
            { "H-125-V", "cryoengine-hecate-1" }, { "H-1875-V", "cryoengine-pavonis-1" }, { "H-25-V", "cryoengine-ulysses-1" },
            { "H-375-V", "cryoengine-tharsis-1" },
            { "M-0625-S", "cryoengine-compsognathus-1" }, { "M-1875-S", "cryoengine-iguanodon-1" }, { "M-25-S", "cryoengine-allosaur-1" },
            { "M-375-S", "cryoengine-tyrannosaur-1" },
            { "M-0625-V", "cryoengine-hawk-1" }, { "M-125-V", "cryoengine-buzzard-1" }, { "M-1875-V", "cryoengine-harrier-1" },
            { "M-375-V", "cryoengine-vulture-1" },
        };

        struct Ref
        {
            public string Name;
            public double ThrustVac, IspSL, IspVac, Mass, Cost;
        }

        static bool Find(PartType t, string[] m, out Ref r)
        {
            r = default(Ref);
            if (t != PartTypes.Liquid || !Refs.TryGetValue(t.Code(m), out string partName)) return false;
            // KSP stores part names with '_' turned into '.'.
            AvailablePart ap = PartLoader.getPartInfoByName(partName.Replace('_', '.'));
            ModuleEngines e = ap?.partPrefab?.FindModuleImplementing<ModuleEngines>();
            if (e == null) return false;
            r = new Ref
            {
                Name = ShortName(ap.title),
                ThrustVac = e.maxThrust,
                IspVac = e.atmosphereCurve.Evaluate(0f),
                IspSL = e.atmosphereCurve.Evaluate(1f),
                Mass = ap.partPrefab.mass,
                Cost = ap.cost,
            };
            return true;
        }

        // 'LV-T30 "Reliant" Liquid Fuel Engine' -> Reliant
        public static string ShortName(string title)
        {
            Match m = Regex.Match(title ?? "", "[\"'“”‘’]([^\"'“”‘’]+)[\"'“”‘’]");
            return m.Success ? m.Groups[1].Value : title;
        }

        static string Signed(double v, string format) => (v >= 0 ? "+" : "−") + Math.Abs(v).ToString(format);

        // Liquid engines only, for now; other types return null.
        public static string Line(PartType t, string[] m, Stats s, int mk)
        {
            if (t != PartTypes.Liquid) return null;
            if (!Find(t, m, out Ref r)) return "There's no comparable engine in your install for this model.";
            double thrust = r.ThrustVac > 0 ? (s[K.ThrustVac] / r.ThrustVac - 1) * 100 : 0;
            double price = r.Cost > 0 ? (s.BuildPrice / r.Cost - 1) * 100 : 0;
            return $"At {Progress.MkName(mk)} compared with the {r.Name} ({r.ThrustVac:N0} kN, Isp {r.IspSL:N0}/{r.IspVac:N0} s, {r.Mass:0.##} t): " +
                   $"thrust {Signed(thrust, "0")}%, Isp {Signed(s[K.IspSL] - r.IspSL, "0")} s at sea level and {Signed(s[K.IspVac] - r.IspVac, "0")} s in vacuum, " +
                   $"weight {Signed(s.Mass - r.Mass, "0.00")} t, build price {Signed(price, "0")}%.";
        }

        // The same comparison in one short line, for the window's bottom bar.
        public static string Short(PartType t, string[] m, Stats s, int mk)
        {
            if (t != PartTypes.Liquid || !Find(t, m, out Ref r)) return null;
            double thrust = r.ThrustVac > 0 ? (s[K.ThrustVac] / r.ThrustVac - 1) * 100 : 0;
            return $"{Progress.MkName(mk)} against the {r.Name}: thrust {Signed(thrust, "0")}%, Isp {Signed(s[K.IspSL] - r.IspSL, "0")} s at sea level " +
                   $"and {Signed(s[K.IspVac] - r.IspVac, "0")} s in vacuum, weight {Signed(s.Mass - r.Mass, "0.00")} t.";
        }

        // "Kestrel: Built for the pad. Strong on thrust. Neglects weight. Under 3% failures from Mk. 4."
        public static string Summary(string name, PartType t, string[] m, int[] points, FailureLevel level)
        {
            int n = t.Categories.Length;
            points = Enumerable.Range(0, n).Select(i => points != null && i < points.Length ? points[i] : 0).ToArray();
            string[] strong = Enumerable.Range(0, n).Where(i => points[i] >= 50 && t.CategoryUsed(m, i) && i != t.CostIndex)
                .Select(i => t.Categories[i].Label.ToLowerInvariant()).ToArray();
            // The first three categories (thrust, efficiency, weight for engines) are the ones that hurt at 0.
            // A liquid vacuum nozzle doesn't need thrust.
            bool vacuum = t == PartTypes.Liquid && m.Length > 2 && m[2] == "V";
            string[] weak = Enumerable.Range(0, Math.Min(3, n)).Where(i => points[i] == 0 && !(vacuum && i == 0) && t.CategoryUsed(m, i) && i != t.CostIndex)
                .Select(i => t.Categories[i].Label.ToLowerInvariant()).ToArray();

            string character = t.Character(m);
            string text = $"{(string.IsNullOrWhiteSpace(name) ? "This " + t.Noun : name.Trim())}: {(character.Length > 0 ? character + " " : "")}";
            text += strong.Length > 0 ? $"Strong on {string.Join(", ", strong)}. " : "No standout strengths. ";
            if (weak.Length > 0) text += $"Neglects {string.Join(", ", weak)}. ";
            int rel = t.ReliabilityCategory;
            if (rel < 0) return text.TrimEnd();
            if (level == FailureLevel.Off) return text + "Failures are off in this game.";
            for (int mk = Progress.MinMk; mk <= Progress.MaxMk; mk++)
                if (t.FailureAt(m, points[rel], mk, level) < 0.03)
                    return text + $"Under 3% failures from {Progress.MkName(mk)}.";
            return text + "Never gets under 3% failures.";
        }
    }
}
