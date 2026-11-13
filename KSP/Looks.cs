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
        // mount: "" in-line, "R" side-mount, "A" adapter (PartType.MountOf).
        public static string ChassisName(PartType t, bool side, string source, string key = "") => ChassisName(t, side ? "R" : "", source, key);
        public static string ChassisName(PartType t, string mount, string source, string key = "") =>
            Prefix + t.ChassisCode + (string.IsNullOrEmpty(key) ? "" : "-" + key) + (string.IsNullOrEmpty(mount) ? "-" : "-" + mount + "-") + (source ?? "").Replace('_', '.');

        #region Making the look chassis (at load)

        // Looks also come from 6.25 and 7.5 m parts, scaled down to PIP's sizes.
        static readonly Dictionary<string, double> RoundSizes = new Dictionary<string, double>
        {
            { "size0", 0.625 }, { "size1", 1.25 }, { "size1p5", 1.875 }, { "size2", 2.5 },
            { "size3", 3.75 }, { "size4", 5 }, { "size5", 6.25 }, { "size6", 7.5 },
        };
        static readonly Dictionary<string, int> NodeSizes = new Dictionary<string, int>
            { { "0625", 0 }, { "125", 1 }, { "1875", 1 }, { "25", 2 }, { "375", 3 }, { "5", 4 } };
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
            public string Fuel = "";        // a liquid engine's own fuel (a fuel table code), to list same-fuel looks first
            public double Diameter, Mass, Output, Height;
            public double Volume;           // a radial tank's, in units of tank volume (an FL-T400 is 400)
            public bool Adapter;            // an adapter tank's look (DESIGN.md 20.2): Diameter is its big end
            public LookScale Basis => Type.ScaleBasis(Side, Kind);
            public string Mount => Side ? "R" : Adapter ? "A" : "";
        }

        static string Localized(string text)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '#') return text ?? "";
            try { return KSP.Localization.Localizer.Format(text); }
            catch { return text; }
        }

        static float Height(List<NodeAt> stack) => stack.Max(v => v.Y) - stack.Min(v => v.Y);

        // A plane fuselage (DESIGN.md 21.2): a fuel tank of one plane section (Mk2 or
        // Mk3), straight along the axis. Its kind is the section, "2" or "3". Parts
        // joining two shapes are plane adapters, not fuselages.
        static Source ClassifyPlane(PartFacts f, Source src)
        {
            List<NodeAt> stack = f.StackNodes;
            if (FuelTanks.KeptTank(f.Name, f.Title)) return null;
            if (stack.Count < 2) return null;
            string[] sections = f.Profiles.Where(InstalledParts.IsPlaneSection).Distinct().ToArray();
            // Two ends: a plane adapter (a tank adapter of its pair, shaped as it is:
            // Mk3 to Mk2 has its narrow end raised, the bicoupler two of them).
            string planeAdapter = FuelTanks.PlaneAdapterKindOf(f.Profiles, InstalledParts.TwinEnd(stack));
            if (planeAdapter != null)
            {
                src.Adapter = true;
                src.Kind = planeAdapter;
                src.Height = Height(stack);
                return src.Height > 0 ? src : null;
            }
            if (src.Kind == "" || sections.Length != 1 || f.Profiles.Any(p => p != sections[0] && p != "srf")) return null;
            if (!InstalledParts.OnAxis(stack)) return null;
            src.Type = PartTypes.Plane;
            src.Kind = sections[0] == "mk3" ? "3" : "2";
            src.Height = Height(stack);
            return src.Height > 0 ? src : null;
        }

        // An adapter (DESIGN.md 20.2): two round sizes of a ratio PIP makes, every
        // stack node on the axis (not slanted, not a multi-adapter), not a part kept
        // on purpose, and not so flat that stretching it would wreck it (a cap).
        static Source ClassifyAdapter(PartFacts f, Source src)
        {
            List<NodeAt> stack = f.StackNodes;
            if (f.Profiles.Any(p => !RoundSizes.ContainsKey(p) && p != "srf")) return null;
            if (stack.Count < 2 || !InstalledParts.OnAxis(stack)) return null;
            if (FuelTanks.KeptTank(f.Name, f.Title)) return null;
            List<double> ends = f.Profiles.Where(RoundSizes.ContainsKey).Select(p => RoundSizes[p]).Distinct().ToList();
            double big = ends.Max(), small = ends.Min();
            string kind = FuelTanks.AdapterKindOf(big, small);
            if (kind == null) return null;
            src.Adapter = true;
            src.Kind = kind;
            src.Diameter = big;
            src.Height = Height(stack);
            return src.Height >= 0.25 * FuelTanks.AdapterHeight(kind, big) ? src : null;
        }

        // A tank's volume in units of tank volume (an FL-T400 is 400): its fuel
        // switch's base volume, or what it holds at PIP's units per volume. Stock
        // LF and Ox are 5 L units, one per unit of volume; Community Resource Pack
        // fuels are 1 L units, five per unit of volume.
        static readonly Dictionary<string, double> UnitsPerVolume = new Dictionary<string, double>
        {
            { "LiquidFuel", 1 }, { "Oxidizer", 1 }, { "MonoPropellant", 1.5 }, { "XenonGas", 15.75 },
            { "ArgonGas", 1000 }, { "Lithium", 5 }, { "LqdHydrogen", 7.5 }, { "LqdMethane", 5 },
        };
        static double TankVolume(ConfigNode n)
        {
            ConfigNode sw = FuelSwitchOf(n);
            if (sw != null && double.TryParse(sw.GetValue("baseVolume") ?? "", NumberStyles.Float, Inv, out double b) && b > 0) return b;
            double v = 0;
            foreach (ConfigNode r in n.GetNodes("RESOURCE"))
            {
                if (!double.TryParse(r.GetValue("maxAmount") ?? "", NumberStyles.Float, Inv, out double units)) continue;
                v += units / (UnitsPerVolume.TryGetValue(r.GetValue("name") ?? "", out double per) ? per : 5);
            }
            return v;
        }

        // A B9PartSwitch that switches propellants, whatever its ID: CryoTanks'
        // "fuelSwitch", Near Future Exploration's "surfaceSwitch" (the MLT tanks,
        // missed until 2026-11-06). One whose subtypes name tank types holding only
        // rocket propellants; an ore or supplies switch doesn't count. B9 only puts
        // the resources on parts in the VAB, so the switch marks a tank.
        internal static ConfigNode FuelSwitchOf(ConfigNode part) =>
            part?.GetNodes("MODULE").FirstOrDefault(m => m.GetValue("name") == "ModuleB9PartSwitch" &&
                m.GetNodes("SUBTYPE").Any(s => IsPropellantTankType(s.GetValue("tankType"))));

        static readonly Dictionary<string, bool> propellantTankTypes = new Dictionary<string, bool>();
        static bool IsPropellantTankType(string name)
        {
            if (string.IsNullOrEmpty(name) || GameDatabase.Instance == null) return false;
            if (propellantTankTypes.TryGetValue(name, out bool ok)) return ok;
            ConfigNode type = GameDatabase.Instance.GetConfigNodes("B9_TANK_TYPE").FirstOrDefault(n => n.GetValue("name") == name);
            string[] held = type?.GetNodes("RESOURCE").Select(r => r.GetValue("name") ?? "").ToArray() ?? new string[0];
            return propellantTankTypes[name] = held.Length > 0 && held.All(InstalledParts.IsTankResource);
        }

        // The program's fuel as engine propellants, from the fuel table.
        static string[][] FuelPropellants(string code)
        {
            FuelDef f = FuelTable.Find(code) ?? FuelTable.All[0];
            return f.Resources.Select((r, i) => new[] { r, f.Ratios[i].ToString("R", Inv) }).ToArray();
        }

        static bool IsEngineModule(ConfigNode m) => (m.GetValue("name") ?? "").StartsWith("ModuleEngines", StringComparison.Ordinal);
        static HashSet<string> PropellantsOf(ConfigNode engine) =>
            new HashSet<string>(engine.GetNodes("PROPELLANT").Select(p => p.GetValue("name") ?? ""));

        // An engine look (its type is InstalledParts.TypeOf, as for Replace). Engines
        // are never scaled: a look is an engine at its own size, and its kind says which.
        static Source ClassifyEngine(ConfigNode n, PartFacts f, PartType t)
        {
            List<HashSet<string>> props = f.EngineModes;
            // Several modes only where the type has them (jets), or where one can be kept (nuclear).
            if (props.Count > 1 && t != PartTypes.Jet && t != PartTypes.Nuclear) return null;

            double diameter = 0;
            foreach (string p in f.Profiles)
                if (RoundSizes.TryGetValue(p, out double d)) { diameter = d; break; }
            int si = Array.FindIndex(Sizes.Diameters, d => Math.Abs(d - diameter) < 1e-3);
            if (si < 0 || f.StackNodes.Count == 0) return null;
            string size = Sizes.Codes[si];

            var src = new Source { Type = t, Engine = true, Diameter = diameter };
            if (t == PartTypes.Liquid)
            {
                string label = (n.GetValue("name") + " " + n.GetValue("title")).ToLowerInvariant();
                src.Kind = size + (label.Contains("aerospike") ? "-A" : "");
                src.Fuel = FuelTable.ForResources(props[0])?.Code ?? (props[0].Contains("LqdHydrogen") ? "H" : props[0].Contains("LqdMethane") ? "M" : "K");
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
                string gas = props[0].FirstOrDefault(InstalledParts.ElectricGases.Contains);
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
            if (t == PartTypes.Decoupler) return "PIP-DEC-" + key;
            if (t == PartTypes.Plane) return "PIP-PLN-" + key + "-" + kind;
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
                    string look = ChassisName(src.Type, src.Mount, name, key);
                    if (names.Contains(look)) continue;   // the database was reloaded
                    try
                    {
                        ConfigNode c = src.Engine ? BuildEngine(u, src, key) : Build(u, src, key, templates);
                        if (c == null) { skipped++; continue; }
                        u.parent.AddConfig(c);
                        names.Add(look);
                        string label = src.Type.Id + (src.Side ? " side" : src.Adapter ? " adapter" : "");
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

        // The part as the sorting rules need it, from its config (EditorHider reads
        // the same facts off the compiled part).
        static PartFacts FactsOf(ConfigNode n)
        {
            ConfigNode[] modules = n.GetNodes("MODULE");
            int.TryParse(n.GetValue("CrewCapacity") ?? "0", NumberStyles.Integer, Inv, out int crew);
            string[] rules = (n.GetValue("attachRules") ?? "").Split(',');
            var f = new PartFacts
            {
                Name = n.GetValue("name") ?? "", Title = Localized(n.GetValue("title")), Category = n.GetValue("category") ?? "",
                Crew = crew, FuelSwitch = FuelSwitchOf(n) != null, Profiles = PartFacts.ProfilesOf(n.GetValue("bulkheadProfiles")),
                SurfaceAttach = rules.Length > 1 && rules[1].Trim() == "1",
            };
            foreach (ConfigNode m in modules) f.Modules.Add(m.GetValue("name") ?? "");
            foreach (ConfigNode r in n.GetNodes("RESOURCE")) f.Resources.Add(r.GetValue("name") ?? "");
            foreach (ConfigNode e in modules.Where(IsEngineModule)) f.EngineModes.Add(PropellantsOf(e));
            ConfigNode rcs = modules.FirstOrDefault(m => (m.GetValue("name") ?? "").StartsWith("ModuleRCS", StringComparison.Ordinal));
            if (rcs != null)
            {
                f.RcsPropellants = new HashSet<string>(rcs.GetNodes("PROPELLANT").Select(p => p.GetValue("name") ?? ""));
                if (f.RcsPropellants.Count == 0 && !string.IsNullOrEmpty(rcs.GetValue("resourceName"))) f.RcsPropellants.Add(rcs.GetValue("resourceName"));
            }
            foreach (ConfigNode.Value v in n.values)
                if (v.name.StartsWith("node_stack_", StringComparison.Ordinal))
                {
                    Vector3 at = Vec(v.value);
                    f.StackNodes.Add(new NodeAt(at.x, at.y, at.z));
                }
            return f;
        }

        // What a part is comes from InstalledParts.TypeOf, the same rules as Replace;
        // then the shapes a look can come from, and what it is as a look (its kind).
        // Side: attaches to a surface rather than in a stack.
        static Source Classify(ConfigNode n)
        {
            if (string.Equals(n.GetValue("TechHidden"), "True", StringComparison.OrdinalIgnoreCase)) return null;
            if (n.GetValue("category") == "none") return null;
            PartFacts f = FactsOf(n);
            PartType t = InstalledParts.TypeOf(f);
            if (t == null || !t.HasLooks) return null;
            if (f.EngineModes.Count > 0) return ClassifyEngine(n, f, t);
            ConfigNode[] moduleNodes = n.GetNodes("MODULE");
            ConfigNode Module(string name) => moduleNodes.FirstOrDefault(m => m.GetValue("name") == name);
            double Num(ConfigNode node, string key) =>
                node != null && double.TryParse(node.GetValue(key) ?? "", NumberStyles.Float, Inv, out double v) ? v : 0;

            var src = new Source { Type = t };
            double.TryParse(n.GetValue("mass") ?? "0", NumberStyles.Float, Inv, out src.Mass);
            // Radial decouplers are kind "R".
            if (t == PartTypes.Decoupler) src.Kind = f.Modules.Contains("ModuleAnchoredDecoupler") ? "R" : "";
            else if (t == PartTypes.Solar)
            {
                if (Module("ModuleDeployableSolarPanel") is ConfigNode panel)
                {
                    src.Kind = string.IsNullOrEmpty(panel.GetValue("animationName")) ? "F" : "D";
                    src.Output = Num(panel, "chargeRate");
                }
                else
                {
                    ConfigNode curved = Module("ModuleCurvedSolarPanel");
                    src.Kind = string.IsNullOrEmpty(curved.GetValue("DeployAnimation")) ? "F" : "D";
                    src.Output = Num(curved, "TotalEnergyRate");
                }
            }
            else if (t == PartTypes.Antenna)
            {
                ConfigNode rt = Module("ModuleRTAntenna"), dt = Module("ModuleDataTransmitter");
                if (rt != null) src.Kind = Num(rt, "Mode1DishRange") > 0 ? "D" : "O";
                else src.Kind = dt?.GetValue("antennaType") == "RELAY" || Num(dt, "antennaPower") > 1e9 ? "D" : "O";
            }
            // A plain structural adapter (kind "") is only ever an adapter tank's look.
            else if (t == PartTypes.Tank)
                src.Kind = !InstalledParts.IsFuelTank(f) ? ""
                    : f.Resources.Count > 0 && f.Resources.All(InstalledParts.ElectricGases.Contains) ? "E"
                    : f.Resources.Count == 1 && f.Resources.Contains("MonoPropellant") ? "M" : "F";

            // Size and shape.
            List<string> profiles = f.Profiles.ToList();
            foreach (string p in profiles)
                if (RoundSizes.TryGetValue(p, out double d)) { src.Diameter = d; break; }
            List<float> stackY = f.StackNodes.Select(p => p.Y).ToList();
            bool surface = f.SurfaceAttach;
            bool inline = src.Diameter > 0 && stackY.Count > 0;

            if (t == PartTypes.Tank)
            {
                // Mk2 and Mk3: a plane fuselage (DESIGN.md 21.2).
                if (profiles.Any(InstalledParts.IsPlaneSection)) return ClassifyPlane(f, src);
                // Two round sizes: an adapter (DESIGN.md 20.2), or nothing.
                if (profiles.Where(RoundSizes.ContainsKey).Distinct().Count() == 2) return ClassifyAdapter(f, src);
                if (src.Kind == "") return null;   // a structural part that isn't an adapter
                // Cluster tanks, nosecones and the like: not a look, as Replace leaves them.
                if (FuelTanks.KeptTank(f.Name, f.Title)) return null;
                // A radial tank (DESIGN.md 20.1): no stack nodes, sticks to a surface,
                // scaled by its volume.
                if (stackY.Count == 0)
                {
                    if (!surface || profiles.Any(p => p != "srf")) return null;
                    src.Side = true;
                    src.Volume = TankVolume(n);
                    return src.Volume > 0 ? src : null;
                }
                // Round stack tanks only, as for Replace: one round size, two stack nodes.
                if (stackY.Count < 2 || profiles.Count(RoundSizes.ContainsKey) != 1 || profiles.Any(p => !RoundSizes.ContainsKey(p) && p != "srf")) return null;
                src.Height = stackY.Max() - stackY.Min();
                return src.Height > 0 && src.Diameter > 0 ? src : null;
            }
            // A Mk2-shaped probe core (the Mk2 Drone Core, DESIGN.md 21.2): only ever its
            // own shape, unscaled, by the adapter mount ("Mk2" in the window).
            if (t == PartTypes.Probe && profiles.Any(InstalledParts.IsPlaneSection))
            {
                if (stackY.Count < 2 || profiles.Any(p => p != "mk2" && p != "srf")) return null;
                src.Adapter = true;
                src.Kind = "mk2";
                return src;
            }
            if (t.HasSideMount && !inline)
            {
                if (!surface) return null;
                src.Side = true;
            }
            // A radial decoupler goes on a side, a stack one in a stack, never the other way.
            if (t == PartTypes.Decoupler && (src.Kind == "R") != src.Side) return null;
            switch (src.Basis)
            {
                case LookScale.Diameter: return inline ? src : null;
                case LookScale.Mass: return src.Mass > 0 ? src : null;
                case LookScale.Output: return src.Output > 0 ? src : null;
                default: return src;
            }
        }

        // A look chassis starts as a copy of its part, renamed and hidden from the
        // parts list and R&D (engines and other parts alike).
        static ConfigNode CopyAsLook(ConfigNode orig, string lookName)
        {
            string name = orig.GetValue("name");
            ConfigNode c = orig.CreateCopy();
            c.SetValue("name", lookName, true);
            c.SetValue("title", "PIP look: " + (orig.GetValue("title") ?? name), true);
            c.SetValue("manufacturer", "Phil's Iterative Parts", true);
            c.SetValue("category", "none", true);
            c.SetValue("TechHidden", "True", true);
            c.SetValue("TechRequired", "start", true);
            c.SetValue("entryCost", "0", true);
            return c;
        }

        static ConfigNode Build(UrlDir.UrlConfig u, Source src, string key, Dictionary<string, ConfigNode> templates)
        {
            PartType t = src.Type;
            ConfigNode orig = u.config;
            ConfigNode c = CopyAsLook(orig, ChassisName(t, src.Mount, orig.GetValue("name"), key));

            foreach (ConfigNode m in c.GetNodes("MODULE"))
            {
                string mn = m.GetValue("name") ?? "";
                if (Stripped.Contains(mn) || (mn == "ModuleB9PartSwitch" && !Cosmetic(m)) || (t is TankKind && TankModules.Contains(mn)))
                    c.RemoveNode(m);
                else if (mn == "ModulePartVariants")
                {
                    // One drag shape: the size switch reshapes a look, so per-paint drag cubes
                    // aren't found (DESIGN.md 29.1).
                    m.SetValue("useMultipleDragCubes", "false", true);
                    foreach (ConfigNode variant in m.GetNodes("VARIANT"))
                    {
                        // Only the paint stays: no node moves, no extra mass or cost.
                        variant.RemoveNodes("NODES");
                        variant.RemoveValue("mass");
                        variant.RemoveValue("cost");
                    }
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
            if (t == PartTypes.Decoupler) SetDecouplerKind(c, key);
            string parentId = null;
            if (t is TankKind)
            {
                // Propellants from PIP's own tank of this kind; empty mass from the program.
                if (!templates.TryGetValue("PIP-TNK-" + key, out ConfigNode tank)) return null;
                foreach (ConfigNode m in tank.GetNodes("MODULE"))
                {
                    string mn = m.GetValue("name");
                    if (mn == nameof(ModulePipBoiloff) || (mn == "ModuleB9PartSwitch" && m.GetValue("moduleID") == "fuelSwitch"))
                    {
                        ConfigNode copy = m.CreateCopy();
                        // An adapter can also be empty: a plain structural adapter
                        // (DESIGN.md 20.2, decision 11), a quarter of a tank's weight.
                        if (src.Adapter && mn == "ModuleB9PartSwitch")
                        {
                            ConfigNode empty = copy.AddNode("SUBTYPE");
                            empty.AddValue("name", "Empty");
                            empty.AddValue("title", "Empty");
                            empty.AddValue("tankType", EmptyTankType);
                            // A new part with no resources gets B9PartSwitch's resourceless
                            // subtype (seen 2026-11-06: adapters came out Empty). The kind's
                            // first propellant on the chassis makes B9 pick that instead.
                            AddFirstPropellant(c, copy);
                        }
                        c.AddNode(copy);
                    }
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
            if (src.Adapter) SetAdapterNodeSizes(subtypes, orig);
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
            ConfigNode c = CopyAsLook(orig, ChassisName(t, false, orig.GetValue("name"), key));

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
            {
                foreach (ConfigNode e in engines) SetPropellants(e, FuelPropellants(key));
                FuelLoader.ApplyPlume(c, key);   // the fuel's Waterfall plume, if the model has Waterfall
            }

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

        static void SetPropellants(ConfigNode engine, string[][] props) => FuelLoader.SetPropellants(engine, props);

        sealed class Subtype
        {
            public string Code, Title, Upgrade, Volume;
            public double XZ = 1, Y = 1;    // model scale across and along
            public bool Default;
            public int NodeSize = -1;
            public Dictionary<string, int> NodeSizeById;   // an adapter's: each end its own
        }

        // PIP's resourceless tank type for an empty adapter (Tools\Generate-PartChassis.ps1).
        public const string EmptyTankType = "PIP-EMPTY";

        // The resources of a fuel switch's first subtype, as placeholders: B9PartSwitch
        // matches a new part's resources to a subtype, then sets their amounts itself.
        static void AddFirstPropellant(ConfigNode part, ConfigNode fuelSwitch)
        {
            string tankType = fuelSwitch.GetNodes("SUBTYPE").FirstOrDefault()?.GetValue("tankType");
            ConfigNode type = string.IsNullOrEmpty(tankType) ? null
                : GameDatabase.Instance.GetConfigNodes("B9_TANK_TYPE").FirstOrDefault(n => n.GetValue("name") == tankType);
            if (type == null) return;
            foreach (ConfigNode r in type.GetNodes("RESOURCE"))
            {
                ConfigNode res = part.AddNode("RESOURCE");
                res.AddValue("name", r.GetValue("name"));
                res.AddValue("amount", "1");
                res.AddValue("maxAmount", "1");
            }
        }

        // An adapter's two ends take their sizes from the size pair; any other node
        // (an NR-AD's inner ones) is scaled with the model. The small end is the
        // smaller node, or the top one when both say the same size: adapters are
        // drawn narrow end up (Making History gives both of its ends size 2).
        static void SetAdapterNodeSizes(List<Subtype> subtypes, ConfigNode orig)
        {
            var ids = new List<string>();
            var ys = new List<float>();
            var sizes = new List<int>();
            foreach (ConfigNode.Value v in orig.values)
            {
                if (!v.name.StartsWith("node_stack_", StringComparison.Ordinal)) continue;
                ids.Add(v.name.Substring("node_stack_".Length));
                ys.Add(Vec(v.value).y);
                string[] p = v.value.Split(',');
                sizes.Add(p.Length >= 7 && int.TryParse(p[6].Trim(), NumberStyles.Integer, Inv, out int z) ? z : -1);
            }
            if (ids.Count < 2) return;
            int top = ys.IndexOf(ys.Max()), bottom = ys.IndexOf(ys.Min());
            bool topSmall = sizes[top] < 0 || sizes[bottom] < 0 || sizes[top] <= sizes[bottom];
            foreach (Subtype s in subtypes)
            {
                string[] p = s.Code.Split('-');
                if (p.Length < 4 || Sizes.IndexOf(p[1]) < 0 || Sizes.IndexOf(p[2]) < 0) continue;
                double big = Sizes.Diameters[Sizes.IndexOf(p[1])], small = Sizes.Diameters[Sizes.IndexOf(p[2])];
                s.NodeSizeById = new Dictionary<string, int>();
                for (int i = 0; i < ids.Count; i++)
                    s.NodeSizeById[ids[i]] = i == top ? NodeSizeFor(topSmall ? small : big)
                        : i == bottom ? NodeSizeFor(topSmall ? big : small)
                        : sizes[i] >= 0 ? NodeSizeFor(Math.Max(0.625, sizes[i] * 1.25) * s.XZ) : -1;
            }
        }

        // KSP's node size for a diameter: 0.625 m is 0, 1.25 m 1, 2.5 m 2 ... (1.875 m is 1).
        static int NodeSizeFor(double d) => Math.Abs(d - 1.875) < 0.05 ? 1 : d < 0.94 ? 0 : (int)Math.Round(d / 1.25);

        // The sizes a look comes in, and how much its model is scaled for each.
        static List<Subtype> Subtypes(Source src, string key, Dictionary<string, ConfigNode> templates)
        {
            PartType t = src.Type;
            var list = new List<Subtype>();
            string[] model = t.LookModel(src.Kind, key);
            // An adapter look serves the size pairs of its own ratio.
            Option[] options = src.Adapter ? t.AdapterVariants(model)?.Where(o => t.AdapterKind(o.Code) == src.Kind).ToArray()
                : t.VariantsFor(model, src.Side);
            if (src.Adapter && (options == null || options.Length == 0)) return list;
            if (options == null || options.Length == 0)
            {
                list.Add(new Subtype { Code = "", Title = "", Default = true });
                return list;
            }
            ConfigNode template = src.Side || src.Adapter ? null : SizeSwitchOf(templates, Template(t, src.Kind, key));
            string def = src.Side ? t.DefaultSideVariant(model) : src.Adapter ? options[0].Code : t.DefaultVariant(model);
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
                    case LookScale.Volume:
                        // A radial tank: the look's own shape, scaled to hold the size's volume.
                        s.XZ = s.Y = Math.Pow(t.NominalVolume(o.Code) / src.Volume, 1 / 3.0);
                        if (string.IsNullOrEmpty(s.Volume)) s.Volume = t.NominalVolume(o.Code).ToString("R", Inv);
                        break;
                    case LookScale.Adapter:
                    {
                        // "A-25-125-L": the big end at 2.5 m, stretched to the ratio's height.
                        string[] p = o.Code.Split('-');
                        int bi = p.Length > 1 ? Sizes.IndexOf(p[1]) : -1;
                        double h = t.NominalHeight(o.Code);
                        if (bi < 0 || h <= 0 || src.Height <= 0) continue;
                        s.XZ = Sizes.Diameters[bi] / src.Diameter;
                        s.Y = h / src.Height;
                        s.Volume = t.NominalVolume(o.Code).ToString("R", Inv);
                        break;
                    }
                    case LookScale.Length:
                    {
                        // A plane fuselage: its own cross-section, stretched to the length.
                        double length = t.NominalLength(o.Code);
                        if (length <= 0 || src.Height <= 0) continue;
                        s.Y = length / src.Height;
                        if (string.IsNullOrEmpty(s.Volume) && t is TankKind tk) s.Volume = tk.TankVolume(o.Code).ToString("R", Inv);
                        break;
                    }
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
                    int size = s.NodeSizeById != null && s.NodeSizeById.TryGetValue(n.Key, out int own) ? own : s.NodeSize;
                    if (size >= 0) nn.AddValue("size", size.ToString(Inv));
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
            // The program sets the autopilot level; a look's own (an HECS2's 3) starts at 0.
            ConfigNode sas = c.GetNodes("MODULE").FirstOrDefault(m => m.GetValue("name") == "ModuleSAS");
            if (sas == null)
            {
                sas = c.AddNode("MODULE");
                sas.AddValue("name", "ModuleSAS");
            }
            sas.SetValue("SASServiceLevel", "0", true);
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

        // A stack decoupler's look fires as the program's kind: a separator lets go
        // of both sides (isOmniDecoupler), a decoupler of one.
        static void SetDecouplerKind(ConfigNode c, string key)
        {
            foreach (ConfigNode m in c.GetNodes("MODULE"))
                if (m.GetValue("name") == "ModuleDecouple") m.SetValue("isOmniDecoupler", key == "S" ? "true" : "false", true);
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
        public static List<Look> List(PartType t, bool side, string[] model) => List(t, side ? "R" : "", model, null);

        // mount: "" in-line, "R" side-mount, "A" adapter. An adapter program's looks
        // are the adapters of its size pair's ratio (every adapter look, with no variant).
        public static List<Look> List(PartType t, string mount, string[] model, string variant)
        {
            if (model == null || !t.HasLooks || PartLoader.Instance == null) return new List<Look>();
            mount = mount ?? "";
            string key = t.LookKey(model);
            string cacheKey = t.Id + "/" + key + "/" + mount;
            if (!lists.TryGetValue(cacheKey, out List<Look> all))
            {
                all = new List<Look>();
                string inline = Prefix + t.ChassisCode + (key == "" ? "" : "-" + key) + "-";
                foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                {
                    if (ap?.name == null || !ap.name.StartsWith(inline, StringComparison.Ordinal)) continue;
                    string rest = ap.name.Substring(inline.Length);
                    string own = rest.StartsWith("R-", StringComparison.Ordinal) ? "R" : rest.StartsWith("A-", StringComparison.Ordinal) ? "A" : "";
                    if (own != mount) continue;
                    string source = own == "" ? rest : rest.Substring(2);
                    all.Add(new Look
                    {
                        Source = source, Title = Title(source), Kind = KindOf(t, ap), Side = own == "R",
                        Fuel = ap.partPrefab?.FindModuleImplementing<ModulePipEngine>()?.lookFuel ?? "",
                    });
                }
                all.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
                lists[cacheKey] = all;
            }
            string ratio = mount == "A" && variant != null ? t.AdapterKind(variant) : null;
            return all.Where(l => t.LookFits(model, l.Kind) && (ratio == null || l.Kind == ratio)).ToList();
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
        public static string Chassis(PartType t, bool side, string source, string[] model) => Chassis(t, side ? "R" : "", source, model, null);

        // An adapter look must also be of the size pair's ratio.
        public static string Chassis(PartType t, string mount, string source, string[] model, string variant)
        {
            if (string.IsNullOrEmpty(source) || model == null) return null;
            string name = ChassisName(t, mount ?? "", source, t.LookKey(model));
            AvailablePart ap = PartLoader.getPartInfoByName(name);
            if (ap == null) return null;
            string kind = KindOf(t, ap);
            if (!t.LookFits(model, kind)) return null;
            return mount == "A" && variant != null && kind != t.AdapterKind(variant) ? null : name;
        }

        public static bool Exists(PartType t, bool side, string source, string[] model) => Chassis(t, side, source, model) != null;
        public static bool Exists(PartType t, string mount, string source, string[] model, string variant) => Chassis(t, mount, source, model, variant) != null;

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

        // The adapter look for a size pair when none was picked: the ratio's stock
        // adapter first (the C7 for 2:1), then the first by title. Null when no
        // adapter of that ratio is installed: the pair is padlocked.
        public static string DefaultAdapter(PartType t, string[] model, string variant)
        {
            if (!t.HasAdapters || !PartType.IsAdapter(variant)) return null;
            foreach (string s in t.AdapterLookPreference(variant)) if (Exists(t, "A", s, model, variant)) return s;
            return List(t, "A", model, variant).FirstOrDefault()?.Source;
        }

        // The look a side-mount or adapter program gets when none was picked.
        public static string DefaultMount(PartType t, string[] model, string variant) =>
            PartType.IsAdapter(variant) ? DefaultAdapter(t, model, variant) : DefaultSide(t, model);

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
            // The fuel table first: looks and engine chassis are made per fuel.
            try { FuelLoader.Load(); }
            catch (Exception e) { Debug.LogError("[PIP] Reading Fuels.cfg failed: " + e); }
            try { Looks.MakeConfigs(); }
            catch (Exception e) { Debug.LogError("[PIP] Making looks failed: " + e); }
            Debug.Log($"[PIP] Plumes: {FuelLoader.PlumesSwapped} Waterfall templates swapped to the engine's fuel; sizes measured for {Plumes.SizePerMeter.Count} templates");
        }
    }
}
