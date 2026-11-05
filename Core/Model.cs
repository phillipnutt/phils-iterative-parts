using System;

namespace PIP
{
    public enum FailureLevel { Off, Light, Normal }

    // Per part type, at game start: not in the game, beside the stock parts, or
    // replacing them (the stock ones are hidden).
    public enum Inclusion { Off, Alongside, Replace }

    // The game-start "PIP prices" option.
    public enum PriceLevel { Quarter, Half, Normal, Double }

    public static class Sizes
    {
        public static readonly double[] Diameters = { 0.625, 1.25, 1.875, 2.5, 3.75, 5 };
        public static readonly string[] Codes = { "0625", "125", "1875", "25", "375", "5" };
        public static readonly string[] Labels = { "0.625 m", "1.25 m", "1.875 m", "2.5 m", "3.75 m", "5 m" };
        public static int Count => Diameters.Length;

        public static int IndexOf(string code) => Array.IndexOf(Codes, code);
        public static string Label(string code) { int i = IndexOf(code); return i < 0 ? code : Labels[i]; }

        // Shared size tooling: one unlock opens a size for every rocket family.
        public static string Tooling(string code)
        {
            switch (code)
            {
                case "1875": return "heavyRocketry";
                case "25": return "heavierRocketry";
                case "375": return "veryHeavyRocketry";
                case "5": return "giganticRocketry";
                default: return null;
            }
        }
    }

    // Number formats for the program window and part descriptions.
    public static class Fmt
    {
        public static string Kn(double v) => v < 10 ? $"{v:0.00} kN" : v < 100 ? $"{v:0.0} kN" : $"{v:N0} kN";
        public static string Sec(double v) => $"{v:N0} s";
        public static string Tons(double v) => v < 1 ? $"{v:0.000} t" : $"{v:0.00} t";
        public static string Pct(double v) => v <= 0 ? "never" : v < 0.01 ? $"{v * 100:0.00}%" : $"{v * 100:0.0}%";
        public static string Deg(double v) => v < 0.05 ? "fixed" : $"±{v:0.0}°";
        public static string Funds(double v) => $"{v:N0} f";
        public static string Ec(double v) => v < 10 ? $"{v:0.00} EC/s" : $"{v:N0} EC/s";
        public static string Rad(double v) => $"{v:0.000} rad/h";
        public static string Mach(double v) => $"Mach {v:0.0}";
        public static string Num(double v) => v < 10 ? $"{v:0.##}" : $"{v:N0}";
    }
}
