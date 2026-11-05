using System;

namespace PIP
{
    // Mk. 0 to Mk. 10: Fibonacci experience steps, and a 1/x² curve that
    // front-loads the gains. At Mk. 0 a program performs as if half its points
    // were spent; at Mk. 10, all of them.
    public static class Progress
    {
        public const int MaxMk = 10;
        public static readonly int[] StepXp = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
        const int CurveOffset = 2;

        public static string MkName(int mk) => "Mk. " + mk;

        // Total experience needed to reach a Mk.
        public static int XpForMk(int mk)
        {
            int total = 0;
            for (int i = 0; i < Math.Min(mk, MaxMk); i++) total += StepXp[i];
            return total;
        }

        public static int Mk(int xp)
        {
            int mk = 0;
            while (mk < MaxMk && xp >= XpForMk(mk + 1)) mk++;
            return mk;
        }

        static double S(int n)
        {
            double s = 0;
            for (int k = 1; k <= n; k++) s += 1.0 / ((k + CurveOffset) * (k + CurveOffset));
            return s;
        }

        // 0 at Mk. 0, 1 at Mk. 10.
        public static double Curve(int mk) => S(Math.Max(0, Math.Min(MaxMk, mk))) / S(MaxMk);

        // Share of the design points delivered at a Mk: 0.5 at Mk. 0, 1 at Mk. 10.
        public static double Share(int mk) => 0.5 + 0.5 * Curve(mk);

        // Points actually delivered at a Mk.
        public static double Effective(int points, int mk) => points * Share(mk);
    }

    public static class Pricing
    {
        public const int MaxPoints = 100;

        // Funds to start a program: 1,000, and every point adds 1% (every 70
        // points doubles it). Times the part type's factor and the game's PIP
        // prices option. Depends on points only, never on size or model.
        public static double ProgramCost(int totalPoints, double typeFactor = 1, PriceLevel level = PriceLevel.Normal) =>
            1000 * Math.Pow(1.01, Math.Max(0, totalPoints)) * typeFactor * Multiplier(level);

        public static double Multiplier(PriceLevel level) =>
            level == PriceLevel.Quarter ? 0.25 : level == PriceLevel.Half ? 0.5 : level == PriceLevel.Double ? 2 : 1;

        public static int Clamp(int points) => Math.Max(0, Math.Min(MaxPoints, points));

        // Build price per part: x0.75 for a bare design up to x1.5 for a maxed
        // one, rising about 5% from Mk. 0 to Mk. 10.
        public static double Ambition(int totalPoints, int maxTotal) =>
            0.75 + 0.75 * Math.Min(maxTotal, Math.Max(0, totalPoints)) / (double)Math.Max(1, maxTotal);

        public static double MkPrice(int mk) => 1 + 0.05 * Progress.Curve(mk);
    }

    // Fails-to-light: 10% at Mk. 0 for every engine program, falling along the
    // Mk curve to a mature value that reliability points move from hi to lo.
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
