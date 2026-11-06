using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PIP
{
    // On every PIP engine (liquid, solid, monopropellant, nuclear, electric, jet).
    // Knows its program and the Mk it was built at, and pushes those stats into
    // the stock engine modules, the gimbal, and (nuclear, with Kerbalism) the
    // radiation emitter. In the VAB a part always builds at the program's
    // current Mk; once launched it keeps its Mk.
    public class ModulePipEngine : PartModule, IPartMassModifier, IPartCostModifier
    {
        // From the chassis config. A look chassis serves every model that fits it,
        // so the model comes from the program and is saved with the part.
        [KSPField] public string typeId = "liquid";
        [KSPField(isPersistant = true)] public string model = "";
        [KSPField] public bool look;
        [KSPField] public string lookKind = "";   // the look's size and shape ("125", "125-M", "125-2")
        [KSPField] public string lookFuel = "";   // a liquid look's own fuel, as installed
        [KSPField] public string chassisSource = "";  // the installed part a standard chassis copies (its standard look)

        // What this engine was built to.
        [KSPField(isPersistant = true)] public string programId = "";
        [KSPField(isPersistant = true)] public string programName = "";
        [KSPField(isPersistant = true)] public int mk = -1;
        [KSPField(isPersistant = true)] public string points = "";
        [KSPField(isPersistant = true)] public string startPoints = "";   // a later version's start (DESIGN.md 18.1)
        [KSPField(isPersistant = true)] public int gimbalDeg;              // the family's gimbal pick (18.4)
        [KSPField(isPersistant = true)] public bool fired;            // ran after leaving the pad (counts recoveries)
        [KSPField(isPersistant = true)] public bool ignitionChecked;   // the one ignition check this flight
        [KSPField(isPersistant = true)] public bool failed;

        [KSPField(guiName = "Program", guiActive = true, guiActiveEditor = true)]
        public string programLabel = "";

        [KSPField(guiName = "Ignition", guiActive = true, guiActiveEditor = true)]
        public string ignitionLabel = "";

        int[] pointValues = new int[0];
        int[] StartValues => string.IsNullOrEmpty(startPoints) ? null : Program.PointsFromText(startPoints, Type.Categories.Length);
        List<ModuleEngines> engines = new List<ModuleEngines>();
        Stats stats = new Stats();

        public PartType Type => PartTypes.Get(typeId) ?? PartTypes.Liquid;
        string[] Model => Type.ParseModel(model);
        public bool Assigned => !string.IsNullOrEmpty(programId) && mk >= 0 && Model != null;
        bool IsPrefab => part.partInfo == null || part.partInfo.partPrefab == null || part == part.partInfo.partPrefab;

        public bool IsBurning
        {
            get
            {
                if (!Assigned || failed) return false;
                foreach (ModuleEngines e in engines)
                    if (e != null && e.EngineIgnited && !e.flameout && e.finalThrust > 0.01f) return true;
                return false;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            pointValues = Program.PointsFromText(points, Type.Categories.Length);
            statsReady = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (IsPrefab) return;
            engines = part.FindModulesImplementing<ModuleEngines>();
            pointValues = Program.PointsFromText(points, Type.Categories.Length);

            Program program = PipScenario.Instance?.Find(programId)
                ?? PipScenario.Instance?.Find(PartFactory.ProgramIdOf(part.partInfo?.name));
            // New builds use the family's newest version.
            if (HighLogic.LoadedSceneIsEditor) program = PipScenario.Instance?.Latest(program);
            if (HighLogic.LoadedSceneIsEditor || (program != null && !Assigned)) Assign(program);
            // A renamed program: the part keeps its version and Mk, and shows the new name.
            else if (program != null && program.Id == programId) programName = program.Title;
            if (HighLogic.LoadedSceneIsEditor)
            {
                fired = false;
                ignitionChecked = false;
                failed = false;
            }

            Apply();
            if (HighLogic.LoadedSceneIsFlight) FlightTracker.Register(this);
            Debug.Log($"[PIP] {part.partInfo?.name} started in {HighLogic.LoadedScene}: program {(Assigned ? programName : "none")} " +
                      $"{Progress.MkName(mk)}, {stats[K.ThrustVac]:0.##} kN, Isp {stats[K.IspSL]:0}/{stats[K.IspVac]:0}, {stats.Mass:0.###} t, fail {stats.FailureChance:P1}");
        }

        void OnDestroy() => FlightTracker.Unregister(this);

        void Assign(Program program)
        {
            if (program == null) return;
            programId = program.Id;
            programName = program.Title;
            model = program.ModelCode;
            mk = program.Mk;
            points = Program.PointsToText(program.Points);
            pointValues = (int[])program.Points.Clone();
            startPoints = program.StartPoints == null ? "" : Program.PointsToText(program.StartPoints);
            gimbalDeg = program.GimbalDegrees;
            statsReady = false;
        }

        // Mass and cost can be asked for before OnStart, so stats are computed on demand.
        bool statsReady;
        Stats Stats
        {
            get
            {
                if (!statsReady) Recompute();
                return stats;
            }
        }

        void Recompute()
        {
            stats = Assigned ? Type.Compute(Model, pointValues, mk, PipSettings.Failures, null, StartValues, gimbalDeg) : new Stats();
            statsReady = true;
        }

        public void Apply()
        {
            Recompute();
            programLabel = Assigned ? $"{programName} {Progress.MkName(mk)}" : "No program";
            ignitionLabel = !Assigned ? "" : failed ? "Failed to light"
                : stats.FailureChance <= 0 ? (Type == PartTypes.Jet ? "Never fails" : "Failures off")
                : $"{stats.FailureChance * 100:0.#}% chance to fail";
            if (!Assigned || engines.Count == 0) return;

            if (Type == PartTypes.Jet) ApplyJet();
            else
            {
                ApplyRocket(engines[0], stats[K.ThrustVac], stats[K.IspVac], stats[K.IspSL]);
                SetGimbal(stats[K.Gimbal]);
            }
            if (Type == PartTypes.Electric) SetPower(engines[0]);
            if (Type == PartTypes.Nuclear && NuclearEngines.Kerbalism) SetRadiation(stats[K.Radiation]);
        }

        // Rockets: thrust in vacuum, Isp falling linearly from vacuum to sea level
        // and on until it reaches ~0.
        static void ApplyRocket(ModuleEngines engine, double thrust, double ispVac, double ispSL)
        {
            double vac = Math.Max(1, ispVac), sl = Math.Max(0.001, ispSL);
            engine.maxThrust = (float)thrust;
            engine.minThrust = 0f;
            engine.atmChangeFlow = false;
            engine.useVelCurve = false;
            engine.useAtmCurve = false;
            var curve = new FloatCurve();
            float slope = (float)(sl - vac);
            curve.Add(0f, (float)vac, slope, slope);
            curve.Add(1f, (float)sl, slope, slope);
            if (vac > sl) curve.Add((float)(1 + sl / (vac - sl)), 0.001f, slope, 0f);
            engine.atmosphereCurve = curve;
            engine.maxFuelFlow = (float)(thrust / (vac * engine.g));
            engine.minFuelFlow = 0f;
        }

        // Jets keep their speed and altitude curves; Speed stretches the speed
        // curve. Two-mode jets: Dry/Wet (turbojet), AirBreathing/ClosedCycle
        // (combined cycle).
        void ApplyJet()
        {
            // Speed and altitude curves come from PIP's standard jet of this type,
            // whatever the look, so a look never changes how a jet performs.
            Part standard = PartFactory.Info(PartFactory.ChassisFor(Type, Model))?.partPrefab ?? part.partInfo?.partPrefab;
            List<ModuleEngines> prefab = standard?.FindModulesImplementing<ModuleEngines>();
            for (int i = 0; i < engines.Count; i++)
            {
                ModuleEngines e = engines[i];
                if (e == null) continue;
                bool air = e.propellants.Exists(p => p.name == "IntakeAir");
                if (!air)
                {
                    ApplyRocket(e, stats[K.RocketThrust], stats[K.RocketIspVac], stats[K.RocketIspSL]);
                    continue;
                }
                ModuleEngines like = prefab?.Where(s => s.propellants.Exists(p => p.name == "IntakeAir"))
                    .OrderBy(s => s.engineID == e.engineID ? 0 : 1).FirstOrDefault();
                if (like != null)
                {
                    e.atmChangeFlow = like.atmChangeFlow;
                    e.useAtmCurve = like.useAtmCurve;
                    e.atmCurve = like.atmCurve;
                    e.useVelCurve = like.useVelCurve;
                }
                // Only the turbojet has a dry mode (the Whiplash's single mode is also called "Dry").
                bool dry = JetEngines.TypeIndex(Model) == 1 && e.engineID == "Dry";
                double thrust = dry ? stats[K.ThrustDry] : stats[K.ThrustVac];
                double isp = Math.Max(1, dry ? stats[K.IspDry] : stats[K.IspVac]);
                e.maxThrust = (float)thrust;
                var curve = new FloatCurve();
                curve.Add(0f, (float)isp, 0f, 0f);
                e.atmosphereCurve = curve;
                e.maxFuelFlow = (float)(thrust / (isp * e.g));
                if (e.useVelCurve && like?.velCurve != null) e.velCurve = Stretch(like.velCurve, stats[K.Speed]);
            }
        }

        static FloatCurve Stretch(FloatCurve source, double factor)
        {
            var curve = new FloatCurve();
            float f = (float)Math.Max(0.1, factor);
            foreach (Keyframe k in source.Curve.keys)
                curve.Add(k.time * f, k.value, k.inTangent / f, k.outTangent / f);
            return curve;
        }

        void SetGimbal(double range)
        {
            foreach (ModuleGimbal gimbal in part.FindModulesImplementing<ModuleGimbal>())
            {
                float g = (float)range;
                gimbal.gimbalRange = g;
                gimbal.gimbalRangeXP = g;
                gimbal.gimbalRangeXN = g;
                gimbal.gimbalRangeYP = g;
                gimbal.gimbalRangeYN = g;
                gimbal.gimbalLock = g <= 0.01f;
            }
        }

        // Electric: set the ElectricCharge ratio so the engine draws the program's
        // EC/s at full thrust.
        void SetPower(ModuleEngines engine)
        {
            Propellant ec = engine.propellants.Find(p => p.name == "ElectricCharge");
            Propellant gas = engine.propellants.Find(p => p.name != "ElectricCharge");
            PartResourceDefinition def = gas == null ? null : PartResourceLibrary.Instance?.GetDefinition(gas.name);
            if (ec == null || def == null || def.density <= 0) return;
            double massFlow = stats[K.ThrustVac] / (Math.Max(1, stats[K.IspVac]) * engine.g);   // t/s
            if (massFlow <= 0) return;
            ec.ratio = (float)(stats[K.EcRate] * gas.ratio * def.density / massFlow);
            engine.SetupPropellant();
            // KSP requests mass x mixtureDensityRecip x ratio units of each propellant.
            Debug.Log($"[PIP] {programName}: draws {massFlow * engine.mixtureDensityRecip * ec.ratio:0.##} EC/s at full thrust (program says {stats[K.EcRate]:0.##})");
        }

        // Kerbalism's Emitter takes rad/s.
        void SetRadiation(double radPerHour)
        {
            foreach (PartModule m in part.Modules)
            {
                if (m.moduleName != "Emitter") continue;
                BaseField f = m.Fields["radiation"];
                if (f != null) f.SetValue(radPerHour / 3600.0, m);
                Debug.Log($"[PIP] {programName}: Kerbalism radiation set to {radPerHour:0.###} rad/h (field {(f == null ? "missing" : f.GetValue(m)?.ToString())})");
            }
        }

        // One check per engine per flight, the first time it's lit.
        void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || engines.Count == 0 || !Assigned) return;
            if (failed)
            {
                foreach (ModuleEngines e in engines) if (e.EngineIgnited) Kill(e);
                return;
            }
            if (ignitionChecked || !engines.Exists(e => e.EngineIgnited)) return;
            ignitionChecked = true;
            if (UnityEngine.Random.value >= Stats.FailureChance) return;

            failed = true;
            foreach (ModuleEngines e in engines) Kill(e);
            ignitionLabel = "Failed to light";
            ScreenMessages.PostScreenMessage($"{programName} {Progress.MkName(mk)} failed to light.", 6f, ScreenMessageStyle.UPPER_CENTER);
        }

        // Solid boosters don't allow a shutdown, so it's switched on first.
        static void Kill(ModuleEngines e)
        {
            e.allowShutdown = true;
            e.Shutdown();
            e.allowRestart = false;
            e.allowShutdown = false;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation situation) =>
            IsPrefab || !Assigned ? 0f : (float)(Stats.Mass - defaultMass);

        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;

        public float GetModuleCost(float defaultCost, ModifierStagingSituation situation) =>
            IsPrefab || !Assigned ? 0f : (float)(Stats.BuildPrice - defaultCost);

        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;

        public const string InfoPrefix = "A Phil's Iterative Parts part.";

        public override string GetInfo() => InfoPrefix + " Its stats come from its program.";
    }
}
