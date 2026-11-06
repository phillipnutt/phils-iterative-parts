# Phil's Iterative Parts (PIP)

> **This mod was written by an AI.** Every line of code, every config and these
> docs were written by Claude, Anthropic's AI, at Phil Nutt's direction. Phil
> designed it and play-tests it, but he isn't a programmer and no human has
> reviewed the code. It's experimental: back up your saves. Details in
> [NOTICE.md](NOTICE.md).

A Kerbal Space Program 1.12 mod. Instead of unlocking finished parts, you start
**development programs**: pick a design (fuel, size, nozzle…), spend points on
what matters (thrust, efficiency, weight, reliability…), and the program gets
better as you fly it, from Mk. 1 to Mk. 5. At Mk. 5 it can move on to its next
version (Kestrel I, II, III … Max), each one better again. Sixteen part types are covered:
liquid, solid, monopropellant, nuclear, electric and jet engines, plus RCS,
batteries, reaction wheels, heat shields, solar panels, antennas, probe cores,
fuel tanks, RTGs and air intakes. Any installed part can lend a program its
model ("looks").

The full design and build log is [DESIGN.md](DESIGN.md); what changed in each
release is in [CHANGELOG.md](CHANGELOG.md).

## Requirements

CKAN installs these with PIP. Without any one of them PIP stays off and says
which is missing at the main menu.

| Mod | Why |
|---|---|
| Harmony 2 | PIP's plugin patches the game with it |
| Module Manager | builds PIP's parts |
| B9PartSwitch | part sizes, tank volumes and propellants, looks |
| Community Tech Tree | where PIP's unlocks live |
| Community Resource Pack | the fuels beyond stock's: hydrogen, methane, and the rest of PIP's fuel table |

## Optional

Each adds better models, more looks and more engines to compare against.
Without them PIP uses stock parts as stand-ins, scaled to size; the numbers
are PIP's either way.

| Mod | Adds |
|---|---|
| Near Future Launch Vehicles | engine and RCS models at 0.625, 1.875, 3.75 and 5 m |
| Near Future Propulsion | electric engine models; **Hall and MPD engines need it** (argon, lithium) |
| Near Future Spacecraft | monopropellant engine models; **five-nozzle RCS blocks need it** |
| Cryo Engines | 1.875–5 m engine models |
| Kerbal Atomics, ReStock+ | nuclear engine models |
| Making History (DLC) | 1.875 m models |
| Kerbalism | nuclear engines get a Shielding category (radiation) |
| Waterfall (with Stock Waterfall Effects or similar) | liquid engines get their fuel's plume color and shape |
| RemoteTech | antennas use RemoteTech ranges; stock CommNet otherwise |

An option that needs a missing mod shows a padlock that names it, and PIP never
charges for a program it can't make a part for.

## Fuels

Liquid engine programs pick a fuel from a table of eleven, from ethalox (the
V-2's) to fluorine and hydrogen (the highest Isp of any chemical rocket), each
unlocked at its own tech node. Their Isp comes from real performance data; the
table, sources and rules are in [FUELS.md](FUELS.md). Every fuel fits in every
standard tank, but standard tanks have no insulation, so hydrogen, methane and
fluorine boil off in them. Cryogenic tanks come later in the tree: insulated, and
cooled while the vessel has electric charge (PIP's own boil-off; CryoTanks isn't
needed).

The table is a config, `GameData/PIP/Fuels.cfg`, so another mod's fuel can be
added without code: a `PIP_FUEL` node names its propellants, Isp and the rest,
and PIP makes its engines and tank recipe. FUELS.md says how.

## Saves

- PIP's settings are in each save's **Difficulty Options → PIP**: per part type,
  Off, Alongside stock parts, or Replace stock parts.
- A save that was already under way before PIP was installed gets every type
  **Off**, so its stock parts stay as they are. Switch types on there if you
  want them.

## License and credits

- **License:** MIT, see [LICENSE](LICENSE). It applies to the extent the work can
  be copyrighted; AI-written work may have limited copyright protection.
- **Written by:** Claude (Anthropic), directed by Phil Nutt. Phil designed the
  mod and tests it; the code is entirely AI-written and hasn't been reviewed by
  a human programmer.
- **Includes nothing from anyone else:** no other mod's code, models or
  textures, and no KSP assets. The mods PIP needs are installed separately.
  Parts copy what's already installed in your game. See [NOTICE.md](NOTICE.md).
- Unofficial fan-made mod; not affiliated with the makers of Kerbal Space
  Program or of the mods it works with.

## Building

Visual Studio 2022 or later (MSBuild), .NET Framework 4.7.2. `PIP.csproj` points
at the game folder (`KspDir`) and copies the DLL and `GameData\PIP` into it
after a build; build with `/p:NoInstall=true` to leave the game alone. The chassis
configs are generated: run `Tools\Generate-Chassis.ps1`,
`Tools\Generate-PartChassis.ps1`, `Tools\Generate-Upgrades.ps1` and
`Tools\Generate-Fuels.ps1`.
`Tools\Test-Core.ps1` tests the rules in `Core\`.
