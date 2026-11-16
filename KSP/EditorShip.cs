using UnityEngine;

namespace PIP
{
    // PIP's parts tell the editor the ship changed (new stats, mass, price) at most
    // once a frame. A craft with many PIP parts loading, an undo or an SPH/VAB switch
    // used to send it once per part, and every listener (Kerbalism's planner, Kerbal
    // Engineer, the Reusability Suite's Build panel) ran again each time, on a ship
    // still being put together (DESIGN.md 32.6).
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorShip : MonoBehaviour
    {
        static bool pending;

        public static void Changed()
        {
            if (HighLogic.LoadedSceneIsEditor) pending = true;
        }

        void LateUpdate()
        {
            if (!pending) return;
            pending = false;
            ShipConstruct ship = EditorLogic.fetch?.ship;
            if (ship != null) GameEvents.onEditorShipModified.Fire(ship);
        }

        void OnDestroy() => pending = false;
    }
}
