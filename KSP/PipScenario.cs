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

        public static double Cost(PartType type, int[] points) =>
            Free ? 0 : Math.Round(Pricing.ProgramCost(points.Sum(), type.PriceFactor, PipSettings.Prices));

        public Program Create(string name, PartType type, string[] model, int[] points, out string error) =>
            Create(name, type, model, points, "", "", type.DefaultVariant(model), out error);

        // look / sideLook: the installed parts the program wears (empty: PIP's own model,
        // and the default side-mount look). variant: its size, in-line or side-mount
        // ("R..."), for types that have sizes. All fixed for the program's life.
        public Program Create(string name, PartType type, string[] model, int[] points, string look, string sideLook, string variant, out string error)
        {
            error = null;
            name = (name ?? "").Replace("{", "").Replace("}", "").Replace("//", "/").Replace(",", "").Trim();
            if (name.Length == 0) { error = "Give the program a name."; return null; }
            if (Programs.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"There's already a program called {name}.";
                return null;
            }
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
            int gimbal = Looks.GimbalCategory(type);
            if (gimbal >= 0 && points[gimbal] > 0 && type.HasLooks && !Looks.Gimbals(type, look, model))
            {
                error = "This look has a fixed nozzle: take the Gimbal points off.";
                return null;
            }
            int cap = TechUnlocks.Cap(type);
            for (int i = 0; i < type.Categories.Length; i++)
            {
                if (points[i] > cap) { error = $"{type.Categories[i].Label} is capped at {cap} for now."; return null; }
                if (points[i] > 0 && !type.CategoryUsed(model, i)) { error = $"{type.Categories[i].Label} does nothing on this model."; return null; }
            }

            double cost = Cost(type, points);
            if (cost > 0 && Funding.Instance != null && Funding.Instance.Funds < cost)
            {
                error = $"Needs {cost:N0} funds; you have {Funding.Instance.Funds:N0}.";
                return null;
            }
            if (cost > 0) Funding.Instance?.AddFunds(-cost, TransactionReasons.RnDPartPurchase);

            Program program = Program.Create(Program.NewId(), name, type, model, points, Planetarium.GetUniversalTime(), cost);
            // A side-mount program wears only its side look, an in-line one only its look.
            program.Look = side ? "" : look ?? "";
            program.SideLook = side ? sideLook ?? "" : "";
            program.Variant = variant ?? "";
            Programs.Add(program);
            PartFactory.EnsurePurchased(new[] { program });
            RefreshEditor();
            return program;
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
                ScreenMessages.PostScreenMessage($"{p.Name} reached {Progress.MkName(p.Mk)}. New builds use it.", 8f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                string toGo = p.Mk < Progress.MaxMk
                    ? $", {Progress.XpForMk(p.Mk + 1) - p.Xp} more for {Progress.MkName(p.Mk + 1)}" : "";
                ScreenMessages.PostScreenMessage($"{p.Name}: {what}{toGo}.", 4f, ScreenMessageStyle.UPPER_LEFT);
            }
        }

        // Each recovered part is worth 1 to its program, if it flew on that flight
        // (an engine that fired after leaving the pad), once per part per flight
        // (DESIGN.md section 5). Keyed by the part's launch, not the vessel: KSP
        // gives a relaunched craft the same vessel ID. StageRecovery recoveries
        // arrive through the same event.
        void OnVesselRecovered(ProtoVessel vessel, bool quick)
        {
            if (vessel?.protoPartSnapshots == null) return;
            var gained = new Dictionary<Program, int[]>();   // { Mk before, parts counted }
            foreach (ProtoPartSnapshot part in vessel.protoPartSnapshots)
                foreach (ProtoPartModuleSnapshot m in part.modules)
                {
                    if (m.moduleValues == null) continue;
                    string flag = m.moduleName == nameof(ModulePipEngine) ? "fired" : m.moduleName == nameof(ModulePipPart) ? "flew" : null;
                    if (flag == null || !string.Equals(m.moduleValues.GetValue(flag), "True", StringComparison.OrdinalIgnoreCase)) continue;
                    Program p = Find(m.moduleValues.GetValue("programId"));
                    if (p == null) continue;
                    int before = p.Mk;
                    if (!p.RecordRecovery(Program.PartKey(part.launchID, part.flightID))) continue;
                    if (gained.TryGetValue(p, out int[] g)) g[1]++;
                    else gained[p] = new[] { before, 1 };
                }
            foreach (KeyValuePair<Program, int[]> g in gained)
                Announce(g.Key, g.Value[0], g.Value[1] == 1 ? "recovery logged" : $"{g.Value[1]} parts recovered");
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
