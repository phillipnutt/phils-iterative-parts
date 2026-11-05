using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PIP
{
    // Looks (DESIGN.md section 16): a program can wear the model of any installed
    // part of its type. At load, right after Module Manager, every such part gets
    // a hidden "look chassis": a copy of its config with PIP's module, PIP's size
    // switch and none of the things that would change its numbers. A program's
    // part is then cloned from its look's chassis instead of PIP's own.
    //
    // Tanks and RCS get one look chassis per program choice that changes the
    // config (a tank's kind, an RCS block's propellant): the "key" in the name.
    public static class Looks
    {
        public const string Prefix = "PIP-LOOK-";

        // KSP turns "_" into "." in part names; looks are named after the part as KSP lists it.
        public static string ChassisName(PartType t, bool side, string source, string key = "") =>
            Prefix + t.ChassisCode + (string.IsNullOrEmpty(key) ? "" : "-" + key) + (side ? "-R-" : "-") + (source ?? "").Replace('_', '.');

        #region Making the look chassis (at load)

        static readonly Dictionary<string, double> RoundSizes = new Dictionary<string, double>
        {
            { "size0", 0.625 }, { "size1", 1.25 }, { "size1p5", 1.875 }, { "size2", 2.5 },
            { "size3", 3.75 }, { "size4", 5 }, { "size5", 6.25 }, { "size6", 7.5 },
        };
        static readonly Dictionary<string, int> NodeSizes = new Dictionary<string, int>
            { { "0625", 0 }, { "125", 1 }, { "1875", 1 }, { "25", 2 }, { "375", 3 }, { "5", 4 } };
        static readonly HashSet<string> Generators = new HashSet<string>
            { "ModuleGenerator", "ModuleResourceConverter", "ModuleDeployableSolarPanel", "ModuleCurvedSolarPanel" };
        static readonly HashSet<string> TankResources = new HashSet<string>
            { "LiquidFuel", "Oxidizer", "LqdHydrogen", "LqdMethane", "MonoPropellant", "XenonGas", "ArgonGas", "Lithium" };
        static readonly HashSet<string> ElectricPropellants = new HashSet<string> { "XenonGas", "ArgonGas", "Lithium" };
        static readonly HashSet<string> RcsPropellants = new HashSet<string> { "MonoPropellant", "LiquidFuel", "Oxidizer" };
        // Modules a look never keeps: other scalers, failure models, contracts' test module.
        static readonly HashSet<string> Stripped = new HashSet<string>
            { "TweakScale", "TweakScalerWaterfallFX", "ModuleTestSubject", "Reliability", "EngineFailures", nameof(ModulePipPart), nameof(ModulePipEngine) };
        // A tank look takes its propellants from PIP's own tank of the program's kind.
        static readonly HashSet<string> TankModules = new HashSet<string>
            { "ModuleCryoTank", "ModuleFuelTanks", "FSfuelSwitch", "InterstellarFuelSwitch" };
        // B9PartSwitch subtypes that only change how a part looks (foil, paint) are kept.
        static readonly HashSet<string> CosmeticValues = new HashSet<string>
            { "name", "title", "descriptionSummary", "descriptionDetail", "primaryColor", "secondaryColor", "transform", "defaultSubtypePriority" };
        static readonly HashSet<string> CosmeticNodes = new HashSet<string> { "TEXTURE", "MATERIAL" };
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // What an installed part is, as far as looks go.
        sealed class Source
        {
            public PartType Type;
            public bool Side, Engine;
            public string Kind = "";        // "F"/"D" panel, "O"/"D" antenna, "F"/"M"/"E" tank contents; engines: size and shape
            public string Fuel = "";        // a liquid engine's own fuel (K/H/M), to list same-fuel looks first
            public double Diameter, Mass, Output, Height;
            public LookScale Basis => Type.ScaleBasis(Side, Kind);
        }

        static readonly HashSet<string> RocketFuels = new HashSet<string> { "LiquidFuel", "LqdHydrogen", "LqdMethane" };
        static readonly HashSet<string> NuclearFuels = new HashSet<string> { "LqdHydrogen", "LiquidFuel", "EnrichedUranium" };
        static readonly Dictionary<string, string[][]> FuelPropellants = new Dictionary<string, string[][]>
        {
            { "K", new[] { new[] { "LiquidFuel", "0.9" }, new[] { "Oxidizer", "1.1" } } },
            { "H", new[] { new[] { "LqdHydrogen", "1.5" }, new[] { "Oxidizer", "0.1" } } },
            { "M", new[] { new[] { "LqdMethane", "3" }, new[] { "Oxidizer", "1" } } },
        };

        static bool IsEngineModule(ConfigNode m) => (m.GetValue("name") ?? "").StartsWith("ModuleEngines", StringComparison.Ordinal);
        static HashSet<string> PropellantsOf(ConfigNode engine) =>
            new HashSet<string>(engine.GetNodes("PROPELLANT").Select(p => p.GetValue("name") ?? ""));

        // Engines, by what they burn (the same rules as Replace). Engines are never
        // scaled: a look is an engine at its own size, and its kind says which.
        static Source ClassifyEngine(ConfigNode n, List<ConfigNode> engines, string category)
        {
            if (category != "Engine" && category != "Propulsion") return null;
            List<HashSet<string>> props = engines.Select(PropellantsOf).ToList();
            PartType t;
            if (props.Any(p => p.Contains("IntakeAir"))) t = PartTypes.Jet;
            else if (props.All(p => p.Contains("Oxidizer") && p.Overlaps(RocketFuels) && p.All(x => x == "Oxidizer" || x == "ElectricCharge" || RocketFuels.Contains(x))))
                t = PartTypes.Liquid;
            else if (props.All(p => p.Count == 1 && p.Contains("SolidFuel"))) t = PartTypes.Solid;
            else if (props.All(p => p.Count == 1 && p.Contains("MonoPropellant"))) t = PartTypes.Monoprop;
            else if (props.Any(p => p.Contains("ElectricCharge") && p.Overlaps(ElectricPropellants))) t = PartTypes.Electric;
            else if (props.Any(p => p.Count > 0 && p.All(NuclearFuels.Contains) && !p.Contains("Oxidizer"))) t = PartTypes.Nuclear;
            else return null;
            if (!t.HasLooks) return null;
            // Several modes only where the type has them (jets), or where one can be kept (nuclear).
            if (engines.Count > 1 && t != PartTypes.Jet && t != PartTypes.Nuclear) return null;

            double diameter = 0;
            foreach (string p in (n.GetValue("bulkheadProfiles") ?? "").Split(','))
                if (RoundSizes.TryGetValue(p.Trim(), out double d)) { diameter = d; break; }
            int si = Array.FindIndex(Sizes.Diameters, d => Math.Abs(d - diameter) < 1e-3);
            bool stack = false;
            foreach (ConfigNode.Value v in n.values) if (v.name.StartsWith("node_stack_", StringComparison.Ordinal)) { stack = true; break; }
            if (si < 0 || !stack) return null;
            string size = Sizes.Codes[si];

            var src = new Source { Type = t, Engine = true, Diameter = diameter };
            if (t == PartTypes.Liquid)
            {
                string label = (n.GetValue("name") + " " + n.GetValue("title")).ToLowerInvariant();
                src.Kind = size + (label.Contains("aerospike") ? "-A" : "");
                src.Fuel = props[0].Contains("LqdHydrogen") ? "H" : props[0].Contains("LqdMethane") ? "M" : "K";
            }
            else if (t == PartTypes.Solid)
            {
                double units = n.GetNodes("RESOURCE").Where(r => r.GetValue("name") == "SolidFuel")
                    .Select(r => double.TryParse(r.GetValue("maxAmount") ?? "", NumberStyles.Float, Inv, out double u) ? u : 0).FirstOrDefault();
                string length = SolidMotors.LengthOf(size, units);
                if (length == null) return null;
                src.Kind = size + "-" + length;
            }
            else if (t == PartTypes.Electric)
            {
                string gas = props[0].FirstOrDefault(ElectricPropellants.Contains);
                string type = gas == null ? null : ElectricEngines.TypeForGas(gas);
                if (type == null) return null;
                src.Kind = type + "-" + size;
            }
            else if (t == PartTypes.Jet)
            {
                int air = props.Count(p => p.Contains("IntakeAir")), closed = props.Count - air;
                string modes = air >= 1 && closed >= 1 ? "C" : air == 2 ? "2" : air == 1 ? "1" : null;
                if (modes == null) return null;
                src.Kind = size + "-" + modes;
            }
            else src.Kind = size;   // monopropellant, nuclear
            return src;
        }

        // PIP's own chassis whose size switch a look copies (titles, unlocks, default).
        static string Template(PartType t, string kind, string key)
        {
            if (t == PartTypes.Intake) return "PIP-INT-S";
            if (t == PartTypes.Solar || t == PartTypes.Antenna) return "PIP-" + t.ChassisCode + "-" + kind;
            if (t == PartTypes.Tank) return "PIP-TNK-" + key;
            if (t == PartTypes.Rcs) return null;
            return "PIP-" + t.ChassisCode;
        }

        // A part with only "mesh =" gets the MODEL node that stands for, so a size
        // switch can find and scale it. Null when no model is found.
        static ConfigNode MeshToModel(UrlDir.UrlConfig u, ConfigNode c)
        {
            string mesh = c.GetValue("mesh");
            if (string.IsNullOrEmpty(mesh)) return null;
            // KSP ignores the file name in "mesh" and uses the folder's model
            // (the MPO probe's mesh = MPO_Probe.mu is really MpoProbe.mu).
            string dir = u.parent.parent.url;
            string url = dir + "/" + Path.GetFileNameWithoutExtension(mesh);
            if (!GameDatabase.Instance.ExistsModel(url))
            {
                GameObject found = GameDatabase.Instance.GetModelPrefabIn(dir);
                UrlDir.UrlFile file = found == null ? null : GameDatabase.Instance.GetModelFile(found);
                if (file == null) return null;
                url = file.url;
            }
            ConfigNode mn = c.AddNode("MODEL");
            mn.AddValue("model", url);
            c.RemoveValue("mesh");
            return mn;
        }

        // PIP's own sized chassis copy stock parts. With ReStock those have MODEL
        // nodes (named PIPModel by the chassis patch); plain stock parts only have
        // "mesh =", so the size switch would find nothing to scale (DESIGN.md 17).
        static void NameChassisModels(UrlDir.UrlConfig[] parts)
        {
            int fixedCount = 0;
            foreach (UrlDir.UrlConfig u in parts)
            {
                string n = u.config.GetValue("name");
                if (n == null || !n.StartsWith("PIP-", StringComparison.Ordinal) || u.config.HasNode("MODEL")) continue;
                bool sized = u.config.GetNodes("MODULE").Any(m => m.GetValue("name") == "ModuleB9PartSwitch" && m.GetValue("moduleID") == "pipSize");
                if (!sized) continue;
                ConfigNode mn = MeshToModel(u, u.config);
                if (mn == null) { Debug.LogWarning($"[PIP] No model found for chassis {n}"); continue; }
                mn.AddValue("name", "PIPModel");
                fixedCount++;
            }
            if (fixedCount > 0) Debug.Log($"[PIP] Gave {fixedCount} chassis a named MODEL (their stock parts only had \"mesh\")");
        }

        public static void MakeConfigs()
        {
            if (GameDatabase.Instance == null) return;
            UrlDir.UrlConfig[] parts = GameDatabase.Instance.GetConfigs("PART");
            NameChassisModels(parts);
            var names = new HashSet<string>();
            var templates = new Dictionary<string, ConfigNode>();
            foreach (UrlDir.UrlConfig u in parts)
            {
                string n = u.config.GetValue("name");
                if (n == null) continue;
                names.Add(n.Replace('_', '.'));
                if (n.StartsWith("PIP-", StringComparison.Ordinal) && !templates.ContainsKey(n)) templates[n] = u.config;
            }

            var counts = new Dictionary<string, int>();
            int skipped = 0;
            foreach (UrlDir.UrlConfig u in parts)
            {
                string name = u.config.GetValue("name");
                if (string.IsNullOrEmpty(name) || name.StartsWith("PIP", StringComparison.Ordinal)) continue;
                Source src = Classify(u.config);
                if (src == null) continue;
                foreach (string key in src.Type.LookKeys(src.Kind))
                {
                    string look = ChassisName(src.Type, src.Side, name, key);
                    if (names.Contains(look)) continue;   // the database was reloaded
                    try
                    {
                        ConfigNode c = src.Engine ? BuildEngine(u, src, key) : Build(u, src, key, templates);
                        if (c == null) { skipped++; continue; }
                        u.parent.AddConfig(c);
                        names.Add(look);
                        string label = src.Type.Id + (src.Side ? " side" : "");
                        counts[label] = (counts.TryGetValue(label, out int k) ? k : 0) + 1;
                    }
                    catch (Exception e)
                    {
                        skipped++;
                        Debug.LogWarning($"[PIP] No look from {name}: {e.Message}");
                    }
                }
            }
            Debug.Log("[PIP] Made looks: " + (counts.Count == 0 ? "none" : string.Join(", ", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Value} {kv.Key}")))
                      + (skipped > 0 ? $" ({skipped} skipped)" : ""));
        }

        // The same rules as Replace (EditorHider), read from the config instead of the
        // compiled part. Side: attaches to a surface rather than in a stack.
        static Source Classify(ConfigNode n)
        {
            if (string.Equals(n.GetValue("TechHidden"), "True", StringComparison.OrdinalIgnoreCase)) return null;
            string category = n.GetValue("category");
            if (category == "none") return null;
            ConfigNode[] moduleNodes = n.GetNodes("MODULE");
            var modules = new HashSet<string>(moduleNodes.Select(m => m.GetValue("name") ?? ""));
            var resources = new HashSet<string>(n.GetNodes("RESOURCE").Select(r => r.GetValue("name") ?? ""));
            List<ConfigNode> engineNodes = moduleNodes.Where(IsEngineModule).ToList();
            if (engineNodes.Count > 0) return ClassifyEngine(n, engineNodes, category);
            ConfigNode Module(string name) => moduleNodes.FirstOrDefault(m => m.GetValue("name") == name);
            double Num(ConfigNode node, string key) =>
                node != null && double.TryParse(node.GetValue(key) ?? "", NumberStyles.Float, Inv, out double v) ? v : 0;

            var src = new Source();
            double.TryParse(n.GetValue("mass") ?? "0", NumberStyles.Float, Inv, out src.Mass);
            ConfigNode rcs = moduleNodes.FirstOrDefault(m => (m.GetValue("name") ?? "").StartsWith("ModuleRCS", StringComparison.Ordinal));

            if (modules.Contains("ModuleCommand"))
            {
                int.TryParse(n.GetValue("CrewCapacity") ?? "0", NumberStyles.Integer, Inv, out int crew);
                if (crew == 0 && category == "Pods") src.Type = PartTypes.Probe;
            }
            else if (rcs != null)
            {
                var props = new HashSet<string>(rcs.GetNodes("PROPELLANT").Select(p => p.GetValue("name") ?? ""));
                if (props.Count == 0 && !string.IsNullOrEmpty(rcs.GetValue("resourceName"))) props.Add(rcs.GetValue("resourceName"));
                if (props.Count > 0 && props.All(RcsPropellants.Contains)) src.Type = PartTypes.Rcs;
            }
            else if (Module("ModuleDeployableSolarPanel") is ConfigNode panel)
            {
                src.Type = PartTypes.Solar;
                src.Kind = string.IsNullOrEmpty(panel.GetValue("animationName")) ? "F" : "D";
                src.Output = Num(panel, "chargeRate");
            }
            else if (Module("ModuleCurvedSolarPanel") is ConfigNode curved)
            {
                src.Type = PartTypes.Solar;
                src.Kind = string.IsNullOrEmpty(curved.GetValue("DeployAnimation")) ? "F" : "D";
                src.Output = Num(curved, "TotalEnergyRate");
            }
            else if (modules.Contains("ModuleRadioisotopeGenerator")) src.Type = category == "Electrical" ? PartTypes.Rtg : null;
            else if (modules.Contains("ModuleAblator")) src.Type = PartTypes.Shield;
            else if (modules.Contains("ModuleReactionWheel")) src.Type = PartTypes.Wheel;
            else if ((modules.Contains("ModuleRTAntenna") || modules.Contains("ModuleDataTransmitter")) && category == "Communication")
            {
                src.Type = PartTypes.Antenna;
                ConfigNode rt = Module("ModuleRTAntenna"), dt = Module("ModuleDataTransmitter");
                if (rt != null) src.Kind = Num(rt, "Mode1DishRange") > 0 ? "D" : "O";
                else src.Kind = dt?.GetValue("antennaType") == "RELAY" || Num(dt, "antennaPower") > 1e9 ? "D" : "O";
            }
            else if (modules.Contains("ModuleResourceIntake") && resources.All(r => r == "IntakeAir") && !modules.Contains("ModuleRoboticServoRotor"))
                src.Type = PartTypes.Intake;
            else if (category == "Electrical" && resources.Count == 1 && resources.Contains("ElectricCharge") && !modules.Overlaps(Generators))
                src.Type = PartTypes.Battery;
            else if (category == "FuelTank" && (resources.Count > 0 ? resources.All(TankResources.Contains)
                         : moduleNodes.Any(m => m.GetValue("name") == "ModuleB9PartSwitch" && m.GetValue("moduleID") == "fuelSwitch")))
            {
                src.Type = PartTypes.Tank;
                src.Kind = resources.Count > 0 && resources.All(ElectricPropellants.Contains) ? "E"
                    : resources.Count == 1 && resources.Contains("MonoPropellant") ? "M" : "F";
            }
            PartType t = src.Type;
            if (t == null || !t.HasLooks) return null;

            // Size and shape.
            var profiles = (n.GetValue("bulkheadProfiles") ?? "").Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            foreach (string p in profiles)
                if (RoundSizes.TryGetValue(p, out double d)) { src.Diameter = d; break; }
            var stackY = new List<float>();
            foreach (ConfigNode.Value v in n.values)
                if (v.name.StartsWith("node_stack_", StringComparison.Ordinal)) stackY.Add(Vec(v.value).y);
            string[] rules = (n.GetValue("attachRules") ?? "").Split(',');
            bool surface = rules.Length > 1 && rules[1].Trim() == "1";
            bool inline = src.Diameter > 0 && stackY.Count > 0;

            if (t == PartTypes.Tank)
            {
                // Round stack tanks only, as for Replace: one round size, two stack nodes.
                if (stackY.Count < 2 || profiles.Count(RoundSizes.ContainsKey) != 1 || profiles.Any(p => !RoundSizes.ContainsKey(p) && p != "srf")) return null;
                src.Height = stackY.Max() - stackY.Min();
                return src.Height > 0 && src.Diameter > 0 ? src : null;
            }
            if (t.HasSideMount && !inline)
            {
                if (!surface) return null;
                src.Side = true;
            }
            switch (src.Basis)
            {
                case LookScale.Diameter: return inline ? src : null;
                case LookScale.Mass: return src.Mass > 0 ? src : null;
                case LookScale.Output: return src.Output > 0 ? src : null;
                default: return src;
            }
        }

        static ConfigNode Build(UrlDir.UrlConfig u, Source src, string key, Dictionary<string, ConfigNode> templates)
        {
            PartType t = src.Type;
            ConfigNode orig = u.config;
            string name = orig.GetValue("name");
            ConfigNode c = orig.CreateCopy();
            c.SetValue("name", ChassisName(t, src.Side, name, key), true);
            c.SetValue("title", "PIP look: " + (orig.GetValue("title") ?? name), true);
            c.SetValue("manufacturer", "Phil's Iterative Parts", true);
            c.SetValue("category", "none", true);
            c.SetValue("TechHidden", "True", true);
            c.SetValue("TechRequired", "start", true);
            c.SetValue("entryCost", "0", true);

            foreach (ConfigNode m in c.GetNodes("MODULE"))
            {
                string mn = m.GetValue("name") ?? "";
                if (Stripped.Contains(mn) || (mn == "ModuleB9PartSwitch" && !Cosmetic(m)) || (t == PartTypes.Tank && TankModules.Contains(mn)))
                    c.RemoveNode(m);
                else if (mn == "ModulePartVariants")
                    foreach (ConfigNode variant in m.GetNodes("VARIANT"))
                    {
                        // Only the paint stays: no node moves, no extra mass or cost.
                        variant.RemoveNodes("NODES");
                        variant.RemoveValue("mass");
                        variant.RemoveValue("cost");
                    }
            }
            foreach (ConfigNode r in c.GetNodes("RESOURCE"))
                if (!Keeps(t, r.GetValue("name"))) c.RemoveNode(r);

            // Every model gets a name, so the size switch can scale it.
            List<ConfigNode> models = c.GetNodes("MODEL").ToList();
            if (models.Count == 0)
            {
                ConfigNode mn = MeshToModel(u, c);
                if (mn == null) return null;
                models.Add(mn);
            }
            var positions = new List<Vector3>();
            for (int i = 0; i < models.Count; i++)
            {
                models[i].SetValue("name", "PIPModel" + i, true);
                positions.Add(Vec(models[i].GetValue("position")));
            }

            if (t == PartTypes.Probe) AddProbeParts(c);
            if (t == PartTypes.Rcs) SetRcsPropellant(c, key);
            string parentId = null;
            if (t == PartTypes.Tank)
            {
                // Propellants from PIP's own tank of this kind; empty mass from the program.
                if (!templates.TryGetValue("PIP-TNK-" + key, out ConfigNode tank)) return null;
                foreach (ConfigNode m in tank.GetNodes("MODULE"))
                {
                    string mn = m.GetValue("name");
                    if (mn == "ModuleCryoTank" || (mn == "ModuleB9PartSwitch" && m.GetValue("moduleID") == "fuelSwitch")) c.AddNode(m.CreateCopy());
                }
                c.SetValue("mass", "0.001", true);
                parentId = "fuelSwitch";
            }

            // Stack nodes, to move with the size.
            var nodes = new List<KeyValuePair<string, Vector3>>();
            foreach (ConfigNode.Value v in orig.values)
                if (v.name.StartsWith("node_stack_", StringComparison.Ordinal))
                    nodes.Add(new KeyValuePair<string, Vector3>(v.name.Substring("node_stack_".Length), Vec(v.value)));

            List<Subtype> subtypes = Subtypes(src, key, templates);
            if (subtypes.Count == 0) return null;
            ConfigNode pip = c.AddNode("MODULE");
            pip.AddValue("name", nameof(ModulePipPart));
            pip.AddValue("typeId", t.Id);
            // No "model": the program sets it, and B9PartSwitch reloads this config
            // when the size changes, which would blank it.
            pip.AddValue("look", "True");
            pip.AddValue("lookKind", src.Kind);
            if (subtypes.Count == 1)
            {
                // One size (side-mount wheels and intakes, omni antennas, RCS): no switch.
                pip.AddValue("variant", subtypes[0].Code);
                pip.AddValue("lookScale", subtypes[0].XZ.ToString("R", Inv));
                if (Math.Abs(subtypes[0].XZ - 1) > 1e-6 || Math.Abs(subtypes[0].Y - 1) > 1e-6)
                    AddSwitch(c, t, subtypes, positions, nodes, parentId);
            }
            else AddSwitch(c, t, subtypes, positions, nodes, parentId);
            return c;
        }

        // An engine look: the engine as it is (no size switch), burning the program's
        // propellants, with PIP's engine module. Its own switches stay if they only
        // change meshes, paint or nodes, not the engine or what it holds.
        static ConfigNode BuildEngine(UrlDir.UrlConfig u, Source src, string key)
        {
            PartType t = src.Type;
            ConfigNode orig = u.config;
            string name = orig.GetValue("name");
            ConfigNode c = orig.CreateCopy();
            c.SetValue("name", ChassisName(t, false, name, key), true);
            c.SetValue("title", "PIP look: " + (orig.GetValue("title") ?? name), true);
            c.SetValue("manufacturer", "Phil's Iterative Parts", true);
            c.SetValue("category", "none", true);
            c.SetValue("TechHidden", "True", true);
            c.SetValue("TechRequired", "start", true);
            c.SetValue("entryCost", "0", true);

            List<ConfigNode> engines = c.GetNodes("MODULE").Where(IsEngineModule).ToList();
            if (t == PartTypes.Nuclear)
            {
                // One hydrogen mode: drop an oxygen boost or a liquid-fuel mode, and
                // whatever else (plumes, heat) belonged to the dropped modes.
                ConfigNode keep = engines.FirstOrDefault(e => { var p = PropellantsOf(e); return p.Count == 1 && p.Contains("LqdHydrogen"); })
                                  ?? engines.FirstOrDefault(e => !PropellantsOf(e).Contains("Oxidizer")) ?? engines[0];
                var dropped = new HashSet<string>();
                foreach (ConfigNode e in engines)
                {
                    if (e == keep) continue;
                    string id = e.GetValue("engineID");
                    if (!string.IsNullOrEmpty(id)) dropped.Add(id);
                    c.RemoveNode(e);
                }
                foreach (ConfigNode m in c.GetNodes("MODULE"))
                {
                    if (m.GetValue("name") == "MultiModeEngine") { c.RemoveNode(m); continue; }
                    string id = m.GetValue("engineID") ?? m.GetValue("engineModuleID");
                    if (!IsEngineModule(m) && id != null && dropped.Contains(id)) c.RemoveNode(m);
                }
                SetPropellants(keep, new[] { new[] { "LqdHydrogen", "1" } });
            }
            if (t == PartTypes.Liquid)
                foreach (ConfigNode e in engines) SetPropellants(e, FuelPropellants[key]);

            foreach (ConfigNode m in c.GetNodes("MODULE"))
            {
                string mn = m.GetValue("name") ?? "";
                if (Stripped.Contains(mn)) { c.RemoveNode(m); continue; }
                if (mn == "ModuleB9PartSwitch")
                {
                    bool changesEngine = m.GetNodes("SUBTYPE").Any(s => s.HasNode("MODULE") || s.HasNode("RESOURCE") || s.HasValue("tankType"));
                    if (changesEngine) c.RemoveNode(m);
                    else foreach (ConfigNode s in m.GetNodes("SUBTYPE")) { s.RemoveValue("addedMass"); s.RemoveValue("addedCost"); }
                }
                else if (mn == "ModulePartVariants")
                    foreach (ConfigNode variant in m.GetNodes("VARIANT")) { variant.RemoveValue("mass"); variant.RemoveValue("cost"); }
            }

            // Only a solid keeps a resource: the program's load for its size and length.
            foreach (ConfigNode r in c.GetNodes("RESOURCE"))
                if (!(t == PartTypes.Solid && r.GetValue("name") == "SolidFuel")) c.RemoveNode(r);
            if (t == PartTypes.Solid)
            {
                string[] k = src.Kind.Split('-');
                string units = SolidMotors.UnitsFor(k[0], k[1]).ToString("R", Inv);
                ConfigNode fuel = c.GetNodes("RESOURCE").FirstOrDefault();
                if (fuel == null) return null;
                fuel.SetValue("amount", units, true);
                fuel.SetValue("maxAmount", units, true);
            }

            ConfigNode pip = c.AddNode("MODULE");
            pip.AddValue("name", nameof(ModulePipEngine));
            pip.AddValue("typeId", t.Id);
            pip.AddValue("look", "True");
            pip.AddValue("lookKind", src.Kind);
            pip.AddValue("lookFuel", src.Fuel);
            return c;
        }

        static void SetPropellants(ConfigNode engine, string[][] props)
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

        sealed class Subtype
        {
            public string Code, Title, Upgrade, Volume;
            public double XZ = 1, Y = 1;    // model scale across and along
            public bool Default;
            public int NodeSize = -1;
        }

        // The sizes a look comes in, and how much its model is scaled for each.
        static List<Subtype> Subtypes(Source src, string key, Dictionary<string, ConfigNode> templates)
        {
            PartType t = src.Type;
            var list = new List<Subtype>();
            string[] model = t.LookModel(src.Kind, key);
            Option[] options = t.VariantsFor(model, src.Side);
            if (options == null || options.Length == 0)
            {
                list.Add(new Subtype { Code = "", Title = "", Default = true });
                return list;
            }
            ConfigNode template = src.Side ? null : SizeSwitchOf(templates, Template(t, src.Kind, key));
            string def = src.Side ? t.DefaultSideVariant(model) : t.DefaultVariant(model);
            foreach (Option o in options)
            {
                ConfigNode ts = template?.GetNodes("SUBTYPE").FirstOrDefault(s => s.GetValue("name") == o.Code);
                var s = new Subtype
                {
                    Code = o.Code, Title = ts?.GetValue("title") ?? o.Label, Upgrade = ts?.GetValue("upgradeRequired"),
                    Volume = ts?.GetValue("volumeAddedToParent"),
                    Default = ts != null ? ts.HasValue("defaultSubtypePriority") : o.Code == def,
                };
                ConfigNode tn = ts?.GetNode("NODE");
                if (tn != null && int.TryParse(tn.GetValue("size") ?? "", NumberStyles.Integer, Inv, out int size)) s.NodeSize = size;
                switch (src.Basis)
                {
                    case LookScale.Diameter:
                    {
                        int i = Sizes.IndexOf(o.Code);
                        if (i < 0) continue;
                        s.XZ = s.Y = Sizes.Diameters[i] / src.Diameter;
                        if (s.NodeSize < 0 && NodeSizes.TryGetValue(o.Code, out int ns)) s.NodeSize = ns;
                        break;
                    }
                    case LookScale.Mass:
                        s.XZ = s.Y = Math.Pow(t.NominalMass(o.Code) / src.Mass, 1 / 3.0);
                        break;
                    case LookScale.Output:
                        s.XZ = s.Y = Math.Sqrt(t.NominalOutput(o.Code) / src.Output);
                        break;
                    case LookScale.Tank:
                    {
                        // "125-L": a 1.25 m tank as long as an FL-T400, stretched from the look's own shape.
                        string[] p = o.Code.Split('-');
                        int si = Sizes.IndexOf(p[0]), li = p.Length > 1 ? Array.IndexOf(FuelTanks.LengthCodes, p[1]) : -1;
                        if (si < 0 || li < 0) continue;
                        double d = Sizes.Diameters[si];
                        s.XZ = d / src.Diameter;
                        s.Y = FuelTanks.LongAspect * d * FuelTanks.LengthFactor[li] / src.Height;
                        if (s.NodeSize < 0 && NodeSizes.TryGetValue(p[0], out int ns)) s.NodeSize = ns;
                        break;
                    }
                }
                list.Add(s);
            }
            if (list.Count > 0 && !list.Any(x => x.Default)) list[0].Default = true;
            return list;
        }

        static ConfigNode SizeSwitchOf(Dictionary<string, ConfigNode> templates, string chassis) =>
            chassis != null && templates.TryGetValue(chassis, out ConfigNode c)
                ? c.GetNodes("MODULE").FirstOrDefault(m => m.GetValue("name") == "ModuleB9PartSwitch" && m.GetValue("moduleID") == "pipSize")
                : null;

        static void AddSwitch(ConfigNode c, PartType t, List<Subtype> subtypes, List<Vector3> positions,
                              List<KeyValuePair<string, Vector3>> nodes, string parentId)
        {
            ConfigNode sw = c.AddNode("MODULE");
            sw.AddValue("name", "ModuleB9PartSwitch");
            sw.AddValue("moduleID", "pipSize");
            sw.AddValue("switcherDescription", t.VariantName);
            if (parentId != null) sw.AddValue("parentID", parentId);
            foreach (Subtype s in subtypes)
            {
                ConfigNode st = sw.AddNode("SUBTYPE");
                st.AddValue("name", s.Code);
                st.AddValue("title", s.Title);
                if (s.Default) st.AddValue("defaultSubtypePriority", "1");
                if (!string.IsNullOrEmpty(s.Upgrade)) st.AddValue("upgradeRequired", s.Upgrade);
                if (!string.IsNullOrEmpty(s.Volume)) st.AddValue("volumeAddedToParent", s.Volume);
                var scale = new Vector3((float)s.XZ, (float)s.Y, (float)s.XZ);
                for (int i = 0; i < positions.Count; i++)
                {
                    ConfigNode tr = st.AddNode("TRANSFORM");
                    tr.AddValue("name", "PIPModel" + i);
                    tr.AddValue("scaleOffset", $"{N(scale.x)}, {N(scale.y)}, {N(scale.z)}");
                    Vector3 p = Vector3.Scale(positions[i], scale - Vector3.one);
                    if (p.sqrMagnitude > 1e-10f) tr.AddValue("positionOffset", $"{N(p.x)}, {N(p.y)}, {N(p.z)}");
                }
                foreach (KeyValuePair<string, Vector3> n in nodes)
                {
                    ConfigNode nn = st.AddNode("NODE");
                    nn.AddValue("name", n.Key);
                    Vector3 p = Vector3.Scale(n.Value, scale);
                    nn.AddValue("position", $"{N(p.x)}, {N(p.y)}, {N(p.z)}");
                    if (s.NodeSize >= 0) nn.AddValue("size", s.NodeSize.ToString(Inv));
                }
                ConfigNode mod = st.AddNode("MODULE");
                mod.AddNode("IDENTIFIER").AddValue("name", nameof(ModulePipPart));
                ConfigNode data = mod.AddNode("DATA");
                data.AddValue("variant", s.Code);
                data.AddValue("lookScale", s.XZ.ToString("R", Inv));
            }
        }

        // Probe cores get what PIP's own chassis has: SAS, a reaction wheel for the
        // Torque category and a battery.
        static void AddProbeParts(ConfigNode c)
        {
            if (!c.GetNodes("MODULE").Any(m => m.GetValue("name") == "ModuleSAS"))
            {
                ConfigNode sas = c.AddNode("MODULE");
                sas.AddValue("name", "ModuleSAS");
                sas.AddValue("SASServiceLevel", "0");
            }
            if (!c.GetNodes("MODULE").Any(m => m.GetValue("name") == "ModuleReactionWheel"))
            {
                ConfigNode w = c.AddNode("MODULE");
                w.AddValue("name", "ModuleReactionWheel");
                w.AddValue("PitchTorque", "1");
                w.AddValue("YawTorque", "1");
                w.AddValue("RollTorque", "1");
                ConfigNode r = w.AddNode("RESOURCE");
                r.AddValue("name", "ElectricCharge");
                r.AddValue("rate", "0.03");
            }
            if (!c.GetNodes("RESOURCE").Any(r => r.GetValue("name") == "ElectricCharge"))
            {
                ConfigNode r = c.AddNode("RESOURCE");
                r.AddValue("name", "ElectricCharge");
                r.AddValue("amount", "50");
                r.AddValue("maxAmount", "50");
            }
        }

        // RCS looks burn the program's propellant: monopropellant, or kerolox (B).
        static void SetRcsPropellant(ConfigNode c, string key)
        {
            foreach (ConfigNode m in c.GetNodes("MODULE"))
            {
                if (!(m.GetValue("name") ?? "").StartsWith("ModuleRCS", StringComparison.Ordinal)) continue;
                m.RemoveNodes("PROPELLANT");
                bool bi = key == "B";
                m.SetValue("resourceName", bi ? "LiquidFuel" : "MonoPropellant", true);
                foreach (KeyValuePair<string, string> p in bi
                             ? new[] { new KeyValuePair<string, string>("LiquidFuel", "0.9"), new KeyValuePair<string, string>("Oxidizer", "1.1") }
                             : new[] { new KeyValuePair<string, string>("MonoPropellant", "1") })
                {
                    ConfigNode pn = m.AddNode("PROPELLANT");
                    pn.AddValue("name", p.Key);
                    pn.AddValue("ratio", p.Value);
                }
            }
        }

        // The resources a look keeps: only what the program sets.
        static bool Keeps(PartType t, string resource)
        {
            if (t == PartTypes.Battery || t == PartTypes.Probe) return resource == "ElectricCharge";
            if (t == PartTypes.Shield) return resource == "Ablator";
            if (t == PartTypes.Intake) return resource == "IntakeAir";
            return false;
        }

        static bool Cosmetic(ConfigNode b9)
        {
            foreach (ConfigNode s in b9.GetNodes("SUBTYPE"))
            {
                foreach (ConfigNode.Value v in s.values) if (!CosmeticValues.Contains(v.name)) return false;
                foreach (ConfigNode n in s.GetNodes()) if (!CosmeticNodes.Contains(n.name)) return false;
            }
            return true;
        }

        static Vector3 Vec(string text)
        {
            if (string.IsNullOrEmpty(text)) return Vector3.zero;
            string[] p = text.Split(',');
            float F(int i) => i < p.Length && float.TryParse(p[i].Trim(), NumberStyles.Float, Inv, out float f) ? f : 0f;
            return new Vector3(F(0), F(1), F(2));
        }

        static string N(double v) => Math.Round(v, 6).ToString(Inv);

        #endregion

        #region In game

        public sealed class Look
        {
            public string Source;   // the installed part, as KSP names it
            public string Title;
            public string Kind;
            public string Fuel;     // liquid engines: the fuel it burns as installed
            public bool Side;
        }

        static readonly Dictionary<string, List<Look>> lists = new Dictionary<string, List<Look>>();
        static readonly Dictionary<string, string> kinds = new Dictionary<string, string>();

        // The looks that suit a program (inline or side-mount), by title. The parts
        // list doesn't change after loading, so each type's list is built once.
        public static List<Look> List(PartType t, bool side, string[] model)
        {
            if (model == null || !t.HasLooks || PartLoader.Instance == null) return new List<Look>();
            string key = t.LookKey(model);
            string cacheKey = t.Id + "/" + key + (side ? "/side" : "");
            if (!lists.TryGetValue(cacheKey, out List<Look> all))
            {
                all = new List<Look>();
                string inline = Prefix + t.ChassisCode + (key == "" ? "" : "-" + key) + "-", sidePrefix = inline + "R-";
                foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                {
                    if (ap?.name == null || !ap.name.StartsWith(inline, StringComparison.Ordinal)) continue;
                    bool isSide = ap.name.StartsWith(sidePrefix, StringComparison.Ordinal);
                    if (isSide != side) continue;
                    string source = ap.name.Substring((side ? sidePrefix : inline).Length);
                    all.Add(new Look
                    {
                        Source = source, Title = Title(source), Kind = KindOf(t, ap), Side = side,
                        Fuel = ap.partPrefab?.FindModuleImplementing<ModulePipEngine>()?.lookFuel ?? "",
                    });
                }
                all.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
                lists[cacheKey] = all;
            }
            return all.Where(l => t.LookFits(model, l.Kind)).ToList();
        }

        // What a look chassis is: written on it at load, except an RCS block's
        // nozzle count, which is only known once its model is loaded.
        static string KindOf(PartType t, AvailablePart chassis)
        {
            if (chassis?.partPrefab == null) return "";
            if (kinds.TryGetValue(chassis.name, out string kind)) return kind;
            kind = chassis.partPrefab.FindModuleImplementing<ModulePipPart>()?.lookKind
                   ?? chassis.partPrefab.FindModuleImplementing<ModulePipEngine>()?.lookKind ?? "";
            if (t == PartTypes.Rcs)
            {
                ModuleRCS rcs = chassis.partPrefab.FindModuleImplementing<ModuleRCS>();
                kind = rcs == null ? "" : chassis.partPrefab.FindModelTransforms(rcs.thrusterTransformName).Length.ToString(Inv);
            }
            kinds[chassis.name] = kind;
            return kind;
        }

        public static string Title(string source)
        {
            if (string.IsNullOrEmpty(source)) return "";
            AvailablePart ap = PartLoader.getPartInfoByName(source);
            return ap?.title ?? source;
        }

        // The look chassis a program uses for this look, if it's installed and suits it.
        public static string Chassis(PartType t, bool side, string source, string[] model)
        {
            if (string.IsNullOrEmpty(source) || model == null) return null;
            string name = ChassisName(t, side, source, t.LookKey(model));
            AvailablePart ap = PartLoader.getPartInfoByName(name);
            return ap != null && t.LookFits(model, KindOf(t, ap)) ? name : null;
        }

        public static bool Exists(PartType t, bool side, string source, string[] model) => Chassis(t, side, source, model) != null;

        // Whether a program with this look (empty: PIP's own model) can gimbal at all.
        // Some engines have a fixed nozzle; their Gimbal category does nothing.
        public static bool Gimbals(PartType t, string source, string[] model)
        {
            string chassis = (string.IsNullOrEmpty(source) ? null : Chassis(t, false, source, model)) ?? PartFactory.ChassisFor(t, model);
            Part prefab = PartFactory.Info(chassis)?.partPrefab;
            return prefab == null || prefab.FindModuleImplementing<ModuleGimbal>() != null;
        }


        // The side-mount look a program gets when none was picked: the type's
        // preferred ones first, then the first by title. Null when none is installed.
        public static string DefaultSide(PartType t, string[] model)
        {
            if (!t.HasSideMount) return null;
            foreach (string s in t.SideLookPreference(model)) if (Exists(t, true, s, model)) return s;
            return List(t, true, model).FirstOrDefault()?.Source;
        }

        #endregion
    }

    // After Module Manager, before KSP compiles the parts: make the look chassis.
    [HarmonyPatch(typeof(PartLoader), nameof(PartLoader.StartLoad))]
    static class MakeLooksPatch
    {
        static void Prefix()
        {
            // Looks need Module Manager and B9PartSwitch; with a required mod missing PIP stays off.
            if (!Requirements.Ok) return;
            try { Looks.MakeConfigs(); }
            catch (Exception e) { Debug.LogError("[PIP] Making looks failed: " + e); }
        }
    }
}
