using System;
using System.Collections.Generic;
using System.Linq;

namespace PIP
{
    // Which R&D entry sells each unlock (DESIGN.md 27.4). With KSP's entry purchase
    // on, an unlock is open once its node is researched and its entry is bought.
    // Each PIP entry (Upgrades.cfg, Fuels.cfg) says what it opens at its node, in
    // its pipUnlocks value: "type:what" items, comma-separated. type is a part
    // type's Id, or * for every type. what is one of:
    //   cap            the type's first programs (at its first node) or a caps step
    //   size           a size: in-line, side-mount, adapter, or an engine's Size
    //   Choice=Code    one option of a choice, e.g. Nozzle=V, Fuel=N
    // An unlock no entry sells (a gimbal, a fuel another mod adds) needs research
    // alone, so it never waits on an entry R&D doesn't show.
    public static class Entries
    {
        public const string Cap = "cap", Size = "size";

        // What a choice's option is called here: engines' Size choice counts as a
        // size, so the tooling entries sell engine sizes and part sizes alike.
        public static string What(Choice c, Option o) =>
            string.Equals(c.Name, "Size", StringComparison.OrdinalIgnoreCase) ? Size : c.Name + "=" + o.Code;

        public static string Gimbal(Option g) => "Gimbal=" + g.Code;

        sealed class Entry { public string Name, Tech, Type, What; }
        static readonly List<Entry> all = new List<Entry>();

        public static int Count => all.Count;
        public static void Clear() => all.Clear();

        // One entry's items. Returns the items that didn't parse, for the log.
        public static List<string> Add(string name, string tech, string unlocks)
        {
            var bad = new List<string>();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(tech) || string.IsNullOrEmpty(unlocks)) return bad;
            foreach (string raw in unlocks.Split(','))
            {
                string item = raw.Trim();
                if (item.Length == 0) continue;
                int colon = item.IndexOf(':');
                if (colon <= 0 || colon == item.Length - 1) { bad.Add(item); continue; }
                all.Add(new Entry { Name = name, Tech = tech, Type = item.Substring(0, colon).Trim(), What = item.Substring(colon + 1).Trim() });
            }
            return bad;
        }

        // The entry that sells type typeId's "what" at node tech, or null when none
        // does. An entry naming the type wins over a * one (the tooling).
        public static string Find(string typeId, string tech, string what)
        {
            Entry best = null;
            foreach (Entry e in all)
            {
                if (e.Tech != tech || e.What != what || (e.Type != typeId && e.Type != "*")) continue;
                if (best == null || (best.Type == "*" && e.Type == typeId)) best = e;
            }
            return best?.Name;
        }

        // How many entries sell it, at the best match's level (the tests want one).
        public static int Matches(string typeId, string tech, string what)
        {
            var hits = all.Where(e => e.Tech == tech && e.What == what && (e.Type == typeId || e.Type == "*")).ToList();
            return hits.Any(e => e.Type == typeId) ? hits.Count(e => e.Type == typeId) : hits.Count;
        }

        // Every entry name that sells something, and every type it names.
        public static IEnumerable<string> Names => all.Select(e => e.Name).Distinct();
        public static IEnumerable<string> TypesNamed => all.Select(e => e.Type).Distinct();
    }
}
