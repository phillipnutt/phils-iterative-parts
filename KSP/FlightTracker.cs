using System.Collections.Generic;
using UnityEngine;

namespace PIP
{
    // Experience from flights, once per program per flight (DESIGN.md section 18.2):
    // +1 when one of its engines fires after leaving the pad, or another of its
    // parts leaves it. Parts register themselves.
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
            counted.Clear();
            for (int i = engines.Count - 1; i >= 0; i--)
            {
                ModulePipEngine m = engines[i];
                if (m == null || m.part == null) { engines.RemoveAt(i); continue; }
                if (!m.IsBurning || OnPad(m.vessel)) continue;
                m.fired = true;
                Count(scenario.Find(m.programId), m.part);
            }
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                ModulePipPart m = parts[i];
                if (m == null || m.part == null) { parts.RemoveAt(i); continue; }
                if (m.flew || !m.Assigned || OnPad(m.vessel)) continue;
                m.flew = true;
                Count(scenario.Find(m.programId), m.part);
            }
            foreach (KeyValuePair<Program, int> c in counted)
                PipScenario.Announce(c.Key, c.Value, $"flight logged (+{Progress.LaunchXp})");
        }

        // Programs that gained experience this frame, with their Mk before.
        readonly Dictionary<Program, int> counted = new Dictionary<Program, int>();

        static bool OnPad(Vessel v) => v == null || v.situation == Vessel.Situations.PRELAUNCH;

        // Once per program per flight (DESIGN.md section 18.2); a retired version's
        // parts count toward the family's newest version.
        void Count(Program p, Part part)
        {
            p = PipScenario.Instance?.Latest(p);
            if (p == null || part == null) return;
            int before = p.Mk;
            if (p.RecordLaunch(Program.FlightKey(part.launchID)) && !counted.ContainsKey(p)) counted[p] = before;
        }
    }
}
