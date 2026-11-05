using System.Collections.Generic;
using UnityEngine;

namespace PIP
{
    // Experience from launches, once per launch per program: +1 when an engine
    // fires after leaving the pad, or when any other PIP part leaves the pad.
    // Parts register themselves.
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightTracker : MonoBehaviour
    {
        static readonly List<ModulePipEngine> engines = new List<ModulePipEngine>();
        static readonly List<ModulePipPart> parts = new List<ModulePipPart>();

        public static void Register(ModulePipEngine e) { if (!engines.Contains(e)) engines.Add(e); }
        public static void Unregister(ModulePipEngine e) => engines.Remove(e);
        public static void Register(ModulePipPart p) { if (!parts.Contains(p)) parts.Add(p); }
        public static void Unregister(ModulePipPart p) => parts.Remove(p);

        void FixedUpdate()
        {
            PipScenario scenario = PipScenario.Instance;
            if (scenario == null) return;
            for (int i = engines.Count - 1; i >= 0; i--)
            {
                ModulePipEngine m = engines[i];
                if (m == null || m.part == null) { engines.RemoveAt(i); continue; }
                if (!m.IsBurning || OnPad(m.vessel)) continue;
                m.fired = true;
                Count(scenario.Find(m.programId), m.part.launchID);
            }
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                ModulePipPart m = parts[i];
                if (m == null || m.part == null) { parts.RemoveAt(i); continue; }
                if (m.flew || !m.Assigned || OnPad(m.vessel)) continue;
                m.flew = true;
                Count(scenario.Find(m.programId), m.part.launchID);
            }
        }

        static bool OnPad(Vessel v) => v == null || v.situation == Vessel.Situations.PRELAUNCH;

        static void Count(Program p, uint launchId)
        {
            if (p == null) return;
            int before = p.Mk;
            if (p.RecordLaunch(launchId)) PipScenario.Announce(p, before, "launch logged");
        }
    }
}
