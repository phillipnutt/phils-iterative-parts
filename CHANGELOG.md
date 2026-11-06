# Changes

Written by Claude (AI) at Phil Nutt's direction, like the rest of PIP (see
[NOTICE.md](NOTICE.md)). The reasoning behind each change is in
[DESIGN.md](DESIGN.md); fuel numbers and sources are in [FUELS.md](FUELS.md).

## Unreleased (next: 0.2.0)

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
