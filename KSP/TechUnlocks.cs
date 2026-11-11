using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace PIP
{
    // Reads the tech tree (Community Tech Tree ids) for each part type: the node
    // that opens its first programs, the nodes that open each choice, and the
    // three nodes that raise its one cap from 25 to 50, 75 and 100. The PARTUPGRADE
    // entries in Upgrades.cfg and Fuels.cfg show these in R&D.
    // With KSP's entry purchase on (a career difficulty option), an unlock also
    // needs its entry bought in R&D, like a stock part, the first programs' too
    // (DESIGN.md 27.4). Core\Entries.cs says which entry sells what; an unlock no
    // entry sells needs research alone. With it off, research is enough.
    public static class TechUnlocks
    {
        static bool TreeApplies =>
            HighLogic.CurrentGame != null && ResearchAndDevelopment.Instance != null &&
            (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);

        public static bool Researched(string tech) =>
            !TreeApplies || string.IsNullOrEmpty(tech) || tech == "start" ||
            ResearchAndDevelopment.GetTechnologyState(tech) == RDTech.State.Available;

        // Entry purchase is on: a career (science games have no funds) with KSP's
        // "Entry Purchase Required on Research" on and part upgrades allowed.
        public static bool PurchaseOn
        {
            get
            {
                Game g = HighLogic.CurrentGame;
                return g != null && g.Mode == Game.Modes.CAREER && ResearchAndDevelopment.Instance != null &&
                       PartUpgradeManager.Handler != null && !g.Parameters.Difficulty.BypassEntryPurchaseAfterResearch &&
                       (g.Parameters.CustomParams<GameParameters.AdvancedParams>()?.PartUpgradesInCareer ?? true);
            }
        }

        // The entry that sells one unlock, or null: none does, or it's at the start node (free).
        public static string EntryFor(PartType t, string tech, string what)
        {
            if (string.IsNullOrEmpty(tech) || tech == "start") return null;
            Load();
            return Entries.Find(t.Id, tech, what);
        }

        public static bool Bought(PartType t, string tech, string what)
        {
            if (!PurchaseOn) return true;
            string e = EntryFor(t, tech, what);
            return e == null || PartUpgradeManager.Handler.IsUnlocked(e);
        }

        public static bool Unlocked(PartType t, string tech, string what) => Researched(tech) && Bought(t, tech, what);

        // What's left to open one unlock: "" when it's open, else "Research Fuel
        // Systems" or "Buy PIP: Adapter Tanks in R&D".
        public static string Todo(PartType t, string tech, string what) =>
            !Researched(tech) ? "Research " + Title(tech)
            : !Bought(t, tech, what) ? "Buy " + EntryTitle(EntryFor(t, tech, what)) + " in R&D"
            : "";

        // For the middle of a sentence: "research Fuel Systems".
        public static string Lower(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

        static string EntryTitle(string entry) => PartUpgradeManager.Handler?.GetUpgrade(entry)?.title ?? entry;

        // A node's name as R&D shows it. No node (a free option) has no name: R&D
        // throws on a null id, and that once broke the program window every frame.
        public static string Title(string tech)
        {
            if (string.IsNullOrEmpty(tech)) return "";
            string t = TreeApplies ? ResearchAndDevelopment.GetTechnologyTitle(tech) : null;
            return string.IsNullOrEmpty(t) ? tech : t;
        }

        // One cap for every category of a type.
        public static int Cap(PartType t) => 25 + 25 * t.CapTechs.Skip(1).Count(x => Unlocked(t, x, Entries.Cap));

        // What raises the cap next ("Research Advanced Rocketry"), or null at 100.
        public static string NextCap(PartType t) =>
            t.CapTechs.Skip(1).Select(x => Todo(t, x, Entries.Cap)).FirstOrDefault(s => s.Length > 0);

        public static bool Opened(PartType t) => Unlocked(t, t.CapTechs[0], Entries.Cap);
        public static string OpenTodo(PartType t) => Todo(t, t.CapTechs[0], Entries.Cap);

        // One option, asked as what it is: Entries.What(choice, o) for a choice's
        // option, Entries.Size for a size, Entries.Gimbal(o) for a gimbal.
        public static bool Available(PartType t, Option o, string what) => Opened(t) && o.Techs.All(x => Unlocked(t, x, what));

        // What to do first for an option: open the type, then each node it needs, in order.
        public static string Todo(PartType t, Option o, string what) =>
            !Opened(t) ? OpenTodo(t) : o.Techs.Select(x => Todo(t, x, what)).FirstOrDefault(s => s.Length > 0) ?? "";

        public static bool Available(PartType t, string[] model, string variant = null) =>
            t.Valid(model) && t.Unlocks(model, variant).All(u => Unlocked(t, u.Key, u.Value));

        // Everything left for a model and size: "Research A, B", "Buy PIP: C in R&D",
        // or "Research A; buy PIP: C in R&D".
        public static string Missing(PartType t, string[] model, string variant = null)
        {
            var u = t.Unlocks(model, variant).Distinct().ToList();
            string research = string.Join(", ", u.Select(x => x.Key).Distinct().Where(x => !Researched(x)).Select(Title));
            string buy = string.Join(", ", u.Where(x => Researched(x.Key) && !Bought(t, x.Key, x.Value))
                .Select(x => EntryTitle(EntryFor(t, x.Key, x.Value))).Distinct());
            var parts = new List<string>();
            if (research.Length > 0) parts.Add("Research " + research);
            if (buy.Length > 0) parts.Add((parts.Count > 0 ? "buy " : "Buy ") + buy + " in R&D");
            return string.Join("; ", parts);
        }

        // Whether one of PIP's R&D entries is at this node.
        public static bool SellsAt(string tech)
        {
            Load();
            return Entries.AnyAt(tech);
        }

        // Every PARTUPGRADE that says what it sells (pipUnlocks), read once.
        static bool loaded;
        static void Load()
        {
            if (loaded || GameDatabase.Instance == null) return;
            loaded = true;
            Entries.Clear();
            var bad = new List<string>();
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("PARTUPGRADE"))
            {
                string name = n.GetValue("name");
                bad.AddRange(Entries.Add(name, n.GetValue("techRequired"), n.GetValue("pipUnlocks")).Select(b => name + ": " + b));
            }
            Debug.Log($"[PIP] {Entries.Names.Count()} R&D entries sell PIP's unlocks" +
                      (bad.Count > 0 ? "; not understood: " + string.Join(", ", bad) : ""));
        }

        // Each entry's price (Pricing.EntryCost), set on KSP's copy every time a game
        // loads and when its settings change; it only counts with entry purchase on.
        public static void ApplyPrices()
        {
            if (PartUpgradeManager.Handler == null) return;
            Load();
            Dictionary<string, int> science = NodeScience();
            PriceLevel level = PipSettings.Prices;
            foreach (string name in Entries.Names)
            {
                PartUpgradeHandler.Upgrade up = PartUpgradeManager.Handler.GetUpgrade(name);
                if (up == null) continue;
                up.entryCost = science.TryGetValue(up.techRequired ?? "", out int s) ? (float)Pricing.EntryCost(s, level) : 0f;
            }
        }

        // Science per node, from the game's tech tree (Community Tech Tree's).
        static Dictionary<string, int> NodeScience()
        {
            var d = new Dictionary<string, int>();
            try
            {
                ProtoTechNode[] techs = AssetBase.RnDTechTree?.GetTreeTechs();
                if (techs != null)
                    foreach (ProtoTechNode n in techs)
                        if (!string.IsNullOrEmpty(n?.techID)) d[n.techID] = n.scienceCost;
            }
            catch (Exception e) { Debug.LogWarning("[PIP] Reading the tech tree's costs failed: " + e.Message); }
            if (d.Count == 0 && GameDatabase.Instance != null)
                foreach (ConfigNode tree in GameDatabase.Instance.GetConfigNodes("TechTree"))
                    foreach (ConfigNode n in tree.GetNodes("RDNode"))
                        if (n.GetValue("id") is string id && int.TryParse(n.GetValue("cost"), out int c)) d[id] = c;
            return d;
        }

        // A game PIP ran in before its entries could be bought (DESIGN.md 27.4):
        // the entries of every node it had researched count as bought, as they were
        // free then, so nothing it already used locks. Once per game (PipScenario).
        // Returns how many, or -1 when R&D isn't loaded yet (try again later).
        public static int SettleOldGame()
        {
            Game g = HighLogic.CurrentGame;
            if (g == null) return -1;
            if (g.Mode != Game.Modes.CAREER && g.Mode != Game.Modes.SCIENCE_SANDBOX) return 0;
            if (PartUpgradeManager.Handler == null || ResearchAndDevelopment.Instance == null) return -1;
            Load();
            int n = 0;
            foreach (string name in Entries.Names)
            {
                PartUpgradeHandler.Upgrade up = PartUpgradeManager.Handler.GetUpgrade(name);
                if (up == null || string.IsNullOrEmpty(up.techRequired) || !Researched(up.techRequired)) continue;
                PartUpgradeManager.Handler.SetUnlocked(name, true);
                n++;
            }
            return n;
        }
    }
}
