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
        }

        // Runs once every scenario (R&D included) has loaded.
        void Start()
        {
            PartFactory.EnsurePurchased(Programs);
            EditorHider.ApplyToResearch();
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
            if (Instance == this) Instance = null;
        }

        public Program Find(string id) => string.IsNullOrEmpty(id) ? null : Programs.Find(p => p.Id == id);

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

        // Names are the family's: the version ("I", "II", "Max") is added for you.
        static readonly System.Text.RegularExpressions.Regex Numeral =
            new System.Text.RegularExpressions.Regex(@"\s(M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})|Max)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        public static string CleanName(string name) =>
            (name ?? "").Replace("{", "").Replace("}", "").Replace("//", "/").Replace(",", "").Trim();

        public Program Create(string name, PartType type, string[] model, int[] points, out string error) =>
            Create(name, type, model, points, "", "", type.DefaultVariant(model), "", out error);

        // look / sideLook: the installed parts the program wears (empty: PIP's own model,
        // and the default side-mount look). variant: its size, in-line or side-mount
        // ("R..."), for types that have sizes. gimbal: degrees, for engines with the
        // pick. All fixed for the program's life.
        public Program Create(string name, PartType type, string[] model, int[] points, string look, string sideLook, string variant, string gimbal, out string error)
        {
            error = null;
            name = CleanName(name);
            if (name.Length == 0) { error = "Give the program a name."; return null; }
            if (Numeral.IsMatch(name) && Numeral.Match(name).Value.Trim().Length > 0)
            {
                error = "Leave the version off the name: it's added for you (I, II, III ... Max).";
                return null;
            }
            if (Programs.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"There's already a program called {name}.";
                return null;
            }
            if (type.HasGimbalPick)
            {
                Option g = PartType.GimbalOptions.FirstOrDefault(o => o.Code == gimbal) ?? PartType.GimbalOptions[0];
                gimbal = type.CanGimbal(model) ? g.Code : "0";
                if (!TechUnlocks.Available(type, g)) { error = $"Research {TechUnlocks.Title(g.Tech)} for a {g.Label} gimbal."; return null; }
                if (gimbal != "0" && type.HasLooks && !Looks.Gimbals(type, look, model)) { error = "This look has a fixed nozzle: pick 0°."; return null; }
            }
            else gimbal = "";
            if (!PipSettings.On(type)) { error = $"{type.Title} are off in this game."; return null; }
            if (!TechUnlocks.Available(type, model)) { error = "That model isn't unlocked yet."; return null; }
            bool sized = type.Variants(model) != null, side = sized && PartType.IsSide(variant);
            if (sized)
            {
                if (type.VariantOption(model, variant) == null) { error = "Pick a size."; return null; }
                if (!TechUnlocks.Available(type, model, variant)) { error = $"Research {TechUnlocks.Missing(type, model, variant)} for that size."; return null; }
                if (side && (string.IsNullOrEmpty(sideLook) || !Looks.Exists(type, true, sideLook, model))) { error = "No side-mount look is installed for this."; return null; }
            }
            else variant = "";
            // Never charge for a part that can't be made (DESIGN.md section 17).
            if (!PartFactory.Made(type, model)) { error = $"That model needs {PartFactory.Needs(type, model)}, which isn't installed."; return null; }
            if (!string.IsNullOrEmpty(look) && !Looks.Exists(type, false, look, model)) { error = "That look isn't installed, or doesn't suit this model."; return null; }
            if (!string.IsNullOrEmpty(sideLook) && !Looks.Exists(type, true, sideLook, model)) { error = "That side-mount look isn't installed."; return null; }
            int cap = TechUnlocks.Cap(type);
            for (int i = 0; i < type.Categories.Length; i++)
            {
                if (points[i] > cap) { error = $"{type.Categories[i].Label} is capped at {cap} for now."; return null; }
                if (points[i] > 0 && !type.CategoryUsed(model, i)) { error = $"{type.Categories[i].Label} does nothing on this model."; return null; }
            }

            double cost = Cost(type, model, points);
            if (!Pay(cost, out error)) return null;

            Program program = Program.Create(Program.NewId(), name, type, model, points, Planetarium.GetUniversalTime(), cost);
            // A side-mount program wears only its side look, an in-line one only its look.
            program.Look = side ? "" : look ?? "";
            program.SideLook = side ? sideLook ?? "" : "";
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
            if (!p.CanImprove(cap)) return $"Every slider is at the research cap ({cap}). Research raises it.";
            return null;
        }

        // Kestrel II: the same design (the look may change), +10 on every slider it
        // uses, starting where this version ends. This version retires; its parts
        // already built keep flying and count toward the new one.
        public Program StartNext(Program p, string look, out string error)
        {
            error = NextBlocked(p);
            if (error != null) return null;
            bool side = p.FixedSize && p.IsSide;
            if (!string.IsNullOrEmpty(look) && !Looks.Exists(p.Type, side, look, p.Model)) { error = "That look isn't installed, or doesn't suit this model."; return null; }
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

        public static void Announce(Program p, int mkBefore, string what)
        {
            if (p.Mk > mkBefore)
            {
                PartFactory.Refresh(p);
                string next = p.Mk >= Progress.MaxMk && NextBlocked(p) == null ? " Its next version is ready to start." : " New builds use it.";
                ScreenMessages.PostScreenMessage($"{p.Title} reached {Progress.MkName(p.Mk)}.{next}", 8f, ScreenMessageStyle.UPPER_CENTER);
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
        // StageRecovery recoveries arrive through the same event.
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
            foreach (KeyValuePair<Program, int> g in gained)
                Announce(g.Key, g.Value, $"recovery logged (+{Progress.RecoveryXp})");
        }

        #endregion

        #region Persistence

        // Whether this game was started with PIP. Decided once, the first time PIP
        // sees the game, and saved. A game already under way when PIP was
        // installed has every PIP type switched off then, so its stock parts stay;
        // its difficulty settings can switch them back on.
        bool startedWithPip = true;
        bool switchedOffNow;

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

            if (node.HasValue("startedWithPip")) startedWithPip = node.GetValue("startedWithPip") != "False";
            else
            {
                FlightState state = HighLogic.CurrentGame?.flightState;
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
            foreach (Program p in Programs)
            {
                ConfigNode n = node.AddNode("PROGRAM");
                foreach (KeyValuePair<string, string> kv in p.ToValues()) n.AddValue(kv.Key, kv.Value);
            }
        }

        #endregion
    }
}
