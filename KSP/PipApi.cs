using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace PIP
{
    // PIP's calls for other mods (DESIGN.md 26.3), on a craft's nodes rather than a
    // loaded ship, so they work at the Space Center. Phil's Reusability Suite calls
    // them by reflection: ToCurrent before it prices a build that never opens the
    // VAB, and on parts-bin and hangar parts for its "Update PIP parts" (26.4).
    // A node can be a .craft file's root or a save's VESSEL (each PART in it), or
    // one PART. Both part formats work: a craft's "part = PIP.x_123" and a save's
    // "name = PIP.x". Parts that aren't PIP's are left alone.
    public static class PipApi
    {
        // Makes every PIP part what a new build of it is now, exactly as the VAB
        // builds it: its family's newest version (an old version's part is renamed),
        // at the program's current Mk, with that version's points, flight flags
        // cleared, and what it holds (battery charge, ablator) resized to the new
        // capacity at the same share full. Returns how many parts changed version or Mk.
        public static int ToCurrent(ConfigNode node) => ToCurrent(node, out _);

        // True for a PIP part that isn't what a new build of it would be now: an older
        // version, or another Mk. The parts ToCurrent would change.
        public static bool IsBehind(ConfigNode part)
        {
            Built b = Read(part);
            if (b == null || b.Program == null) return false;
            Program now = Latest(b.Program);
            return now != b.Program || b.Mk != now.Mk;
        }

        // The Mk a PIP part was built at (1 to 5), or 0 for a part that isn't PIP's.
        public static int Mk(ConfigNode part) => Read(part)?.Mk ?? 0;

        // A PIP part's title with its own Mk ("Augur I Mk. 1"), or null for a part
        // that isn't PIP's (use the part's own title).
        public static string Title(ConfigNode part)
        {
            Built b = Read(part);
            return b == null ? null : PartFactory.TitleOf(b.Program, b.PartName, b.ProgramName, b.Mk);
        }

        #region The new-build rule

        // What a new part of a program is built to (DESIGN.md 26.3), written into a
        // ModulePipPart or ModulePipEngine node. The part modules load it the same way
        // in the VAB, so the VAB and a Space Center build agree.
        internal static void WriteNewBuild(ConfigNode module, Program p, bool engine)
        {
            module.SetValue("programId", p.Id, true);
            module.SetValue("programName", p.Title, true);
            module.SetValue("model", p.ModelCode, true);
            module.SetValue("mk", p.Mk.ToString(CultureInfo.InvariantCulture), true);
            module.SetValue("points", Program.PointsToText(p.Points), true);
            module.SetValue("startPoints", p.StartPoints == null ? "" : Program.PointsToText(p.StartPoints), true);
            if (engine)
            {
                module.SetValue("gimbalDeg", p.GimbalDegrees.ToString(CultureInfo.InvariantCulture), true);
                module.SetValue("fired", "False", true);
                module.SetValue("ignitionChecked", "False", true);
                module.SetValue("failed", "False", true);
            }
            else module.SetValue("flew", "False", true);
        }

        internal static int ToCurrent(ConfigNode node, out int versions)
        {
            versions = 0;
            int changed = 0;
            if (node == null || PipScenario.Instance == null) return 0;
            foreach (ConfigNode part in PartsIn(node))
            {
                Built b = Read(part);
                if (b == null || b.Program == null) continue;
                Program now = Latest(b.Program);
                bool newVersion = now != b.Program, newMk = b.Mk != now.Mk;
                if (newVersion && !Rename(part, b, now)) continue;
                foreach (ConfigNode m in part.GetNodes("MODULE"))
                {
                    bool engine = m.GetValue("name") == nameof(ModulePipEngine);
                    if (!engine && m.GetValue("name") != nameof(ModulePipPart)) continue;
                    WriteNewBuild(m, now, engine);
                    if (!engine) Refill(part, m, now);
                }
                if (newVersion) versions++;
                if (newVersion || newMk) changed++;
            }
            if (changed > 0) Debug.Log($"[PIP] Brought {changed} part{(changed == 1 ? "" : "s")} to the current Mk ({versions} to a newer version)");
            return changed;
        }

        // An older version's part becomes the newest version's part of the same kind.
        static bool Rename(ConfigNode part, Built b, Program now)
        {
            PartFactory.Ensure(now);
            string newName = PartFactory.IsSidePart(b.PartName) ? now.SidePartName : now.PartName;
            if (PartLoader.getPartInfoByName(newName) == null) return false;
            part.SetValue(b.NameKey, newName + b.NameRest);
            b.PartName = newName;
            return true;
        }

        // A battery's charge, a probe core's battery, a heat shield's ablator: the new
        // capacity, as full as the part was (a new build's design is full).
        static void Refill(ConfigNode part, ConfigNode module, Program now)
        {
            string variant = module.GetValue("variant");
            Stats s = now.Stats(now.Mk, PipSettings.Failures, string.IsNullOrEmpty(variant) ? null : variant);
            if (!ModulePipPart.Held(now.Type.Id, s, out string held, out double amount)) return;
            foreach (ConfigNode r in part.GetNodes("RESOURCE"))
            {
                if (r.GetValue("name") != held) continue;
                double max = Num(r.GetValue("maxAmount")), had = Num(r.GetValue("amount"));
                double full = max > 0 ? Math.Max(0, Math.Min(1, had / max)) : 1;
                r.SetValue("maxAmount", amount.ToString("R", CultureInfo.InvariantCulture), true);
                r.SetValue("amount", (amount * full).ToString("R", CultureInfo.InvariantCulture), true);
            }
        }

        #endregion

        #region Reading a part node

        sealed class Built
        {
            public string NameKey, PartName, NameRest, ProgramName;
            public Program Program;
            public int Mk;
        }

        static IEnumerable<ConfigNode> PartsIn(ConfigNode node)
        {
            // A node with no PART in it is taken as a part itself (Read skips it if it isn't one).
            if (node.name == "PART" || !node.HasNode("PART")) { yield return node; yield break; }
            foreach (ConfigNode part in node.GetNodes("PART")) yield return part;
        }

        // A PIP part's name and what it was built to, from its PIP module; null if it isn't one.
        static Built Read(ConfigNode part)
        {
            if (part == null) return null;
            // A craft's "part = PIP.x_4293969904" carries the part's ID after the name.
            string key = part.HasValue("part") ? "part" : "name";
            string value = part.GetValue(key);
            if (string.IsNullOrEmpty(value)) return null;
            int cut = key == "part" ? value.IndexOf('_') : -1;
            var b = new Built { NameKey = key, PartName = cut < 0 ? value : value.Substring(0, cut), NameRest = cut < 0 ? "" : value.Substring(cut) };
            if (!PartFactory.IsProgramPart(b.PartName)) return null;
            ConfigNode module = null;
            foreach (ConfigNode m in part.GetNodes("MODULE"))
                if (m.GetValue("name") == nameof(ModulePipPart) || m.GetValue("name") == nameof(ModulePipEngine)) { module = m; break; }
            if (module == null) return null;
            PipScenario s = PipScenario.Instance;
            b.Program = s?.Find(module.GetValue("programId")) ?? s?.Find(PartFactory.ProgramIdOf(b.PartName));
            b.ProgramName = b.Program?.Title ?? module.GetValue("programName");
            b.Mk = int.TryParse(module.GetValue("mk"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mk) ? mk : 0;
            return b;
        }

        static Program Latest(Program p) => PipScenario.Instance?.Latest(p) ?? p;

        static double Num(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;

        #endregion
    }

    // A part's own window shows its own Mk (DESIGN.md 26.2): KSP titles it with the
    // parts list's title, which every part built from it shares, so it has no Mk.
    [HarmonyPatch(typeof(UIPartActionWindow), nameof(UIPartActionWindow.Setup))]
    static class PartWindowTitlePatch
    {
        static readonly System.Reflection.FieldInfo titleText = AccessTools.Field(typeof(UIPartActionWindow), "titleText");

        static void Postfix(UIPartActionWindow __instance, Part part)
        {
            try
            {
                string title = PartFactory.OwnTitle(part);
                if (title == null) return;
                object text = titleText?.GetValue(__instance);
                text?.GetType().GetProperty("text")?.SetValue(text, title, null);
            }
            catch (Exception e) { Debug.LogError("[PIP] Titling a part window failed: " + e); }
        }
    }
}
