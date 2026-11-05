using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PIP
{
    // The mods PIP can't work without (DESIGN.md section 17): Module Manager makes
    // its parts, B9PartSwitch sizes them, the Community Tech Tree holds its unlocks,
    // and CryoTanks adds hydrogen and methane (and the tank switch PIP's tanks use).
    // Harmony is checked by KSP itself: without it this assembly doesn't load at all.
    // When one is missing, PIP stays off and says why once, at the main menu.
    // Everything else PIP uses is optional (stand-ins and padlocks, PartFactory).
    public static class Requirements
    {
        static List<string> missing;

        // The required mods that aren't installed; empty when all are. Read once the
        // game database is loaded (it's known before parts compile).
        public static List<string> Missing
        {
            get
            {
                if (missing != null) return missing;
                if (GameDatabase.Instance == null || AssemblyLoader.loadedAssemblies == null) return new List<string>();
                var list = new List<string>();
                if (!Assembly("ModuleManager")) list.Add("Module Manager");
                if (!Assembly("B9PartSwitch")) list.Add("B9PartSwitch");
                if (!TechNode("giganticRocketry")) list.Add("Community Tech Tree");
                // CryoTanks' plugin is SimpleBoiloff; the fuels come with it (from Community Resource Pack).
                if (!Assembly("SimpleBoiloff") || !Resource("LqdHydrogen") || !Resource("LqdMethane")) list.Add("CryoTanks");
                missing = list;
                if (list.Count > 0) Debug.LogWarning("[PIP] Off: missing " + string.Join(", ", list));
                return list;
            }
        }

        public static bool Ok => Missing.Count == 0;

        static bool Assembly(string name) =>
            AssemblyLoader.loadedAssemblies.Any(a => string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase) ||
                                                     string.Equals(a.assembly?.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

        static bool TechNode(string id) =>
            GameDatabase.Instance.GetConfigNodes("TechTree").Any(tree => tree.GetNodes("RDNode").Any(n => n.GetValue("id") == id));

        static bool Resource(string name) =>
            GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION").Any(n => n.GetValue("name") == name);
    }

    // R&D shows each PIP unlock with one of PIP's parts as its picture
    // (Upgrades.cfg). Without the optional mods some of those parts don't exist
    // (DESIGN.md 17), and R&D errors on an unlock whose picture is missing, so it
    // shows the model's stock stand-in instead, or another part of the same type.
    public static class UpgradeIcons
    {
        public static void Fix()
        {
            if (GameDatabase.Instance == null || PartLoader.Instance == null || PartUpgradeManager.Handler == null) return;
            var changed = new List<string>();
            foreach (UrlDir.UrlConfig u in GameDatabase.Instance.GetConfigs("PARTUPGRADE"))
            {
                string name = u.config.GetValue("name"), icon = u.config.GetValue("partIcon");
                if (name == null || !name.StartsWith("PIP-", StringComparison.Ordinal) || string.IsNullOrEmpty(icon) || PartFactory.Info(icon) != null) continue;
                string use = PartFactory.Resolve(icon) ?? SameType(icon);
                if (use == null) { Debug.LogWarning($"[PIP] R&D unlock {name} has no picture: {icon} isn't installed"); continue; }
                u.config.SetValue("partIcon", use);
                PartUpgradeHandler.Upgrade up = PartUpgradeManager.Handler.GetUpgrade(name);
                if (up != null) up.partIcon = use;
                changed.Add($"{icon} -> {use}");
            }
            if (changed.Count > 0) Debug.Log($"[PIP] R&D unlock pictures using a stand-in ({changed.Count}): " + string.Join(", ", changed));
        }

        // "PIP-ELE-M-0625" -> an installed "PIP-ELE-..." chassis, preferred models first.
        static string SameType(string icon)
        {
            string[] bits = icon.Split('-');
            if (bits.Length < 3) return null;
            string prefix = bits[0] + "-" + bits[1] + "-";
            return PartLoader.LoadedPartsList.Select(p => p.name)
                .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(n => n.EndsWith(PartFactory.StandInSuffix, StringComparison.Ordinal)).ThenBy(n => n, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RequirementsNotice : MonoBehaviour
    {
        void Start()
        {
            if (Requirements.Ok)
            {
                Debug.Log(PartFactory.ModelReport());
                try { UpgradeIcons.Fix(); }
                catch (Exception e) { Debug.LogError("[PIP] Fixing R&D unlock pictures failed: " + e); }
                return;
            }
            string list = string.Join(", ", Requirements.Missing);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "PIPRequirements", "Phil's Iterative Parts",
                $"Phil's Iterative Parts is off: it needs {list}, which {(Requirements.Missing.Count == 1 ? "isn't" : "aren't")} installed.\n\n" +
                "Install them (CKAN does this for you) and restart KSP. Your other mods and saves are unaffected.",
                "OK", false, HighLogic.UISkin);
        }
    }
}
