using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PIP
{
    // What an antenna's numbers mean depends on what's installed (DESIGN.md 29.2):
    // RemoteTech's antennas or stock's, and Kerbalism's data rates when its science is
    // on. Read at the main menu; Kerbalism's switch is read each time it's asked, since
    // Kerbalism settles its features as it loads.
    static class Comms
    {
        public static void Detect()
        {
            Antennas.RemoteTech = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleRTAntenna") != null;
            FieldInfo science = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly?.GetType("KERBALISM.Features", false)).FirstOrDefault(t => t != null)
                ?.GetField("Science", BindingFlags.Public | BindingFlags.Static);
            Antennas.KerbalismCheck = science == null ? (Func<bool>)null : () => science.GetValue(null) is bool on && on;
            Debug.Log($"[PIP] Antennas: {(Antennas.RemoteTech ? "RemoteTech" : "stock CommNet")}, " +
                      $"Kerbalism science {(science == null ? "not installed" : Antennas.KerbalismScience ? "on" : "off")}");
        }
    }
}
