using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

[assembly: KSPAssembly("PhilsIterativeParts", 1, 0)]
[assembly: KSPAssemblyDependency("0Harmony", 0, 0)]

namespace PIP
{
    // The programs in this save, paying for new ones, and experience from recoveries.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames,
        GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class PipScenario : ScenarioModule
    {
        public static PipScenario Instance { get; private set; }

        public readonly List<Program> Programs = new List<Program>();

        public override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
            GameEvents.OnGameSettingsApplied.Add(TechUnlocks.ApplyPrices);
        }

        // Runs once every scenario (R&D included) has loaded.
        void Start()
        {
            PartFactory.EnsurePurchased(Programs);
            EditorHider.ApplyToResearch();
            // R&D entries: this game's prices, and (once) the entries a game already
            // under way had researched count as bought (DESIGN.md 27.4).
            try
            {
                TechUnlocks.ApplyPrices();
                if (!entriesSettled)
                {
                    int n = TechUnlocks.SettleOldGame();
                    entriesSettled = n >= 0;
                    if (n > 0) Debug.Log($"[PIP] {n} R&D entries this game had already researched count as bought");
                }
            }
            catch (Exception e) { Debug.LogError("[PIP] Setting up R&D entries failed: " + e); }
            if (switchedOffNow)
            {
                switchedOffNow = false;
                ScreenMessages.PostScreenMessage("Phil's Iterative Parts is off in this game: it was started before PIP was installed. " +
                    "Your stock parts are unchanged. Part types can be switched on in Settings → Difficulty Options → PIP.", 12f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        void OnDestroy()
        {
            GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
            GameEvents.OnGameSettingsApplied.Remove(TechUnlocks.ApplyPrices);
            if (Instance == this) Instance = null;
        }

        public Program Find(string id) => string.IsNullOrEmpty(id) ? null : Programs.Find(p => p.Id == id);

        // The program a part belongs to (both part modules ask this): by the id it
        // was built with, else by its part name (a part fresh from the parts list).
        // newest: the family's newest version, which new builds in the VAB use.
        public static Program ProgramOf(string programId, string partName, bool newest)
        {
            PipScenario s = Instance;
            Program p = s?.Find(programId) ?? s?.Find(PartFactory.ProgramIdOf(partName));
            return newest ? s?.Latest(p) : p;
        }

        public IEnumerable<Program> Active => Programs.Where(p => !p.Retired && PipSettings.On(p.Type));

        #region Starting and retiring programs

        public static bool Free =>
            HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode != Game.Modes.CAREER;

        // A brand-new program (DESIGN.md section 18.5): from about 1,000 with no
        // points spent to the type's ceiling with every slider it uses at 100.
        public static double Cost(PartType type, string[] model, int[] points) =>
            Free ? 0 : Math.Round(Pricing.ProgramCost(type.CostCeiling, type.PointsShare(model, points), PipSettings.Prices));

        // The family's newest version: where a retired version's parts send their experience.
        public Program Latest(Program p)
        {
            for (int guard = 0; p != null && !string.IsNullOrEmpty(p.SuccessorId) && guard < 100; guard++)
            {
                Program next = Find(p.SuccessorId);
                if (next == null) break;
                p = next;
            }
            return p;
        }

        // Every version of a family, first to newest.
        public List<Program> Family(Program p)
        {
            var list = new List<Program>();
            Program first = p;
            for (int guard = 0; first != null && !string.IsNullOrEmpty(first.PredecessorId) && guard < 100; guard++)
                first = Find(first.PredecessorId) ?? first;
            for (Program v = first; v != null && list.Count < 100; v = string.IsNullOrEmpty(v.SuccessorId) ? null : Find(v.SuccessorId))
            {
                if (list.Contains(v)) break;
                list.Add(v);
            }
            return list;
        }

        // Names are the family's: the version ("I", "II", "Max") is added for you. The
        // rules are in Core/Names.cs.
        bool NameTaken(string name, Program except = null)
        {
            var family = except != null ? Family(except) : null;
            return Programs.Any(p => (family == null || !family.Contains(p)) && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // Renames a whole family (Kestrel I, II, Max become Falcon I, II, Max), free.
        // Parts are found by the program's id, never its name, so craft files and
        // vessels in flight keep working; built parts show the new name when they
        // next load (DESIGN.md 22).
        public bool Rename(Program p, string name, out string error)
        {
            name = ProgramNames.Clean(name);
            error = ProgramNames.Problem(name, n => NameTaken(n, p));
            if (error != null) return false;
            if (name == p.Name) return true;
            foreach (Program v in Family(p))
            {
                v.Name = name;
                PartFactory.Refresh(v);
            }
            RefreshEditor();
            return true;
        }

        public Program Create(string name, PartType type, string[] model, int[] points, out string error) =>
            Create(name, type, model, points, "", "", type.DefaultVariant(model), "", out error);

        // look / sideLook: the installed parts the program wears (empty: PIP's own model,
        // and the default side-mount look). variant: its size, in-line or side-mount
        // ("R..."), for types that have sizes. gimbal: degrees, for engines with the
        // pick. All fixed for the program's life.
        public Program Create(string name, PartType type, string[] model, int[] points, string look, string sideLook, string variant, string gimbal, out string error)
        {
            error = null;
            name = ProgramNames.Clean(name);
            error = ProgramNames.Problem(name, n => NameTaken(n));
            if (error != null) return null;
            if (type.HasGimbalPick)
            {
                Option g = PartType.GimbalOptions.FirstOrDefault(o => o.Code == gimbal) ?? PartType.GimbalOptions[0];
                gimbal = type.CanGimbal(model) ? g.Code : "0";
                if (!TechUnlocks.Available(type, g, Entries.Gimbal(g))) { error = $"{TechUnlocks.Todo(type, g, Entries.Gimbal(g))} for a {g.Label} gimbal."; return null; }
                if (gimbal != "0" && type.HasLooks && !Looks.Gimbals(type, look, model)) { error = "This look has a fixed nozzle: pick 0°."; return null; }
            }
            else gimbal = "";
            if (!PipSettings.On(type)) { error = $"{type.Title} are off in this game."; return null; }
            if (!TechUnlocks.Available(type, model)) { error = "That model isn't unlocked yet."; return null; }
            bool sized = type.Variants(model) != null, side = sized && PartType.IsSide(variant), adapter = sized && PartType.IsAdapter(variant);
            if (sized)
            {
                if (type.VariantOption(model, variant) == null) { error = "Pick a size."; return null; }
                if (!TechUnlocks.Available(type, model, variant)) { error = $"{TechUnlocks.Missing(type, model, variant)} for that size."; return null; }
                if (side && (string.IsNullOrEmpty(sideLook) || !Looks.Exists(type, true, sideLook, model))) { error = "No side-mount look is installed for this."; return null; }
                // An adapter wears an installed adapter of its ratio (DESIGN.md 20.2), passed as sideLook.
                if (adapter && (string.IsNullOrEmpty(sideLook) || !Looks.Exists(type, "A", sideLook, model, variant)))
                {
                    error = $"No adapter of that taper is installed: it needs {type.AdapterNeeds(type.AdapterKind(variant)) ?? "an adapter part"}.";
                    return null;
                }
            }
            else variant = "";
            // Never charge for a part that can't be made (DESIGN.md section 17).
            if (!PartFactory.Made(type, model)) { error = $"That model needs {PartFactory.Needs(type, model)}, which isn't installed."; return null; }
            if (!string.IsNullOrEmpty(look) && !Looks.Exists(type, false, look, model)) { error = "That look isn't installed, or doesn't suit this model."; return null; }
            if (!adapter && !string.IsNullOrEmpty(sideLook) && !Looks.Exists(type, true, sideLook, model)) { error = "That side-mount look isn't installed."; return null; }
            int cap = TechUnlocks.Cap(type);
            for (int i = 0; i < type.Categories.Length; i++)
            {
                if (points[i] > cap) { error = $"{type.Categories[i].Label} is capped at {cap} for now."; return null; }
                if (points[i] > 0 && !type.CategoryUsed(model, i)) { error = $"{type.Categories[i].Label} does nothing on this model."; return null; }
            }

            double cost = Cost(type, model, points);
            if (!Pay(cost, out error)) return null;

            Program program = Program.Create(Program.NewId(), name, type, model, points, Planetarium.GetUniversalTime(), cost);
            // A side-mount or adapter program wears only that look, an in-line one only its look.
            program.Look = side || adapter ? "" : look ?? "";
            program.SideLook = side || adapter ? sideLook ?? "" : "";
            program.Variant = variant ?? "";
            program.Gimbal = gimbal;
            Programs.Add(program);
            PartFactory.EnsurePurchased(new[] { program });
            RefreshEditor();
            return program;
        }

        static bool Pay(double cost, out string error)
        {
            error = null;
            if (cost > 0 && Funding.Instance != null && Funding.Instance.Funds < cost)
            {
                error = $"Needs {cost:N0} funds; you have {Funding.Instance.Funds:N0}.";
                return false;
            }
            if (cost > 0) Funding.Instance?.AddFunds(-cost, TransactionReasons.RnDPartPurchase);
            return true;
        }

        // The next version's cost: a tenth of a brand-new program with its points (section 18.1).
        public static double NextCost(Program p) =>
            Math.Round(Cost(p.Type, p.Model, p.NextPoints(TechUnlocks.Cap(p.Type))) * Pricing.SuccessorShare);

        // Why a version can't move on yet, or null when it can.
        public static string NextBlocked(Program p)
        {
            if (p.Retired || !string.IsNullOrEmpty(p.SuccessorId)) return "This version has a successor already.";
            if (p.IsMax) return $"{p.Title} is the last version.";
            if (p.Mk < Progress.MaxMk) return $"Reach {Progress.MkName(Progress.MaxMk)} first.";
            int cap = TechUnlocks.Cap(p.Type);
            if (!p.CanImprove(cap))
            {
                string next = TechUnlocks.NextCap(p.Type);
                return $"Every slider is at the research cap ({cap})." + (next == null ? "" : $" {next} to raise it.");
            }
            return null;
        }

        // Kestrel II: the same design (the look may change), +10 on every slider it
        // uses, starting where this version ends. This version retires; its parts
        // already built keep flying and count toward the new one.
        public Program StartNext(Program p, string look, out string error)
        {
            error = NextBlocked(p);
            if (error != null) return null;
            // Side-mount and adapter programs change their mount look (SideLook).
            bool side = p.Mounted;
            if (!string.IsNullOrEmpty(look) && !Looks.Exists(p.Type, side ? PartType.MountOf(p.Variant) : "", look, p.Model, p.Variant))
            {
                error = "That look isn't installed, or doesn't suit this model.";
                return null;
            }
            if (p.GimbalDegrees > 0 && !side && p.Type.HasLooks && !Looks.Gimbals(p.Type, look ?? p.Look, p.Model))
            {
                error = "That look has a fixed nozzle, and this family gimbals.";
                return null;
            }
            double cost = NextCost(p);
            if (!Pay(cost, out error)) return null;

            Program next = p.NextVersion(Program.NewId(), TechUnlocks.Cap(p.Type), Planetarium.GetUniversalTime(), cost);
            if (look != null)
            {
                if (side) next.SideLook = look;
                else next.Look = look;
            }
            p.SuccessorId = next.Id;
            p.Retired = true;
            Programs.Add(next);
            PartFactory.EnsurePurchased(new[] { next });
            RefreshEditor();
            return next;
        }

        public void Retire(Program p)
        {
            p.Retired = true;
            RefreshEditor();
        }

        public static void RefreshEditor()
        {
            if (HighLogic.LoadedSceneIsEditor && EditorPartList.Instance != null) EditorPartList.Instance.Refresh();
        }

        #endregion

        #region Experience

        static bool NextReady(Program p) => p.Mk >= Progress.MaxMk && NextBlocked(p) == null;

        // In flight: a pop-up for each flight logged, a bigger one for a new Mk.
        public static void Announce(Program p, int mkBefore, string what)
        {
            if (p.Mk > mkBefore)
            {
                PartFactory.Refresh(p);
                ScreenMessages.PostScreenMessage(Progress.GainLine(p.Title, 0, mkBefore, p.Mk, p.Xp, NextReady(p)), 8f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                string toGo = p.Mk < Progress.MaxMk
                    ? $", {Progress.XpForMk(p.Mk + 1) - p.Xp} more for {Progress.MkName(p.Mk + 1)}" : "";
                ScreenMessages.PostScreenMessage($"{p.Title}: {what}{toGo}.", 4f, ScreenMessageStyle.UPPER_LEFT);
            }
        }

        // A recovered vessel is worth 3 to each program with a part aboard that
        // flew on that flight (an engine that fired after leaving the pad), once
        // per program per flight (DESIGN.md section 18.2). Keyed by the parts'
        // launch, not the vessel: KSP gives a relaunched craft the same vessel ID.
        // A retired version's parts count toward the family's newest version.
        // StageRecovery's and Phil's Reusability Suite's recoveries arrive through
        // the same event. What each program gained goes in KSP's message inbox
        // (DESIGN.md 25): the recovery screen covered the pop-ups this used to post.
        void OnVesselRecovered(ProtoVessel vessel, bool quick)
        {
            if (vessel?.protoPartSnapshots == null) return;
            var gained = new Dictionary<Program, int>();   // Mk before
            foreach (ProtoPartSnapshot part in vessel.protoPartSnapshots)
                foreach (ProtoPartModuleSnapshot m in part.modules)
                {
                    if (m.moduleValues == null) continue;
                    string flag = m.moduleName == nameof(ModulePipEngine) ? "fired" : m.moduleName == nameof(ModulePipPart) ? "flew" : null;
                    if (flag == null || !string.Equals(m.moduleValues.GetValue(flag), "True", StringComparison.OrdinalIgnoreCase)) continue;
                    Program p = Latest(Find(m.moduleValues.GetValue("programId")));
                    if (p == null) continue;
                    int before = p.Mk;
                    if (p.RecordRecovery(Program.FlightKey(part.launchID)) && !gained.ContainsKey(p)) gained[p] = before;
                }
            if (gained.Count == 0) return;
            var lines = new List<string>();
            var log = new List<string>();
            bool newMk = false;
            foreach (KeyValuePair<Program, int> g in gained)
            {
                Program p = g.Key;
                if (p.Mk > g.Value) { PartFactory.Refresh(p); newMk = true; }
                lines.Add(Progress.GainLine($"<b>{p.Title}</b>", Progress.RecoveryXp, g.Value, p.Mk, p.Xp, NextReady(p)));
                log.Add(Progress.GainLog(p.Title, Progress.RecoveryXp, g.Value, p.Mk, p.Xp));
            }
            Debug.Log($"[PIP] Recovered {vessel.vesselName}: {string.Join(", ", log)}");
            if (newMk) lines.Add("\nParts already built, like this craft's, keep the Mk they were built at.");
            string body = string.Join("\n", lines);
            try
            {
                MessageSystem.Instance.AddMessage(new MessageSystem.Message($"PIP: {vessel.vesselName} recovered", body,
                    newMk ? MessageSystemButton.MessageButtonColor.GREEN : MessageSystemButton.MessageButtonColor.BLUE,
                    newMk ? MessageSystemButton.ButtonIcons.ACHIEVE : MessageSystemButton.ButtonIcons.MESSAGE), true);
            }
            catch (Exception e)
            {
                // No inbox in this scene, or not ready yet: a pop-up instead.
                Debug.LogWarning($"[PIP] Message inbox unavailable ({e.GetType().Name}); showing a pop-up instead");
                ScreenMessages.PostScreenMessage(body, 10f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        #endregion

        #region Persistence

        // Whether this game was started with PIP. Decided once, the first time PIP
        // sees the game, and saved. A game already under way when PIP was
        // installed has every PIP type switched off then, so its stock parts stay;
        // its difficulty settings can switch them back on.
        bool startedWithPip = true;
        bool switchedOffNow;
        // Whether this game's researched R&D entries have been counted as bought, once
        // (TechUnlocks.SettleOldGame): saves from before entries could be bought haven't.
        bool entriesSettled;

        public override void OnLoad(ConfigNode node)
        {
            Programs.Clear();
            foreach (ConfigNode n in node.GetNodes("PROGRAM"))
            {
                Program p = Program.FromValues(n.GetValue);
                if (p == null) continue;
                Programs.Add(p);
                PartFactory.Ensure(p);
            }

            entriesSettled = node.GetValue("entriesSettled") == "True";
            FlightState state = HighLogic.CurrentGame?.flightState;
            bool underWay = !PipGuard.StartedWithPip(false, state?.universalTime ?? 0, state?.protoVessels?.Count ?? 0);
            if (node.HasValue("startedWithPip"))
            {
                startedWithPip = node.GetValue("startedWithPip") != "False";
                // Types added to PIP since this game last saved start off in it,
                // unless it's still brand new (DESIGN.md 21).
                var known = new HashSet<string>(node.HasValue("knownTypes")
                    ? node.GetValue("knownTypes").Split(',').Select(x => x.Trim())
                    : PipGuard.FirstTypes);
                if (underWay)
                    foreach (PartType t in PipGuard.NewTypesOff(known))
                        Debug.Log($"[PIP] {t.Title} are new in this version of PIP: off in this game, which was already under way. The difficulty settings can turn them on.");
            }
            else
            {
                startedWithPip = PipGuard.StartedWithPip(Programs.Count > 0, state?.universalTime ?? 0, state?.protoVessels?.Count ?? 0);
                if (!startedWithPip)
                {
                    PipGuard.AllOff();
                    switchedOffNow = true;
                    Debug.Log($"[PIP] This game was under way before PIP was installed (UT {state?.universalTime:N0}, {state?.protoVessels?.Count} vessels): every PIP type is off.");
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("startedWithPip", startedWithPip ? "True" : "False");
            node.AddValue("knownTypes", string.Join(",", PartTypes.All.Select(t => t.Id)));
            node.AddValue("entriesSettled", entriesSettled ? "True" : "False");
            foreach (Program p in Programs)
            {
                ConfigNode n = node.AddNode("PROGRAM");
                foreach (KeyValuePair<string, string> kv in p.ToValues()) n.AddValue(kv.Key, kv.Value);
            }
        }

        #endregion
    }
}
