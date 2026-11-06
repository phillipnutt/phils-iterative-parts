using System.Linq;

namespace PIP
{
    // Reads the tech tree (Community Tech Tree ids) for each part type: the node
    // that opens its first programs, the nodes that open each choice, and the
    // three nodes that raise its one cap from 25 to 50, 75 and 100. The PARTUPGRADE
    // entries in Upgrades.cfg only display these in R&D.
    public static class TechUnlocks
    {
        static bool TreeApplies =>
            HighLogic.CurrentGame != null && ResearchAndDevelopment.Instance != null &&
            (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);

        public static bool Researched(string tech) =>
            !TreeApplies || string.IsNullOrEmpty(tech) || tech == "start" ||
            ResearchAndDevelopment.GetTechnologyState(tech) == RDTech.State.Available;

        // A node's name as R&D shows it. No node (a free option) has no name: R&D
        // throws on a null id, and that once broke the program window every frame.
        public static string Title(string tech)
        {
            if (string.IsNullOrEmpty(tech)) return "";
            string t = TreeApplies ? ResearchAndDevelopment.GetTechnologyTitle(tech) : null;
            return string.IsNullOrEmpty(t) ? tech : t;
        }

        // One cap for every category of a type.
        public static int Cap(PartType t) => 25 + 25 * t.CapTechs.Skip(1).Count(Researched);

        public static bool Opened(PartType t) => Researched(t.CapTechs[0]);

        public static bool Available(PartType t, Option o) => Opened(t) && o.Techs.All(Researched);

        // What to research first for an option: the type's first node, then each
        // node the option needs, in order.
        public static string NextFor(PartType t, Option o) =>
            !Opened(t) ? t.CapTechs[0] : o.Techs.FirstOrDefault(x => !Researched(x)) ?? o.Tech;

        public static bool Available(PartType t, string[] model, string variant = null) => t.Valid(model) && t.Techs(model, variant).All(Researched);

        public static string Missing(PartType t, string[] model, string variant = null) =>
            string.Join(", ", t.Techs(model, variant).Distinct().Where(x => !Researched(x)).Select(Title));
    }
}
