using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PIP
{
    // One development program: its part type and model, its points (fixed for
    // life), and the experience that carries it from Mk. 0 to Mk. 10.
    public sealed class Program
    {
        public string Id;
        public string Name;
        public PartType Type;
        public string[] Model;
        public int[] Points;
        public double CreatedUT;
        public double FundsPaid;
        public int Launches;
        public int Recoveries;
        public bool Retired;
        // Looks (DESIGN.md section 16), fixed when the program starts: the
        // installed part whose model it wears, inline and side-mount. Empty: PIP's
        // own model (programs from before looks).
        public string Look = "";
        public string SideLook = "";
        // The size, for part types that have sizes (DESIGN.md section 14): fixed
        // for the program's life, in-line or side-mount ("R..."). Empty for engines,
        // and for programs from before sizes were fixed, which still cover every size.
        public string Variant = "";
        // What has been counted, so nothing counts twice: "launch:part" keys, one
        // per part per flight (older saves hold bare launch and vessel IDs).
        public readonly HashSet<string> CountedLaunches = new HashSet<string>();
        public readonly HashSet<string> CountedRecoveries = new HashSet<string>();

        public int Total => Points.Sum();
        public int Xp => Launches + Recoveries;
        public int Mk => Progress.Mk(Xp);
        public string ModelCode => Type.Code(Model);
        public bool FixedSize => !string.IsNullOrEmpty(Variant);
        public bool IsSide => PartType.IsSide(Variant);
        public string Label => Type.Label(Model) + (FixedSize ? ", " + Type.VariantLabel(Model, Variant).ToLowerInvariant()
                                                    : Type.Variants(Model) != null ? ", every size" : "");

        // The part this program shows up as in the parts list, and its side-mount
        // part for types that have one.
        public string PartName => "PIP." + Id;
        public string SidePartName => "PIP." + Id + ".side";
        public string ChassisPart => Type.ChassisPart(Model);
        public string DisplayName(int mk) => Name + " " + Progress.MkName(mk);
        public string DisplayName() => DisplayName(Mk);

        public Stats Stats(int mk, FailureLevel level, string variant = null) =>
            Type.Compute(Model, Points, mk, level, variant ?? (FixedSize ? Variant : null));

        // Each part counts (DESIGN.md section 5, Phil 2026-11-05): +1 for every part
        // of the program that flies, +1 for every one recovered, once per flight each.
        public static string PartKey(uint launchId, uint flightId) => launchId.ToString(Inv) + ":" + flightId.ToString(Inv);

        public bool RecordLaunch(string partKey)
        {
            if (string.IsNullOrEmpty(partKey) || !CountedLaunches.Add(partKey)) return false;
            Launches++;
            return true;
        }

        public bool RecordRecovery(string partKey)
        {
            if (string.IsNullOrEmpty(partKey) || !CountedRecoveries.Add(partKey)) return false;
            Recoveries++;
            return true;
        }

        public static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 10);

        public static Program Create(string id, string name, PartType type, string[] model, int[] points, double ut, double funds)
        {
            var p = new Program
            {
                Id = id, Name = name, Type = type, Model = (string[])model.Clone(), CreatedUT = ut, FundsPaid = funds,
                Points = new int[type.Categories.Length],
            };
            for (int i = 0; i < p.Points.Length && points != null && i < points.Length; i++) p.Points[i] = Pricing.Clamp(points[i]);
            return p;
        }

        // Points as "25,25,25,25,10" (category order), for saves and parts.
        public static string PointsToText(int[] points) =>
            string.Join(",", (points ?? new int[0]).Select(v => v.ToString(CultureInfo.InvariantCulture)));

        public static int[] PointsFromText(string text, int count)
        {
            var points = new int[count];
            string[] parts = (text ?? "").Split(',');
            for (int i = 0; i < points.Length && i < parts.Length; i++)
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) points[i] = Pricing.Clamp(v);
            return points;
        }

        #region Saving (plain key/values so it can be tested outside KSP)

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public List<KeyValuePair<string, string>> ToValues()
        {
            var v = new List<KeyValuePair<string, string>>();
            void Add(string k, string val) => v.Add(new KeyValuePair<string, string>(k, val));
            Add("id", Id);
            Add("name", Name);
            Add("type", Type.Id);
            Add("model", ModelCode);
            Add("points", PointsToText(Points));
            Add("created", CreatedUT.ToString("R", Inv));
            Add("fundsPaid", FundsPaid.ToString("R", Inv));
            Add("launches", Launches.ToString(Inv));
            Add("recoveries", Recoveries.ToString(Inv));
            Add("retired", Retired ? "True" : "False");
            if (!string.IsNullOrEmpty(Look)) Add("look", Look);
            if (!string.IsNullOrEmpty(SideLook)) Add("sideLook", SideLook);
            if (FixedSize) Add("size", Variant);
            if (CountedLaunches.Count > 0) Add("launchIds", string.Join(",", CountedLaunches));
            if (CountedRecoveries.Count > 0) Add("recoveryIds", string.Join(",", CountedRecoveries));
            return v;
        }

        // Saves from before part types had no "type": those are liquid engines.
        public static Program FromValues(Func<string, string> get)
        {
            string id = get("id");
            PartType type = PartTypes.Get(get("type"));
            string[] model = type?.ParseModel(get("model"));
            if (string.IsNullOrEmpty(id) || model == null) return null;
            var p = Create(id, get("name") ?? "Unnamed", type, model, PointsFromText(get("points"), type.Categories.Length),
                D(get("created")), D(get("fundsPaid")));
            p.Launches = I(get("launches"));
            p.Recoveries = I(get("recoveries"));
            p.Retired = string.Equals(get("retired"), "True", StringComparison.OrdinalIgnoreCase);
            p.Look = get("look") ?? "";
            p.SideLook = get("sideLook") ?? "";
            string size = get("size");
            if (!string.IsNullOrEmpty(size) && type.VariantOption(model, size) != null) p.Variant = size;
            foreach (string x in Ids(get("launchIds"))) p.CountedLaunches.Add(x);
            foreach (string x in Ids(get("recoveryIds"))) p.CountedRecoveries.Add(x);
            return p;
        }

        static IEnumerable<string> Ids(string text)
        {
            foreach (string s in (text ?? "").Split(','))
                if (s.Trim().Length > 0) yield return s.Trim();
        }

        static double D(string s) => double.TryParse(s, NumberStyles.Float, Inv, out double v) ? v : 0;
        static int I(string s) => int.TryParse(s, NumberStyles.Integer, Inv, out int v) ? v : 0;

        #endregion
    }
}
