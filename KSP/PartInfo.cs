using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using KSP.UI.Screens.Editor;

namespace PIP
{
    // Program parts share their chassis's prefab, so KSP's part tooltip and
    // "more info" panel would show the borrowed model's stock numbers. These fix
    // both to show the program's current Mk.
    public static class PartInfo
    {
        // The engine and gimbal entries in the "more info" panel, rebuilt per program.
        public static List<AvailablePart.ModuleInfo> ModuleInfos(AvailablePart chassis, Program p, Stats s, bool side = false)
        {
            Option[] sizes = p.Type.VariantsFor(p.Model, side);
            string listed = p.FixedSize ? p.Variant : side ? p.Type.DefaultSideVariant(p.Model) : p.Type.DefaultVariant(p.Model);
            Option shown = p.FixedSize ? null : sizes?.FirstOrDefault(v => v.Code == listed);
            var list = new List<AvailablePart.ModuleInfo>();
            if (chassis?.moduleInfos == null) return list;
            bool engineDone = false;
            foreach (AvailablePart.ModuleInfo mi in chassis.moduleInfos)
            {
                var copy = new AvailablePart.ModuleInfo
                {
                    moduleName = mi.moduleName,
                    moduleDisplayName = mi.moduleDisplayName,
                    primaryInfo = mi.primaryInfo,
                    info = mi.info,
                    onDrawWidget = mi.onDrawWidget,
                };
                string info = mi.info ?? "";
                if (info.Contains("Thrust") && info.Contains("Isp"))
                {
                    // Two-mode jets list each mode; the program's rows cover both.
                    if (engineDone) continue;
                    engineDone = true;
                    copy.info = RowsText(p.Type, p.Model, s) +
                        (p.Type == PartTypes.Jet || p.Type == PartTypes.Electric ? "" : Propellants(chassis, s));
                    if (!string.IsNullOrEmpty(mi.primaryInfo)) copy.primaryInfo = $"<b>Max. Thrust:</b> {s[K.ThrustVac]:0.##} kN";
                }
                else if (info.Contains("Dry Mass"))
                {
                    copy.info = DryMassRx.Replace(info, m => m.Groups[1].Value + s.Mass.ToString("0.000"), 1);
                }
                // A decoupler's force is the program's, not the chassis's (seen 2026-11-06:
                // a 75 kN Snap showed the TD-12's 100).
                else if (p.Type == PartTypes.Decoupler && EjectRx.IsMatch(info))
                {
                    copy.info = EjectRx.Replace(info, m => m.Groups[1].Value + s[Decouplers.Eject].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), 1);
                }
                else if ((mi.moduleName ?? "").IndexOf("Gimbal", StringComparison.OrdinalIgnoreCase) >= 0 && p.Type != PartTypes.Jet)
                {
                    double g = s[K.Gimbal];
                    copy.info = g >= 0.05 ? $"<b>Gimbal range:</b> ±{g:0.0}°" : "Fixed nozzle: no gimbal.";
                }
                else if (info.StartsWith(ModulePipEngine.InfoPrefix, StringComparison.Ordinal))
                {
                    int mk = p.Mk;
                    copy.info =
                        $"<b>Program:</b> {p.Title}, {Progress.MkName(mk)}\n" +
                        $"{p.Label}\n" +
                        (mk < Progress.MaxMk
                            ? $"<b>Experience:</b> {p.Xp} of {Progress.XpForMk(mk + 1)} for {Progress.MkName(mk + 1)}\n"
                            : "Fully developed.\n") +
                        "Parts keep the Mk they were built at." +
                        (p.Type.IsEngine ? "" : "\n\n" + RowsText(p.Type, p.Model, s) +
                            (shown != null && sizes.Length > 1 ? $"(at {shown.Label}; pick the {p.Type.VariantName.ToLowerInvariant()} on the part)" : ""));
                }
                list.Add(copy);
            }
            return list;
        }

        // Every row the program window shows, as "Label: value" lines.
        public static string RowsText(PartType t, string[] model, Stats s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (Row r in t.Rows(model))
            {
                if (r.Category >= 0 && !t.CategoryUsed(model, r.Category)) continue;
                sb.Append($"<b>{r.Label.Trim()}:</b> {r.Format(s[r.Key])}\n");
            }
            return sb.ToString();
        }

        // Propellant use at full thrust, from the program's thrust and Isp.
        static string Propellants(AvailablePart chassis, Stats s)
        {
            ModuleEngines engine = chassis?.partPrefab?.FindModuleImplementing<ModuleEngines>();
            if (engine?.propellants == null || engine.propellants.Count == 0 || PartResourceLibrary.Instance == null) return "";
            double massFlow = s[K.ThrustVac] / (Math.Max(1, s[K.IspVac]) * Engines.G0);   // t/s
            double perUnit = engine.propellants.Sum(p => p.ratio * (PartResourceLibrary.Instance.GetDefinition(p.name)?.density ?? 0));
            if (perUnit <= 0) return "";
            string text = "\n<b><color=#99ff00ff>Propellants:</color></b>\n";
            foreach (Propellant p in engine.propellants)
            {
                PartResourceDefinition def = PartResourceLibrary.Instance.GetDefinition(p.name);
                text += $"- <b>{def?.displayName ?? p.name}:</b> {massFlow * p.ratio / perUnit:0.###}/sec. Max.\n";
            }
            return text;
        }

        static readonly Regex DryMassRx = new Regex(@"(Dry Mass:\s*(?:<[^>]*>\s*)*)([\d.,]+)");
        static readonly Regex EjectRx = new Regex(@"(Ejection Force:\s*(?:<[^>]*>\s*)*)([\d.,]+)");
        static readonly Regex MassRx = new Regex(@"(Mass:\s*(?:<[^>]*>\s*)*)([\d.,]+)");
        static readonly Regex AslRx = new Regex(@"(Thrust \(ASL\):\s*(?:<[^>]*>\s*)*)([\d.,]+)");
        static readonly Regex VacRx = new Regex(@"(Thrust \(Vac\.?\):\s*(?:<[^>]*>\s*)*)([\d.,]+)");

        // Rewrites the mass and thrust in the tooltip header.
        public static string Stats(string text, Stats s)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // Tanks and intakes work their mass out on the part (it depends on the size picked).
            if (s.Mass > 0) text = MassRx.Replace(text, m => m.Groups[1].Value + s.Mass.ToString("0.###"), 1);
            if (s.Has(K.ThrustSL)) text = AslRx.Replace(text, m => m.Groups[1].Value + s[K.ThrustSL].ToString("0.###"), 1);
            if (s.Has(K.ThrustVac)) text = VacRx.Replace(text, m => m.Groups[1].Value + s[K.ThrustVac].ToString("0.###"), 1);
            return text;
        }

    }

    // After the tooltip is built, rewrite its stats text (mass, thrust) for
    // program parts. Done on the finished text because other mods replace the
    // stats delegate the tooltip calls.
    [HarmonyPatch]
    static class TooltipSetupPatch
    {
        static readonly FieldInfo InfoBasic = AccessTools.Field(typeof(PartListTooltip), "textInfoBasic");

        static MethodBase TargetMethod() =>
            AccessTools.GetDeclaredMethods(typeof(PartListTooltip)).First(m =>
                m.Name == "Setup" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(AvailablePart));

        static void Postfix(PartListTooltip __instance, AvailablePart availablePart)
        {
            try
            {
                if (!PartFactory.IsProgramPart(availablePart) || InfoBasic == null) return;
                Program program = PipScenario.Instance?.Find(PartFactory.ProgramIdOf(availablePart.name));
                object tmp = InfoBasic.GetValue(__instance);
                PropertyInfo text = tmp?.GetType().GetProperty("text");
                if (program == null || text == null) return;
                string before = text.GetValue(tmp, null) as string;
                Stats s = program.Stats(program.Mk, PipSettings.Failures, PartFactory.ListVariant(program, availablePart.name));
                text.SetValue(tmp, PartInfo.Stats(before, s), null);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[PIP] Tooltip stats failed: " + e.Message);
            }
        }
    }
}
