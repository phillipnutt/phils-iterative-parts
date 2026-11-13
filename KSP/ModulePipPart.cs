using System;
using System.Collections.Generic;
using UnityEngine;

namespace PIP
{
    // On every PIP part that isn't an engine. Knows its program, the Mk it was
    // built at, and the size picked on the part (B9PartSwitch writes "variant"
    // here), and pushes the program's numbers into the stock and mod modules.
    public class ModulePipPart : PartModule, IPartMassModifier, IPartCostModifier
    {
        // From the chassis config. A look chassis serves every model of its type,
        // so the model comes from the program and is saved with the part.
        [KSPField] public string typeId = "battery";
        [KSPField(isPersistant = true)] public string model = "";
        [KSPField] public bool look;
        [KSPField] public string lookKind = "";   // what the look's part is: "F"/"D" panel, "O"/"D" antenna...
        [KSPField] public string chassisSource = "";  // the installed part a standard chassis copies (its standard look)

        // The size picked on the part; set by B9PartSwitch's subtype, or by the
        // chassis config for a look with one size. lookScale: how much a look's
        // model is scaled for that size (moves its surface-attach point).
        [KSPField(isPersistant = true)] public string variant = "";
        [KSPField(isPersistant = true)] public float lookScale;

        // What this part was built to.
        [KSPField(isPersistant = true)] public string programId = "";
        [KSPField(isPersistant = true)] public string programName = "";
        [KSPField(isPersistant = true)] public int mk = -1;
        [KSPField(isPersistant = true)] public string points = "";
        [KSPField(isPersistant = true)] public string startPoints = "";   // a later version's start (DESIGN.md 18.1)
        [KSPField(isPersistant = true)] public bool flew;   // left the pad on this flight (counts recoveries)
        // The layout "points" is saved in (PartType.Format); 0 on parts from before it was saved.
        [KSPField(isPersistant = true)] public int pointsFormat;

        [KSPField(guiName = "Program", guiActive = true, guiActiveEditor = true)]
        public string programLabel = "";

        int[] pointValues = new int[0];
        Stats stats = new Stats();
        bool statsReady, started;

        // The size the program fixed (DESIGN.md section 14), or "" for programs from
        // before that, which still pick it on the part. In the VAB the size switch is
        // kept on it and hidden.
        string fixedSize = "";
        PartModule sizeSwitch;
        bool sizeSwitchSearched;

        public PartType Type => PartTypes.Get(typeId) ?? PartTypes.Battery;
        string[] Model => Type.ParseModel(model) ?? (Type.Choices.Length == 0 ? new string[0] : null);
        string Variant => string.IsNullOrEmpty(variant) ? Type.DefaultVariant(Model) : variant;
        public bool Assigned => !string.IsNullOrEmpty(programId) && mk >= 0 && Model != null;
        bool IsPrefab => part.partInfo == null || part.partInfo.partPrefab == null || part == part.partInfo.partPrefab;
        Part Prefab => part.partInfo?.partPrefab;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (pointsFormat < Type.Format && !string.IsNullOrEmpty(points)) UpgradePoints();
            pointValues = Program.PointsFromText(points, Type.Categories.Length);
            statsReady = false;
            // B9PartSwitch sends a new size this way, and can reload the chassis config
            // with it, which doesn't carry the program; the Reusability Suite's "Use it" puts
            // a hangar copy's hardware data on a part this way. In the VAB, re-link the
            // program only if that lost it (a part keeps the version and Mk it was given,
            // DESIGN.md 26.4) and apply straight away.
            if (started && !IsPrefab && HighLogic.LoadedSceneIsEditor)
            {
                if (!Assigned) Assign(PipScenario.ProgramOf(programId, part.partInfo?.name, true));
                Apply();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (IsPrefab) return;
            pointValues = Program.PointsFromText(points, Type.Categories.Length);
            // A part placed from the parts list is new: the family's newest version at its
            // current Mk. A craft the VAB loads was made current before it was built
            // (NewestVersionOnLoadPatch), leaving built hardware as it was (DESIGN.md 26.4).
            Program program = PipScenario.ProgramOf(programId, part.partInfo?.name, HighLogic.LoadedSceneIsEditor);
            if (program != null && !Assigned) Assign(program);
            // A renamed program: the part keeps its version and Mk, and shows the new name.
            else if (program != null && program.Id == programId) programName = program.Title;
            started = true;
            Apply();
            if (HighLogic.LoadedSceneIsFlight) FlightTracker.Register(this);
            Debug.Log($"[PIP] {part.partInfo?.name} ({typeId} {Variant}) started in {HighLogic.LoadedScene}: program {(Assigned ? programName : "none")} {Progress.MkName(mk)}, {stats.Mass:0.###} t");
        }

        void OnDestroy() => FlightTracker.Unregister(this);

        // A part built before its type's sliders changed (antennas' Data rate, DESIGN.md
        // 29.2): its own points, at its own Mk, in today's layout, by the type's rule.
        void UpgradePoints()
        {
            try
            {
                string look = PipScenario.Instance?.Find(programId)?.Look;
                if (Type.Upgrade(pointsFormat, Model, variant, look, points, startPoints, out int[] now, out int[] start))
                {
                    Debug.Log($"[PIP] {part?.partInfo?.name} ({programName} {Progress.MkName(mk)}): points {points} -> {Program.PointsToText(now)}");
                    points = Program.PointsToText(now);
                    startPoints = Program.PointsToText(start);
                }
            }
            catch (Exception e) { Debug.LogError($"[PIP] Updating {part?.partInfo?.name}'s points failed: {e}"); }
            pointsFormat = Type.Format;
        }

        // The new-build rule (DESIGN.md 26.3), the same one PipApi.ToCurrent applies to a
        // craft at the Space Center: the family's newest version at its current Mk.
        void Assign(Program program)
        {
            if (program == null) return;
            var built = new ConfigNode("MODULE");
            PipApi.WriteNewBuild(built, program, false);
            Fields.Load(built);
            pointValues = (int[])program.Points.Clone();
            fixedSize = program.FixedSize ? program.Variant : "";
            statsReady = false;
        }

        void Update()
        {
            if (started && HighLogic.LoadedSceneIsEditor && fixedSize.Length > 0) KeepSize();
        }

        // B9PartSwitch's "pipSize" module: switched to the program's size, its picker hidden.
        void KeepSize()
        {
            if (!sizeSwitchSearched)
            {
                sizeSwitchSearched = true;
                foreach (PartModule m in part.Modules)
                    if (m.moduleName == "ModuleB9PartSwitch" && ModuleIdOf(m) == "pipSize") { sizeSwitch = m; break; }
            }
            if (sizeSwitch == null) return;
            BaseField pick = sizeSwitch.Fields["currentSubtypeIndex"];
            if (pick != null && (pick.guiActiveEditor || pick.guiActive)) pick.guiActiveEditor = pick.guiActive = false;
            foreach (BaseEvent e in sizeSwitch.Events) if (e.guiActiveEditor) e.guiActiveEditor = false;
            System.Type t = sizeSwitch.GetType();
            if (t.GetProperty("CurrentSubtypeName")?.GetValue(sizeSwitch, null) as string == fixedSize) return;
            try { t.GetMethod("SwitchSubtype", new[] { typeof(string) })?.Invoke(sizeSwitch, new object[] { fixedSize }); }
            catch (Exception e)
            {
                Debug.LogError($"[PIP] Couldn't set {part.partInfo?.name} to size {fixedSize}: {e.InnerException?.Message ?? e.Message}");
                fixedSize = "";
            }
        }

        static string ModuleIdOf(PartModule m) =>
            m.Fields["moduleID"]?.GetValue(m) as string ?? m.GetType().GetField("moduleID")?.GetValue(m) as string;

        // A tank's boil-off and cooling factor (ModulePipBoiloff): from Insulation on a
        // cryogenic tank, FuelTanks.Uninsulated on a standard one.
        public double BoiloffFactor => Assigned && Type is TankKind ? Stats[FuelTanks.Boiloff] : 1;

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
            int[] start = string.IsNullOrEmpty(startPoints) ? null : Program.PointsFromText(startPoints, Type.Categories.Length);
            stats = Assigned ? Type.Compute(Model, pointValues, mk, PipSettings.Failures, Variant, start, 0) : new Stats();
            statsReady = true;
        }

        public void Apply()
        {
            Recompute();
            programLabel = Assigned ? $"{programName} {Progress.MkName(mk)}" : "No program";
            MoveSurfaceNode();
            if (!Assigned) return;
            try
            {
                ApplyNumbers();
                if (Held(typeId, stats, out string held, out double amount)) SetResource(held, amount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PIP] Applying {typeId} program {programName} to {part.partInfo?.name} failed: {e}");
            }
            if (HighLogic.LoadedSceneIsEditor) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch?.ship);
        }

        // The program's numbers onto the stock and mod modules. No geometry and no
        // resources, so it serves a program's master copy too (SetUpMasterCopy).
        void ApplyNumbers()
        {
            switch (typeId)
            {
                case "rcs": ApplyRcs(); break;
                case "wheel": SetWheel(stats[ReactionWheels.TorqueKey], stats[K.EcRate]); break;
                case "shield": ApplyShield(); break;
                case "solar": ApplySolar(); break;
                case "antenna": ApplyAntenna(); break;
                case "probe": ApplyProbe(); break;
                case "tank": ApplyTank(); break;
                case "plane": ApplyTank(); ApplyLift(); break;
                case "rtg": ApplyRtg(); break;
                case "intake": ApplyIntake(); break;
                case "decoupler": ApplyDecoupler(); break;
            }
        }

        // This program's own master copy (DESIGN.md 29.3, PartFactory.MasterCopy): its numbers
        // at the current Mk on the copy's other modules, for what reads a master copy instead
        // of a part (Kerbalism, for a ship out of range). This module is then put back as it
        // was, blank, so a part placed from the copy is set up from its program as always.
        public void SetUpMasterCopy(Program program, string size)
        {
            string id = programId, name = programName, mdl = model, pts = points, start = startPoints, size0 = variant, label = programLabel, fixedWas = fixedSize;
            int mkWas = mk, formatWas = pointsFormat;
            bool flewWas = flew;
            try
            {
                Assign(program);
                if (!string.IsNullOrEmpty(size)) variant = size;
                Recompute();
                if (Assigned) ApplyNumbers();
            }
            finally
            {
                programId = id; programName = name; model = mdl; points = pts; startPoints = start; variant = size0;
                programLabel = label; fixedSize = fixedWas; mk = mkWas; pointsFormat = formatWas; flew = flewWas;
                pointValues = Program.PointsFromText(points, Type.Categories.Length);
                stats = new Stats();
                statsReady = false;
            }
        }

        // The size switch moves stack nodes but can't move the surface-attach
        // point, so a scaled look would float off (or sink into) what it's stuck to.
        void MoveSurfaceNode()
        {
            if (lookScale <= 0 || part.srfAttachNode == null || Prefab?.srfAttachNode == null) return;
            Vector3 at = Prefab.srfAttachNode.originalPosition * lookScale;
            part.srfAttachNode.position = at;
            part.srfAttachNode.originalPosition = at;
        }

        // PIP's own chassis for this model: where numbers that aren't in the program
        // (an intake's speed curve, a shield's ablation) come from, whatever the look.
        Part StandardPrefab => PartFactory.Info(PartFactory.ChassisFor(Type, Model))?.partPrefab ?? Prefab;

        #region Per type

        void ApplyRcs()
        {
            foreach (ModuleRCS rcs in part.FindModulesImplementing<ModuleRCS>())
            {
                rcs.thrusterPower = (float)stats[K.ThrustVac];
                double vac = Math.Max(1, stats[K.IspVac]), sl = Math.Max(0.001, stats[K.IspSL]);
                var curve = new FloatCurve();
                float slope = (float)(sl - vac);
                curve.Add(0f, (float)vac, slope, slope);
                curve.Add(1f, (float)sl, slope, slope);
                if (vac > sl) curve.Add((float)(1 + sl / (vac - sl)), 0.001f, slope, 0f);
                rcs.atmosphereCurve = curve;
            }
        }

        void SetWheel(double torque, double ecRate)
        {
            foreach (ModuleReactionWheel w in part.FindModulesImplementing<ModuleReactionWheel>())
            {
                w.PitchTorque = w.YawTorque = w.RollTorque = (float)torque;
                foreach (ModuleResource r in w.resHandler.inputResources)
                    if (r.name == "ElectricCharge") r.rate = ecRate;
            }
        }

        // A look keeps its model but takes PIP's standard shield's ablation numbers.
        static readonly string[] AblatorFields = { "lossExp", "lossConst", "ablationTempThresh", "reentryConductivity" };

        void ApplyShield()
        {
            ModuleAblator prefab = StandardPrefab?.FindModuleImplementing<ModuleAblator>();
            foreach (ModuleAblator a in part.FindModulesImplementing<ModuleAblator>())
            {
                if (prefab == null) continue;
                foreach (string f in AblatorFields)
                {
                    var fi = typeof(ModuleAblator).GetField(f);
                    if (fi != null) fi.SetValue(a, fi.GetValue(prefab));
                }
                a.pyrolysisLossFactor = prefab.pyrolysisLossFactor * stats[HeatShields.AblatorFactor];
            }
            part.maxTemp = part.skinMaxTemp = stats[HeatShields.MaxTemp];
        }

        // Output from the program; how much wind and g-force a deployable takes is
        // PIP's standard deployable's, times Durability. Near Future's curved panels
        // keep their output in TotalEnergyRate.
        void ApplySolar()
        {
            ModuleDeployableSolarPanel prefab = StandardPrefab?.FindModuleImplementing<ModuleDeployableSolarPanel>();
            foreach (ModuleDeployableSolarPanel p in part.FindModulesImplementing<ModuleDeployableSolarPanel>())
            {
                p.chargeRate = (float)stats[K.EcRate];
                if (!SolarPanels.Deployable(Model) || prefab == null) continue;
                p.retractable = stats[SolarPanels.Retractable] > 0;
                p.windResistance = (float)(prefab.windResistance * stats[SolarPanels.Toughness]);
                p.gResistance = prefab.gResistance * stats[SolarPanels.Toughness];
            }
            SetField("ModuleCurvedSolarPanel", "TotalEnergyRate", (float)stats[K.EcRate]);
            // Kerbalism runs panels itself, from a rate it reads off the panel before
            // this module has set it.
            SetField("SolarPanelFixer", "nominalRate", stats[K.EcRate]);
        }

        // Range, cone, speed and power (DESIGN.md 29.2), on RemoteTech's antenna when it's
        // installed, stock's transmitter otherwise. Speed is in Kerbalism's terms when its
        // science is on (packets of MB, one a second), stock's otherwise (Mits a packet).
        void ApplyAntenna()
        {
            bool dish = Antennas.Dish(Model), kerbalism = Antennas.KerbalismScience;
            double range = stats[Antennas.RangeKey], cone = stats[Antennas.ConeKey];
            double kb = stats[Antennas.SpeedKbKey], mits = stats[Antennas.SpeedMitsKey];
            bool rt = false;
            foreach (PartModule m in part.Modules)
            {
                if (m.moduleName == "ModuleRTAntenna")
                {
                    rt = true;
                    if (dish)
                    {
                        SetValue(m, "Mode1DishRange", range);
                        SetValue(m, "DishAngle", cone);
                        // RemoteTech works its cone out once, as the part loads, and saves it with
                        // the ship: set only DishAngle and the look's own cone stays.
                        SetValue(m, "RTDishCosAngle", Math.Cos(cone / 2 * Math.PI / 180));
                    }
                    else SetValue(m, "Mode1OmniRange", range);
                    SetValue(m, "EnergyCost", stats[K.EcRate]);
                    // Kerbalism reads these; RemoteTech copies them into its science sender.
                    double interval = kerbalism ? 1 : Positive(GetValue(m, "RTPacketInterval"), 0.3);
                    SetValue(m, "RTPacketInterval", interval);
                    SetValue(m, "RTPacketSize", kerbalism ? kb / 1000 : mits * interval);
                }
                else if (m.moduleName == "ModuleRTDataTransmitter")
                {
                    // RemoteTech's sender, made while the antenna is switched on.
                    double interval = kerbalism ? 1 : Positive(GetValue(m, "PacketInterval"), 0.3);
                    SetValue(m, "PacketInterval", interval);
                    SetValue(m, "PacketSize", kerbalism ? kb / 1000 : mits * interval);
                }
            }
            if (rt) return;
            foreach (ModuleDataTransmitter t in part.FindModulesImplementing<ModuleDataTransmitter>())
            {
                SetValue(t, "antennaPower", range);
                if (kerbalism)
                {
                    // Kerbalism: MB a second, and Power draw is what sending costs a second.
                    SetValue(t, "packetInterval", 1);
                    SetValue(t, "packetSize", kb / 1000);
                    SetValue(t, "packetResourceCost", stats[K.EcRate]);
                }
                else
                {
                    // Stock: Mits a packet at the transmitter's own pace, and a charge per Mit.
                    double interval = Positive(t.packetInterval, 0.6);
                    double size = mits * interval;
                    SetValue(t, "packetInterval", interval);
                    SetValue(t, "packetSize", size);
                    SetValue(t, "packetResourceCost", stats[Antennas.MitCostKey] * size);
                }
            }
        }

        static double Positive(double v, double otherwise) => v > 0 && !double.IsNaN(v) ? v : otherwise;

        // A field of another mod's module by name, in that field's own type (float or double).
        internal static void SetValue(PartModule m, string field, double value)
        {
            System.Reflection.FieldInfo f = m.GetType().GetField(field, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(m, Convert.ChangeType(value, f.FieldType, System.Globalization.CultureInfo.InvariantCulture));
        }

        internal static double GetValue(PartModule m, string field)
        {
            object v = m.GetType().GetField(field, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(m);
            return v == null ? double.NaN : Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        void ApplyProbe()
        {
            int level = (int)Math.Round(stats[ProbeCores.Sas]);
            foreach (ModuleSAS sas in part.FindModulesImplementing<ModuleSAS>())
            {
                // ModuleSAS resets its level from targetSASServiceLevel every physics frame
                // (taken from the config when it starts), so both are set (DESIGN.md 27.3).
                sas.SASServiceLevel = sas.targetSASServiceLevel = Math.Max(0, level);
                sas.moduleIsEnabled = level >= 0;
                sas.enabled = level >= 0;
            }
            foreach (ModuleCommand c in part.FindModulesImplementing<ModuleCommand>())
                foreach (ModuleResource r in c.resHandler.inputResources)
                    if (r.name == "ElectricCharge") r.rate = stats[K.EcRate];
            SetWheel(stats[ProbeCores.TorqueKey], stats[ProbeCores.TorqueKey] * 0.03);
        }

        void ApplyTank()
        {
            part.crashTolerance = (float)stats[FuelTanks.Crash];
            part.maxTemp = part.skinMaxTemp = stats[FuelTanks.MaxTemp];
            // Boil-off is PIP's own module (ModulePipBoiloff), which reads BoiloffFactor.
        }

        // A Mk2 fuselage's body lift goes with its length, as stock's do (0.35 short,
        // 0.7 long), whatever the look's own model had. Mk3 fuselages have none.
        void ApplyLift()
        {
            double length = PlaneFuselages.Length(Variant);
            if (length <= 0) return;
            foreach (ModuleLiftingSurface l in part.FindModulesImplementing<ModuleLiftingSurface>())
                if (!(l is ModuleControlSurface)) l.deflectionLiftCoeff = (float)(PlaneFuselages.Mk2LiftPerMeter * length);
        }

        // Stack and radial decouplers alike (ModuleDecouple, ModuleAnchoredDecoupler).
        void ApplyDecoupler()
        {
            part.crashTolerance = (float)stats[Decouplers.Crash];
            part.maxTemp = part.skinMaxTemp = stats[Decouplers.MaxTemp];
            foreach (ModuleDecouplerBase d in part.FindModulesImplementing<ModuleDecouplerBase>())
                d.ejectionForce = (float)stats[Decouplers.Eject];
        }

        // Area from the program; the speed curve from PIP's standard intake of this
        // speed type, stretched by Top speed.
        void ApplyIntake()
        {
            ModuleResourceIntake prefab = StandardPrefab?.FindModuleImplementing<ModuleResourceIntake>();
            foreach (ModuleResourceIntake i in part.FindModulesImplementing<ModuleResourceIntake>())
            {
                i.area = stats[AirIntakes.Area];
                if (prefab == null) continue;
                i.intakeSpeed = prefab.intakeSpeed;
                // No curve (the shock cone): behave exactly as the standard part does.
                if (prefab.machCurve?.Curve == null || prefab.machCurve.Curve.keys.Length == 0) { i.machCurve = prefab.machCurve; continue; }
                var curve = new FloatCurve();
                float f = (float)Math.Max(0.1, stats[K.Speed]);
                foreach (Keyframe k in prefab.machCurve.Curve.keys) curve.Add(k.time * f, k.value, k.inTangent / f, k.outTangent / f);
                i.machCurve = curve;
            }
        }

        // The resource a type's program fills its part with, and how much. The part
        // (SetResource) and its VAB tooltip (PartFactory) both read it from here.
        public static bool Held(string typeId, Stats s, out string resource, out double amount)
        {
            switch (typeId)
            {
                case "battery": resource = "ElectricCharge"; amount = s[Batteries.Ec]; return true;
                case "probe": resource = "ElectricCharge"; amount = s[ProbeCores.BatteryKey]; return true;
                case "shield": resource = "Ablator"; amount = s[HeatShields.AblatorAmount]; return true;
                default: resource = null; amount = 0; return false;
            }
        }

        // Resources only change in the VAB: a built part keeps what it was built with.
        void SetResource(string name, double amount)
        {
            if (!HighLogic.LoadedSceneIsEditor) return;
            PartResource r = part.Resources[name];
            if (r == null) return;
            r.maxAmount = amount;
            r.amount = amount;
        }

        // Kerbalism's RTG decays (ModuleRadioisotopeGenerator); without it the stock RTG's
        // generator makes the program's power, steady. It used to keep stock's 0.75 EC/s.
        void ApplyRtg()
        {
            bool decays = false;
            foreach (PartModule m in part.Modules)
                if (m.moduleName == "ModuleRadioisotopeGenerator")
                {
                    decays = true;
                    SetValue(m, "BasePower", stats[K.EcRate]);
                    SetValue(m, "HalfLife", stats[Rtgs.HalfLife]);
                }
            if (decays) return;
            foreach (ModuleGenerator g in part.FindModulesImplementing<ModuleGenerator>())
                foreach (ModuleResource r in g.resHandler.outputResources)
                    if (r.name == "ElectricCharge") r.rate = stats[K.EcRate];
        }

        void SetField(string module, string field, object value)
        {
            foreach (PartModule m in part.Modules)
                if (m.moduleName == module) m.Fields[field]?.SetValue(value, m);
        }

        #endregion

        #region Mass and cost

        // The part's whole mass for this program; other modules' extra mass
        // (B9PartSwitch tank mass) is counted first, so the total comes out right.
        double DesiredMass(float defaultMass, double others)
        {
            if (!(Type is TankKind)) return stats.Mass;
            // Today's tank of this volume and propellant (the tank type's own
            // mass, from B9PartSwitch), times the Weight factor.
            double today = others > 0 ? others : defaultMass * stats[FuelTanks.VolumeKey] / 400.0;
            return defaultMass + today * stats[FuelTanks.WeightFactor];
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation situation)
        {
            if (IsPrefab || !Assigned) return 0f;
            if (!statsReady) Recompute();
            double others = 0;
            foreach (PartModule m in part.Modules)
                if (m != this && m is IPartMassModifier mm) others += mm.GetModuleMass(defaultMass, situation);
            return (float)(DesiredMass(defaultMass, others) - defaultMass - others);
        }

        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;

        // KSP counts a part's listed cost as including full resources, so the
        // build price (empty) plus what it holds when full.
        public float GetModuleCost(float defaultCost, ModifierStagingSituation situation)
        {
            if (IsPrefab || !Assigned) return 0f;
            if (!statsReady) Recompute();
            double full = 0;
            foreach (PartResource r in part.Resources)
                full += r.maxAmount * (PartResourceLibrary.Instance?.GetDefinition(r.resourceName)?.unitCost ?? 0);
            return (float)(stats.BuildPrice + full - defaultCost);
        }

        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;

        #endregion

        public override string GetInfo() => ModulePipEngine.InfoPrefix + " Its stats come from its program.";
    }
}
