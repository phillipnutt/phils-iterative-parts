using System;
using System.Collections.Generic;
using UnityEngine;

namespace PIP
{
    // PIP's own boil-off (DESIGN.md sections 19 and 19.7). A cryogenic fuel boils
    // away unless the tank can be cooled, its cooling is on and the vessel has
    // electric charge to run it. Rates per resource come from Fuels.cfg
    // (PIP_BOILOFF, CryoTanks' numbers for hydrogen and methane). A cryogenic tank's
    // Insulation scales the loss and the cooling cost; a standard tank has no
    // insulation and no cooling (canCool = false), so it boils at the uninsulated
    // rate. A vessel that comes back after being away catches up: if it was cooled
    // and powered when last seen it lost nothing; otherwise it boiled off.
    public class ModulePipBoiloff : PartModule
    {
        [KSPField] public bool canCool = true;
        [KSPField(isPersistant = true)] public bool coolingOn = true;
        [KSPField(isPersistant = true)] public bool cooledLast = true;
        [KSPField(isPersistant = true)] public double lastUT = -1;

        [KSPField(guiName = "Boil-off", guiActive = true, guiActiveEditor = true)]
        public string status = "";

        // % of the fuel lost per hour, and EC per second per 1,000 units to stop it.
        public static readonly Dictionary<string, double[]> Rates = new Dictionary<string, double[]>
        {
            { "LqdHydrogen", new[] { 0.05, 0.09 } },
            { "LqdMethane", new[] { 0.005, 0.045 } },
        };

        double nextEditorUpdate;
        bool Cooling => canCool && coolingOn;

        double Factor
        {
            get
            {
                ModulePipPart p = part.FindModuleImplementing<ModulePipPart>();
                return p == null ? 1 : Math.Max(0.01, p.BoiloffFactor);
            }
        }

        [KSPEvent(guiName = "Turn cooling off", guiActive = true, guiActiveEditor = true)]
        public void ToggleCooling()
        {
            coolingOn = !coolingOn;
            UpdateEvent();
        }

        void UpdateEvent()
        {
            BaseEvent e = Events[nameof(ToggleCooling)];
            if (e == null) return;
            e.guiName = coolingOn ? "Turn cooling off" : "Turn cooling on";
            // Only on a tank that can be cooled, and only while it holds something that boils.
            Needs(true, out double ec, out _);
            e.guiActive = e.guiActiveEditor = canCool && ec > 0;
        }

        void ShowStatus(bool show)
        {
            BaseField f = Fields[nameof(status)];
            if (f != null) f.guiActive = f.guiActiveEditor = show;
        }

        public override void OnStart(StartState state)
        {
            UpdateEvent();
            if (!HighLogic.LoadedSceneIsFlight) return;
            double now = Planetarium.GetUniversalTime();
            if (lastUT > 0 && now > lastUT && !(Cooling && cooledLast)) Boil(now - lastUT);
            lastUT = now;
        }

        // EC per second to keep what's in the tank cold, and the loss per hour without it.
        void Needs(bool full, out double ecPerSecond, out double lossPerHour)
        {
            ecPerSecond = 0;
            lossPerHour = 0;
            double f = Factor;
            foreach (PartResource r in part.Resources)
            {
                if (!Rates.TryGetValue(r.resourceName, out double[] rate)) continue;
                double amount = full ? r.maxAmount : r.amount;
                if (amount <= 0) continue;
                ecPerSecond += rate[1] * f * amount / 1000.0;
                lossPerHour = Math.Max(lossPerHour, rate[0] * f);
            }
        }

        void Update()
        {
            if (!HighLogic.LoadedSceneIsEditor || Time.time < nextEditorUpdate) return;
            nextEditorUpdate = Time.time + 1;
            Needs(true, out double ec, out double loss);
            ShowStatus(loss > 0);
            UpdateEvent();
            if (loss <= 0) status = "";
            else if (!canCool) status = $"{loss:0.###}%/h: no insulation or cooling";
            else status = $"{loss:0.###}%/h, or {ec:0.###} EC/s to cool";
        }

        void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null) return;
            double now = Planetarium.GetUniversalTime();
            double dt = lastUT > 0 ? now - lastUT : 0;
            lastUT = now;
            if (dt <= 0) return;
            Needs(false, out double ec, out double loss);
            ShowStatus(loss > 0);
            if (loss <= 0) { status = ""; cooledLast = true; return; }
            if (Cooling)
            {
                double want = ec * dt;
                double got = part.RequestResource(PartResourceLibrary.ElectricityHashcode, want);
                if (got >= want * 0.999)
                {
                    cooledLast = true;
                    status = $"none (cooling {ec:0.###} EC/s)";
                    return;
                }
                // Only part of the power: the rest of the time it boils.
                Boil(dt * (1 - got / Math.Max(want, 1e-12)));
                cooledLast = false;
                status = $"{loss:0.###}%/h: not enough power";
                return;
            }
            Boil(dt);
            cooledLast = false;
            status = canCool ? $"{loss:0.###}%/h, cooling off" : $"{loss:0.###}%/h, no insulation";
        }

        // A share of each cryogenic fuel, compounding by the hour.
        void Boil(double seconds)
        {
            if (seconds <= 0) return;
            double f = Factor, hours = seconds / 3600.0;
            foreach (PartResource r in part.Resources)
            {
                if (!Rates.TryGetValue(r.resourceName, out double[] rate) || r.amount <= 0) continue;
                double keep = Math.Pow(Math.Max(0, 1 - rate[0] * f / 100.0), hours);
                r.amount = Math.Max(0, r.amount * keep);
            }
        }

        public override string GetInfo() => canCool
            ? "Cryogenic fuels boil off unless the tank is cooled, which takes electric charge."
            : "No insulation or cooling: cryogenic fuels in it boil off.";
    }
}
