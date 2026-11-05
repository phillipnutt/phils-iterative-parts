using System;

namespace PIP
{
    // Mk. 1 to Mk. 5 (DESIGN.md section 18.2): experience steps of 2, 5, 7 and 11,
    // and a 1/x² curve that front-loads the gains. A first version performs at
    // Mk. 1 as if half its points were spent; a later version starts where the
    // one before it ended. At Mk. 5, all of its points.
    public static class Progress
    {
        public const int MinMk = 1, MaxMk = 5;
        public static readonly int[] StepXp = { 2, 5, 7, 11 };   // to Mk. 2, 3, 4, 5
        public const int LaunchXp = 1, RecoveryXp = 3;
        const int CurveOffset = 2;

        public static string MkName(int mk) => "Mk. " + mk;

        // Total experience needed to reach a Mk.: 0, 2, 7, 14, 25.
        public static int XpForMk(int mk)
        {
            int total = 0;
            for (int i = 0; i < Math.Min(mk, MaxMk) - MinMk; i++) total += StepXp[i];
            return total;
        }

        public static int Mk(int xp)
        {
            int mk = MinMk;
            while (mk < MaxMk && xp >= XpForMk(mk + 1)) mk++;
            return mk;
        }

        static double S(int n)
        {
            double s = 0;
            for (int k = 1; k <= n; k++) s += 1.0 / ((k + CurveOffset) * (k + CurveOffset));
            return s;
        }

        // 0 at Mk. 1, 1 at Mk. 5.
        public static double Curve(int mk) => S(Math.Max(0, Math.Min(MaxMk, mk) - MinMk)) / S(MaxMk - MinMk);

        // Points actually delivered at a Mk., climbing from where the version
        // started (half its points for a first version) to all of them.
        public static double Effective(int points, double start, int mk) => start + (points - start) * Curve(mk);
        public static double Effective(int points, int mk) => Effective(points, points / 2.0, mk);
    }

    public static class Pricing
    {
        public const int MaxPoints = 100;
        public const double StartCost = 1000;
        public const double SuccessorShare = 0.1;

        // Funds to start a program (DESIGN.md section 18.5): about 1,000 with no
        // points spent, rising exponentially with the share of the points spent to
        // the type's ceiling, set by how complex the part is. Times the game's PIP
        // prices option. A later version costs a tenth.
        public static double ProgramCost(double ceiling, double share, PriceLevel level = PriceLevel.Normal) =>
            StartCost * Math.Pow(Math.Max(1, ceiling / StartCost), Math.Max(0, Math.Min(1, share))) * Multiplier(level);

        public static double Multiplier(PriceLevel level) =>
            level == PriceLevel.Quarter ? 0.25 : level == PriceLevel.Half ? 0.5 : level == PriceLevel.Double ? 2 : 1;

        public static int Clamp(int points) => Math.Max(0, Math.Min(MaxPoints, points));

        // Build price per part: x0.75 for a bare design up to x1.5 for a maxed one,
        // not counting the Build cost slider ...
        public static double Ambition(int totalPoints, int maxTotal) =>
            0.75 + 0.75 * Math.Min(maxTotal, Math.Max(0, totalPoints)) / (double)Math.Max(1, maxTotal);

        // ... which works against it: 1.5x at 0 points, 0.5x at 100 (section 18.3).
        public static double CostFactor(double effectivePoints) => 1.5 - effectivePoints / 100.0;
    }

    // Fails-to-light: 10% at Mk. 1 of every version (new versions bring new
    // problems), falling along the Mk curve to a mature value that reliability
    // points move from hi to lo.
    public static class Failure
    {
        public static double Mature(double hi, double lo, int reliabilityPoints) =>
            hi * Math.Pow(lo / hi, Pricing.Clamp(reliabilityPoints) / 100.0);

        public static double Chance(double hi, double lo, int reliabilityPoints, int mk, FailureLevel level)
        {
            if (level == FailureLevel.Off) return 0;
            double mature = Mature(hi, lo, reliabilityPoints);
            double chance = mature + (0.10 - mature) * (1 - Progress.Curve(mk));
            return level == FailureLevel.Light ? chance / 2 : chance;
        }
    }
}
