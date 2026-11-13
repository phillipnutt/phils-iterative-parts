# Changes

Written by Claude (AI) at Phil Nutt's direction, like the rest of PIP (see
[NOTICE.md](NOTICE.md)). The reasoning behind each change is in
[DESIGN.md](DESIGN.md); fuel numbers and sources are in [FUELS.md](FUELS.md).

## Unreleased

**Rename programs** (DESIGN.md 22.1): the pencil after the Mk badge on a
program's page. The whole family takes the new name (Kestrel I, II, Max become
Falcon I, II, Max), free, with the same rules as a new program's name. Craft
files and vessels in flight keep working; parts already built show the new name
the next time they load.

**Suggested names** (DESIGN.md 22.2): 751 names in a themed group for each part
type, tied to what the part does: birds of prey for liquid engines, winds for
jets, casks and great bottles for tanks, dances for RCS, turtles and shields for
heat shields, and so on. The die now deals them at random (each comes up once
before any repeats) instead of going down a list of 30, and a suggested name
changes with the type you pick.

**Change all** (DESIGN.md 23): a new top row in PIP's difficulty settings sets
every part type, in both columns, to Off, Alongside or Replace at once. It shows
Mixed while the rows differ.

**Simpler code** (DESIGN.md 24): places where several pieces of code decided the
same thing now have one rule each, chiefly the rules for what an installed part
is, which Replace and looks each had a copy of. Nothing changes in play.

**Recovery experience you can see** (DESIGN.md 25): after a recovery, KSP's
message inbox (the envelope on the toolbar) says what each program gained: a
new Mk, or how far it is to the next one. The pop-ups this replaces were hidden
behind the recovery screen. KSP.log now has a line for each flight and recovery
logged. Recoveries through Phil's Reusability Suite always counted.

**One Mk rule, wherever a part is built** (DESIGN.md 26)
- **One build price at every Mk:** a program version's parts cost what its
  Mk. 1 does. Stats still improve with Mk. The program window shows one price.
- **A part's window shows its own Mk.** The parts list's title no longer has a
  Mk ("Augur I"), because it's shared by parts built at different Mks. A part's
  right-click window is titled with the Mk that part was built at.
- **New parts are the current Mk wherever the build starts.** The VAB's rule
  (newest version, current Mk) is now one step that other mods can call too,
  so a build Phil's Reusability Suite starts at the Space Center gets the same
  parts as one from the VAB. Flown and built parts keep their Mk, in the VAB
  too when the Suite brings them there, unless they're updated on purpose
  through the Suite.
- **Fixed:** a part reused from an earlier flight (a hangar copy, a parts-bin
  part) didn't count its new flight, and a craft recovered straight off the
  pad could still gain recovery experience.

**Autopilot steps** (DESIGN.md 27.3): a probe core's SAS holds attitude from 10
autopilot points, prograde and retrograde from 50, normal, radial and target
from 75, the maneuver node from 100. Each probe research entry says which
modes it brings.

**Entry purchase** (DESIGN.md 27.4): with KSP's "Entry Purchase Required on
Research" on, PIP's research entries are bought in R&D like stock parts, a
part type's first programs' too, at 30 funds per science point of the node
(times the PIP prices option). With it off, research alone opens them, as
before. In a game already under way, the entries for nodes it had researched
count as bought. The program window says what's left: research a node, or buy
an entry. The size tooling entries now say they open every type's sizes.

**Antennas: Omni, Wide Dish, Focused Dish** (DESIGN.md 29.2)
- **The kind sets the reach:** omni 0.5 to 6 Mm, Wide Dish 5 to 200 Mm, Focused
  Dish 5 to 500 Gm. Range points move it along that, and widen a dish's cone
  with it (Wide 12.5° to 90°, Focused 0.025° to 0.25°).
- **New slider, Data rate:** top speed, the same for every kind. It's 1 to 900
  kB/s with Kerbalism's science on, 2 to 40 Mits/s without. Speed no longer
  comes from the look.
- **Power draw now counts everywhere:** what the antenna draws while on (with
  RemoteTech), uses while sending (Kerbalism without RemoteTech), or charges per
  Mit sent (a plain game, where it did nothing before).
- **Fixed:** under RemoteTech, a dish's cone was always its look's own.
- **Antennas already in a game get no worse:** short-range dishes become Wide
  and interplanetary ones Focused, both on the same points. Deep-space ones
  become Focused with their reach kept, keeping their size. Each keeps at least
  its look's speed, and its price; its cone follows its Range points.

**Ships out of range use their program** (DESIGN.md 29.3): each program's part
now has its own master copy, carrying the program's numbers at its current Mk.
Kerbalism reads a ship out of physics range from it, so a background probe sends
at its antenna program's speed and an RTG gives its program's power, not the
basic part's.

**Fixed** (DESIGN.md 29.4): without Kerbalism, a PIP RTG made stock's 0.75 EC/s
whatever its program; it now makes its program's power.

**Fixed** (DESIGN.md 29.1): with ReStock, PIP's probe core, dish antenna, RCS,
heat shield, decoupler and separator showed two overlapping shells that
flickered (the Seer probe: half gold, half silver). Every PIP part now keeps its
model's paint variants, paint only, so one shell shows, and the paint can be
picked in the VAB.

**Fixed** (DESIGN.md 28): with Hide Empty Tech Tree Nodes installed, R&D hid
nodes whose stock parts Replace had hidden, so they, and the nodes after them,
couldn't be researched. A node now never looks empty because of PIP. The stock
parts stay hidden.

**Fixed** (DESIGN.md 27):
- A probe core offered every SAS mode its chassis part (the OKTO2) had,
  whatever its program's autopilot points.
- Changing the paint variant of a held part wearing a shrunk look (a 1.25 m
  tank with a 3.75 m tank's look) moved its top and bottom nodes to the
  full-size model's spacing.
- On the New program page, a locked mount (Side-mount on a decoupler before
  Stability) looked open; it now shows its lock and what to research.

**Fixed:** a battery's part tooltip listed its chassis's 100 electric charge, not
what the program holds; heat shields (ablator) and probe cores (built-in battery)
had the same mistake. The part itself always held the right amount.

## 0.4.0 (2026-11-06, pre-release)

**Decouplers** (DESIGN.md 21.1): a new part type. A program is a stack
decoupler or a separator, 0.625 to 5 m, or a side-mount (radial) decoupler,
small, medium or large. Sliders: Ejection force, Weight, Strength. Any installed
decoupler or separator can lend it its look. With Replace, stock and mod stack
decouplers, separators and radial decouplers are hidden; engine plates, docking
ports, hardpoints and 7.5 m parts stay. Unlocks from Engineering 101.

**Plane fuselages** (DESIGN.md 21.2): a new part type for Mk2 and Mk3 fuel
fuselages, tanks in every other way (kinds, any fuel, the same sliders). Mk2
short and long, Mk3 short, medium and long, holding what stock's do; Mk2 keeps its
body lift. In the VAB they're under Fuel Tanks. Unlocks from Supersonic Flight.

**Plane adapters:** tank adapter programs can also be Mk3 to Mk2, Mk3 to 2.5 m or
3.75 m, 2.5 m to Mk2, Mk2 to 1.25 m (short or long), or the Mk2 bicoupler, each
wearing its stock part. Replace hides the stock ones (not the slanted Mk3 to 2.5 m).

**Probes:** a probe program can be Mk2-shaped, wearing the Mk2 Drone Core. Replace
already hid the drone core; now there's a program to replace it.

**Games already under way:** a part type that's new in a version of PIP starts
off in a game that was already under way, so it doesn't hide that game's stock
parts unasked. The difficulty settings can turn it on. Brand-new games get it on
Replace, as usual.

**Fixed:** a decoupler's part tooltip showed its chassis's ejection force; an
adapter tank's listed its placeholder propellant as 1.0 units.

## 0.3.0 (2026-11-06, pre-release)

**Radial and adapter tanks** (DESIGN.md 20)
- A fuel tank program can be **side-mount** (radial): tiny, small, medium, large
  or huge, by volume (5 to 1,600). It wears any installed radial tank, scaled; the
  Baguette, Stratus-V and PB-X50R are the standard looks. Unlocks at Propulsion
  Systems, large and huge with their tooling.
- A fuel tank program can be an **adapter** between two sizes (2.5 to 1.25 m and
  eight other pairs), short or long. It wears an installed adapter of the same
  taper: the C7 for 2:1, the ADTP-2-3 for 3:2; 4:3 and 3:1 need Making History,
  ReStock+ or Near Future Launch Vehicles. Each taper holds what its stock
  adapter does (a long 2.5 to 1.25 m holds 800). Unlocks at Fuel Systems, plus
  each size's tooling.
- An adapter can be set to **Empty** on the part: a plain structural adapter at a
  quarter of the weight.
- **Replace** now also hides stock and mod radial tanks, fuel adapters and plain
  structural adapters. Kept: slanted, multi, skeletal, truss and station adapters,
  nosecones, cluster tanks, ore tanks, fuel ducts.
- Replace and looks now recognize tanks by any propellant switch, not only one
  named `fuelSwitch`, so Near Future Exploration's MLT-R tanks are replaced too.
- Once a session the log lists every tank Replace hides and every fuel tank it
  leaves.

## 0.2.0 (2026-11-05, pre-release)

**Requirements changed:** PIP no longer needs CryoTanks. It needs **Community
Resource Pack** instead (CKAN installs it). CryoTanks can stay installed; PIP
doesn't use it.

**Fuels** (DESIGN.md 19, FUELS.md)
- Liquid engine programs choose from eleven fuels instead of three: kerolox,
  ethalox, nitric acid, peroxide, Aerozine, hydrolox, syntin, methalox, FLOX,
  ClF5 and fluorine, each unlocked at its own tech node.
- Isp comes from real propellant data (one table for every fuel); mixture ratios
  and tank recipes follow real densities.
- The fuels are a config file (`GameData/PIP/Fuels.cfg`), so another mod's fuel
  can be added with a `PIP_FUEL` node, no code. PIP makes its engines, looks and
  tank recipe.

**Tanks**
- PIP's tanks use their own propellant switch, so they work without CryoTanks.
- **Standard tanks hold every fuel** but have no insulation or cooling, so
  hydrogen, methane and fluorine boil off in them (hydrogen 0.2% an hour).
- **Cryogenic tanks** now unlock at Large Volume Containment (300 science,
  was 160). They have Insulation and a cooling switch: no boil-off while the
  vessel has electric charge.
- **Tanks weigh by size, like stock**, whatever they hold, so dense fuels carry
  more propellant per ton of tank (ClF5 12 tons per ton of tank, kerolox 8).

**Boil-off:** PIP's own. Loss per hour unless cooled; Insulation scales the loss
and the power; a vessel catches up on what it lost while away.

**Prices:** PIP sets the prices of the nine Community Resource Pack resources its
fuels use, so filling a tank costs at most about 4 times kerolox (a Jumbo-64 of
ClF5: about 10,400 instead of 311,000). Skipped when Real Fuels is installed.

**Plumes:** with Waterfall installed, liquid engines show their fuel's plume,
sized to the engine. PIP ships none of Waterfall's files; it names its templates.

**Fixes**
- The program window no longer fills the log with errors in a career where liquid
  engines aren't researched yet (the gimbal row asked R&D for a node that
  doesn't exist). If the window ever hits an error again, it closes and says so
  once.

**Saves:** existing programs and craft keep working. Tank propellant picks keep
their names, so saved craft load with the same propellant. Checked on craft
built during this work, not yet on a full 0.1.1 career.

**Downloads:** a second zip, "with dependencies", bundles the five required mods
unmodified for installing by hand, with their credits and licenses in
`GameData/PIP/Bundled/` (added the same day; `Tools/Make-Release.ps1` builds
both zips).

## 0.1.1 (2026-11-05, pre-release)

- Same build as 0.1.0, plus the MIT license and NOTICE.md, which says plainly
  that PIP is written by an AI.

## 0.1.0 (2026-11-05, pre-release)

First public build.
- Sixteen part types as development programs: liquid, solid, monopropellant,
  nuclear, electric and jet engines, RCS, batteries, reaction wheels, heat
  shields, solar panels, antennas, probe cores, fuel tanks, RTGs and air intakes.
- Programs improve as they fly, Mk. 1 to Mk. 5, then move on to their next
  version (I, II, III … Max).
- Any installed part can lend a program its model ("looks"); side-mount parts.
- Unlocks in the Community Tech Tree; stock stand-ins when optional mods are
  missing.
- Required Harmony 2, Module Manager, B9PartSwitch, Community Tech Tree and
  CryoTanks.
