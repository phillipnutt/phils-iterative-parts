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

        public static PipSettings Current => HighLogic.CurrentGame?.Parameters?.CustomParams<PipSettings>();
        public static FailureLevel Failures => Current?.failures ?? FailureLevel.Normal;
        public static PriceLevel Prices => Current?.prices ?? PriceLevel.Normal;

        public static Inclusion For(PartType t)
        {
            PipSettings s = Current;
            PipPartSettings p = PipPartSettings.Current;
            switch (t.Id)
            {
                case "liquid": return s?.liquidEngines ?? Inclusion.Replace;
                case "solid": return s?.solidMotors ?? Inclusion.Replace;
                case "monoprop": return s?.monopropEngines ?? Inclusion.Replace;
                case "nuclear": return s?.nuclearEngines ?? Inclusion.Replace;
                case "electric": return s?.electricEngines ?? Inclusion.Replace;
                case "jet": return s?.jetEngines ?? Inclusion.Replace;
                case "rcs": return p?.rcs ?? Inclusion.Replace;
                case "battery": return p?.batteries ?? Inclusion.Replace;
                case "wheel": return p?.wheels ?? Inclusion.Replace;
                case "shield": return p?.shields ?? Inclusion.Replace;
                case "solar": return p?.solar ?? Inclusion.Replace;
                case "antenna": return p?.antennas ?? Inclusion.Replace;
                case "probe": return p?.probes ?? Inclusion.Replace;
                case "tank": return p?.tanks ?? Inclusion.Replace;
                case "rtg": return p?.rtgs ?? Inclusion.Replace;
                case "intake": return p?.intakes ?? Inclusion.Replace;
                default: return Inclusion.Off;
            }
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

        [GameParameters.CustomParameterUI("Fuel tanks", toolTip = RowTip + " Plane fuselages, radial and odd-shaped tanks stay.")]
        public Inclusion tanks = Inclusion.Replace;

        [GameParameters.CustomParameterUI("RTGs", toolTip = RowTip)]
        public Inclusion rtgs = Inclusion.Replace;

        [GameParameters.CustomParameterUI("Air intakes", toolTip = RowTip)]
        public Inclusion intakes = Inclusion.Replace;

        public static PipPartSettings Current => HighLogic.CurrentGame?.Parameters?.CustomParams<PipPartSettings>();
    }

    public static class PipGuard
    {
        // Every PIP type off in this game, on both settings pages. The choice is
        // saved with the game, so it can be switched back on in its difficulty settings.
        public static void AllOff()
        {
            PipSettings s = PipSettings.Current;
            if (s != null)
                s.liquidEngines = s.solidMotors = s.monopropEngines = s.nuclearEngines = s.electricEngines = s.jetEngines = Inclusion.Off;
            PipPartSettings p = PipPartSettings.Current;
            if (p != null)
                p.rcs = p.batteries = p.wheels = p.shields = p.solar = p.antennas = p.probes = p.tanks = p.rtgs = p.intakes = Inclusion.Off;
        }

        // Whether a game PIP is seeing for the first time was started with PIP:
        // it already has programs, or it's brand new (no time on the clock, no
        // vessels). A game already under way (a career started before PIP was
        // installed) wasn't, and gets every type off rather than PIP's defaults.
        public static bool StartedWithPip(bool hasPrograms, double ut, int vessels) =>
            hasPrograms || (ut < 3600 && vessels == 0);
    }
}
