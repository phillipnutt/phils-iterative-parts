using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PIP
{
    // One version of a development program: its part type and model, its points
    // (fixed for life), and the experience that carries it from Mk. 1 to Mk. 5.
    // At Mk. 5 a family moves on to its next version (DESIGN.md section 18.1).
    public sealed class Program
    {
        public string Id;
        public string Name;            // the family's name, "Kestrel"; the version is added for display
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
        // own model (programs from before looks). An adapter tank program (DESIGN.md
        // 20.2) wears its adapter look in SideLook, as a side-mount one does.
        public string Look = "";
        public string SideLook = "";
        // The size, for part types that have sizes (DESIGN.md section 14): fixed
        // for the program's life, in-line or side-mount ("R..."). Empty for engines,
        // and for programs from before sizes were fixed, which still cover every size.
        public string Variant = "";
        // Gimbal, degrees, for engine types with the pick ("" for others). Fixed
        // for the whole family (DESIGN.md section 18.4).
        public string Gimbal = "";
        // Versions (section 18.1): 1 for Kestrel I. A later version starts from the
        // points of the one before it (StartPoints; null for a first version).
        public int Version = 1;
        public int[] StartPoints;
        public string PredecessorId = "", SuccessorId = "";
        // What has been counted, so nothing counts twice: one key per flight
        // (its launch ID; older saves hold other keys).
        public readonly HashSet<string> CountedLaunches = new HashSet<string>();
        public readonly HashSet<string> CountedRecoveries = new HashSet<string>();

        public int Total => Points.Sum();
        // +1 a flight, +3 a recovery (section 18.2).
        public int Xp => Launches * Progress.LaunchXp + Recoveries * Progress.RecoveryXp;
        public int Mk => Progress.Mk(Xp);

        // Every slider it uses at 100: the family's last version, "Kestrel Max".
        public bool IsMax => Enumerable.Range(0, Points.Length).All(i => !Type.CategoryUsed(Model, i) || Points[i] >= Pricing.MaxPoints);
        public string Title => Name + " " + (IsMax ? "Max" : Roman(Version));

        public static string Roman(int n)
        {
            if (n <= 0) return n.ToString(Inv);
            var vals = new[] { 10, 9, 5, 4, 1 };
            var syms = new[] { "X", "IX", "V", "IV", "I" };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < vals.Length && n > 0; i++) while (n >= vals[i]) { sb.Append(syms[i]); n -= vals[i]; }
            return n > 0 ? sb + "+" : sb.ToString();
        }

        // The next version's points: +10 on every slider it uses, never past the
        // research cap or 100; sliders it doesn't use stay at 0.
        public int[] NextPoints(int cap) =>
            Points.Select((v, i) => !Type.CategoryUsed(Model, i) ? 0 : Math.Max(v, Math.Min(Math.Min(cap, Pricing.MaxPoints), v + 10))).ToArray();

        // Whether a next version would be any better at this cap, and whether more
        // research would make one (every slider at the cap, not all at 100).
        public bool CanImprove(int cap) => !IsMax && NextPoints(cap).Where((v, i) => v > Points[i]).Any();
        public bool WaitingOnResearch(int cap) => !IsMax && !CanImprove(cap);

        // Kestrel II: same design, the next points, starting where this one ends.
        public Program NextVersion(string id, int cap, double ut, double funds)
        {
            var p = Create(id, Name, Type, Model, NextPoints(cap), ut, funds);
            p.Version = Version + 1;
            p.StartPoints = (int[])Points.Clone();
            p.PredecessorId = Id;
            p.Look = Look; p.SideLook = SideLook; p.Variant = Variant; p.Gimbal = Gimbal;
            return p;
        }
        public string ModelCode => Type.Code(Model);
        public bool FixedSize => !string.IsNullOrEmpty(Variant);
        public bool IsSide => PartType.IsSide(Variant);
        public bool IsAdapter => PartType.IsAdapter(Variant);
        // Side-mount and adapter programs are always made from a look, kept in SideLook.
        public bool Mounted => FixedSize && (IsSide || IsAdapter);
        public string Label => Type.Label(Model) + (FixedSize ? ", " + Type.VariantLabel(Model, Variant).ToLowerInvariant()
                                                    : Type.Variants(Model) != null ? ", every size" : "");

        // The part this program shows up as in the parts list, and its side-mount
        // part for types that have one.
        public string PartName => "PIP." + Id;
        public string SidePartName => "PIP." + Id + ".side";
        public string ChassisPart => Type.ChassisPart(Model);
        public string DisplayName(int mk) => Title + " " + Progress.MkName(mk);
        public string DisplayName() => DisplayName(Mk);
        public int GimbalDegrees => PartType.GimbalDegrees(Gimbal);

        public Stats Stats(int mk, FailureLevel level, string variant = null) =>
            Type.Compute(Model, Points, mk, level, variant ?? (FixedSize ? Variant : null), StartPoints, GimbalDegrees);

        // Once per program per flight (DESIGN.md section 18.2): a flight is keyed by
        // its launch ID, however many of the program's parts were aboard.
        public static string FlightKey(uint launchId) => launchId.ToString(Inv);

        public bool RecordLaunch(string flightKey)
        {
            if (string.IsNullOrEmpty(flightKey) || !CountedLaunches.Add(flightKey)) return false;
            Launches++;
            return true;
        }

        public bool RecordRecovery(string flightKey)
        {
            if (string.IsNullOrEmpty(flightKey) || !CountedRecoveries.Add(flightKey)) return false;
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
            if (!string.IsNullOrEmpty(Gimbal)) Add("gimbal", Gimbal);
            if (Version != 1) Add("version", Version.ToString(Inv));
            if (StartPoints != null) Add("startPoints", PointsToText(StartPoints));
            if (!string.IsNullOrEmpty(PredecessorId)) Add("predecessor", PredecessorId);
            if (!string.IsNullOrEmpty(SuccessorId)) Add("successor", SuccessorId);
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
            p.Gimbal = type.HasGimbalPick ? get("gimbal") ?? "" : "";
            p.Version = Math.Max(1, I(get("version")));
            if (!string.IsNullOrEmpty(get("startPoints"))) p.StartPoints = PointsFromText(get("startPoints"), type.Categories.Length);
            p.PredecessorId = get("predecessor") ?? "";
            p.SuccessorId = get("successor") ?? "";
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
