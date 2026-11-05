using System.Collections.Generic;
using UnityEngine;

namespace PIP
{
    // Experience from launches, for each part (DESIGN.md section 5): +1 for every
    // engine that fires after leaving the pad, and for every other PIP part that
    // leaves it, once per flight each. Parts register themselves.
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
            // One message per program, however many of its parts counted at once.
            foreach (KeyValuePair<Program, int[]> c in counted)
                PipScenario.Announce(c.Key, c.Value[0], c.Value[1] == 1 ? "launch logged" : $"{c.Value[1]} parts flew");
        }

        // Programs that gained experience this frame: { Mk before, parts counted }.
        readonly Dictionary<Program, int[]> counted = new Dictionary<Program, int[]>();

        static bool OnPad(Vessel v) => v == null || v.situation == Vessel.Situations.PRELAUNCH;

        void Count(Program p, Part part)
        {
            if (p == null || part == null) return;
            int before = p.Mk;
            if (!p.RecordLaunch(Program.PartKey(part.launchID, part.flightID))) return;
            if (counted.TryGetValue(p, out int[] c)) c[1]++;
            else counted[p] = new[] { before, 1 };
        }
    }
}
