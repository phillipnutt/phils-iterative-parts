using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PIP
{
    // Program names: the rules a name follows, and the names the new-program window
    // suggests (its dice button). Each part type has its own group, themed on what the
    // part does, so a suggested name says something about the part (DESIGN.md 22).
    // One word each, none a stock or common mod part's name (no Panther, Rapier,
    // Terrier, Poodle ...), so a program never reads like a stock part.
    // Phil, 2026-11-06: "a longer list of more interesting names", dealt at random,
    // and "name groups that actually make sense and tie into what the part is".
    public static class ProgramNames
    {
        public const int MaxLength = 30;

        // The version ("I", "II", "Max") is added for you, so a name can't end in one.
        static readonly Regex Numeral =
            new Regex(@"\s(M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})|Max)$", RegexOptions.IgnoreCase);

        // What a name is stored as: no characters that break a save file, no spaces
        // around it.
        public static string Clean(string name) =>
            (name ?? "").Replace("{", "").Replace("}", "").Replace("//", "/").Replace(",", "").Trim();

        // Why a (cleaned) name can't be used, or null when it can. taken: whether
        // another program already has it.
        public static string Problem(string name, Func<string, bool> taken)
        {
            if (string.IsNullOrEmpty(name)) return "Give the program a name.";
            if (name.Length > MaxLength) return $"Keep the name to {MaxLength} characters.";
            Match m = Numeral.Match(name);
            if (m.Success && m.Value.Trim().Length > 0)
                return "Leave the version off the name: it's added for you (I, II, III ... Max).";
            if (taken != null && taken(name)) return $"There's already a program called {name}.";
            return null;
        }

        // Each part type's names, by PartType.Id.
        public static readonly Dictionary<string, string[]> ByType = new Dictionary<string, string[]>
        {
            // Liquid engines: birds of prey, firebirds, and falconry's words (a stoop is a falcon's dive).
            ["liquid"] = new[]
            {
                "Kestrel", "Peregrine", "Osprey", "Goshawk", "Gyrfalcon", "Saker", "Lanner", "Merlin", "Hobby", "Falcon",
                "Condor", "Albatross", "Lammergeier", "Eagle", "Kite", "Buzzard", "Harpy", "Caracara", "Sparrowhawk", "Shrike",
                "Griffon", "Roc", "Phoenix", "Thunderbird", "Firebird", "Garuda", "Simurgh", "Vulture", "Stoop", "Tiercel",
                "Eyas", "Erne", "Bateleur", "Aplomado", "Accipiter", "Aquila", "Strix", "Skua", "Frigatebird", "Raptor",
            },
            // Solid boosters: fireworks, cannon and siege engines.
            ["solid"] = new[]
            {
                "Squib", "Petard", "Mortar", "Howitzer", "Culverin", "Bombard", "Ballista", "Trebuchet", "Catapult", "Onager",
                "Mangonel", "Carronade", "Falconet", "Arquebus", "Blunderbuss", "Congreve", "Katyusha", "Sparkler", "Skyrocket", "Whizzbang",
                "Cherrybomb", "Banger", "Starburst", "Peony", "Chrysanthemum", "Willow", "Brocade", "Crossette", "Girandole", "Gerb",
                "Salute", "Thunderflash", "Grenade", "Broadside", "Cannonade", "Fusillade", "Salvo", "Volley", "Barrage", "Firecracker",
                "Bottlerocket",
            },
            // Monopropellant engines: small darting birds and insects.
            ["monoprop"] = new[]
            {
                "Hummingbird", "Wren", "Finch", "Dragonfly", "Damselfly", "Hornet", "Wasp", "Firefly", "Cicada", "Locust",
                "Mayfly", "Gnat", "Midge", "Sparrow", "Swallow", "Martin", "Linnet", "Siskin", "Tanager", "Warbler",
                "Kinglet", "Goldcrest", "Firecrest", "Bumblebee", "Hoverfly", "Lacewing", "Katydid", "Cricket", "Mantis", "Scarab",
                "Junebug", "Moth", "Fritillary", "Swallowtail", "Admiral", "Ladybird", "Chickadee", "Nuthatch", "Pipit", "Bunting",
                "Vireo", "Phoebe", "Robin",
            },
            // Nuclear engines: titans, giants, gods of fire and the forge, and the atom.
            ["nuclear"] = new[]
            {
                "Prometheus", "Hephaestus", "Vulcan", "Hyperion", "Cronus", "Rhea", "Theia", "Oceanus", "Coeus", "Crius",
                "Iapetus", "Themis", "Tethys", "Atlas", "Epimetheus", "Typhon", "Cyclops", "Brontes", "Steropes", "Arges",
                "Talos", "Surtr", "Muspel", "Ymir", "Jotun", "Agni", "Pele", "Brigid", "Svarog", "Wayland",
                "Ilmarinen", "Kagutsuchi", "Chantico", "Sekhmet", "Isotope", "Neutron", "Fission", "Nucleon", "Gamma", "Colossus",
                "Titan", "Behemoth",
            },
            // Electric engines: lightning, and the gods who throw it.
            ["electric"] = new[]
            {
                "Raijin", "Perun", "Indra", "Zeus", "Jupiter", "Taranis", "Shango", "Thor", "Thunderhead", "Thunderbolt",
                "Levin", "Fulgur", "Fulmen", "Fulgurite", "Sprite", "Aurora", "Plasma", "Ionosphere", "Thunderclap", "Stormfront",
                "Tempest", "Squall", "Nimbus", "Lightning", "Thunderer", "Tonitrus", "Keraunos", "Brontide", "Vajra", "Mjolnir",
                "Thunderstone", "Stormbird", "Leishen", "Ukko", "Perkunas", "Tlaloc", "Enlil", "Adad", "Teshub", "Susanoo",
            },
            // Jet engines: the world's winds, and the gods who blow them.
            ["jet"] = new[]
            {
                "Zephyr", "Mistral", "Sirocco", "Chinook", "Bora", "Foehn", "Khamsin", "Simoom", "Harmattan", "Levanter",
                "Tramontane", "Williwaw", "Haboob", "Monsoon", "Gale", "Cyclone", "Typhoon", "Whirlwind", "Derecho", "Etesian",
                "Boreas", "Notus", "Eurus", "Aeolus", "Fujin", "Vayu", "Shamal", "Pampero", "Bise", "Meltemi",
                "Gregale", "Libeccio", "Zonda", "Brickfielder", "Halny", "Koshava", "Kona", "Jetstream", "Slipstream", "Tailwind",
                "Updraft", "Swift",
            },
            // RCS thrusters: dances, small precise steps and turns.
            ["rcs"] = new[]
            {
                "Pirouette", "Arabesque", "Fouette", "Entrechat", "Glissade", "Sashay", "Waltz", "Tango", "Polka", "Mazurka",
                "Gavotte", "Minuet", "Galliard", "Pavane", "Quadrille", "Reel", "Jig", "Hornpipe", "Fandango", "Bolero",
                "Rumba", "Samba", "Foxtrot", "Quickstep", "Shimmy", "Cancan", "Charleston", "Lindy", "Jive", "Tarantella",
                "Saltarello", "Farandole", "Bourree", "Sarabande", "Allemande", "Courante", "Rigadoon", "Passepied", "Cotillion", "Habanera",
                "Chaconne",
            },
            // Batteries: the pioneers of electricity, electric fish, and amber (the Greeks' "elektron").
            ["battery"] = new[]
            {
                "Volta", "Galvani", "Faraday", "Ampere", "Ohm", "Coulomb", "Leyden", "Franklin", "Gilbert", "Davy",
                "Daniell", "Leclanche", "Plante", "Edison", "Tesla", "Maxwell", "Kelvin", "Henry", "Weber", "Siemens",
                "Oersted", "Joule", "Watt", "Gauss", "Kirchhoff", "Torpedo", "Electrophorus", "Numbfish", "Stargazer", "Capacitor",
                "Accumulator", "Dynamo", "Anode", "Cathode", "Amber", "Electrum", "Lodestone", "Magnetite", "Gramme", "Grove",
                "Bunsen",
            },
            // Reaction wheels: things that spin, from tops to whirlpools to clockwork.
            ["wheel"] = new[]
            {
                "Gyre", "Dervish", "Whirligig", "Spindle", "Bobbin", "Teetotum", "Dreidel", "Diabolo", "Yoyo", "Pinwheel",
                "Carousel", "Roundabout", "Vortex", "Eddy", "Gyro", "Orrery", "Armillary", "Capstan", "Windlass", "Whorl",
                "Spool", "Distaff", "Whirlpool", "Maelstrom", "Charybdis", "Tourbillon", "Escapement", "Sprocket", "Cog", "Flywheel",
                "Governor", "Ratchet", "Turnstile", "Spiral", "Helix", "Tornado", "Waterspout", "Dustdevil", "Gyrocompass", "Trundle",
                "Torque",
            },
            // Heat shields: turtles and armored beasts, shields, walls and armor, and the
            // salamander, which legend says walks through fire.
            ["shield"] = new[]
            {
                "Testudo", "Carapace", "Tortoise", "Terrapin", "Leatherback", "Loggerhead", "Hawksbill", "Pangolin", "Armadillo", "Glyptodon",
                "Scute", "Aegis", "Pavise", "Buckler", "Targe", "Hoplon", "Clipeus", "Mantlet", "Bulwark", "Rampart",
                "Bastion", "Parapet", "Barbican", "Palisade", "Cuirass", "Hauberk", "Brigandine", "Gambeson", "Pauldron", "Vambrace",
                "Gorget", "Bascinet", "Sallet", "Barbute", "Armet", "Salamander", "Ankylosaur", "Nautilus", "Conch", "Ironclad",
                "Monitor",
            },
            // Solar panels: sun gods, sunflowers, the dawn and light itself.
            ["solar"] = new[]
            {
                "Helios", "Sol", "Surya", "Ra", "Amaterasu", "Inti", "Apollo", "Aten", "Sunna", "Saule",
                "Utu", "Shamash", "Tonatiuh", "Sunflower", "Marigold", "Heliotrope", "Dandelion", "Buttercup", "Primrose", "Goldenrod",
                "Calendula", "Daybreak", "Sunrise", "Dayspring", "Eos", "Ushas", "Corona", "Solstice", "Equinox", "Zenith",
                "Midsummer", "Lumen", "Lux", "Radiance", "Sunbeam", "Heliostat", "Parhelion", "Sundog", "Sunburst", "Photon",
                "Prism", "Frond", "Canopy",
            },
            // Antennas: messengers, heralds and voices; signals; and the stars that broadcast.
            ["antenna"] = new[]
            {
                "Hermes", "Mercury", "Iris", "Herald", "Courier", "Envoy", "Emissary", "Legate", "Nuncio", "Harbinger",
                "Echo", "Siren", "Calliope", "Clio", "Hugin", "Munin", "Raven", "Semaphore", "Beacon", "Heliograph",
                "Telegraph", "Morse", "Marconi", "Hertz", "Pulsar", "Quasar", "Whisper", "Chorus", "Bard", "Skald",
                "Minstrel", "Troubadour", "Ambassador", "Messenger", "Tidings", "Clarion", "Trumpet", "Tocsin", "Carillon", "Lyre",
                "Rumor",
            },
            // Probe cores: seers and sages, navigators, minds and thinking machines.
            ["probe"] = new[]
            {
                "Oracle", "Sibyl", "Pythia", "Cassandra", "Delphi", "Augur", "Haruspex", "Seer", "Sage", "Mentor",
                "Minerva", "Athena", "Metis", "Sophia", "Thoth", "Mimir", "Kvasir", "Saraswati", "Nabu", "Tiresias",
                "Magus", "Pilot", "Navigator", "Helmsman", "Coxswain", "Wayfinder", "Lodestar", "Polaris", "Astrolabe", "Sextant",
                "Cortex", "Synapse", "Axon", "Cogito", "Golem", "Automaton", "Homunculus", "Daemon", "Abacus", "Babbage",
                "Lovelace", "Turing", "Antikythera",
            },
            // Fuel tanks: casks, jars and wine bottles by size, cisterns, and the camel.
            ["tank"] = new[]
            {
                "Cask", "Hogshead", "Barrel", "Firkin", "Kilderkin", "Puncheon", "Tun", "Tierce", "Rundlet", "Amphora",
                "Pithos", "Krater", "Hydria", "Flagon", "Ewer", "Carafe", "Decanter", "Demijohn", "Carboy", "Magnum",
                "Jeroboam", "Rehoboam", "Methuselah", "Salmanazar", "Balthazar", "Nebuchadnezzar", "Melchior", "Cistern", "Reservoir", "Aquifer",
                "Wellspring", "Cauldron", "Vat", "Urn", "Canteen", "Gourd", "Calabash", "Flask", "Wineskin", "Churn",
                "Silo", "Camel", "Dromedary", "Bactrian", "Pelican",
            },
            // Plane fuselages: whales, sharks and the great fish, the long bodies of the sea.
            ["plane"] = new[]
            {
                "Leviathan", "Narwhal", "Orca", "Beluga", "Humpback", "Rorqual", "Minke", "Cachalot", "Bowhead", "Finback",
                "Grampus", "Manatee", "Dugong", "Mako", "Thresher", "Hammerhead", "Megalodon", "Whaleshark", "Wobbegong", "Porbeagle",
                "Blacktip", "Manta", "Stingray", "Marlin", "Sailfish", "Swordfish", "Barracuda", "Wahoo", "Tarpon", "Dolphin",
                "Porpoise", "Albacore", "Bonito", "Bluefin", "Skipjack", "Dorado", "Kraken", "Cetus", "Sunfish", "Coelacanth",
                "Mosasaur", "Pliosaur",
            },
            // Decouplers: blades that cut loose, and partings.
            ["decoupler"] = new[]
            {
                "Cleaver", "Hatchet", "Broadaxe", "Guillotine", "Scythe", "Sickle", "Machete", "Cutlass", "Kukri", "Sabre",
                "Scimitar", "Falchion", "Katana", "Glaive", "Halberd", "Claymore", "Shears", "Secateurs", "Severance", "Schism",
                "Rift", "Sunder", "Cleave", "Breakaway", "Farewell", "Adieu", "Godspeed", "Sayonara", "Valediction", "Bowie",
                "Dirk", "Stiletto", "Tanto", "Kris", "Khopesh", "Billhook", "Tomahawk", "Labrys", "Bardiche", "Gladius",
                "Wakizashi", "Excalibur",
            },
            // RTGs: hearths, lamps and slow fires that never go out, and the pioneers of radioactivity.
            ["rtg"] = new[]
            {
                "Hestia", "Vesta", "Hearth", "Brazier", "Lantern", "Candle", "Taper", "Wick", "Ember", "Cinder",
                "Kindle", "Tinder", "Afterglow", "Smolder", "Inglenook", "Fireside", "Chimney", "Stove", "Kiln", "Forge",
                "Furnace", "Crucible", "Athanor", "Lampad", "Vigil", "Watchfire", "Pharos", "Lighthouse", "Torch", "Flambeau",
                "Glowworm", "Foxfire", "Wisp", "Lanternfish", "Anglerfish", "Phosphor", "Radium", "Curie", "Becquerel", "Rutherford",
                "Firekeeper",
            },
            // Air intakes: lungs and gills, things that gulp and filter, windcatchers,
            // and instruments that breathe through bellows.
            ["intake"] = new[]
            {
                "Bellows", "Gills", "Bronchus", "Trachea", "Snorkel", "Blowhole", "Spiracle", "Nostril", "Funnel", "Scoop",
                "Siphon", "Venturi", "Maw", "Gullet", "Windcatcher", "Badgir", "Malqaf", "Windsock", "Weathervane", "Baleen",
                "Accordion", "Concertina", "Harmonium", "Bandoneon", "Melodeon", "Bagpipe", "Ocarina", "Didgeridoo", "Inhale", "Gasp",
                "Gulp", "Lungfish", "Axolotl", "Bichir", "Mudskipper", "Pipefish", "Spoonbill", "Flamingo", "Paddlefish", "Anteater",
                "Tapir", "Elephant",
            },
        };

        // What each group is, in a few words (the dice button's tooltip).
        public static readonly Dictionary<string, string> Themes = new Dictionary<string, string>
        {
            ["liquid"] = "birds of prey and firebirds",
            ["solid"] = "fireworks, cannon and siege engines",
            ["monoprop"] = "small darting birds and insects",
            ["nuclear"] = "titans, giants and gods of the forge",
            ["electric"] = "lightning and the gods who throw it",
            ["jet"] = "the world's winds",
            ["rcs"] = "dances",
            ["battery"] = "pioneers of electricity",
            ["wheel"] = "things that spin",
            ["shield"] = "turtles, shields and armor",
            ["solar"] = "the sun, sunflowers and the dawn",
            ["antenna"] = "messengers, heralds and voices",
            ["probe"] = "seers, navigators and thinking machines",
            ["tank"] = "casks, jars and great bottles",
            ["plane"] = "whales, sharks and great fish",
            ["decoupler"] = "blades and farewells",
            ["rtg"] = "hearths, lamps and slow fires",
            ["intake"] = "lungs, gills and bellows",
        };

        public static string Theme(string typeId) =>
            typeId != null && Themes.TryGetValue(typeId, out string t) ? t : "any";

        // Every name in every group, once.
        public static readonly string[] All = ByType.Values.SelectMany(n => n).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // A part type's group; every name for a type without one.
        public static string[] For(string typeId) =>
            typeId != null && ByType.TryGetValue(typeId, out string[] names) ? names : All;
    }

    // A shuffled deck of names, dealt in order: each roll of the dice gives a random
    // name, and every name comes up once before the deck is shuffled again. Names
    // already in use (another program's, or the one showing) are skipped.
    public sealed class NameDeck
    {
        readonly Random random;
        readonly string[] names;
        readonly List<string> deck = new List<string>();
        int at;

        public NameDeck(int seed) : this(seed, ProgramNames.All) { }
        public NameDeck(int seed, string[] names) { random = new Random(seed); this.names = names; }

        // The next name not taken, or null when every name is.
        public string Next(Func<string, bool> taken)
        {
            for (int tries = 0; tries < names.Length * 2; tries++)
            {
                if (at >= deck.Count) Shuffle();
                if (deck.Count == 0) return null;
                string n = deck[at++];
                if (taken == null || !taken(n)) return n;
            }
            return null;
        }

        void Shuffle()
        {
            deck.Clear();
            deck.AddRange(names);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string t = deck[i]; deck[i] = deck[j]; deck[j] = t;
            }
            at = 0;
        }
    }
}
