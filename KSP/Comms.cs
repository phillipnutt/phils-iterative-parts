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
            Antennas.LookSpeed = Speed;
            Debug.Log($"[PIP] Antennas: {(Antennas.RemoteTech ? "RemoteTech" : "stock CommNet")}, " +
                      $"Kerbalism science {(science == null ? "not installed" : Antennas.KerbalismScience ? "on" : "off")}");
        }

        static Part Prefab(string look) => string.IsNullOrEmpty(look) ? null
            : (PartLoader.getPartInfoByName(look) ?? PartLoader.getPartInfoByName(look.Replace('_', '.')))?.partPrefab;

        // A look's own top speed, in Data rate's units (kB/s with Kerbalism, Mits/s
        // without): what an antenna wearing it sent before (Antennas.Upgrade).
        static double Speed(string look)
        {
            Part p = Prefab(look);
            if (p == null) return double.NaN;
            foreach (PartModule m in p.Modules)
            {
                bool rt = m.moduleName == "ModuleRTAntenna";
                if (!rt && !(m is ModuleDataTransmitter)) continue;
                double size = ModulePipPart.GetValue(m, rt ? "RTPacketSize" : "packetSize");
                double interval = ModulePipPart.GetValue(m, rt ? "RTPacketInterval" : "packetInterval");
                if (size > 0 && interval > 0) return Antennas.KerbalismScience ? size / interval * 1000 : size / interval;
            }
            return double.NaN;
        }
    }
}
