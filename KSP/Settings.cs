using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PIP
{
    // Game-start options, in the difficulty settings. One row per part type:
    // Off, Alongside stock, or Replace stock (the stock ones are hidden).
    public class PipSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Phil's Iterative Parts: engines";
        public override string Section => "PIP";
        public override string DisplaySection => "PIP";
        public override int SectionOrder => 1;
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;

        const string RowTip = "Off: not in this game. Alongside: next to the stock and mod parts. Replace: those are hidden.";

        // Change all (DESIGN.md 23): the top row sets every row in both columns,
        // and shows Mixed while they differ.
        [GameParameters.CustomParameterUI("Change all",
            toolTip = "Sets every row in both columns to Off, Alongside or Replace. Mixed: the rows aren't all the same.")]
        public AllInclusion changeAll = AllInclusion.Replace;
        AllInclusion? changeAllShown;
        int changeAllThumb = 2;   // the stop the thumb is on: 0 Off, 1 Alongside, 2 Replace

        [GameParameters.CustomParameterUI("Liquid engines", toolTip = RowTip)]
        public Inclusion liquidEngines = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Solid boosters", toolTip = RowTip + " Sepratrons and escape towers always stay.")]
        public Inclusion solidMotors = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Monopropellant engines", toolTip = RowTip + " RCS thrusters are separate.")]
        public Inclusion monopropEngines = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Nuclear engines", toolTip = RowTip)]
        public Inclusion nuclearEngines = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Electric engines", toolTip = RowTip)]
        public Inclusion electricEngines = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Jet engines", toolTip = RowTip)]
        public Inclusion jetEngines = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Engine failures",
            toolTip = "Engines can fail to light the first time they're lit each flight. Light = half the chance. Jets never fail.")]
        public FailureLevel failures = FailureLevel.Normal;

        [GameParameters.CustomParameterUI("PIP prices",
            toolTip = "Scales what every program costs to start: a quarter, half, normal or double.")]
        public PriceLevel prices = PriceLevel.Normal;

        // KSP asks this of every row, every frame the settings are open, and its
        // sliders show whatever the fields hold, so Change all works from here.
        // It uses the parameters being edited, which in a new game's setup aren't
        // the current game's.
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == nameof(changeAll)) SyncChangeAll(parameters?.CustomParams<PipPartSettings>());
            return true;
        }

        void SyncChangeAll(PipPartSettings parts)
        {
            bool opening = !changeAllShown.HasValue;
            // Moved since last frame: set every row (Mixed sets nothing).
            if (!opening && changeAll != changeAllShown.Value && changeAll != AllInclusion.Mixed)
                PipGuard.SetAll(this, parts, PipGuard.ToInclusion(changeAll));
            changeAll = PipGuard.Common(this, parts);
            changeAllShown = changeAll;
            // Mixed isn't a stop: the thumb stays put, or opens where most rows are.
            if (changeAll != AllInclusion.Mixed) changeAllThumb = (int)PipGuard.ToInclusion(changeAll);
            else if (opening) changeAllThumb = (int)PipGuard.Majority(this, parts);
        }

        // Change all's slider has the same three stops as every other row. KSP
        // places a slider's thumb by finding the value in this list. Mixed isn't in
        // it, so it answers with the stop the thumb is already on; otherwise KSP
        // would snap the thumb to Off and set every row Off.
        public override IList ValidValues(MemberInfo member) =>
            member.Name == nameof(changeAll) ? new ChangeAllStops(this) : base.ValidValues(member);

        sealed class ChangeAllStops : ArrayList
        {
            readonly PipSettings settings;
            public ChangeAllStops(PipSettings settings) : base(new[] { AllInclusion.Off, AllInclusion.Alongside, AllInclusion.Replace }) => this.settings = settings;
            public override int IndexOf(object value) =>
                value is AllInclusion a && a == AllInclusion.Mixed ? settings.changeAllThumb : base.IndexOf(value);
        }

        public static PipSettings Current => HighLogic.CurrentGame?.Parameters?.CustomParams<PipSettings>();
        public static FailureLevel Failures => Current?.failures ?? FailureLevel.Normal;
        public static PriceLevel Prices => Current?.prices ?? PriceLevel.Normal;

        public static Inclusion For(PartType t)
        {
            if (!Requirements.Ok) return Inclusion.Off;   // a required mod is missing: PIP stays off
            SettingRow row = SettingRow.Of(t.Id);
            return row == null ? Inclusion.Off : row.Get(Current, PipPartSettings.Current) ?? Inclusion.Replace;
        }

        public const string RowTipShared = RowTip;

        public static bool On(PartType t) => t != null && For(t) != Inclusion.Off;
        public static bool Replacing(PartType t) => t != null && For(t) == Inclusion.Replace;
        public static bool AnyOn
        {
            get
            {
                foreach (PartType t in PartTypes.All) if (On(t)) return true;
                return false;
            }
        }
    }

    // The second column: part types that aren't engines.
    public class PipPartSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Phil's Iterative Parts: parts";
        public override string Section => "PIP";
        public override string DisplaySection => "PIP";
        public override int SectionOrder => 2;
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;

        const string RowTip = PipSettings.RowTipShared;

        [GameParameters.CustomParameterUI("RCS thrusters", toolTip = RowTip + " Pods with built-in RCS and electric RCS stay.")]
        public Inclusion rcs = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Batteries", toolTip = RowTip)]
        public Inclusion batteries = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Reaction wheels", toolTip = RowTip)]
        public Inclusion wheels = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Heat shields", toolTip = RowTip)]
        public Inclusion shields = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Solar panels", toolTip = RowTip)]
        public Inclusion solar = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Antennas", toolTip = RowTip)]
        public Inclusion antennas = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Probe cores", toolTip = RowTip)]
        public Inclusion probes = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Fuel tanks", toolTip = RowTip + " In-line, radial and adapter tanks. Cluster tanks, nosecones and odd shapes stay.")]
        public Inclusion tanks = Inclusion.Replace;

        [GameParameters.CustomParameterUI("RTGs", toolTip = RowTip)]
        public Inclusion rtgs = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Air intakes", toolTip = RowTip)]
        public Inclusion intakes = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Plane fuselages", toolTip = RowTip + " Mk2 and Mk3 fuel fuselages. Cockpits, cabins and cargo bays stay.")]
        public Inclusion planes = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Decouplers", toolTip = RowTip + " Stack decouplers, separators and radial decouplers. Engine plates stay.")]
        public Inclusion decouplers = Inclusion.Replace;

        public static PipPartSettings Current => HighLogic.CurrentGame?.Parameters?.CustomParams<PipPartSettings>();
    }

    // The Change all row's values. Mixed shows while the rows differ; it isn't one
    // of the slider's stops (PipSettings.ValidValues).
    public enum AllInclusion { Mixed, Off, Alongside, Replace }

    // Each part type's row in the settings: the one list that the game's setting
    // (PipSettings.For), Change all and the game-start guard all read. A new part
    // type needs one line here.
    public sealed class SettingRow
    {
        public readonly string Id;
        readonly Func<PipSettings, PipPartSettings, Inclusion?> get;
        readonly Action<PipSettings, PipPartSettings, Inclusion> set;
        SettingRow(string id, Func<PipSettings, PipPartSettings, Inclusion?> get, Action<PipSettings, PipPartSettings, Inclusion> set)
        { Id = id; this.get = get; this.set = set; }

        // Null when the row's page isn't there.
        public Inclusion? Get(PipSettings s, PipPartSettings p) => get(s, p);
        public void Set(PipSettings s, PipPartSettings p, Inclusion v) => set(s, p, v);

        // Engines are on the first page, other parts on the second.
        static SettingRow E(string id, Func<PipSettings, Inclusion> get, Action<PipSettings, Inclusion> set) =>
            new SettingRow(id, (s, p) => s == null ? (Inclusion?)null : get(s), (s, p, v) => { if (s != null) set(s, v); });
        static SettingRow P(string id, Func<PipPartSettings, Inclusion> get, Action<PipPartSettings, Inclusion> set) =>
            new SettingRow(id, (s, p) => p == null ? (Inclusion?)null : get(p), (s, p, v) => { if (p != null) set(p, v); });

        public static readonly SettingRow[] All =
        {
            E("liquid", s => s.liquidEngines, (s, v) => s.liquidEngines = v),
            E("solid", s => s.solidMotors, (s, v) => s.solidMotors = v),
            E("monoprop", s => s.monopropEngines, (s, v) => s.monopropEngines = v),
            E("nuclear", s => s.nuclearEngines, (s, v) => s.nuclearEngines = v),
            E("electric", s => s.electricEngines, (s, v) => s.electricEngines = v),
            E("jet", s => s.jetEngines, (s, v) => s.jetEngines = v),
            P("rcs", p => p.rcs, (p, v) => p.rcs = v),
            P("battery", p => p.batteries, (p, v) => p.batteries = v),
            P("wheel", p => p.wheels, (p, v) => p.wheels = v),
            P("shield", p => p.shields, (p, v) => p.shields = v),
            P("solar", p => p.solar, (p, v) => p.solar = v),
            P("antenna", p => p.antennas, (p, v) => p.antennas = v),
            P("probe", p => p.probes, (p, v) => p.probes = v),
            P("tank", p => p.tanks, (p, v) => p.tanks = v),
            P("rtg", p => p.rtgs, (p, v) => p.rtgs = v),
            P("intake", p => p.intakes, (p, v) => p.intakes = v),
            P("plane", p => p.planes, (p, v) => p.planes = v),
            P("decoupler", p => p.decouplers, (p, v) => p.decouplers = v),
        };

        public static SettingRow Of(string id) => All.FirstOrDefault(r => r.Id == id);
    }

    public static class PipGuard
    {
        // Every PIP type off in this game, on both settings pages. The choice is
        // saved with the game, so it can be switched back on in its difficulty settings.
        public static void AllOff() => SetAll(PipSettings.Current, PipPartSettings.Current, Inclusion.Off);

        // Every row in both columns.
        public static void SetAll(PipSettings s, PipPartSettings p, Inclusion v)
        {
            foreach (SettingRow r in SettingRow.All) r.Set(s, p, v);
        }

        static IEnumerable<Inclusion> Rows(PipSettings s, PipPartSettings p) =>
            SettingRow.All.Select(r => r.Get(s, p)).Where(v => v.HasValue).Select(v => v.Value);

        // What Change all shows: the rows' one setting, or Mixed.
        public static AllInclusion Common(PipSettings s, PipPartSettings p)
        {
            Inclusion[] rows = Rows(s, p).Distinct().ToArray();
            return rows.Length == 1 ? ToAll(rows[0]) : AllInclusion.Mixed;
        }

        // The setting most rows have.
        public static Inclusion Majority(PipSettings s, PipPartSettings p) =>
            Rows(s, p).GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key).DefaultIfEmpty(Inclusion.Replace).First();

        public static Inclusion ToInclusion(AllInclusion a) =>
            a == AllInclusion.Off ? Inclusion.Off : a == AllInclusion.Alongside ? Inclusion.Alongside : Inclusion.Replace;

        static AllInclusion ToAll(Inclusion v) =>
            v == Inclusion.Off ? AllInclusion.Off : v == Inclusion.Alongside ? AllInclusion.Alongside : AllInclusion.Replace;

        // The part types of PIP 0.3.0 and before, which games didn't record.
        public static readonly string[] FirstTypes =
            { "liquid", "solid", "monoprop", "nuclear", "electric", "jet", "rcs", "battery", "wheel", "shield", "solar", "antenna", "probe", "tank", "rtg", "intake" };

        // A type that's new in this version of PIP starts off in a game already
        // under way. Its setting would otherwise take its default, Replace, and
        // hide that game's stock parts unasked. A brand-new game gets the default.
        // Returns the types switched off.
        public static List<PartType> NewTypesOff(ICollection<string> known)
        {
            var off = new List<PartType>();
            foreach (PartType t in PartTypes.All)
            {
                SettingRow row = SettingRow.Of(t.Id);
                if (known.Contains(t.Id) || row == null) continue;
                row.Set(PipSettings.Current, PipPartSettings.Current, Inclusion.Off);
                off.Add(t);
            }
            return off;
        }

        // Whether a game PIP is seeing for the first time was started with PIP:
        // it already has programs, or it's brand new (no time on the clock, no
        // vessels). A game already under way (a career started before PIP was
        // installed) wasn't, and gets every type off rather than PIP's defaults.
        public static bool StartedWithPip(bool hasPrograms, double ut, int vessels) =>
            hasPrograms || (ut < 3600 && vessels == 0);
    }
}
