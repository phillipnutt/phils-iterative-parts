# Phil's Iterative Parts (PIP)

A Kerbal Space Program 1.12 mod. Instead of unlocking finished parts, you start
**development programs**: pick a design (fuel, size, nozzle…), spend points on
what matters (thrust, efficiency, weight, reliability…), and the program gets
better as you fly it, from Mk. 0 to Mk. 10. Sixteen part types are covered:
liquid, solid, monopropellant, nuclear, electric and jet engines, plus RCS,
batteries, reaction wheels, heat shields, solar panels, antennas, probe cores,
fuel tanks, RTGs and air intakes. Any installed part can lend a program its
model ("looks").

The full design and build log is [DESIGN.md](DESIGN.md).

## Requirements

CKAN installs these with PIP. Without any one of them PIP stays off and says
which is missing at the main menu.

| Mod | Why |
|---|---|
| Harmony 2 | PIP's plugin patches the game with it |
| Module Manager | builds PIP's parts |
| B9PartSwitch | part sizes, tank volumes and propellants, looks |
| Community Tech Tree | where PIP's unlocks live |
| CryoTanks | hydrogen and methane fuels, and the tank switch PIP's tanks use |

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
| Kerbalism | nuclear engines get a Shielding category (radiation) instead of Gimbal |
| RemoteTech | antennas use RemoteTech ranges; stock CommNet otherwise |

An option that needs a missing mod shows a padlock that names it, and PIP never
charges for a program it can't make a part for.

## Saves

- PIP's settings are in each save's **Difficulty Options → PIP**: per part type,
  Off, Alongside stock parts, or Replace stock parts.
- A save that was already under way before PIP was installed gets every type
  **Off**, so its stock parts stay as they are. Switch types on there if you
  want them.

## Building

Visual Studio 2022 or later (MSBuild), .NET Framework 4.7.2. `PIP.csproj` points
at the game folder (`KspDir`) and copies the DLL and `GameData\PIP` into it
after a build; build with `/p:NoInstall=true` to leave the game alone. The chassis
configs are generated: run `Tools\Generate-Chassis.ps1`,
`Tools\Generate-PartChassis.ps1` and `Tools\Generate-Upgrades.ps1`.
`Tools\Test-Core.ps1` tests the rules in `Core\`.
