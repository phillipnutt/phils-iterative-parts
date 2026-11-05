using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace PIP
{
    // Reads GameData/PIP/Fuels.cfg (DESIGN.md section 19) after Module Manager and
    // before parts compile:
    //  - PIP_FUEL nodes become the fuel table liquid engine programs choose from;
    //  - PIP_BOILOFF nodes set which resources boil off, and how fast;
    //  - a fuel with no engine chassis of its own gets them, copied from the
    //    kerolox chassis with its own propellants.
    // A fuel whose resources aren't installed is left out, so any mod's fuel can be
    // added with a config (its tank recipe goes in with a Module Manager patch to
    // PIP's tank chassis, as Fuels.cfg does for its own).
    public static class FuelLoader
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly string[] Nozzles = { "S", "B", "V", "A" };

        public static void Load()
        {
            if (GameDatabase.Instance == null) return;
            var resources = new HashSet<string>(GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION").Select(n => n.GetValue("name") ?? ""));

            var fuels = new List<FuelDef>();
            var left = new List<string>();
            Plumes.ByFuel.Clear();
            templates = null;
            MeasurePlumes();
            PlumesSwapped = 0;
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("PIP_FUEL"))
            {
                FuelDef f = Read(n, out string problem);
                if (f == null) { Debug.LogWarning($"[PIP] Fuels.cfg: skipped {n.GetValue("code") ?? "a fuel"}: {problem}"); continue; }
                string absent = f.Resources.FirstOrDefault(r => !resources.Contains(r));
                if (absent != null) { left.Add($"{f.Title} (no {absent})"); continue; }
                fuels.Add(f);
                ConfigNode plume = n.GetNode("PLUME");
                if (plume != null) Plumes.ByFuel[f.Code] = ReadPlume(plume);
            }
            if (fuels.Count > 0) FuelTable.Set(fuels);
            AddTanks(fuels.Select(f => f.Code));
            Debug.Log($"[PIP] Fuels: {string.Join(", ", FuelTable.All.Select(f => f.Code + " " + f.Title))}" +
                      (left.Count > 0 ? "; left out: " + string.Join(", ", left) : ""));

            var rates = GameDatabase.Instance.GetConfigNodes("PIP_BOILOFF").ToList();
            if (rates.Count > 0)
            {
                ModulePipBoiloff.Rates.Clear();
                foreach (ConfigNode n in rates)
                {
                    string r = n.GetValue("resource");
                    if (string.IsNullOrEmpty(r) || !Num(n, "rate", out double rate)) continue;
                    Num(n, "cooling", out double cooling);
                    ModulePipBoiloff.Rates[r] = new[] { rate, cooling };
                }
            }

            MakeChassis();

            // Every liquid engine chassis gets its fuel's plume (the stock engine a
            // hydrolox chassis copies has a kerolox flame).
            foreach (UrlDir.UrlConfig u in GameDatabase.Instance.GetConfigs("PART"))
            {
                ConfigNode pip = u.config.GetNodes("MODULE").FirstOrDefault(m => m.GetValue("name") == nameof(ModulePipEngine) && m.GetValue("typeId") == "liquid");
                string model = pip?.GetValue("model");
                if (!string.IsNullOrEmpty(model)) ApplyPlume(u.config, model.Split('-')[0]);
            }
        }

        static PlumeSet ReadPlume(ConfigNode n)
        {
            string[] List(string key) => (n.GetValue(key) ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            var set = new PlumeSet { Match = List("match") };
            foreach (string role in new[] { Plumes.Lower, Plumes.Upper, Plumes.Core, Plumes.Generator })
            {
                string[] options = List(role);
                if (options.Length > 0) set.Roles[role] = options;
            }
            return set;
        }

        // Waterfall's installed templates (none without Waterfall: nothing to swap).
        static HashSet<string> templates;
        public static int PlumesSwapped;

        static readonly Dictionary<string, double> ProfileDiameter = new Dictionary<string, double>
            { { "size0", 0.625 }, { "size1", 1.25 }, { "size1p5", 1.875 }, { "size2", 2.5 }, { "size3", 3.75 }, { "size4", 5 }, { "size5", 7.5 } };

        // How big each template is drawn per meter of engine, across every installed
        // engine that uses it (Plumes.SizePerMeter): how its own authors size it.
        static void MeasurePlumes()
        {
            Plumes.SizePerMeter.Clear();
            var seen = new Dictionary<string, List<double>>();
            foreach (UrlDir.UrlConfig u in GameDatabase.Instance.GetConfigs("PART"))
            {
                string name = u.config.GetValue("name") ?? "";
                if (name.StartsWith("PIP", StringComparison.Ordinal)) continue;
                ConfigNode[] fx = u.config.GetNodes("MODULE").Where(m => m.GetValue("name") == "ModuleWaterfallFX").ToArray();
                if (fx.Length == 0) continue;
                double d = 0;
                foreach (string p in (u.config.GetValue("bulkheadProfiles") ?? "").Split(','))
                    if (ProfileDiameter.TryGetValue(p.Trim(), out d)) break;
                if (d <= 0) continue;
                foreach (ConfigNode m in fx)
                    foreach (ConfigNode t in m.GetNodes("TEMPLATE"))
                    {
                        string tn = t.GetValue("templateName");
                        double[] s = Vec(t.GetValue("scale"));
                        if (string.IsNullOrEmpty(tn) || s[0] <= 0) continue;
                        if (!seen.TryGetValue(tn, out List<double> list)) seen[tn] = list = new List<double>();
                        list.Add(s[0] / d);
                    }
            }
            foreach (var kv in seen) Plumes.SizePerMeter[kv.Key] = Plumes.Median(kv.Value);
        }

        static double[] Vec(string v)
        {
            string[] p = (v ?? "").Split(',');
            var r = new double[] { 1, 1, 1 };
            for (int i = 0; i < 3 && i < p.Length; i++)
                if (double.TryParse(p[i].Trim(), NumberStyles.Float, Inv, out double x)) r[i] = x;
            return r;
        }

        // Gives an engine config its fuel's Waterfall plume: each fuel-specific
        // template on it becomes the fuel's template of the same role, at the same
        // place and size (Plumes.Swap). Engines without Waterfall are untouched.
        public static void ApplyPlume(ConfigNode part, string fuel)
        {
            if (Plumes.ByFuel.Count == 0 || GameDatabase.Instance == null) return;
            if (templates == null)
                templates = new HashSet<string>(GameDatabase.Instance.GetConfigNodes("EFFECTTEMPLATE").Select(t => t.GetValue("templateName") ?? ""));
            if (templates.Count == 0) return;
            foreach (ConfigNode m in part.GetNodes("MODULE"))
            {
                if (m.GetValue("name") != "ModuleWaterfallFX") continue;
                foreach (ConfigNode t in m.GetNodes("TEMPLATE"))
                {
                    string from = t.GetValue("templateName");
                    string to = Plumes.Swap(from, fuel, templates.Contains);
                    if (to == null) continue;
                    PlumesSwapped++;
                    if (to.Length == 0) { m.RemoveNode(t); continue; }
                    t.SetValue("templateName", to);
                    double f = Plumes.Rescale(from, to);
                    double[] s = Vec(t.GetValue("scale"));
                    t.SetValue("scale", string.Join(", ", s.Select(x => (x * f).ToString("0.####", Inv))), true);
                }
            }
        }

        static FuelDef Read(ConfigNode n, out string problem)
        {
            problem = null;
            var f = new FuelDef { Code = n.GetValue("code") ?? "", Title = n.GetValue("title") ?? "", Tech = n.GetValue("tech") ?? "", Note = n.GetValue("note") ?? "" };
            if (f.Code.Length == 0 || f.Code.Contains("-") || f.Code.Contains(".")) { problem = "its code must be set, without - or ."; return null; }
            if (f.Title.Length == 0) f.Title = f.Code;

            ConfigNode[] props = n.GetNodes("PROPELLANT");
            if (props.Length == 0) { problem = "no PROPELLANT"; return null; }
            f.Resources = new string[props.Length];
            f.Ratios = new double[props.Length];
            for (int i = 0; i < props.Length; i++)
            {
                f.Resources[i] = props[i].GetValue("name");
                if (string.IsNullOrEmpty(f.Resources[i]) || !Num(props[i], "ratio", out f.Ratios[i]) || f.Ratios[i] <= 0) { problem = "a PROPELLANT needs a name and a ratio"; return null; }
            }

            ConfigNode isp = n.GetNode("ISP");
            if (isp == null) { problem = "no ISP"; return null; }
            f.Isp = new double[4][];
            for (int i = 0; i < Nozzles.Length; i++)
            {
                double[] w = (isp.GetValue(Nozzles[i]) ?? "").Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => double.TryParse(x, NumberStyles.Float, Inv, out double v) ? v : double.NaN).ToArray();
                if (w.Length != 4 || w.Any(double.IsNaN)) { problem = $"ISP {Nozzles[i]} needs four numbers"; return null; }
                f.Isp[i] = w;
            }

            if (Num(n, "thrust", out double d)) f.Thrust = d;
            if (Num(n, "twr", out d)) f.Twr = d;
            if (Num(n, "price", out d)) f.Price = d;
            if (Num(n, "failHi", out d)) f.FailHi = d;
            if (Num(n, "failLo", out d)) f.FailLo = d;
            if (f.Thrust <= 0 || f.Twr <= 0 || f.Price <= 0) { problem = "thrust, twr and price must be above 0"; return null; }
            return f;
        }

        // A fuel's TANK nodes put its recipe on PIP's tank of that kind ("S" standard,
        // "C" cryogenic, which boils off), as one more pick on the part's propellant
        // switch. The recipe is a B9_TANK_TYPE written in the config itself.
        static void AddTanks(IEnumerable<string> codes)
        {
            var wanted = new HashSet<string>(codes);
            var tankTypes = new HashSet<string>(GameDatabase.Instance.GetConfigNodes("B9_TANK_TYPE").Select(t => t.GetValue("name") ?? ""));
            var tanks = GameDatabase.Instance.GetConfigs("PART").Where(u => (u.config.GetValue("name") ?? "").StartsWith("PIP-TNK-", StringComparison.Ordinal))
                .GroupBy(u => u.config.GetValue("name")).ToDictionary(g => g.Key, g => g.First().config);
            var added = new List<string>();
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("PIP_FUEL"))
            {
                if (!wanted.Contains(n.GetValue("code") ?? "")) continue;
                foreach (ConfigNode t in n.GetNodes("TANK"))
                {
                    string kind = t.GetValue("kind") ?? "S", name = t.GetValue("name"), type = t.GetValue("tankType");
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type)) continue;
                    if (!tankTypes.Contains(type)) { Debug.LogWarning($"[PIP] Fuels.cfg: no B9_TANK_TYPE {type} for tank {name}"); continue; }
                    if (!tanks.TryGetValue("PIP-TNK-" + kind, out ConfigNode part)) continue;
                    ConfigNode sw = part.GetNodes("MODULE").FirstOrDefault(m => m.GetValue("name") == "ModuleB9PartSwitch" && m.GetValue("moduleID") == "fuelSwitch");
                    if (sw == null || sw.GetNodes("SUBTYPE").Any(s => s.GetValue("name") == name)) continue;
                    ConfigNode s2 = sw.AddNode("SUBTYPE");
                    s2.AddValue("name", name);
                    s2.AddValue("title", t.GetValue("title") ?? name);
                    s2.AddValue("tankType", type);
                    added.Add($"{name} ({kind})");
                }
            }
            if (added.Count > 0) Debug.Log("[PIP] Tank recipes added: " + string.Join(", ", added));
        }

        static bool Num(ConfigNode n, string key, out double v) =>
            double.TryParse(n.GetValue(key) ?? "", NumberStyles.Float, Inv, out v);

        // Every fuel needs an engine chassis per size and nozzle: PIP-<code>-125-S and
        // so on. Kerolox, hydrolox and methalox have their own (Chassis/Engines.cfg);
        // any other fuel gets copies of kerolox's, stand-ins included.
        static void MakeChassis()
        {
            UrlDir.UrlConfig[] parts = GameDatabase.Instance.GetConfigs("PART");
            var names = new HashSet<string>(parts.Select(u => u.config.GetValue("name") ?? ""));
            List<UrlDir.UrlConfig> kerolox = parts.Where(u => (u.config.GetValue("name") ?? "").StartsWith("PIP-K-", StringComparison.Ordinal)).ToList();
            var needs = GameDatabase.Instance.GetConfigs("PIP_NEEDS").Where(x => (x.config.GetValue("chassis") ?? "").StartsWith("PIP-K-", StringComparison.Ordinal)).ToList();
            int made = 0;
            foreach (FuelDef f in FuelTable.All)
            {
                if (f.Code == "K") continue;
                string[][] props = f.Resources.Select((r, i) => new[] { r, f.Ratios[i].ToString("R", Inv) }).ToArray();
                foreach (UrlDir.UrlConfig u in kerolox)
                {
                    string from = u.config.GetValue("name");
                    string name = "PIP-" + f.Code + "-" + from.Substring("PIP-K-".Length);
                    if (names.Contains(name)) continue;
                    ConfigNode c = u.config.CreateCopy();
                    c.SetValue("name", name, true);
                    c.SetValue("title", (c.GetValue("title") ?? "").Replace("kerolox", Fmt.Lower(f.Title)), true);
                    foreach (ConfigNode m in c.GetNodes("MODULE"))
                    {
                        string mn = m.GetValue("name") ?? "";
                        if (mn.StartsWith("ModuleEngines", StringComparison.Ordinal)) SetPropellants(m, props);
                        else if (mn == nameof(ModulePipEngine))
                        {
                            string model = m.GetValue("model") ?? "";
                            if (model.StartsWith("K-", StringComparison.Ordinal)) m.SetValue("model", f.Code + model.Substring(1));
                        }
                    }
                    u.parent.AddConfig(c);
                    names.Add(name);
                    made++;
                }
                foreach (UrlDir.UrlConfig nd in needs)
                {
                    ConfigNode copy = nd.config.CreateCopy();
                    copy.SetValue("chassis", "PIP-" + f.Code + "-" + nd.config.GetValue("chassis").Substring("PIP-K-".Length));
                    nd.parent.AddConfig(copy);
                }
            }
            if (made > 0) Debug.Log($"[PIP] Made {made} engine chassis for fuels beyond kerolox, hydrolox and methalox");
        }

        public static void SetPropellants(ConfigNode engine, string[][] props)
        {
            engine.RemoveNodes("PROPELLANT");
            bool first = true;
            foreach (string[] p in props)
            {
                ConfigNode pn = engine.AddNode("PROPELLANT");
                pn.AddValue("name", p[0]);
                pn.AddValue("ratio", p[1]);
                if (first) pn.AddValue("DrawGauge", "True");
                first = false;
            }
        }
    }
}
