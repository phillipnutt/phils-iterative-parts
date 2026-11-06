# PIP: Phil's Iterative Parts — design log

> Written by an AI: this design log, and all of PIP's code, were written by
> Claude (Anthropic) at Phil Nutt's direction. The decisions recorded here are
> Phil's; the wording and the code are Claude's. See [NOTICE.md](NOTICE.md).

**Status, 2026-11-06:** sixteen part types built and play-tested, in Phil's
modded game and on a clean install; fuel tank programs can be in-line, radial or
adapters (section 20). Released as 0.3.0 (pre-release); what has changed in each
release is in [CHANGELOG.md](CHANGELOG.md).

**This is a log, not one spec.** Later sections change earlier ones; where they
disagree, the later one stands. The big ones:
- **Section 18** replaced Mk. 0–10 with Mk. 1–5 and versions (I, II … Max), and
  added the Build cost slider and the free gimbal pick.
- **Section 19** replaced CryoTanks with PIP's own fuel table (eleven fuels,
  [FUELS.md](FUELS.md)), tanks, boil-off and plumes; 19.6 made dense fuels pay off
  and capped fuel prices; 19.7 let every fuel into standard tanks and moved
  cryogenic tanks to 300 science.

The first draft (2026-11-03) replaced the old "Iterative Development" design
(design blocks and development points); that version is retired.

---

## 1. The idea

Researching a technology doesn't hand you a finished engine. It lets you
**start a program**: you choose what kind of engine it is, spend money on what
it should be good at, and it rolls off the line crude and unreliable. Every
time you fly it, the program learns, and the engine improves on its own, from
**Mk. 0** to **Mk. 10**.

**Core concept:** your design sets the ceiling. A cheap early program (around
25 points in its key stats) is still not very good even at Mk. 10. A big-budget
program starts at about stock level and ends well past it.

**Keep it uncomplicated.** All the realism (chemistry limits, nozzle physics,
weight following thrust) lives under the hood. The player only ever sees plain
numbers: kN, seconds, tons, degrees, percent, funds.

---

## 2. Scope of version 1

- **Liquid rocket engines only.** Solid boosters, jets, nuclear, electric and
  RCS are untouched.
- **Fresh careers only.** PIP is not meant to be switched on in an existing save.
- Later part types (tanks, batteries, antennas, ...) will each get their own
  categories and realistic floors and ceilings, designed the same way.

### Game-start options

Shown when a new game is created, in the difficulty settings:

| Option | Choices | Default |
|---|---|---|
| Include PIP engines | on / off | on |
| Hide all other liquid rocket engines | on / off | on |
| Engine failures | off / light / normal | normal |
| PIP prices (program costs) | 25% / 50% / 100% / 200% | 100% |

"Hide" removes the other engines from the parts list and the R&D screen. It
never touches solids, jets, nuclear, electric or RCS.

---

## 3. Starting a program

Opened from a toolbar button at the Space Center and in the VAB/SPH.

### Choices (free — decisions, not costs)

| Choice | Options |
|---|---|
| **Fuel** | Kerolox, Hydrolox, Methalox (unlocked in that order) |
| **Size** | 0.625, 1.25, 1.875, 2.5, 3.75, 5 m |
| **Nozzle** | Sea level, Balanced, Vacuum, Aerospike (late unlock) |
| **Mount** | In-line (radial comes in a later version) |
| **Name** | Typed by the player, e.g. "Kestrel" |

Choices you haven't unlocked show greyed out with "unlocks at <tech node>".

### Points (cost money)

Five categories, **0 to 100 points each**. There is no limit on the total, but
each extra point costs more than the last (see Pricing). Each category is also
capped by the tech tree: 25 at the start, raised to 50, 75 and 100 by unlocks.
One cap covers all five categories at once (section 8).

| Category | What it buys |
|---|---|
| **Thrust** | kN, within a window set by size and nozzle |
| **Efficiency** | Isp, within a window set by fuel and nozzle |
| **Weight** | how light the engine is for its thrust |
| **Reliability** | how rarely it fails to light, once mature |
| **Gimbal** | steering range |

Points are fixed for the life of the program. A better design means a new
program.

---

## 4. The categories in detail

0 points is **really bad but realistic**; 100 points is **extremely good, not
game-breaking**. Values in between are a straight line.

### Thrust (kN)

Shown in vacuum, with sea-level thrust as a sub-row. KSP scales thrust with Isp,
so a vacuum nozzle makes only a fraction of its thrust on the pad.

| Size | Sea level, Balanced, Aerospike | Vacuum nozzle |
|---|---|---|
| 0.625 m | 15 → 110 | 4 → 30 |
| 1.25 m | 90 → 550 | 20 → 150 |
| 1.875 m | 250 → 1,500 | 60 → 400 |
| 2.5 m | 500 → 2,200 | 110 → 600 |
| 3.75 m | 1,500 → 5,000 | 350 → 1,400 |
| 5 m | 3,000 → 9,000 | 700 → 2,500 |

Fuel adjusts the 100-point end: hydrolox ×0.9, methalox ×1.15 (methalox has
the best thrust).

### Efficiency (Isp, seconds)

Shown in vacuum, with sea-level Isp as a sub-row. The nozzle decides how the
efficiency splits between sea level and vacuum. No amount of points makes a
vacuum nozzle good at sea level. Each cell is *sea level / vacuum*, 0 → 100
points:

| Fuel | Sea-level nozzle | Balanced | Vacuum | Aerospike |
|---|---|---|---|---|
| Kerolox | 220–310 / 245–340 | 190–285 / 270–360 | 50–100 / 300–385 | 240–305 / 280–350 |
| Hydrolox | 300–380 / 340–425 | 270–355 / 370–450 | 60–120 / 400–475 | 310–370 / 360–440 |
| Methalox | 280–340 / 300–362 | 255–320 / 320–378 | 50–110 / 340–395 | 290–332 / 315–370 |

Chemistry order holds: hydrolox has the best Isp, methalox sits between,
kerolox is lowest. The aerospike is good at every altitude and best at none.

### Weight (tons)

Weight follows the thrust you designed, so a big-thrust engine is heavier.

- **100 points:** 20% lighter than the lightest existing engine of that size
  and thrust.
- **0 points:** twice the 100-point weight.

Same rule for every nozzle; no nozzle is heavier than another. Methalox gets
the lightest engines. (Working approximation: 100-point weight = thrust ÷
(thrust-to-weight × 9.81), with thrust-to-weight 24.5 kerolox, 22 hydrolox,
28 methalox.)

### Reliability (chance to fail to light)

See Failures below. Reliability points decide how low the failure chance ends up
at Mk. 10.

### Gimbal (degrees)

Cheap up front, then a long climb.

| Points | 0 | 5 | 10 | 25 | 50 | 100 |
|---|---|---|---|---|---|---|
| Gimbal | fixed | 2° | 4° | 6.7° | 11° | 20° |

---

## 5. Mk. 0 to Mk. 10

### Experience

| Event | Experience |
|---|---|
| Each engine from this program that fires after leaving the pad | +1 |
| Each of those engines recovered | +1 |

- **Each part counts** (Phil, 2026-11-05: "Each part counts"). Nine engines on
  a booster = +9 for the launch and +9 if they're recovered. This replaces
  "once per program per launch", which Claude had proposed and Phil never
  decided. Other PIP parts (batteries, tanks, probes...) count the same way:
  each one that leaves the pad, and each one recovered.
- Each part counts once per launch, however many times it fires.
- A static fire on the pad doesn't count.
- **Boosters recovered by StageRecovery count** as recoveries.
- Reverting a flight takes back its experience, the same way revert takes back
  everything else.
- **Built 2026-11-05.** Launches and recoveries are counted per part, keyed by
  the part's launch and flight IDs ("launch:part" in the save). Recoveries used
  to be keyed by the ship's ID, which KSP reuses when the same craft is
  relaunched, so a relaunch's recovery was lost. Several parts counting at once
  give one message ("5 parts flew").

### Experience needed (Fibonacci)

| Mk. | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Step | 1 | 1 | 2 | 3 | 5 | 8 | 13 | 21 | 34 | 55 |
| Total | 1 | 2 | 4 | 7 | 12 | 20 | 33 | 54 | 88 | 143 |

For a single engine, Mk. 10 is about 72 flights that are launched and
recovered, or 143 one-way launches. Clusters get there faster: a recovered
9-engine booster earns 18 a flight, so about 8 flights.

### How good each Mk. is

At Mk. 0 a program performs **as if half its points were spent**. By Mk. 10
it has all of them. The climb follows a 1/x² curve, so the big jumps come early.

| Mk. | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Share of points | 50% | 68% | 78% | 84% | 88% | 92% | 94% | 96% | 98% | 99% | 100% |

Example: 25 weight points on a scale where 0 points = 4 t and 100 points = 1 t.
Mk. 0 = 3.63 t, Mk. 1 = 3.49 t, Mk. 4 = 3.34 t, Mk. 10 = 3.25 t.

A category with 0 points stays at its floor at every Mk.

### Built parts keep their Mk.

- In the VAB, a program's part always builds at the **current** Mk.
- Once launched, a part stays at the Mk it was built at. "Kestrel Mk. 3" flies
  as Mk. 3 even if that flight pushes the program to Mk. 4.
- When a program goes up a Mk.: "Kestrel reached Mk. 5. New builds use it."

---

## 6. Failures

- **Only one kind: the engine fails to light.** No shutdowns and no explosions.
- **One check per engine per flight**, when it is first ignited (first
  staging). There's no continuous failure calculation.
- On the pad, a failed engine just means recover and try again (full refund),
  so failures mostly matter on upper stages and big clusters.

Failure chance with 50 reliability points (normal setting):

| Mk. | 0 | 1 | 2 | 3 | 4 | 5 | 7 | 10 |
|---|---|---|---|---|---|---|---|---|
| Chance | 10% | 6.6% | 4.7% | 3.5% | 2.7% | 2.1% | 1.2% | 0.5% |

Every program starts at 10% at Mk. 0. Reliability points set the Mk. 10 value:

| Reliability points | 0 | 50 | 100 |
|---|---|---|---|
| Mk. 10 chance (kerolox, hydrolox) | 2% | 0.5% | 0.1% |
| Mk. 10 chance (methalox) | 1.5% | 0.3% | 0.05% |

Kerbalism's own engine wear model (rated burn time, rated ignitions) is
switched off for PIP engines; PIP's one check replaces it.

---

## 7. Pricing

### Starting a program (funds)

Depends only on the total points spent, not on size, fuel or nozzle.

**A program starts at 1,000 funds, and every point adds 1%.** That is,
cost = 1,000 × 1.01^points, so every 70 points doubles the price. (Phil,
2026-11-04. This replaced the first spec's curve of 5k → 100k → 1M, which ran
6–8× over what stock charges to unlock comparable engines.)

| Total points | 0 | 50 | 100 | 125 | 250 | 375 | 500 |
|---|---|---|---|---|---|---|---|
| Program cost | 1,000 | 1.6k | 2.7k | 3.5k | 12k | 41k | 145k |

It tracks stock's unlock prices at the node where each cap opens. Phil's career
pays to unlock parts after research, so this is the price he's used to:

| Points | Cap, where it opens | Stock unlock there | PIP |
|---|---|---|---|
| 125 | 25, Basic Rocketry | Swivel 3,500 | 3.5k |
| 250 | 50, Advanced Rocketry | Skipper 14,000 | 12k |
| 375 | 75, Heavier Rocketry | Mainsail 38,000 | 41k |
| 500 | 100, Experimental Rocketry | Vector, Mammoth 115,000 | 145k |

- 125 points is the most you can spend at the start (5 × 25 cap).
- 250 points is about stock level (50 in each); 500 is maxed.
- The next 10 points always cost about 10% more: about 100 at the bottom,
  1.3k around stock level, 14k near the top.
- Part types with fewer categories come out cheaper on their own (a
  two-category type tops out at 200 points, about 7k).
- The **PIP prices** game-start option scales every program price.
- Funds pay for programs. Science pays for tech unlocks.
- Starting funds stay stock (about 25k on Normal).

### Building an engine (funds per part)

| Factor | Value |
|---|---|
| Base by size | 0.625 m 300, 1.25 m 1,200, 1.875 m 2,500, 2.5 m 6,000, 3.75 m 20,000, 5 m 40,000 |
| Fuel | kerolox ×1, hydrolox ×1.3, methalox ×1.2 |
| Aerospike | ×2 |
| Design ambition | ×0.75 for a bare design, up to ×1.5 for a maxed one |
| Mk. | rises about 5% from Mk. 0 to Mk. 10. Upgrades must never feel like a punishment |

Example: a 1.25 m kerolox engine with 100 points builds for about 1,080 at
Mk. 0 and about 1,130 at Mk. 10. (The Reliant costs 1,100.)

---

## 8. Tech tree

Fits the Community Tech Tree + Probes Before Crew tree. Each unlock shows
inside its node in R&D like a part, with a one-line description. It's free
once the node is researched.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Basic Rocketry (5) | Liquid-Fuel Engines | First programs: 1.25 m, kerolox, sea-level nozzle; caps 25 |
| General Rocketry (20) | Altitude Nozzles | Vacuum nozzle |
| Advanced Rocketry (45) | Turbopumps | Caps 50 |
| Propulsion Systems (90) | Miniaturized Combustion Chambers | 0.625 m |
| Heavy Rocketry (90) | 1.875 m Tooling | 1.875 m |
| Heavier Rocketry (160) | 2.5 m Tooling | 2.5 m |
| | Staged Combustion | Caps 75 |
| Precision Propulsion (160) | Sustainer Nozzles | Balanced nozzle |
| Adv. Fuel Systems (160) | Cryogenic Hydrogen | Hydrolox |
| Very Heavy Rocketry (550) | 3.75 m Tooling | 3.75 m |
| High-Performance Fuel Systems (550) | Methalox Combustion | Methalox |
| Experimental Rocketry (1000) | Linear Aerospike | Aerospike nozzle |
| | Full-Flow Staged Combustion | Caps 100 |
| Gigantic Rocketry (1500) | 5 m Tooling | 5 m |

**One cap for the whole family** (Phil, 2026-11-04): each cap unlock raises
every category at once. This replaced the first spec's one cap per category
(15 unlocks for liquids alone), because PIP is meant to grow into most part
types and the tree would balloon. The size tooling is shared by every rocket
family (section 13).

New caps apply only to programs started afterwards.

---

## 9. Programs in the game

- **Each program is its own part** in a PIP section of the parts list, named
  with its current Mk., e.g. "Kestrel Mk. 4".
- **Look:** each part borrows the 3D model of an existing engine that matches
  its size and nozzle. Exhaust plumes come from Waterfall, by fuel.
- **Several programs per model are allowed**, e.g. two 1.25 m kerolox programs
  with different points. Each earns its own experience.
- **Retiring** a program removes it from the parts list. Engines already flying
  are unaffected. No refund.
- **No successor programs** in v1: a new program always starts at Mk. 0.

### The new-program window

As in the approved mockup:
- **Top:** name, fuel, size, nozzle and mount.
- **Category rows:** one per category, with −/+ buttons (5 points per click,
  shift-click for 1) and the cap shown.
- **Columns:** Mk. 0, a preview Mk. on a slider, and Mk. 10.
- **Sub-rows:** sea-level thrust and sea-level Isp.
- **Comparison:** a line against a familiar engine of the same size, fuel and
  nozzle.
- **Totals:** points spent, program cost, cost of the next 10 points, and the
  build price at Mk. 0 and Mk. 10.
- **Summary:** one plain-English line, e.g. "Built for the pad. Strong on
  thrust. Under 3% failures from Mk. 4."

### The program view

Shows:
- the name and current Mk.;
- experience toward the next Mk., with launches and recoveries counted
  separately;
- current stats next to Mk. 10 stats;
- a Retire button.

---

## 10. Assumptions to confirm

These came up while writing and weren't explicitly decided:

1. **A failed engine stays dead for the rest of that flight.** No retrying the
   ignition.
2. **"Light" failures** = half the normal chance.
3. **Science mode:** programs are free (there are no funds). **Sandbox:**
   everything is unlocked and free.
4. **Program names:** typed by the player, with a suggested name pre-filled.

---

## 11. Tune once playable

- Every number in sections 4, 6 and 7.
- Which existing engine each size/nozzle borrows its look from.
- The PIP prices option's steps (25% to 200%), once a career has been played
  on the new prices.

## 12. Later versions

- Radial engines.
- Other engine families: solids, jets, nuclear, electric. Proposed in section 13.
- Successor programs that carry experience over.
- The other part types: tanks, batteries, antennas and more. Proposed in
  section 14.

---

## 13. The other engine families (2026-11-04)

**Agreed, not built.** Claude drafted it, Phil checked it on 2026-11-04, and
his answers are under "Decided" at the end. Sections 1–12 still describe
liquids; this adds five families on the same skeleton. The
numbers come from the engines in Phil's install (stock, Near Future, Kerbal
Atomics, ReStock+), read from Module Manager's cache.

### What every family shares

- The whole program system: each program is its own part, Mk. 0 → Mk. 10,
  experience from launches and recoveries, half the points at Mk. 0, built parts
  keep their Mk., Retire with no refund, one failure check per flight.
- Categories are 0–100 points each. **Thrust, Efficiency and Weight in every
  family.** Reliability in every rocket family. Rocket families get one more of
  their own, for five; jets get one in place of Reliability, for four.
- Same window rule: 0 points is really bad but realistic; 100 points is about
  20% better than the best existing engine of that kind.
- Never part of PIP, never hidden: Sepratrons, launch escape towers, RCS.

| Family | Free choices | Categories |
|---|---|---|
| Liquid (built) | Fuel, Size, Nozzle | Thrust, Efficiency, Weight, Reliability, **Gimbal** |
| Solid | Size, Length | Thrust, Efficiency, Weight, Reliability, **Gimbal** |
| Monopropellant | Size | Thrust, Efficiency, Weight, Reliability, **Gimbal** |
| Nuclear | Core, Size | Thrust, Efficiency, Weight, Reliability, **Shielding** (Gimbal without Kerbalism) |
| Electric | Thruster type, Size | Thrust, Efficiency, Weight, Reliability, **Power** |
| Jet | Engine type, Size | Thrust, Efficiency, Weight, **Speed** |

### Game-start options

The two liquid toggles become one row per family, each **Off / Alongside stock
/ Replace stock**. Replace hides the non-PIP engines of that family, as the
liquid "hide" does now. Default: Replace for every family. The failures setting
stays as it is, one for everything.

### Program cost by family

The same curve as liquids, times a family factor:

| Family | Factor | 0 points | 125 points (start max) | 250 points (stock level) |
|---|---|---|---|---|
| Solid, monopropellant | ×0.5 | 500 | ~1.7k | ~6k |
| Liquid, jet | ×1 | 1,000 | ~3.5k | ~12k |
| Electric | ×1.5 | 1.5k | ~5.2k | ~18k |
| Nuclear | ×2 | 2k | ~6.9k | ~24k |

(Prices updated to the 1,000 + 1% per point rule, 2026-11-04. They sit close
to stock's unlock prices: Hammer 1,200, Dawn 16,800, Nerv 45,000.)

Why: a career's first rockets are solids, and a Mite-class program shouldn't
cost what a Mainsail-class one does. Nuclear and electric come late, when money
is easier.

### Caps

**One cap for the whole family** (Phil, 2026-11-04), raised 25 → 50 → 75 →
100, to keep the tree short as PIP grows to other part types. Liquids use it
too (section 8).
Size tooling is shared: Heavy, Heavier and Very Heavy Rocketry open 1.875, 2.5
and 3.75 m for every rocket family at once.

---

### Solid boosters

**Choices:** size 0.625 / 1.25 / 1.875 / 2.5 / 3.75 m, and **length**: short,
medium or long. Length sets how much propellant it holds (points don't change
that):

| Size | Short | Medium | Long |
|---|---|---|---|
| 0.625 m | 0.3 t (Mite) | 0.7 t (Shrimp) | 2.4 t |
| 1.25 m | 2.8 t (Hammer) | 6.2 t (Thumper) | 19.5 t (Kickback) |
| 1.875 m | 9.5 t | 21 t | 43.5 t (Pollux) |
| 2.5 m | 22.5 t | 60 t (Thoroughbred) | 123 t (Clydesdale) |
| 3.75 m | 76 t | 202 t | 415 t |

**Thrust** (vacuum kN, medium length; short ×0.6, long ×2):

| Size | Medium | Short | Long | Stock |
|---|---|---|---|---|
| 0.625 m | 12 → 50 | 7 → 30 | 24 → 100 | Mite 12.5, Shrimp 30 |
| 1.25 m | 120 → 480 | 72 → 288 | 240 → 960 | Hammer 227, Thumper 300, Kickback 670 |
| 1.875 m | 300 → 1,100 | 180 → 660 | 600 → 2,200 | Pollux 1,300 (long) |
| 2.5 m | 650 → 2,500 | 390 → 1,500 | 1,300 → 5,000 | Thoroughbred 1,700, Clydesdale 3,300 |
| 3.75 m | 2,000 → 7,500 | 1,200 → 4,500 | 4,000 → 15,000 | none (Shuttle SRB ≈ 12,500) |

At full thrust, 0 thrust points burns for 1–4 minutes and 100 points for
15–70 seconds (more Isp burns longer). KSP's thrust limiter trades thrust for
burn time, so more thrust is never worse.

**Efficiency:** 150–240 sea level / 170–265 vacuum. (Flea 140/165 is the
worst; Clydesdale 210/235 the best.) One nozzle; no nozzle choice for solids.

**Weight:** the casing, per ton of propellant: 0.27 t at 0 points (Hammer-heavy)
→ 0.13 t at 100. (Clydesdale is 0.17.)

**Reliability:** Mk. 10 chance 1% at 0 points → 0.05% at 100. Solids are
simple, but a strap-on that doesn't light makes a lopsided rocket.

**Gimbal:** 0 points is a fixed nozzle; 2° at 10 points; 8° at 100. (Solids in
this pack have 1–3°; the Shuttle's boosters had about 8°.)

**Build price:** about stock, roughly 1 fund per unit of propellant: 1.25 m
short ~400, medium ~850, long ~2,700.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Start (0) | Solid Rocket Motors | First programs: 0.625 and 1.25 m, short and medium; caps 25 |
| General Rocketry (20) | Segmented Casings | Long; caps 50 |
| Heavier Rocketry (160) | Composite Propellant | Caps 75 |
| Experimental Rocketry (1000) | HTPB Propellant | Caps 100 |

---

### Monopropellant engines

**Choices:** size 0.625 / 1.25 / 2.5 / 3.75 m. **One nozzle**, for vacuum:
orbital maneuvering engines like the LV-601. There is no landing-engine
(Hummingbird-style) version; Phil only uses monopropellant engines in vacuum.

**Thrust** (kN):

| Size | Window | Stock |
|---|---|---|
| 0.625 m | 4 → 20 | LV-95 14, Puff 20 |
| 1.25 m | 25 → 170 | LV-601 140 |
| 2.5 m | 60 → 350 | LV-95-6 cluster 110 |
| 3.75 m | 150 → 700 | LV-601-4 cluster 560 |

**Efficiency:** 150–215 sea level / 260–345 vacuum (LV-601-4 is 213/337).

**Weight:** thrust-to-weight 12.5 at 0 points → 25 at 100. (LV-601 is 16,
Puff 23.)

**Reliability:** Mk. 10 chance 1% → 0.05%.

**Gimbal:** same as liquids: fixed, 4° at 10 points, 20° at 100.

**Build price:** 0.625 m 400, 1.25 m 1,000, 2.5 m 2,500, 3.75 m 6,000.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Propulsion Systems (90) | Monopropellant Thrusters | First programs: 0.625 and 1.25 m; caps 25 |
| Precision Propulsion (160) | Throttleable Catalysts | Caps 50 |
| Heavy Landing (300) | Catalyst Beds | Caps 75 |
| Experimental Rocketry (1000) | Green Monopropellants | Caps 100 |

---

### Nuclear thermal

**Choices:** **core**: solid core (Nerv-style) or gas core (late unlock,
Liberator-style). Size 0.625 m (solid core only), 1.25, 1.875, 2.5, 3.75 m.
Liquid hydrogen only, as Kerbal Atomics does. No built-in generator.

**Thrust** (vacuum kN):

| Size | Solid core | Gas core | Stock |
|---|---|---|---|
| 0.625 m | 6 → 25 | — | Eel 12 |
| 1.25 m | 30 → 120 | 100 → 400 | Nerv 60, Stubber 80 |
| 1.875 m | 60 → 250 | 200 → 800 | |
| 2.5 m | 120 → 500 | 400 → 1,600 | Poseidon 310; Liberator 820, Emancipator 1,540 (gas) |
| 3.75 m | 400 → 1,500 | 1,000 → 4,000 | Scylla 1,450 |

**Efficiency** (sea level / vacuum):
- Solid core: 150–400 / 750–1,000 (Nerv 400/900, Neptune 950 vacuum).
- Gas core: 500–1,200 / 1,500–3,000 (Liberator 1,625, Emancipator 2,850).

**Weight:** thrust-to-weight 2.75 → 5.5 solid core (Nerv 2.7, Stubber 4.5);
6 → 12 gas core (Liberator 7.6, Emancipator 9.8).

**Reliability:** Mk. 10 chance 3% → 0.2%.

**Shielding** (the fifth category): Kerbalism already makes nuclear engines
radioactive. The Nerv gives off 0.25 rad/h; 50 rad over a lifetime kills a
kerbal (distance, and parts in between, cut it down). Shielding points cut what
the engine gives off, in rad/h:

| Size | 0 points | 100 points |
|---|---|---|
| 0.625 m | 0.05 | 0.005 |
| 1.25 m | 0.25 | 0.025 |
| 1.875 m | 0.35 | 0.035 |
| 2.5 m | 0.5 | 0.05 |
| 3.75 m | 1.0 | 0.1 |

Gas core ×2. Probe programs skip it; crewed-ship programs pay for it.
**Gimbal** is fixed at 3° (Kerbal Atomics engines have 1.5–4°).

**Kerbalism is optional, not required.** Shielding only means something when
Kerbalism is installed. Without it, the nuclear family's fifth category is
Gimbal (the liquid rule), and PIP runs the same otherwise. (Phil,
2026-11-04.)

**Build price:** 0.625 m 8,000, 1.25 m 12,000, 1.875 m 20,000, 2.5 m 40,000,
3.75 m 100,000. Gas core ×2.5.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Nuclear Propulsion (300) | Solid-Core Reactors | First programs: solid core, 1.25 m and any tooling sizes; caps 25 |
| Improved Nuclear Propulsion (550) | Compact Cores | 0.625 m; caps 50 |
| High Efficiency Nuclear Propulsion (1000) | Hot-Running Fuel Elements | Caps 75 |
| Experimental Nuclear Propulsion (1500) | Gas-Core Reactors | Gas core; caps 100 |

---

### Electric

**Choices:** **thruster type**: gridded ion (xenon), Hall (argon), or
magnetoplasmadynamic, MPD (lithium; later, needs a reactor). Size 0.625 /
1.25 / 2.5 m.

**Thrust** (kN):

| Size | Gridded ion | Hall | MPD |
|---|---|---|---|
| 0.625 m | 0.5 → 5 | 1 → 10 | 15 → 60 |
| 1.25 m | 2 → 20 | 4 → 40 | 35 → 130 |
| 2.5 m | 8 → 80 | 15 → 150 | 90 → 320 |

Stock: Dawn 1, HI-SNAP 4.3 (ion); Gyro-1 1.5, Triplet 8.4 (Hall); Charon 47,
Pyrios 96, Colossus 242 (MPD).

**Efficiency** (vacuum Isp; worthless in air, as now): gridded ion 3,000 →
12,000 (Dawn 3,690, HI-SNAP 9,650); Hall 1,800 → 3,800 (Triplet 3,300); MPD
2,200 → 4,200 (Colossus 3,400).

**Weight:** thrust-to-weight: ion 1.1 → 2.2, Hall 1.8 → 3.6, MPD 9 → 18.

**Reliability:** Mk. 10 chance 2% → 0.1%.

**Power** (the fifth category): electricity used at full thrust.
EC/s = thrust (kN) × Isp ÷ 1,000 × a factor. Power points move the factor from
6 (0 points) to 2.5 (100). Near Future's engines sit between 3.1 and 5.4. So
more thrust or more Isp always costs power, and Power points win some back:

| An engine like | 0 points | 100 points | Near Future's |
|---|---|---|---|
| Dawn (1 kN, 3,690 s) | 22 EC/s | 9 EC/s | 15 EC/s |
| Triplet (8.4 kN, 3,300 s) | 166 | 69 | 100 |
| Pyrios (96 kN, 3,000 s) | 1,735 | 723 | 1,000 |

**Build price:** 0.625 m 2,500, 1.25 m 6,000, 2.5 m 15,000. Hall ×0.8, MPD ×3.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Ion Propulsion (550) | Electrostatic Thrusters | First programs: gridded ion and Hall, 0.625 m; caps 25 |
| Advanced Ion Propulsion (1000) | Larger Grids | 1.25 m; caps 50 |
| Plasma Propulsion (1500) | Magnetoplasmadynamics | MPD |
| Advanced Electrostatic Thrusters (1500) | Carbon Grids | Caps 75 |
| Advanced Plasma Propulsion (2250) | High-Power Thrusters | 2.5 m; caps 100 |

Not types: Near Future's VASIMR (variable Isp) and pulsed inductive thrusters.
Under Replace they'd be hidden with the other electric engines.

---

### Jets

**Choices:** **engine type**:
- Turbofan (Juno, Wheesley, Goliath): efficient, subsonic.
- Afterburning turbojet (Panther): dry and afterburner modes, supersonic.
- Ramjet (Whiplash): weak at low speed, strong around Mach 3.
- Combined cycle (RAPIER): air-breathing, then a rocket. A late unlock.

Size 0.625 / 1.25 / 1.875 / 2.5 m.

**Four categories: no Reliability for jets.** A jet that won't start on the
runway costs a click, so the points would be wasted. In its place jets get
**Speed**. Jets never fail to light, and a maxed jet program is 400 points
(about 54k), not 500.

**Thrust** (kN, the figure KSP lists):

| Size | Window | Stock |
|---|---|---|
| 0.625 m | 10 → 40 | Juno 20 |
| 1.25 m | 60 → 200 | Wheesley 120, Panther 130, Whiplash 130 |
| 1.875 m | 120 → 360 | |
| 2.5 m | 200 → 600 | Goliath 360 |

The same window for every type; the type decides where in the speed range it
pushes (a ramjet makes about 6× that thrust at Mach 3, as the Whiplash does).
For the turbojet this is afterburner thrust and dry thrust is two-thirds. The
combined cycle's rocket mode makes 1.7×.

**Efficiency** (Isp):
- Turbofan 5,000 → 15,000 (Goliath 12,600).
- Turbojet dry 5,000 → 10,000, afterburner 2,500 → 4,500 (Panther 9,000 / 4,000).
- Ramjet 2,500 → 5,000 (Whiplash 4,000).
- Combined cycle: air 2,500 → 4,000 (RAPIER 3,200); rocket 240–300 sea level /
  290–340 vacuum.

**Weight:** thrust-to-weight 6 → 12. (Juno, Wheesley and Goliath are 8.2,
Panther 11, Whiplash 7.4.)

**Speed:** stretches the type's whole speed curve, ×0.8 at 0 points to ×1.2 at
100. Top speed:

| Type | 0 points | 100 points | Stock |
|---|---|---|---|
| Turbofan | Mach 1.8 | Mach 2.6 | Goliath 2.1, Wheesley 2.5 |
| Turbojet | Mach 2.6 | Mach 4.0 | Panther 3.3 |
| Ramjet | Mach 4.4 | Mach 6.6 | Whiplash 5.5 |
| Combined cycle | Mach 4.8 | Mach 7.2 | RAPIER 6 |

**Spool-up** is fixed by type, as the stock engines have it (turbofans slow,
the turbojet quick). Phil cut a Response category: simpler is better.

**Gimbal:** fixed by type: the turbojet keeps the Panther's 10°, the combined
cycle gets 3°, the rest none.

**Build price:** 0.625 m 500, 1.25 m 1,500, 1.875 m 2,200, 2.5 m 3,000.
Turbojet ×1.3, ramjet ×1.5, combined cycle ×3.

**Experience:** a plane flies and lands home every time, so each flight is +1
launch and +1 recovery. Mk. 10 takes about 72 flights.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Aviation (45) | Jet Engines | First programs: turbofan, 0.625 and 1.25 m; caps 25 |
| Supersonic Flight (160) | Afterburners | Turbojet; caps 50 |
| High Altitude Flight (300) | High-Bypass Fans | 1.875 and 2.5 m; caps 75 |
| Hypersonic Flight (550) | Ramjets | Ramjet |
| Aerospace Tech (1000) | Combined Cycle | Combined cycle; caps 100 |

---

### Decided (Phil, 2026-11-04)

1. **Jets:** no Reliability and no Response. Four categories: Thrust,
   Efficiency, Weight, Speed. "Simpler is better."
2. **Monopropellant:** one vacuum nozzle, no landing version.
3. **Program price by family** as above: solid and monopropellant ×0.5,
   liquid and jet ×1, electric ×1.5, nuclear ×2.
4. **Nuclear's fifth category is Shielding, and Kerbalism stays optional.**
   With Kerbalism installed, nuclear gets Shielding; without it, Gimbal.
5. **One cap per family**, for the new families and for liquids (section 8).
6. **Game start:** Off / Alongside stock / Replace stock, per family.
7. **Hyper-specialized engines stay out:** VASIMR-style variable-Isp
   electrics, Kerbal Atomics' LOX-augmented (trimodal) mode. Under Replace
   they're hidden with the rest of their family.

Nothing in this section is still open; it's ready to build when Phil says so.

### For the build

- 53 more chassis: solids 15, monopropellant 4, nuclear 9, electric 9, jets 16.
- Liquids: the built mod still has one cap per category (15 cap upgrades in
  `Upgrades.cfg`, `TechUnlocks.CapTechs`). Replace them with the three family
  caps in section 8. Programs already saved keep their points.
- Kerbalism: detect it at load. Shielding and the radiation change only exist
  when it's installed; without it nuclear shows Gimbal.
- Pricing: the built mod still uses the old curve (`Pricing.ProgramCost` in
  `Core/Progress.cs`, and its checks in `Tools/Test-Core.ps1`). Switch it to
  1,000 × 1.01^points × family factor, and add the PIP prices option.
- New code: engines with two modes (turbojet, combined cycle); stretching jet
  speed curves; Kerbalism radiation on nuclear engines (only when Kerbalism is
  installed); electric charge use; solid propellant load and casing mass.
- Check in game first: whether Kerbalism's engine radiation runs all the time
  or only while burning. All the time makes Shielding matter a lot; only while
  burning makes it a small thing, and gimbal might be the better fifth.

---

## 14. Other part types (2026-11-04)

**Agreed, not built.** Phil asked for "as many as possible", in the order Claude
thought best, then approved all of it: "love it all - yes to all". Numbers come from the 1,573 parts in his install. Same rules as
engines unless a line here says otherwise.

### What changes for parts that aren't engines

- **Size is chosen per program, like engines (changed 2026-11-05).** Phil: "A
  part is a part. ... if we're replacing (hiding) every part, we should build
  back up that part count with custom parts, not just see one tank in the
  editor." Each program is one part with one size, fixed for its life: a tank
  program picks its diameter and length, a battery program its size, a solar
  program its size class, and so on. In-line or side-mount is a program choice
  too (Phil: yes, per program). The in-VAB size switch goes away.
  - *Was:* size went on the part. One program covered every size, picked with
    a "Size and length" switch in the VAB. Phil tested it on 2026-11-05 and
    found one tank part in the editor "too odd".
  - Sizes keep their research gates; a size not yet researched is padlocked
    in the window, as engine sizes are.
  - **Old programs in test saves aren't converted** (Phil: test saves only).
    Non-engine programs made before this change may break or need recreating;
    test in a new game.
  - Windows below are given for each size. "× today's" wording is replaced by
    real numbers (Phil's play-test notes, item 10).
- **No failures.** Failing to light stays an engine thing.
- **Experience:** the same. A program earns +1 for each of its parts on a
  vessel that leaves the pad, and +1 for each one recovered (section 5, "Each
  part counts", 2026-11-05).
- **Price:** the same rule, 1,000 × 1.01^points, with no family factor. Fewer
  categories means a cheaper maxed program:

  | Categories | 1 | 2 | 3 | 4 |
  |---|---|---|---|---|
  | Maxed program | 2.7k | 7.3k | 20k | 54k |

- **Build price:** about what today's part of the same size costs.
- **One cap per type**, 25 → 50 → 75 → 100, as for engines. Sizes follow the
  shared rocket tooling (Heavy, Heavier, Very Heavy, Gigantic Rocketry) unless
  a type lists its own.
- **Game start:** Off / Alongside stock / Replace stock, one row per type.
- **Other mods stay optional,** as with Kerbalism: antennas use RemoteTech's
  ranges when it's installed and stock CommNet's otherwise; insulation needs
  CryoTanks.

**The order:**
1. RCS thrusters.
2. Batteries, reaction wheels and heat shields.
3. Solar panels and antennas.
4. Probe cores.
5. Fuel tanks.
6. RTGs and air intakes.

RCS is nearly an engine. The small types prove "size on the part" cheaply.
Fuel tanks cut the most clutter but are the most work.

| Type | Program choice | On the part | Categories |
|---|---|---|---|
| RCS thrusters | Propellant | Layout | Thrust, Efficiency, Weight |
| Batteries | none | Size, shape | Capacity |
| Reaction wheels | none | Size | Torque, Power draw, Weight |
| Heat shields | none | Size | Ablator, Weight, Heat tolerance |
| Solar panels | Fixed / deployable | Size | Output, Weight, Durability (deployable only) |
| Antennas | Omni / dish | Dish class | Range, Power draw, Weight |
| Probe cores | none | Size | Autopilot, Power draw, Weight, Torque |
| Fuel tanks | Kind | Size, length, propellant | Weight, Strength, Insulation (cryogenic only) |
| RTGs | none | Size | Output, Lifetime |
| Air intakes | Speed type | Inline / radial | Airflow, Top speed, Weight |

**Left stock:**
- Structural parts and fairings.
- Wings and control surfaces.
- Crew pods and cabins.
- Science instruments.
- Docking ports.
- Parachutes (RealChute).
- Decouplers, legs and wheels.
- Fuel cells (Kerbalism runs them as processes).
- Fission reactors and radiators (SystemHeat ties them together).
- Electric RCS.
- Plane fuselage tanks.

---

### 14.1 RCS thrusters

**Program choice:** **propellant**: monopropellant, or bipropellant
(kerolox: heavy thrusters for big ships, a later unlock).
**On the part:** layout: single linear port, 4-way block or 5-way block.

| Category | Monopropellant | Bipropellant | Stock |
|---|---|---|---|
| **Thrust** (per nozzle, kN) | 0.25 → 3 | 2 → 12 | RV-105 1, Place-Anywhere 7 2, RJ-88 8, Vernor 12 |
| **Efficiency** (sea level / vacuum) | 70–120 / 220–300 | 100–160 / 240–300 | RV-105 100/240, RX blocks 70/290, AVCS 100/265 |
| **Weight** (thrust-to-weight, all nozzles) | 6 → 12.5 | 12.5 → 25 | RV-105 10.2, RJ-88x4 20.4 |

| Node (science) | Unlock | What it opens |
|---|---|---|
| Flight Control (45) | Attitude Thrusters | First programs: monopropellant; caps 25 |
| Advanced Flight Control (90) | Pulse-Mode Valves | Caps 50 |
| Specialized Control (160) | Bipropellant Thrusters | Bipropellant; caps 75 |
| Experimental Rocketry (1000) | Green Monopropellants | Caps 100 |

Pods with built-in RCS are untouched. Near Future's electric RCS stays stock.

### 14.2 Batteries

**One category, one program.** Every battery in the install holds exactly
20,000 EC per ton, so the only thing to develop is how much it holds. The
size and shape (radial, 0.625–3.75 m) is picked on the part; each shape keeps
today's mass.

| Category | 0 → 100 points | Stock |
|---|---|---|
| **Capacity** (EC per ton) | 12,000 → 30,000 | 20,000 for all (about 45 points) |

A 1.25 m battery (Z-1k size, 0.05 t) holds 600 → 1,500 EC. A maxed battery
program costs 2.7k.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Start (0) | Rechargeable Cells | First program; caps 25 |
| Electrics (90) | Nickel-Cadmium | Caps 50 |
| Large Electrics (300) | Lithium-Ion | Caps 75 |
| Experimental Electrics (1000) | Solid-State Cells | Caps 100 |

### 14.3 Reaction wheels

**On the part:** size: radial, 0.625, 1.25, 1.875, 2.5, 3.75 m.

| Torque (kNm) | 0 → 100 points | Stock |
|---|---|---|
| Radial | 0.5 → 3 | Small Radial Gyroscope 2.5 |
| 0.625 m | 2 → 8 | Small Inline 5 |
| 1.25 m | 6 → 25 | Advanced Inline Stabilizer 15 |
| 1.875 m | 10 → 35 | Medium RWA 22.5 |
| 2.5 m | 15 → 50 | Advanced Large 30 |
| 3.75 m | 30 → 100 | none |

- **Power draw:** 0.08 → 0.015 EC/s per kNm (stock 0.02–0.05).
- **Weight:** 60 → 180 kNm of torque per ton (stock 83–150).

| Node (science) | Unlock | What it opens |
|---|---|---|
| Stability (18) | Momentum Wheels | First program; caps 25 |
| Flight Control (45) | Brushless Motors | Caps 50 |
| Advanced Flight Control (90) | Magnetic Bearings | Caps 75 |
| Specialized Control (160) | Control Moment Gyroscopes | Caps 100 |

### 14.4 Heat shields

**On the part:** size 0.625–5 m. How much ablator it carries stays as
today's shield of that size (50, 200, 500, 800 and 1,800 for 0.625 to 3.75 m;
3,200 at 5 m).

| Category | 0 → 100 points | Stock |
|---|---|---|
| **Ablator** (heat soaked up per unit) | ×0.6 → ×1.5 | ×1 |
| **Weight** (the shield, not the ablator) | ×1.6 → ×0.8 | ×1 (1.25 m: 0.1 t) |
| **Heat tolerance** (max temperature) | 2,400 → 3,600 K | 3,300 K |

| Node (science) | Unlock | What it opens |
|---|---|---|
| Survivability (15) | Ablative Shields | First program; caps 25 |
| Enhanced Survivability (45) | Phenolic Resins | Caps 50 |
| Heat Management Systems (160) | PICA | Caps 75 |
| Advanced Heat Management (550) | Carbon-Carbon | Caps 100 |

### 14.5 Solar panels

**Program choice:** **fixed** (always out, never tracks; can't break) or
**deployable** (folds out and tracks the sun).
**On the part:** size class: small, medium (fixed panels stop here), large,
huge.

| Output (EC/s in Kerbin's sunlight) | 0 → 100 points | Stock |
|---|---|---|
| Small | 0.5 → 3 | OX-4 1.64, Globe 3 |
| Medium | 3 → 15 | OX-10 8.25, K14 14 |
| Large | 15 → 60 | Gigantor 24.4, K55 55 |
| Huge | 60 → 300 | OKEB-150 150, OKEB-250 250 |

- **Weight:** 40 → 150 EC/s per ton (stock 35–120).
- **Durability** (deployable only): how much air pressure and g-force it
  takes before it snaps off. **From 50 points it can fold back up.**

| Node (science) | Unlock | What it opens |
|---|---|---|
| Survivability (15) | Photovoltaics | First programs: fixed, small; caps 25 |
| Electrics (90) | Deployable Arrays | Deployable, medium; caps 50 |
| Large Electrics (300) | Large Arrays | Large |
| Advanced Solar Tech (550) | Gallium Arsenide | Caps 75 |
| Cutting-Edge Solar Tech (1000) | Multi-Junction Cells | Huge; caps 100 |

### 14.6 Antennas

**Program choice:** **omni** or **dish**.
**On the part (dishes):** class: short-range (Kerbin and its moons),
interplanetary, deep-space. Range runs on a log scale: every 25 points
multiplies it by the same amount.

| | Range | Power draw (EC/s) | Weight (t) | Cone | Stock (RemoteTech) |
|---|---|---|---|---|---|
| Omni | 0.5 → 6 Mm | 0.3 → 0.02 | 0.03 → 0.005 | all round | 0.5–5 Mm |
| Short-range dish | 5 → 100 Mm | 1.0 → 0.1 | 0.1 → 0.01 | 25° | up to 90 Mm |
| Interplanetary dish | 5 → 80 Gm | 1.5 → 0.3 | 0.3 → 0.05 | 0.1° | 10–75 Gm |
| Deep-space dish | 50 → 500 Gm | 3 → 0.6 | 1.0 → 0.15 | 0.01° | 100–400 Gm |

At 50 points: omni about 1.7 Mm, short-range dish about 22 Mm (past the Mun,
short of Minmus), interplanetary about 20 Gm, deep-space about 160 Gm.
Without RemoteTech, Range becomes stock CommNet antenna power and the cone is
ignored.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Start (0) | Radio | First programs: omni; caps 25 |
| Basic Science (45) | Directional Antennas | Short-range dish |
| Precision Engineering (160) | Parabolic Dishes | Interplanetary dish; caps 50 |
| Unmanned Tech (300) | Deep Space Network | Deep-space dish; caps 75 |
| Large Unmanned (1000) | Phased Arrays | Caps 100 |

### 14.7 Probe cores

**On the part:** size 0.625, 1.25, 1.875, 2.5 m. Each keeps a built-in battery
for its size. RemoteTech's signal processor and flight computer are always
built in.

**Autopilot** works in steps:

| Points | 0–9 | 10 | 25 | 50 | 75 |
|---|---|---|---|---|---|
| SAS | none | hold attitude | + prograde, retrograde | + normal, radial, target | + maneuver node |

Mk. 0 performs at half points, so a 50-point core starts out with prograde
hold and gains target hold as it matures.

| Category | 0 → 100 points | Stock |
|---|---|---|
| **Power draw** | 0.1 → 0.015 EC/s | 0.02–0.18 |
| **Weight** (t) | 0.625 m 0.065 → 0.03; 1.25 m 0.2 → 0.1; 1.875 m 0.3 → 0.15; 2.5 m 0.6 → 0.3 | OKTO2 0.04, DSK 0.13, MPO 0.395 |
| **Torque** (kNm) | 0.625 m 0 → 1; 1.25 m 0 → 5; 1.875 m 0 → 10; 2.5 m 0 → 20 | HECS2 10, MTM 12 |

| Node (science) | Unlock | What it opens |
|---|---|---|
| Start (0) | Guidance Units | First program: 0.625 m; caps 25 |
| Basic Science (45) | Larger Avionics Bays | 1.25 m |
| Precision Engineering (160) | Digital Autopilots | Caps 50 |
| Unmanned Tech (300) | 1.875 m Avionics | 1.875 m |
| Advanced Unmanned (550) | Maneuver Computers | Caps 75 (maneuver-node SAS) |
| Large Unmanned (1000) | Heavy Avionics | 2.5 m; caps 100 |

### 14.8 Fuel tanks

**Program choice:** **kind**:
- Standard: kerolox, liquid fuel, oxidizer.
- Cryogenic: hydrolox, hydrogen, methalox, methane.
- Monopropellant.
- Electric propellant: xenon, argon, lithium.

**On the part:** size 0.625–5 m, length (short, medium, long, extra long, in
FL-T100/200/400/800 steps), and which propellant of its kind, switched in the
VAB as CryoTanks does now.

| Category | 0 → 100 points | Stock |
|---|---|---|
| **Weight** (empty mass for the propellant it holds) | ×1.6 → ×0.8 | ×1 (kerolox tanks: empty = 1/8 of the propellant) |
| **Strength** (landings, re-entry) | impact 4 → 20 m/s; heat 1,400 → 2,600 K | 6 m/s, 2,000 K |
| **Insulation** (cryogenic only) | hydrogen boil-off 0.1 → 0.01% per hour; cooling 0.1 → 0.01 EC/s per 1,000 units | CryoTanks: 0.05% per hour, 0.05 EC/s |

Methane is ten times gentler than hydrogen on both counts, as in CryoTanks.
Strength is what lets recovered boosters and re-entering tanks survive.

Replace hides round stack tanks only. Plane fuselage tanks (Mk1, Mk2, Mk3),
radial tanks and odd shapes stay.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Basic Rocketry (5) | Welded Tanks | First programs: standard; caps 25 |
| Flight Control (45) | Monopropellant Tanks | Monopropellant kind |
| Fuel Systems (90) | Stir-Welded Aluminum | Caps 50 |
| Adv. Fuel Systems (160) | Cryogenic Tanks | Cryogenic kind |
| Large Volume Containment (300) | Aluminum-Lithium Tanks | Caps 75 |
| Ion Propulsion (550) | Pressurized Gas Tanks | Electric-propellant kind |
| Specialized Fuel Storage (1000) | Composite Tanks | Caps 100 |

### 14.9 RTGs

**On the part:** size: small (PB-ACK class) or large (PB-NUK-II class).
Near Future's RTGs decay with a half-life, which makes a second category.

| Category | 0 → 100 points | Stock |
|---|---|---|
| **Output** (EC/s per ton) | 6 → 15 | 8.3–11.1 |
| **Lifetime** (half-life, years) | 4 → 20 | 8.35 |

| Node (science) | Unlock | What it opens |
|---|---|---|
| Specialized Electrics (550) | Radioisotope Power | First program; caps 25 |
| Experimental Electrics (1000) | Thermocouple Upgrades | Caps 50 |
| High-Tech Electrical Systems (1500) | Stirling Converters | Caps 75 |
| High-Power Electrical Systems (2250) | Advanced Isotopes | Caps 100 |

### 14.10 Air intakes

**Program choice:** **speed type**, matched to the jet types:
- Subsonic (circular, radial): good to about Mach 2.5.
- Supersonic (ramp): to about Mach 5.
- Hypersonic (shock cone).

**On the part:** inline 0.625 / 1.25 m, or radial.

| Category | 0 → 100 points | Stock |
|---|---|---|
| **Airflow** (intake area) | ×0.6 → ×1.5 of today's intake of that shape | ×1 |
| **Top speed** | stretches the speed curve ×0.8 → ×1.2, as for jets | ×1 |
| **Weight** | ×1.6 → ×0.8 | ×1 |

Precoolers, nacelles and Breaking Ground turboshafts stay stock.

| Node (science) | Unlock | What it opens |
|---|---|---|
| Aviation (45) | Air Intakes | First programs: subsonic; caps 25 |
| Aerodynamics (90) | Variable Inlets | Caps 50 |
| High Altitude Flight (300) | Ramp Intakes | Supersonic; caps 75 |
| Hypersonic Flight (550) | Shock Cones | Hypersonic; caps 100 |

---

### Decided (Phil, 2026-11-04: "yes to all")

1. **Size on the part** for every type above, with one program covering all
   sizes. Engines keep size in the program.
2. **Batteries are a one-category program** (Capacity).
3. **Probe-core autopilot comes in steps** (10 / 25 / 50 / 75 points).
4. **RemoteTech and CryoTanks are optional**, with fallbacks, like Kerbalism.
5. **RTGs and air intakes are in.**
6. **The order** above.

### For the build, once agreed

- **The new piece:** one part that switches between several models (sizes,
  lengths, layouts), using B9PartSwitch, which CryoTanks already installs.
  Prove it on batteries first; they're the simplest.
- **The code:** make each part type a data file (its choices, categories,
  windows, and which part numbers each category moves), so adding a type is
  mostly config.
- **Mod support:** RemoteTech antenna fields, CryoTanks boil-off fields, Near
  Future RTG half-life.

---

## 15. Side-mount parts (2026-11-04)

**Built 2026-11-04,** with Looks step 1 (see "Looks and side-mount build" at
the end). Phil: "Yes do that". The spec (14.2, 14.3, 14.10)
already had side-mount (radial) sizes, but the second build left them out. In
Replace mode, PIP hides every side-mount battery, reaction wheel and air
intake, and puts nothing back.

What Replace hides today with no PIP stand-in, in Phil's install:

| Type | Side-mount parts hidden |
|---|---|
| Batteries | Z-100 and Z-400 (stock), Near Future's radial 1.25 m battery, Near Future Exploration's mini battery |
| Reaction wheels | ReStock+'s Small Radial Gyroscope, Near Future Exploration's mini reaction wheel |
| Air intakes | XM-G50 Radial Air Intake, Adjustable Ramp Intake (Radial) |

**Correction:** on 11/04 Claude told Phil that no side-mount wheels were lost,
because stock has none. That's true of stock but not of his install: two mods
add side-mount wheels, and Replace hides both.

**The rule: one program, two parts.** Where a part attaches doesn't change
what's being developed, so it isn't a program choice and costs nothing extra.
Each battery, wheel and intake program puts two entries in the VAB parts list,
inline and side-mount. Both share the program's Mk, its numbers and one
experience pool, so flying either one counts.

It's two parts and not a switch on one part because KSP fixes how a part
attaches per part, and the size switch can't change it.

| | Side-mount sizes (on the part) | Looks like | Numbers |
|---|---|---|---|
| Batteries | small 0.005 t, medium 0.02 t, large 0.04 t | Z-100, Z-400, Z-400 scaled up | Capacity per ton from the program, as for inline. Today's parts all hold 20,000 EC per ton, so at that rate the sizes hold 100, 400 and 800 EC |
| Reaction wheels | one size | ReStock+'s radial gyroscope when installed; otherwise the Small Inline Reaction Wheel, shrunk | Torque 0.5 → 3 kNm (14.3's radial row); power draw and weight as inline |
| Air intakes | one size | Subsonic: XM-G50. Supersonic: the radial ramp. Hypersonic: none (inline shock cone only) | Airflow and weight × that intake's own (XM-G50 area 0.0031, 0.02 t; radial ramp 0.001, 0.01 t) |

Side-mount parts unlock with the program. They have no tech of their own.

---

## 16. Looks: wearing any installed part's model (2026-11-04)

**All three steps built 2026-11-04** (Phil: "Yes, build it", "Yes, step 2", "Yes,
step 3"): every PIP part type has looks, engines included. Phil asked whether a PIP part could use the
model of other parts the player has installed, for engines and most other
parts. PIP already recognises every installed part of each type; that's how
Replace knows what to hide. The same recognizer offers those parts as looks.

### Decided (Phil, 2026-11-04)

1. **The look is picked when the program starts, and it's fixed.** It's one
   more free choice in the new-program window, next to fuel, size and nozzle.
   One program has one look. For a different look, start a new program. Phil:
   "where to pick should be fixed, upon the program start".
2. **Looks are free.** Phil: "Free for sure".
3. **Engines use a model at its own size only:** a 2.5 m model can only be the
   look of a 2.5 m program, and so on. Nothing is scaled, so there are no
   awkward plumes. Phil: "only allow 2.5 models to function as 2.5 programs,
   and so on".

### What follows from those (Claude's reading, for Phil to check)

- **A look never changes the numbers.** It changes only the model and how the
  part attaches.
- **No installed model at that size** (for example, liquid at 5 m in Phil's
  install): the program gets PIP's default look, today's model. The size rule
  narrows the list; it never blocks a program.
- **Parts that aren't engines still scale their one look** across the sizes
  picked on the part, as today. They have no plumes, and one program still
  covers every size.
- **Side-mount:** battery, wheel and intake programs pick two looks at start,
  one inline and one side-mount (section 15).
- **A look with no gimbal** greys out the Gimbal category in the new-program
  window, so no points are wasted on it.
- **Existing programs** keep today's look.
- **Replace stops costing you variety.** It hides the originals, but every one
  of them is still there as a look.

Rough count from the config files in Phil's install: about 9 to 15 liquid
engine models at each size from 0.625 m to 3.75 m. Solids, jets and nuclear
are thin at some sizes and empty at others; those sizes fall back to the
default look.

### Which looks fit which program

| Type | Rule |
|---|---|
| Liquid | A liquid engine at the program's size. Looks that burn the same fuel are listed first, because the flame's color comes from the look. |
| Solid | A booster at the program's size and length. Length is short, medium or long, judged by the booster's fuel for its size. |
| Monoprop, nuclear, electric | An engine of that family at the program's size. |
| Jets | At the program's size, with the same modes: a turbojet look needs dry and afterburner, and a combined-cycle look needs air-breathing and rocket. |
| RCS | The look's number of nozzles must match the program's layout. |
| Solar panels | Fixed with fixed, deployable with deployable. |
| Antennas | Omni with omni, dish with dish. |
| Fuel tanks | Round tanks only, as for Replace; the length setting stretches the look. |
| Everything else | Any part of the type. |

### How hard it is

It's the biggest single piece since the engine families, but most of the
building blocks already exist:

- **Already built:**
  - Recognising parts (Replace mode).
  - One VAB part per program, made from a chassis; a look is just another
    chassis.
  - Scaling a model to a size (the size switch), for parts that aren't
    engines.
- **New:**
  - Making a clean PIP copy of any installed part. Today a script does this by
    hand for 147 chassis: it strips TweakScale, other switchers and failure
    modules, then adds PIP's own.
  - Reading each look's own size, to match engine looks to the program's size.
  - A Look choice in the new-program window.

**The risk isn't the code. It's the long tail of mod parts.**
- Simple parts should mostly just work: batteries, wheels, RTGs, intakes, heat
  shields and probe cores.
- The native-size rule takes away the worst engine problem, scaled plumes.
  Flames (Waterfall), nozzle glow, gimbals and multi-mode engines are still
  tied to each model, so some looks may still misbehave.
- Expect a "hide this look" list, built up from testing.

**Scale:** Phil's install has about 320 candidate parts (the Replace counts).
Only looks that some program has picked need copies, which keeps load time and
memory down.

### Suggested order

1. **Simple parts:** batteries, wheels and intakes, so side-mount comes with
   it; then RTGs, heat shields and probe cores.
2. **Solar panels, antennas and tanks:** these have deploy animations and
   their own fuel switches.
3. **Engines.**

### Still open

- **Side-mount on its own now, or as the first step of Looks?** It's the same
  skeleton either way. Side-mount alone means picking its looks by hand.
- **Phil to check** "What follows from those" above, especially that parts
  which aren't engines still scale their one look across sizes.

### For the build, once agreed

- **The look is stored on the program** (the source part's name). A program
  with no look uses today's chassis, so existing saves keep working.
  - Engines and single-mount parts keep one VAB part per program.
  - Battery, wheel and intake programs make a second, side-mount part.
- **Copies:** each look needs a prefab with PIP's module added and the source
  part's switchers, TweakScale, failure modules and `ModuleTestSubject`
  stripped, with its MODEL nodes named PIPModel. There are two ways to make
  them; prototype both on batteries:
  - A Module Manager wildcard copy of every recognised part at load. This is
    simple, but it means about 320 extra prefabs, and MM can't know which
    looks a save uses.
  - PIP code that clones a source prefab only when a program first needs it,
    including from the load-dialog and save-load hooks that already register
    program parts.
- **Native size:** read from the look's `bulkheadProfiles` (size0 = 0.625 m,
  size1 = 1.25, size1p5 = 1.875, size2 = 2.5, size3 = 3.75, size4 = 5), or
  from its stack nodes when that's missing.
  - Engines must match the program's size exactly.
  - Other parts scale: target diameter / native diameter. Side-mount looks
    scale by mass class instead.
- **Gimbal:** a look with no `ModuleGimbal` makes the Gimbal category unused.

---

## 17. Not just Phil's pack (design, 2026-11-04)

Phil: "it is ok for the mod to have dependencies btw - like community tech tree.
mod dependency is pretty normal, I just want to make sure this isn't hyper
specialized for MY modpack". **Design only; nothing here is built.**

### What a player without Phil's pack would hit today

- **Three hard requirements nobody declares:** Harmony (the plugin won't load),
  Module Manager (every PIP part is a Module Manager patch) and B9PartSwitch
  (sizes on the part, tank volumes, looks).
- **Community Tech Tree:** 17 of PIP's unlock nodes exist only in CTT
  (Experimental Rocketry, Gigantic Rocketry, Advanced Ion Propulsion, Plasma
  Propulsion, the CTT nuclear and solar nodes, Specialized Fuel Storage…). On the
  stock tree those unlocks never happen: no aerospike, no 5 m, no gas core, and
  several caps stop short of 100.
- **CryoTanks:** hydrolox and methalox (its LqdHydrogen and LqdMethane), the
  Cryogenic tank kind, and every PIP fuel tank's volume: a PIP tank adds its
  size's volume to the CryoTanks fuel switch on the stock tank it copies.
- **Mod models:** PIP's 147 chassis parts copy 64 existing parts. Of those
  sources, 37 are stock and 3 are Making History; 24 are mod parts (Near Future
  Launch Vehicles, Propulsion and Spacecraft, Cryo Engines, Kerbal Atomics,
  ReStock+). A missing source means Module Manager never makes that chassis,
  and **the window still lets the player start the program and takes the funds,
  but no part appears.** The standard look names the mod part too.
- **Already general, no change needed:** Kerbalism (optional; nuclear gets
  Gimbal without it), RemoteTech (optional; stock CommNet otherwise), Near
  Future Solar's curved panels, the looks list (whatever is installed), and the
  Replace recognizer (it reads what a part does, not its name).

### Proposal

1. **Declared dependencies, which CKAN installs with PIP:** Harmony 2, Module
   Manager, B9PartSwitch, Community Tech Tree, CryoTanks. All common; CTT and
   CryoTanks are what PIP's progression is built on.
2. **Everything else optional:** Near Future Launch Vehicles, Propulsion and
   Spacecraft, Cryo Engines, Kerbal Atomics, ReStock+, Making History, Kerbalism,
   RemoteTech. Each adds better models, more looks and comparison engines; none
   is needed.
3. **Every standard model gets a stock stand-in.** The chassis copies the
   preferred part when it's installed, otherwise a stock part scaled to the size
   (Module Manager's `:NEEDS` picks between them at load). PIP's own models
   already scale stock parts where needed; the "own size only" rule is for looks
   and stays. Numbers never change: they're PIP's.
4. **What has no stand-in sits behind a padlock that names what's missing,**
   like research does today. Found from what's loaded (resources, parts), not
   mod names: Hall and MPD electric engines need a mod that adds argon and
   lithium (Near Future Propulsion or Community Resource Pack); five-nozzle RCS
   blocks need Near Future Spacecraft.
5. **Never charge for a part that can't be made.** Start refuses when the
   chassis or the look is missing. A program whose model disappears later (a mod
   removed) keeps its record and says what it needs.
6. **The standard look follows the model the chassis actually got.**
7. **A missing required dependency gets one clear message** at the main menu,
   and PIP stays off instead of making broken parts.
8. **Release:** a README listing what's required and what's optional, and CKAN
   metadata (depends / recommends).

### Stand-ins

The same for kerolox, hydrolox and methalox.

| Model | Preferred part (mod) | Stock stand-in |
|---|---|---|
| Liquid 0.625 m sea level | Rutherford (NF Launch Vehicles) | 48-7S Spark |
| Liquid 0.625 m vacuum | Rutherford Vac (NF Launch Vehicles) | LV-1 Ant |
| Liquid 1.25 m balanced | RE-I2 Skiff, scaled (Making History) | LV-T45 Swivel |
| Liquid 1.875 m sea level | LV-TX87 Bobcat (Making History) | LV-T30 Reliant ×1.5 |
| Liquid 1.875 m balanced | RD-704 (NF Launch Vehicles) | LV-T45 Swivel ×1.5 |
| Liquid 1.875 m vacuum | Pavonis (Cryo Engines) | LV-909 Terrier ×1.5 |
| Liquid 3.75 m sea level | RD-701 (NF Launch Vehicles) | KS-25x4 Mammoth |
| Liquid 3.75 m balanced | Etna (Cryo Engines) | KR-2L+ Rhino |
| Liquid 5 m sea level, balanced | RD-701, Etna | Mammoth, Rhino ×1.33 |
| Solid 1.875 m long | THK Pollux (Making History) | Kickback ×1.5 |
| Monoprop 0.625–3.75 m | orbital engines (NF Spacecraft) | LV-1 Ant / LV-909 Terrier, scaled |
| Nuclear solid core 0.625, 2.5, 3.75 m | Kerbal Atomics, Cherenkov (ReStock+) | LV-N Nerv, scaled |
| Nuclear gas core, every size | Kerbal Atomics | LV-N Nerv, scaled |
| Electric gridded ion, every size | NF Propulsion | IX-6315 Dawn, scaled |
| Electric Hall, MPD | NF Propulsion | none: padlock (needs argon / lithium) |
| RCS bipropellant, 1 nozzle | NF Launch Vehicles | Vernor |
| RCS bipropellant, 4 nozzles | NF Launch Vehicles | RV-105 burning LF/Ox |
| RCS five nozzles (mono or biprop) | NF Spacecraft | none: padlock |

### For Phil to decide

1. **CryoTanks required?** Recommended. The alternative is making hydrolox and
   methalox optional, which means PIP tanks need their own volume switch:
   noticeably more work.
2. **Scaled stock stand-ins for PIP's own models?** Recommended; looks keep
   their own-size rule.
3. **A padlock for what has no stand-in?** Recommended, instead of hiding it.

### Test plan

A second, clean KSP install on D: (a copy of the base game) with only the
required mods, installed through CKAN. Start a career and a sandbox, start one
program of every type and option, and check that each makes a part, flies, and
that nothing is charged without a part. Then Phil's full pack again, for
regressions.

### Size of the build

The two chassis generators learn "preferred, else stand-in"; plus the
dependency check, the padlocks and the Start guard. About one working session,
most of it testing.

### Built (2026-11-04, Phil: "Yes, build it", all three recommendations)

- **Stand-ins:** every chassis copied from a mod or DLC part also gets
  `<name>-stock` from a stock part (47 in all). `PartFactory.Resolve` picks the
  preferred chassis when its part is installed, else the stand-in. Module
  Manager's `:NEEDS` wasn't used: it can't reliably see the Making History DLC.
  Changes from the table above: 3.75 and 5 m balanced stand in with the Skipper
  (×1.5, ×2), not the Rhino; monopropellant engines with the Ant (0.625 m) and
  the Terrier (×1–3) burning monopropellant.
- **Each chassis records its source part** (`chassisSource` on ModulePipEngine /
  ModulePipPart). `PartFactory.StandardSource` is the standard look: the
  preferred part when installed, else the stand-in's.
- **Padlocks:** the generators write `PIP_NEEDS { chassis, needs }` for the 8
  models with no stand-in. The window's design rows padlock an option whose
  model can't be made ("Needs Near Future Propulsion"), the bottom bar says the
  same, `PipScenario.Create` refuses (no charge), and a program whose model went
  missing says so on its page.
- **Required mods** (`KSP\Requirements.cs`): Module Manager and B9PartSwitch
  (assemblies), Community Tech Tree (the giganticRocketry node), CryoTanks
  (SimpleBoiloff plus the LqdHydrogen and LqdMethane resources). Any missing:
  every type Off, no looks made, and one popup at the main menu naming them.
- **The Nerv chassis** now set hydrogen propellant explicitly (the stock Nerv
  burns liquid fuel; Kerbal Atomics gave it a hydrogen mode).
- **Found on the clean install, fixed:**
  - Plain stock parts have `mesh =` and no MODEL node, so the size switch on the
    solar, RTG, battery, wheel and ramp-intake chassis found nothing to scale.
    The loader now gives such chassis a MODEL named PIPModel.
    (Phil's pack never showed it: ReStock gives those parts MODEL nodes.)
  - The electric-propellant tank used Near Future's Xenon, Argon and Lithium tank
    types. PIP now defines PIPXenon, PIPArgon and PIPLithium with the same
    numbers.
- **A load report** in the log at the main menu: "Models: N preferred, N stock
  stand-ins, N need a mod", with the lists.
- **README.md** (requirements and optional mods) and
  **PhilsIterativeParts.netkan** (CKAN depends, recommends, suggests) in the repo.

**Tested:**
- **Clean install** (a separate test install: stock without DLC, the five required
  mods, PIP), from logs only. Phil declined screen access to that copy.
  - 92 models preferred, 47 stock stand-ins (each standard look the right stock
    part), 8 padlocked naming the right mod.
  - No B9PartSwitch or PIP errors; looks made from stock parts.
  - Not seen there: the window, padlocks on screen, starting a program, flying.
- **Phil's install:**
  - 147 preferred, 0 stand-ins, 0 padlocks, the same 507 looks, no errors.
  - Window, Bolide's page and electric engines unchanged.
- Core tests: 178 pass.

**Phil's own test of the clean install (2026-11-05), fixed:**
- **"Replace" hid nothing, and the editor broke** (every pod shown, researched
  or not; no other category could be opened). Sorting stock parts into PIP
  types (`EditorHider`) crashed on a part with no readable resource list. The
  likely culprit is the EVA kerbals, which have no resources on plain KSP;
  Kerbalism adds some in Phil's pack. That one crash stopped R&D hiding when
  the game loaded, and it ran on every editor parts-list refresh. Now EVA
  kerbals and the flag are skipped, the resource list is checked first, and any
  part that still can't be sorted is left alone with a log line ("Left X
  alone").
- **R&D unlock pictures:** 16 of the 49 PIP unlocks show a chassis that only
  exists with the optional mods, and R&D errored on them. At the main menu
  (`UpgradeIcons.Fix`), each such unlock now shows the model's stock stand-in,
  or another installed part of the same type when there's none (MPD). The log
  lists the swaps.
- Not seen in game yet: Phil is testing the clean copy himself.

---

## 18. Versions, Mk. 1 to 5, gimbal and prices (2026-11-05)

Agreed with Phil on 2026-11-05 after his play-testing, item by item. Where this
disagrees with earlier sections (5, 6, 7, 14), this section wins.

**Built 2026-11-05** (Phil: "Build it"; commits a750e00 and 544178a). 210
offline checks pass. Checked in game on a clean test install (a
copy of Phil's career as "zz PIP Versions Test", with Zephyr I given 26
experience):
- **New-program screen:** Gimbal row 0/5/10/20°, Mk 1–5 preview strip, Build
  cost slider (build price √387 at Mk. 1 falling to √352 at Mk. 5), program cost
  √6,223 for a 25%-points liquid engine, as in the table.
- **Zephyr I at Mk. 5:** "Zephyr II is ready", Start Zephyr II at √1,293 (a
  tenth of a 35-point program).
- **Zephyr II:** Mk. 1 thrust equals Zephyr I's Mk. 5, failures back to 10%,
  version history shown, Zephyr I retired.
- **Found and fixed in that test:** Zephyr II's Mk. 1 weighed and cost more
  than Zephyr I's Mk. 5 (weight and price followed the full design points);
  they now grow from the predecessor's points with Mk.
- **Not yet checked in game:** experience from real flights, the VAB swap of
  an old version's parts in a loaded craft, a gimbal pick on a flying engine,
  a "Max" version.

Implementation notes: a successor's starting points ride along each Compute
(`PartType.Compute(..., startPoints, gimbal)`; `E` and `D` read them).
Retired versions route experience to the newest (`PipScenario.Latest`). Old
craft are renamed in a Harmony prefix on `ShipConstruct.LoadShip`.

### 18.1 Versions (successor programs)

A program family improves in versions: **Kestrel I, Kestrel II, Kestrel III ...**
- A new program is automatically named "<name> I". Names can't be typed with a
  numeral. Programs in old test saves show as "<name> I".
- When a version reaches **Mk. 5**, its page offers **Start Kestrel II**:
  - Every slider the part uses goes up **10 points**. Sliders the part doesn't
    use stay at 0. No slider goes past **100** or past the **research cap**.
  - The design stays as it was: type, model, size, length, mount, and gimbal.
    Only the **look** can be changed.
  - Its **program cost is 1/10** of what a brand-new program with those
    points would cost.
  - The old version is **retired automatically**: it leaves the parts list,
    parts already built keep flying, and their launches and recoveries count
    toward the newest version.
  - Its stats start where the previous version's Mk. 5 ended, and Mk. 1 to 5
    climb only through the +10. Each step is smaller than in version I, which
    is fine because the version is cheap.
  - **Failures start over.** Its fail-to-light chance goes back to Mk. 1's 10%
    and falls to its mature value by Mk. 5. Phil: "new versions come with
    challenges. If it stayed, we'd never have failures past version 1."
- **Waiting on research:** when every slider is at the cap but not at 100, the
  page says which research raises the cap.
- **Max:** the version with every used slider at 100 is named
  **"<name> Max"** instead of a numeral. It has no successor.
- Its page has a short history: each version's points and Mk. 5 numbers.
- A saved craft that carries an older version's parts swaps them for the
  newest version when it's loaded in the VAB.

### 18.2 Mk. 1 to Mk. 5

- **There's no Mk. 0 any more.** Programs start at Mk. 1, and Mk. 5 is the
  top. This replaces section 5's Mk. 0 to 10.
- **Experience needed:** 2, 5, 7 and 11 for Mk. 2, 3, 4 and 5, so **2, 7, 14
  and 25 in total**. The same for every version. (Phil first set 1/4/8/15,
  then "let's bump experience numbers a bit" when recoveries went to 3.)
- **Counting:** once per program per flight. **+1** when one of its parts
  flies (an engine fires after leaving the pad), and **+3** when one is
  recovered (Phil). This replaces "each part counts" (also 2026-11-05).
  Phil's aim: the parts on a craft advance together at roughly the same rate.
  A heat shield, flown one at a time, kept up with nothing.
  Recoveries stay keyed by launch, not by the ship's ID, so a relaunched
  craft's recovery counts.
- **Flights to Mk. 5:** 25 one-way, 10 with half recovered, 7 all recovered.
  From Kestrel I at 25 points per slider to unlocking Kestrel Max (eight
  versions to Mk. 5): 200, 80 or 56 flights. Research caps pace it too.
- Mk. 1 performs as if half the points were spent, as Mk. 0 did. Mk. 5 uses
  all of them. The curve keeps its big-steps-early shape over 4 steps.
- Fail-to-light: 10% at Mk. 1, falling to the mature value at Mk. 5.

### 18.3 Build cost: a slider

- Every part type gets a **Build cost** slider. 0 points builds at 1.5× the
  size's normal price; 100 points at 0.5×.
- Like every stat it matures with Mk.: Mk. 1 builds as if half its points
  were spent, Mk. 5 as designed. So parts get cheaper as the program matures,
  and a successor carries its build cost on.
- Price still depends on the part's size. Spending more points on the other
  sliders still makes the part dearer to build ("ambition"); the Build cost
  slider works against that.
- This replaces the earlier idea of a 1.5× premium at Mk. 0, and the old 5%
  price rise from Mk. 0 to Mk. 10.

### 18.4 Gimbal: a free pick

- The Gimbal slider goes away. Gimbal is a design choice, like fuel or nozzle,
  of **0°, 5°, 10° or 20°**, at no point cost.
- Research (Phil: "perfect"): 0° always; 5° with the type's first programs;
  10° at General Rocketry; 20° at Advanced Rocketry.
- Liquid, solid, monopropellant and nuclear engines. Aerospikes and looks
  with a fixed nozzle only offer 0°. Jets and electric engines keep the
  gimbal they have.
- Fixed for the whole family: successors keep it.

### 18.5 Program cost: a ceiling for each type

- A program with no points spent costs about **√1,000** whatever its type, so
  early career stays affordable.
- With every slider at 100, a brand-new program costs its type's **ceiling**,
  set by how complex the part is, not by how many sliders it has.
- In between, cost rises exponentially with the share of points spent:
  cost = 1,000 × (ceiling ÷ 1,000) ^ (points ÷ (sliders used × 100)).
- Ceilings are high on purpose (Phil: "we can really jack up the prices of a
  brand new program with max sliders"). The cheap way to a great part is
  through versions at 1/10 the cost.
- The game-start "PIP prices" option still scales everything.

Ceilings (Phil: "that sounds perfect"; to tune once played), and what a new
program costs at a quarter, half and three quarters of its points:

| Type | Ceiling (all 100) | 25% | 50% | 75% |
|---|---|---|---|---|
| Tanks, batteries, heat shields, air intakes | √100k | √3.2k | √10k | √32k |
| Reaction wheels, solar panels, antennas, RCS | √250k | √4.0k | √16k | √63k |
| Probe cores, RTGs | √600k | √5.0k | √24k | √121k |
| Solid and monopropellant engines | √500k | √4.7k | √22k | √106k |
| Jet engines | √1M | √5.6k | √32k | √178k |
| Liquid engines | √1.5M | √6.2k | √39k | √241k |
| Electric engines | √2M | √6.7k | √45k | √299k |
| Nuclear engines | √3M | √7.4k | √55k | √405k |

Example: a liquid Kestrel I at 25 points in each slider costs √6.2k new.
Kestrel II at 35 each costs √1.3k (1/10 of √12.9k).

### 18.6 Old saves

Not converted (Phil: "I don't want to waste time making old saves compatible
until we get this thing more fully developed"). Programs in test saves may
show odd numbers or need recreating; test in a new game.

---

## Notes for the build (technical)

Phil can skip this section.

- **Reuse:**
  - From the existing source: the Mk curve, the Fibonacci experience and the
    launch/recovery counting (`Core/Rules.cs`, `KSP/FlightTracker.cs`,
    `KSP/DevelopmentScenario.cs`).
  - Must change:
    - Stats become absolute windows by fuel, size and nozzle (not factors on a
      reference engine).
    - Five categories, the new pricing curve, and a one-shot ignition check
      (not Kerbalism ratings).
- **One part per program:** create an `AvailablePart` per program at runtime by
  cloning a size/nozzle template part. This needs a prototype first; it's the
  riskiest piece.
- **Unlocks:** `PARTUPGRADE` entries placed in the CTT nodes; caps read from
  which upgrades are unlocked.
- **Hide others:** a per-save runtime filter (the same mechanism as DEPartHider
  for the parts list, plus R&D), keyed on the game-start option. It never edits
  configs.
- **StageRecovery:** confirm whether it raises `onVesselRecovered`; otherwise
  use its API.
- **Failures:** strip Kerbalism `EngineFailures` from PIP parts with an MM
  patch, then do the single ignition roll in the engine module.
- **Install:** replaces the untested v1 build in `GameData/IterativeDevelopment`.
  Back up first.

---

## Second build status (2026-11-04)

Built from sections 7, 8, 13 and 14 after Phil's "yes to all, send it". Backup of
the first draft was kept.

**Built, checked offline** (`Tools\Test-Core.ps1`, 128 checks):
- The core handles any part type (`Core\PartType.cs`): choices, categories,
  rows, one cap per type, the new prices, and sizes picked on the part.
- All six engine families (`Core\LiquidEngines.cs`, `Core\OtherEngines.cs`) and
  the ten part types (`Core\Parts.cs`).
- Saves from the first draft still load: programs without a type are liquid
  engines.

**Built, not yet seen in game:**
- 125 engine chassis (`Tools\Generate-Chassis.ps1`) and 22 part chassis
  (`Tools\Generate-PartChassis.ps1`). 82 R&D unlock entries
  (`Tools\Generate-Upgrades.ps1`).
- `ModulePipEngine` for every engine family, covering two-mode jets, electric
  power draw and Kerbalism radiation.
- `ModulePipPart` for the part types.
- Game-start settings: two columns of Off/Alongside/Replace, plus failures and
  PIP prices.
- Replace hides every type's stock parts.
- The program window draws any type, with a size preview.

**How "size on the part" works:** each part type's chassis carries a
B9PartSwitch module ("pipSize"). Each of its subtypes:
- scales the whole model (the transform named "model");
- moves the stack nodes;
- sets ModulePipPart's `variant`.

Tanks add their volume to CryoTanks' fuel switch, which is cut down to the
kind's propellants.

**Changed from the spec while building (for Phil):**
- **RCS layout is a program choice, not picked on the part.** Each layout is a
  different model with its own nozzles, and one part can't safely switch
  between them.
- **Tanks scale like stock tanks:** volume grows with the cube of the diameter,
  so a long 2.5 m tank holds what an X200-32 does.
- **No radial batteries or radial intakes** yet; stack sizes only.
- Monopropellant and electric-propellant tanks use the FL-T400's look for now.
- Most part types use one stock model, scaled per size.

**Seen in game, 2026-11-04 (zz PIP Sandbox, about 40 minutes):**
- **Loading:** clean. No B9PartSwitch errors once the models were named. The
  chassis have no Kerbalism engine wear, the Nerv chassis has only its hydrogen
  mode, and the jets keep both modes.
- **Replace hides every type's stock parts:** 54 liquid, 44 solar, 38 antenna,
  33 RCS, 32 tank, 27 probe, 16 electric, 14 battery, 10 nuclear, 9 solid,
  9 intake, and 6 each of monoprop, jet, wheel, shield and RTG.
- **First-draft programs** load as liquid engines.
- **The window** shows the part-type rows (wrapped), and the battery, tank,
  probe core, reaction wheel and solid forms with the expected numbers.
- **Fuel tank:**
  - FL-T400 size: 180 LF / 220 Ox, 2.376 t full. That is 0.25 t of stock tank
    × 1.5 at Mk. 0, plus the fuel.
  - "1.25 m extra long" stretches the model to twice the height and holds 800
    units (4.751 t).
  - "1.875 m short" holds 337.5.
  - A probe core attached on top sat flush, so the nodes move with the size.
- **Probe core:** the size switch shows in its menu, and its battery is set to
  200 EC.
- **Solid booster, 1.875 m medium:** 400 kN at Mk. 0, 2,767.5 SolidFuel, 2,491
  funds.

**Fixed during the test:**
- B9 can't see the part's own "model" root, so every chassis MODEL is now
  named PIPModel.
- Types with no choices made a chassis name with a trailing dash, so their
  programs had no part.
- In sandbox, new programs never made their part (there's no R&D).
- The part-type row ran off the window and its right-hand buttons didn't
  click.
- Default preview sizes now match the parts.
- The summary no longer calls an unused category "neglected".

**Flight and career test, 2026-11-04 (sandbox, then the zz PIP Test career):**
- **Solid flight** (probe core on a 1.25 m short booster): it flew as designed,
  and the probe core's launch counted, so it reached Mk. 1.
- **Turbojet on the pad** (intake, liquid-fuel tank, probe core):
  - Dry: 46.8 kN at low speed, Isp 5,625 s. Afterburner: 70.6 kN, Isp 2,750 s.
    Both are the program's numbers.
  - "Never fails" shows on the part.
  - All four programs logged the launch.
- **Electric:** the engine's EC ratio gives 24.4 EC/s at full thrust, the
  program's figure. This was checked against how KSP itself draws propellant.
- **Nuclear:** Kerbalism's own radiation panel on the part reads the program's
  0.250 rad/h.
- **Career locks:**
  - Nuclear says "Locked. Research: Nuclear Propulsion".
  - Liquid offers only kerolox, 1.25 m, and sea-level and vacuum nozzles.
  - The family caps differ in one save: liquid 25, solid 50 (General Rocketry
    researched).
  - Prices are right: 2,988 for 110 liquid points, 1,352 for 100 solid points,
    5,410 for 100 nuclear points.

**Fixed during the flight test:**
- **A solid booster that "failed to light" kept burning,** because solids
  refuse a normal shutdown, and it still earned a launch. Now it shuts down and
  earns nothing.
- **Loading a save in a fresh session dropped its PIP vessels.** KSP's load
  dialog checks vessels for missing parts by reading the save file itself,
  before anything else. PIP now registers that save's program parts in that
  check, and the save loads with no warning.
- **The tooltip showed 0 t** for tanks and intakes, whose mass depends on the
  size picked. It now keeps KSP's own number.

**Still not seen in game:**
- Recovery experience for the new types; no test vessel was recovered.
- Solar panels, antennas, RTGs, RCS, wheels and heat shields on a craft.
- B9 sizes locked by upgrades in career.
- The game-start settings columns.
- Whether the Emitter radiation is on all the time, or only while burning.

## 19. Fuels as data, and PIP's own boil-off (2026-11-05)

Phil, 11/5: drop CryoTanks, use Community Resource Pack for the fuels, write a
basic boil-off, and get numbers for all the fuel types. Why: CKAN lists CryoTanks
as "restricted" (its art is All Rights Reserved, though its code and configs are
MIT), and a fuel list in code can't take other mods' fuels.

### 19.1 What changed

- **Requirements:** Community Resource Pack instead of CryoTanks. PIP checks for
  its hydrogen and methane resources (it has no plugin). The netkan depends on it.
- **The fuel table** (`Core/Fuels.cs`, `GameData/PIP/Fuels.cfg`): every number a
  fuel had in code (Isp windows per nozzle, thrust, thrust-to-weight, price,
  failure rates, tech node) is a `PIP_FUEL` node. The code keeps kerolox,
  hydrolox and methalox as defaults; the config replaces them when it loads.
  `FuelLoader` reads it after Module Manager and before parts compile.
- **Engines for any fuel:** a fuel with no chassis of its own gets copies of the
  kerolox chassis (`PIP-K-*`, stand-ins too) with its propellants. Looks are made
  per fuel as before. The rule for "this is a liquid rocket engine" (for looks
  and for hiding stock parts) is stock's rule or any fuel in the table.
- **Tanks:** PIP's tank chassis have their own B9PartSwitch fuel switch, with
  PIP's own tank types (`PIP-LFOX` and so on, CryoTanks' numbers). Anything the
  FL-T400 came with is stripped: its resources, any fuel switch, CryoTanks'
  module. A fuel's `TANK` node adds its recipe to the standard or cryogenic tank.
  The subtype names stay as before ("LF/O", "LH2/O"…) so saved craft load.
- **Boil-off** (`ModulePipBoiloff`, on cryogenic tanks): each resource in
  `PIP_BOILOFF` loses a % per hour unless cooling is on and the vessel has
  electric charge; partial power, partial loss. The program's Insulation scales
  the loss and the power (the existing boil-off factor, 2× to 0.2×). A vessel
  that comes back from the background catches up: if it was cooled and powered
  when last seen it lost nothing, otherwise it boiled for the whole time away.
  A toggle turns cooling off and on. Not modeled: power used while unloaded.

### 19.2 The fuels

Eleven, chosen to differ: kerolox, ethalox, nitric acid/UDMH, peroxide/kerosene,
Aerozine/NTO, hydrolox, syntin, methalox, FLOX/kerosene, ClF5/hydrazine,
fluorine/hydrogen. MMH/NTO, UDMH/NTO, hydrazine/NTO, hydyne and hydrazine/LOX
were left out: each is within a few seconds of one that's in, and each fuel
costs a full set of engine chassis and looks (about 34 chassis and one look per
installed liquid engine model).

The table, the real data, sources and rules are in [FUELS.md](FUELS.md);
`Tools/Generate-Fuels.ps1` writes `Fuels.cfg`. In short: Isp is kerolox's
windows times the real Isp ratio (one table, [W], for every fuel), mixes and
tanks follow real mixture ratios and densities, and thrust, weight, price,
failures and tech are game choices.

### 19.3 Open questions for Phil

1. **Propellant prices.** Settled in 19.6.
2. **Storable vs cryogenic.** `Oxidizer` (KSP's liquid oxygen) doesn't boil, as in
   stock, so "storable" only matters against hydrogen, methane and fluorine.

Load time, measured (cached loads, after the first launch that re-runs Module
Manager): Phil's pack 59 s before, 65 s with eleven fuels (272 new chassis, 550
liquid looks); the clean copy 16–23 s. Phil, 11/5, on the part count: "it's just
text right? Who cares?" It costs about six seconds.

### 19.4 Plumes per fuel (2026-11-05)

Phil, 11/5: make the exhaust plume differ by fuel, using what his pack has, with
sharable licenses. Waterfall (and its stock configs) are CC-BY-NC-SA, so PIP
ships none of them; it only names templates already installed, as looks name
parts. Without Waterfall nothing changes.

- Each fuel's `PLUME` node (Fuels.cfg) lists Waterfall templates by role:
  `lower` (sea-level plume), `upper` (vacuum), `core` (glow in the bell),
  `generator` (gas-generator exhaust), best first, and `match` words.
- A liquid engine look or chassis burning a fuel its model wasn't made for gets
  each fuel-specific template swapped for the fuel's template of the same role,
  at the same place. A glow with no equivalent is dropped; anything else without
  one is kept. Aerospikes, lights and non-fuel effects are never touched.
- **Size:** templates draw at very different sizes. The Goldfish's own plume is a
  vernier template; swapped 1:1 for Aerozine's main plume it came out several
  meters wide on a 0.625 m engine (seen in flight). Now a swapped template is
  rescaled by the new template's size over the old one's, where a template's
  size is the scale installed mods give it per meter of engine (median over every
  engine using it, measured at load: 59 templates in Phil's pack), else a typical
  size for its role (main plume 0.55, vacuum 0.7, vernier 2.4, glow 0.3, gas
  generator 0.35). Re-flown: nozzle-sized, Aerozine-colored.
- In Phil's pack 682 templates are swapped at load.

### 19.5 Play-test (2026-11-05, Claude, sandbox save in Phil's pack)

Seen working: 11 fuels in the new-program window with their tech; Aerozine Isp
matches the table (257 s vacuum, 234 s sea level at 25 points, Mk. 5); labels
("0.625 m aerozine"); standard tank switch LF/Ox, LF, Ox, Eth/Ox, UDMH/IRFNA,
LF/HTP, A-50/NTO (114.7 + 135.3 units in 50 volume, as computed), Syntin/Ox,
N2H4/ClF5; cryogenic tank switch LH2/Ox, LH2, LCH4/Ox, LCH4, LF/FLOX, LH2/LF2,
each showing its own boil-off and cooling cost; stock delta-v readout 1,400 m/s
for the Aerozine stack (matches a hand calculation); cooling on draws EC; with the
battery flat "not enough power"; cooling off loses 1.4 of 250 LH2 in 5 h 33 m at
0.1 %/h (as computed); the engine fires and drains both propellants; the flight
logs experience (Mk. 2). Clean copy (no CryoTanks, no Waterfall): loads with no
errors or exceptions.

Notes:
- BetterTimeWarp (Phil's pack) keeps electric charge free during time warp, so a
  cooled tank never runs dry while warping there. Not PIP's to change.
- RemoteTech blocks part-menu buttons on an uncontrolled probe, so "Turn cooling
  off" can't be pressed once the probe is out of power. Fair.
- The cryogenic tank's description was cut off in the program window; shortened.
- Not seen yet: hydrolox, methalox and the other fuels' plumes in flight; a
  vessel catching up on boil-off after being unloaded.

### 19.6 Dense fuels and fuel prices (2026-11-05)

Phil asked what a Jumbo-64 of ClF5 would cost to fill: 311,000 at Community
Resource Pack's prices (kerolox 2,940). "That seems exorbitant ... I do like the
price variability for better / denser / more efficient fuels, but surely not to
that extent." And it wasn't worth much at any price: barely more Isp than
kerolox, less than methalox, and the first tank rule (empty tank = propellant
mass ÷ 8) gave density no advantage at all. Phil approved both changes ("yep
perfect - make these changes"):

1. **Tanks weigh by size, like stock.** Every storable or non-hydrogen recipe
   weighs stock's 0.000625 t per unit of volume, a hydrogen mix CryoTanks'
   0.000278904. Propellant per ton of tank: ClF5 12.2, fluorine 11.1, peroxide
   10.9, nitric acid 10.4, FLOX 10.3, Aerozine 9.6, kerolox 8, syntin 7.6,
   methalox 7.5, ethalox 7.4, hydrolox 7.2. Dense fuels now carry more
   propellant per ton of tank, the real reason rockets use them.
2. **Fuel prices up to about 4× kerolox's** to fill a tank, by performance:
   ethalox 158, kerolox 184, nitric acid 220, peroxide 260, syntin 321, Aerozine
   370, methalox 383, FLOX 550, ClF5 650, fluorine 740 (hydrolox 97). PIP sets the
   prices of the Community Resource Pack resources its fuels use (UDMH,
   IRFNA-III, HTP, Aerozine50, NTO, FLOX70, Hydrazine, ClF5, LqdFluorine) with
   `:NEEDS[!RealFuels]:FINAL` patches, so they hold everywhere in the game
   (launch, recovery, ISRU) and Real Fuels' economy is left alone. In Phil's
   pack nothing else buys or sells them (Kerbalism only names hydrazine in
   comments and the hypergolics in its engine-failure list). A Jumbo-64 of ClF5
   now costs about 10,400 to fill.

Rejected: cutting the price only in PIP's tanks (B9 tank-cost offsets). An empty
tank would then cost less than nothing, and the fuel would refund at the full
price when recovered from anywhere else.

### 19.7 Every fuel in standard tanks; cryogenic tanks later (2026-11-05)

Phil: "allow all fuels in all tanks, whether cryogenic or standard, but non-cryo
tanks will just lose some fuel in the earlier missions". Before this, hydrolox
engines and cryogenic tanks both unlocked at Advanced Fuel Systems (160), so a
cryogenic fuel never existed without its tank. Built as proposed ("Yes, build
it"):

- **Standard tanks hold every mix:** LF/Ox, LF, Ox, LH2/Ox, LH2, LCH4/Ox, LCH4,
  and every fuel-table recipe (kind S and C). They carry `ModulePipBoiloff` with
  `canCool = False`: no Insulation slider, no cooling button, and cryogenic fuels
  boil at `FuelTanks.Uninsulated` = 4 times the base rate, always. The status line
  and button only show while the tank holds something that boils.
- **Cryogenic tanks move to Large Volume Containment (300),** the next node on the
  fuel-tank line. They keep only the cryogenic mixes, the Insulation slider
  (2× the base rate at 0 points to 0.2× at 100) and cooling. Keeping storables out
  of them keeps standard tanks the right pick for storable fuels.
- So hydrolox (160) flies its first missions in leaky standard tanks: a Mun trip
  loses about 2.4% of its hydrogen, a Minmus trip about 6%, a coast to Duna nearly
  all of it. Cryogenic tanks become an upgrade for long missions instead of a
  prerequisite.
- Saved craft load unchanged (subtype names kept; new ones added after them).
  Cryogenic tank programs already started stay.

Not done (discussed, for later): boil-off that follows sunlight and distance from
Kerbol, and liquid oxygen boiling at about methane's rate.

## 20. Radial and adapter tanks (design, 2026-11-06)

Phil, on a new test save: "lots of 'probodyne' tanks im seeing. also the adapters
don't get replaced". Replace hides only plain round stack tanks of one size (77 in
his install); radial tanks, adapters and odd shapes stay as fixed parts (80 tank
parts visible). Phil approved every recommendation below ("Go with yours").

### 20.1 Radial (side-mount) tanks

1. **A side-mount size, picked for the program's life**, as for batteries and
   intakes (section 14). A tank program is in-line or radial.
2. **Five sizes, by volume** (an FL-T400 is 400): Tiny 5, Small 25, Medium 50,
   Large 200, Huge 1,600. Today's radial tanks: Stratus-V Mini ~5, Dumpling 22,
   Baguette 54, HR-1 160, HR-18 1,760.
3. **All four kinds** (standard, cryogenic, monopropellant, electric).
4. **Standard look:** stock parts, always installed: the Baguette for fuel tanks,
   the Stratus-V Cylindrified for monopropellant, the PB-X50R for electric.
5. **Looks:** any installed radial tank of the same kind, scaled to the program's
   volume (by the cube root of volume over the look's own volume).
6. **Unlock:** Propulsion Systems (90), where stock puts the Baguette; Large with
   1.875 m tooling, Huge with 2.5 m tooling.
7. **Replace hides** radial tanks that hold only fuel (16 in Phil's install:
   Dumpling, Baguette, 4 MLT-R, 3 HR, 3 Stratus-V, 2 radial xenon, radial argon and
   lithium). Kept: ore tanks, Glykerol, KAS's transfer station, the fuel duct and
   the drain valve.

### 20.2 Adapter tanks

An adapter model's taper is built in: PIP can scale it up or stretch it, not change
the ratio of its two ends. So each ratio needs its own model:

| Ratio | Pairs | Model | Where |
|---|---|---|---|
| 2:1 | 0.625↔1.25, 1.25↔2.5, 1.875↔3.75, 2.5↔5 | C7 Brand Adapter (stock) | everywhere |
| 3:2 | 1.25↔1.875, 2.5↔3.75 | Kerbodyne ADTP-2-3 (stock) | everywhere |
| 4:3 | 1.875↔2.5, 3.75↔5 | FL-A215 (Making History), NR-AD (Near Future Launch Vehicles) | padlocked without them |
| 3:1 | 0.625↔1.875 | FL-A150 (Making History) | padlocked without it |

8. **An adapter size pair, picked for the program's life** (e.g. 2.5→1.25 m). One
   part works either way up; the editor flips it.
9. **Short and long:** long is the model's own shape, short half its height.
10. **Volume from the cone shape**, set so the long 2.5→1.25 m adapter holds 800,
    as the stock C7 does.
11. **An "Empty" pick** on adapter tanks makes one a plain structural adapter, so
    PIP can stand in for simple structural adapters too.
12. **Replace hides** 9 fuel adapters (C7, ADTP-2-3, FL-A150, FL-A151L, FL-A151S,
    FL-A215, the three NR-AD tanks) and 7 plain structural adapters (FL-A5,
    FL-A10, the two Rockomax Brand Adapters, FL-XA15, FL-XA30, ADTP-2-3A).
13. **Kept:** slanted adapters, multi-adapters (bi/tri/quad/OMNI, SM-2 to 4),
    skeletal and truss adapters, station adapters, nosecones, the engine cluster
    adapter, Mk2/Mk3 parts, and the multi-size MLT cluster tanks.
14. **Unlock:** Fuel Systems (90), plus each size's tooling.

### 20.3 Both

15. **Numbers as in-line tanks:** weight by volume, the Weight / Strength /
    Insulation sliders, the same prices. (Stock adapters weigh ~14% more per
    volume; ignored.)
16. **Radial first**, then adapters.

### 20.4 As built (2026-11-06)

Both are built as decided, with these details settled in the build:

- **Fill, changed from decision 10.** Measured from the installed parts, the C7
  holds about 112 units per cubic meter of its cone; the ADTP-2-3, FL-A215, NR-AD
  and FL-A150 hold 160 to 172 (and PIP's in-line tanks 172). One fill set by the
  C7 would leave the other tapers a third short of their stock adapters. So each
  taper holds what its own stock adapter holds, scaled by size cubed: 2:1 the
  C7 (800 at 2.5 to 1.25 m), 3:2 the ADTP-2-3 (3,000 at 3.75 to 2.5), 4:3 the
  FL-A215 (1,200 at 2.5 to 1.875), 3:1 the FL-A150 (160 at 1.875 to 0.625). The
  long 2.5 to 1.25 m adapter still holds 800, as decided.
- **Height:** each taper is as tall, for its big end, as its stock adapter (the
  C7 is 2.5 m at 2.5 m; the ADTP-2-3 0.6 of its big end; the FL-A215 0.75; the
  FL-A150 0.4). Short is half. Other looks of the taper are stretched to it, as
  in-line looks are to a tank's length.
- **Looks:** any installed fuel adapter or plain structural adapter of the taper
  (two round sizes, every stack node on the axis), unless it's under a quarter
  of the taper's height (flat caps like the FL-XA15 would be stretched 8×). A
  plain structural adapter has only harmless modules (variants, cargo, IVA,
  TweakScale and the like): station adapters with habitats or switches stay out.
- **All four kinds** (standard, cryogenic, monopropellant, electric) can be
  adapters; any adapter look serves all four.
- **Empty** is a propellant pick on the part (tank type `PIP-EMPTY`, no
  resources) weighing a quarter of a standard tank of that volume: stock's
  ADTP-2-3A weighs 27% of the ADTP-2-3. Lesson from the first test: B9PartSwitch
  gives a new part that holds nothing its resourceless subtype, so adapters came
  out Empty. Adapter looks now carry their kind's first propellant as
  placeholders, which B9 matches and refills.
- **Replace's tank list:** once a session the log names every tank Replace hides
  and every fuel-tank-category part it leaves (`[PIP] Replace hides ... tanks`).
  The first list (2026-11-06) showed 108 hidden: the 17 adapters as planned, but
  the four MLT-R radial tanks were left, because Near Future Exploration names its
  propellant switch `surfaceSwitch`, not `fuelSwitch`. A tank's switch is now any
  B9PartSwitch whose subtypes hold only rocket propellants, whatever its ID. The
  keep-words now apply to every tank shape, so the MLT-10 (a one-size cluster)
  stays as decided for clusters.
- **Tested 2026-11-06** in a test career in Phil's game (three launches, all with his OK):
  52 adapter looks (13 adapter parts × 4 kinds) and 27 radial looks made, no
  errors. "Funnel I", a long 2.5 to 1.25 m adapter, wears the C7, holds 800 and
  weighs 4.75 t full (0.751 t dry); "Saddle I", a medium radial tank, wears the
  Baguette and sits flush on Funnel's side. The 4:3 pair 2.5 to 1.875 m takes
  the FL-A215 (Making History) with both NR-AD tanks as other looks. Replace
  hides 112 tanks (77 before): the 17 adapters, 14 radial tanks, the 4 MLT-R.
  It leaves 45: Mk2/Mk3 parts, the slanted C7, cluster and skeletal parts,
  nosecones, ore and nuclear-fuel tanks, the fuel line, release valve and KAS
  station, a solid-fuel tank, and 7.5 m and 10 m tanks PIP doesn't make.
- **Where the look lives:** an adapter program keeps its look in the program's
  side-mount look field, as a side-mount program does; both are "mounts" made
  only from a look (`PartType.MountOf`: "" in-line, "R" side-mount, "A" adapter).
- **Unlocks:** a size option can now need several nodes (`Option.Needing`):
  Fuel Systems plus each end's tooling.
- **Node sizes:** each end gets its own (the small end the smaller node, or the
  top when both say the same, as Making History's do).
- **Replace** hides fuel adapters and plain structural adapters of PIP's nine
  pairs. Kept by name or title: skeletal, SKL, truss, girder, cluster, nose,
  slant. In Phil's install that should be 9 fuel adapters and 8 structural ones:
  the 7 listed in decision 12 plus Near Future Construction's SM-1 Stack Adapter.
  The NR-AD-CAP stays (it has a texture switch).

## Looks and side-mount build (2026-11-04)

Built from sections 15 and 16 after Phil's "Yes, build it". This is Looks
step 1: batteries, reaction wheels, air intakes (with side-mount), RTGs, heat
shields and probe cores. Backed up first.

**How it works:**
- At load, right after Module Manager, PIP copies every installed part of
  those six types into a hidden "look chassis" (`KSP\Looks.cs`). The rules
  for which part is which type are Replace's, read from the config.
- Each copy:
  - keeps the model, its paint variants and cosmetic switches (ReStock foil,
    Near Future surface options);
  - drops other scalers, failure modules, fuel and other switches;
  - gets PIP's module and PIP's size switch, scaled from the part's own size.
- A program's part is cloned from its look's chassis. No look means PIP's own
  chassis, so old programs are unchanged.
- **Side-mount parts are a second part for the same program** (`PIP.<id>.side`).
  A program from before this build gets the default side-mount look the first
  time it loads, and keeps it.
- **Numbers never come from the look.** An intake's area, weight and speed
  curve come from the program and PIP's standard intake of that speed type. A
  shield's ablation comes from PIP's standard shield.

**In Phil's install:**
- 60 looks: 10 battery and 4 side-mount battery, 4 intake and 2 side-mount
  intake, 25 probe, 4 RTG, 5 shield, 4 wheel and 2 side-mount wheel.
- 15 tests cover the side-mount sizes, the intake numbers and the look
  defaults. 7 cover saving looks, and one the side part's name: 150 checks pass.

**Seen in game, 2026-11-04 (zz PIP Sandbox):**
- **Loading:** clean, with no PIP or B9 errors.
- **The window:** Look and Side-mount rows for each type.
- **Batteries:** a program with the B-12K look and the Z-400 side-mount look.
  - Both parts are in the parts list.
  - The inline part switches sizes: at 3.75 m, 0.5 t and 7,125 EC.
  - The side-mount part is small, medium or large (71 / 285 / 570 EC at 25
    points) and sits flush on the surface at every size.
- **Intakes:** a subsonic program in the shock cone look, 0.06 t, with the
  XM-G50 as its side-mount part (0.03 t).
- **Probe core:** the HECS2 look, with 200 EC at 1.25 m and HECS2's own foil
  variants.
- **Reaction wheels:** the Small Radial Gyroscope is the default side-mount
  look. It reads 0.8 kNm and 0.015 t.
- **Saving and flying:** the craft saved and reloaded with every size and look
  intact. On the runway, every part loaded with the program's mass. The flight
  was reverted, so nothing was left in the save.
- **Older programs:** they got side-mount parts automatically (Kestrel's
  battery the Z-100, Aurora's intake the XM-G50).

**Fixed during the test:**
- **The MPO probe look had no model.** KSP ignores the file name in old-style
  `mesh =` lines and uses the folder's model; PIP now does the same.
- **Long part names pushed look buttons off the window.** Rows now break by
  width.

Both fixes were checked in a second launch: every look button sits inside the
window, and the MPO probe is listed (25 probe looks).

**Also changed:** Replace no longer hides Breaking Ground's turboshafts (14.10
says they stay stock) or Near Future's utility pod, which has an RTG inside.

**Not done yet:**
- Looks for solar panels, antennas, fuel tanks and RCS (step 2), and for
  engines (step 3).
- **Looks aren't locked by tech.** Any installed part can be a look from the
  start.
- **Recovery experience is still untested.**

## Looks step 2 build (2026-11-04)

Solar panels, antennas, fuel tanks and RCS (Phil: "Yes, step 2"). Backed up
first.

**Rules (section 16's table):**

| Type | A look suits a program when | Scaled to each size on the part by |
|---|---|---|
| Solar panels | It's fixed for a fixed program, deployable (it has a deploy animation) for a deployable one | Output: panel area to the middle of each size class's window (S 1.2, M 6.7, L 30, H 134 EC/s) |
| Antennas | It's an omni for an omni program, a dish (RemoteTech dish range) for a dish one | Dishes: weight, to the middle of each class's window. Omnis keep their size |
| Fuel tanks | Fuel tanks suit standard and cryogenic programs. Monoprop tanks suit monoprop programs, and xenon, argon and lithium tanks suit electric-propellant ones | Diameter, and a length stretched from the tank's own height (an FL-T400's proportions are "long") |
| RCS | Its number of nozzles equals the layout (1, 4 or 5). Two-way and three-way blocks never fit | Not scaled |

- **Tanks and RCS get one look part per program choice** (tank kind; RCS
  propellant), because those choices change the part's config.
  - A tank look takes its propellant switch from PIP's own tank of that kind.
  - An RCS look is set to burn monopropellant or kerolox.
- **Program numbers win, not the look's.** Panel output, deployable toughness,
  dish range and RCS thrust all come from the program.

**In Phil's install:** 315 looks in all: 44 solar, 38 antenna, 127 tank
(fuel tanks twice, for standard and cryogenic) and 46 RCS (23 blocks × 2
propellants), plus step 1's 60. 164 offline checks pass.

**Seen in game, 2026-11-04 (zz PIP Sandbox):**
- **Loading:** clean, with no PIP or B9 errors.
- **Look lists filter by model:**
  - Fixed panels show 6 looks and deployable 37.
  - Omni antennas show 6 and dishes 32.
  - Single RCS shows only one-nozzle ports; 4-way shows only four-nozzle
    blocks.
- **Tank, Rockomax Jumbo-64 look:**
  - It switches LF/Ox, LF or Ox, and keeps Rockomax's paint variants.
  - 1.25 m long holds 400 units, extra long 800, and 1.875 m medium 675.
- **Solar, K14 'Sunrise' look:** it unfolds on the PIP part. Kerbalism runs it
  at the program's 4.5 EC/s.
- **Antenna, DR-1 look:** it attaches; scaled 1.47× for the short-range class.
- **RCS, AVCS-4 look:** it attaches as a 4-way monopropellant block.
- **Saving and flying:** the craft saved, reloaded and launched to the runway.
  It was reverted afterwards. The sandbox now has a `quicksave.sfs` from
  checking Kerbalism's panel state.

**Fixed during the test:**
- **Tank looks lost their program when resized.** The size switch reloads the
  chassis config, and the look config carried an empty "model".
  - Look configs no longer carry one.
  - The part re-links its program whenever the size switch reloads it.
- **Kerbalism ignored PIP's solar output, for PIP's own panels too.**
  Kerbalism reads a panel's rate before PIP has set it. PIP now sets
  Kerbalism's own copy (`SolarPanelFixer.nominalRate`).

**Still untested:**
- A dish's RemoteTech range on an active dish.
- RCS thrust in flight.
- Kerbalism's "Solar Panel Status" line stays blank on the runway. Its stored
  state is right ("ExtendedFixed", 4.5 EC/s).
- Engines (step 3).

## Looks step 3 build: engines (2026-11-04)

Phil: "Yes, step 3". Backed up first.

**Rules (Phil's decisions in section 16):**
- **An engine look is never scaled.** It suits a program only at its own size
  (from its `bulkheadProfiles`).
  - Radial engines (Spider, Thud, Twitch, Puff) have no size, so they're never
    looks. Neither is the Goliath.
  - Sizes with no installed engine use PIP's standard model.
- **The program's numbers win:** thrust, Isp, mass, gimbal range, failures and
  power draw. Jets take their speed and altitude curves from PIP's standard
  jet of that type, so a ramjet program wearing a Juno look is still a ramjet.
- **A look with a fixed nozzle greys out Gimbal** in the new-program window.
  Starting a program with Gimbal points on such a look is refused.

| Type | A look suits a program when | Notes |
|---|---|---|
| Liquid | It's at the program's size; aerospikes only for aerospike programs | One look part per fuel (kerolox, hydrolox, methalox), with the program's propellants. Looks that burn the program's fuel are listed first |
| Solid | It's at the program's size and length. The length is whichever of that size's three loads the booster's fuel is nearest | It carries the program's SolidFuel for that size and length |
| Monoprop | It's at the program's size | |
| Nuclear | It's at the program's size, for either core | It keeps one hydrogen mode. The Nerv's LF mode and the trimodals' oxygen boost are dropped, with their plumes and heat modules |
| Electric | It's at the program's size, burning the type's gas (xenon ion, argon Hall, lithium MPD) | Multi-mode thrusters (VASIMR) are left out, as decided in 13 |
| Jets | It's at the program's size, with the same modes: one air mode for turbofans and ramjets, dry/wet for turbojets, air plus rocket for combined cycle | |

- **An engine look keeps its own switches** (mesh, shroud, nodes) unless they
  change the engine or what it holds.

**In Phil's install:** 507 looks in all. Engines: 150 liquid (50 engines × 3
fuels), 10 nuclear, 13 electric, 9 solid, 5 monoprop and 5 jet. 178 offline
checks pass.

**Seen in game, 2026-11-04 (zz PIP Sandbox):**
- **Loading:** clean. The only warnings were about hidden chassis prices.
- **Look lists:**
  - Liquid at 0.625, 1.25 and 3.75 m showed only engines of that size, the
    program's fuel first. 1.25 m had 15 looks and no aerospike.
  - Nuclear at 2.5 m: Cherenkov, Poseidon, Liberator, Deliverance and
    Emancipator.
  - Jets at 1.25 m: turbofan and ramjet show Wheesley and Whiplash; turbojet
    shows only the Panther.
- **Fixed nozzle:** picking the Reliant greyed out Gimbal with the note.
- **Buzzard look on a kerolox program** (the Buzzard burns methane as
  installed):
  - In the editor, Kerbal Engineer showed it drawing the tank's LF/Ox (2,345
    m/s, TWR 2.27).
  - In flight it ran on the program's numbers: 147.5 kN, Isp 231/257 s, 1.6 t.
  - The flight was reverted.

**Still untested:**
- An engine look actually firing. It wasn't burned: the test craft tipped over
  on the runway. (Done in the flight test below.)
- Jet, nuclear, electric and solid looks on a craft.

## Looks flight test (2026-11-04)

Phil: "Yes, flight test". zz PIP Sandbox only. The save was
backed up first.

**The craft ("PIP Flight Test", built in the VAB, flown from the runway):**
- Sirocco probe core, HECS2 look.
- Peregrine tank, Rockomax Jumbo-64 look, 1.25 m long.
- Bolide engine: 1.25 m kerolox, sea level, Buzzard look. The Buzzard burns
  methane as installed.
- Vortex dish, DR-1 look.

**Results:**
- **Stacking on looks works in the VAB.** The tank snapped under the probe and
  the engine under the tank. (In the SPH, nodes wouldn't snap; that looked like
  a placement problem, since the node markers sat in the right places.)
- **The engine look fires.** It burned the tank's LF/Ox; Δv fell as it ran.
  It didn't fail to light at its 10% Mk. 0 chance.
- **Launch experience:** after a short hop, Bolide, Peregrine, Sirocco and
  Vortex each reached Mk. 1. That's the engine's "fired after leaving the pad"
  and the parts' "flew".
- **Recovery experience:** recovered from the Tracking Station, all four
  reached Mk. 2. First recovery test of any PIP type.

**Seen, not fixed:**
- **MechJeb error.** MechJeb's deploy controller throws a NullReferenceException
  (`MechJebModuleDeployableController.DiscoverDeployablePartModules`) when the
  DR-1-look dish is added in the editor. It's harmless to flight. Not yet
  checked whether the plain Near Future DR-1 does the same.
- **The sandbox's programs moved on.** The four programs above are now Mk. 2.
- **No parachutes** are listed in the sandbox's parts list, so the test used a
  hop instead of a flight.

**Still untested:** a dish's range once it's switched on, RCS thrust, and
jet, nuclear, electric and solid looks on a craft.

## Program window redesign (2026-11-04)

Phil: "let's take another crack at the UI ... cleaner, sleeker, sexier, HOTTER!"
Phil approved a mockup first: "Yes, build it".
The code and DLL before it were backed up.

**What it is (`KSP\PipWindow.cs`, `KSP\PipSkin.cs`, `KSP\PartPreview.cs`):**
- **Own look, no assets:** dark flat panels and PIP orange. Every rounded box,
  the slider knob, the padlock, the striped "past the cap" track and the die are
  textures drawn in code. Bahnschrift (shipped with Windows) for names and
  numbers, Segoe UI for text; KSP's font where they're missing.
- **Same window size (1100 × 720),** dragged by its title bar. The title bar
  shows funds (or "Programs are free in this game").
- **Left rail:** New program, then programs grouped by type. Each row: type
  symbol, name, model, Mk badge and a thin bar of experience to the next Mk.
  Retired programs fold away.
- **New program, left half:** name with a die for another suggestion; the 16
  types as tiles (Lq, So, Tk…); the type's choices as segmented controls,
  locked options showing a padlock and the research they need on hover (sizes
  drop the " m" when they don't fit, and the label says "Size, m"); the look
  (and side-mount look) as a card with its picture.
- **New program, right half:** the part's real model turning slowly; a size
  preview where the size is picked on the part; a 0–10 strip for the Mk being
  previewed; one row per points category with a slider (striped past the cap,
  with a marker), − / + (5, or 1 with Shift), the value at the previewed Mk and
  "Mk. 0 → Mk. 10" under it. Sub-rows (Isp at sea level and so on) sit under
  their category.
- **Bottom bar:** points, program cost (red when funds are short), build price
  each, one line comparing with the reference engine (hover for the full line and
  the summary) or the warning/error, and Start program.
- **Look picker** replaces the right side: search, filters (All, Burns <fuel>,
  Gimbals), one row per look with a picture, its mod (from the part's folder:
  Stock, Making History, Cryo Engines, ReStock+…) and tags (Standard, fuel in
  green or yellow, Fixed nozzle in red).
- **A program's page:** the look turning, name with a big Mk badge, the
  experience track to Mk 10, chips (launches, recoveries, points and cost, look),
  and the stats now against Mk. 10 in two columns with a bar of how close it is.
  Retire asks in place.

**The 3D preview (`PartPreview`):** copies the source part's `model` transform
under an inactive holder (so no script wakes), strips scripts, colliders,
lights and particle renderers, applies the default variant's game objects,
hides ModuleJettison shrouds, puts it on layer 31 at y = −40,000, and films it
with a private camera and three directional lights into a 400 px render
texture (thumbnails: 96 px, one made per frame, kept up to 300). The rig is
switched on only while the camera renders, in LateUpdate, so no other camera
sees it. Any exception turns the preview off for the session and the window
falls back to the type symbol.

**Seen in game (sandbox, KSC and SPH):** the window, picker and program page all
draw as designed; the turntable and thumbnails render in both scenes. First
pass showed engine shrouds covering the nozzles (now hidden), a cut-off
"Progress to Mk 10" heading (now "Progress"), a full bar for a fixed nozzle
(now none), and long look titles clipped in captions (now the short name, e.g.
"Buzzard look"). Second pass: fuel tanks' 21 size-and-length options filled the
right side and pushed the points out of sight; labels like "1.25 m long" now
split into a Size control and a Length control (lengths a size doesn't come in
show a padlock: "Not made at 0.625 m"). Wrapped segmented rows are evened out,
so no option sits alone on a line ("Combined cycle", "Electric propellant").
Batteries, antennas and jets checked; a slider click set 75 points and the
values followed. Last: a long value ("×1.40 today's tanks") was clipped; such
values now step down to a smaller size (built, not yet seen in game).

## Guard for games started before PIP (2026-11-04)

Phil: "add the guard". A save with no PIP settings (like Phil's main career, started before PIP
was installed) would get PIP's defaults when loaded: every type on Replace,
hiding its stock engines, tanks and the rest.

**What it does (`PipScenario.OnLoad`, `PipGuard` in `KSP\Settings.cs`):** the
first time PIP sees a game, it decides whether the game was started with PIP:
it already has programs, or it's brand new (clock under an hour, no vessels).
A game already under way wasn't: every PIP type is set to Off on both settings
pages, a screen message says so once, and the PIP toolbar button doesn't show
(it follows the settings). The decision is saved in the scenario
(`startedWithPip`), and the Off settings are saved with the game, so its
difficulty settings can switch types back on later. Existing PIP saves have
programs and keep their settings.

Left as is: the PIP unlock entries ("PIP: Liquid-Fuel Engines" and so on) still
appear in R&D nodes in such a game; they cost nothing and unlock nothing there.

**Repository:** the source is in Git and pushed to the private GitHub repo
https://github.com/phillipnutt/phils-iterative-parts, on Phil's personal account
(phillipnutt). Commits use his personal GitHub no-reply email.

## Phil's play-test notes (2026-11-05, clean install)

Phil is playing a fresh career on a clean test install and collecting notes; the
open ones get built together when he says so.

**Fixed the same day:** the stock-part sorting crash (Replace hid nothing, the
editor broke) and the R&D unlock pictures. See section 17, "Phil's own test".

**Built 2026-11-05 (Phil: "run all this stuff"), commit 5b95a4c:** items 3, 5,
6, 9, 10 and 11 below. Item 8 was not a bug.

3. **Small stock tanks still show** in R&D and the editor: FL-T100, FL-T200,
   FL-T400, Oscar-B. *Cause:* no B9 tank was ever classified. CryoTanks
   replaces stock tanks' resources with a B9PartSwitch fuel switch, and B9 only
   adds resources to parts placed in the VAB, so the prefab looked empty. The
   cargo-part pattern was a coincidence; most stock tanks were showing, in
   Phil's pack too. *Fix:* a fuel switch marks a tank, as Looks already did;
   Propulsion-category tanks (stock 3.75 m) count too.
5. **Gray out what can't be built yet.** Type tiles not unlocked are dimmed with
   a padlock but still clickable to browse; Start program is gray and can't be
   pressed while the design is locked (the "Research X" line stays); the
   window opens on a type that can be built.
6. **Clipped text:** "Output in Kerbin's sunlight" under Output (solar), and the
   points hint at the bottom of the right column ("...or 1 with Shift").
8. **Sea-level thrust:** the Harrier's description says 8.91 kN at sea level
   (Isp 161/182), but KSP's Δv panel shows 5.47 kN at altitude 0. *Not a bug:*
   the craft's Harriers had the Thrust Limiter at 53%. At Mk 2,
   11.62 × 167/188 × 0.53 = 5.47 kN exactly. The Δv app and flight both apply
   the limiter; PIP's numbers are at 100%.
9. **Experience counts each part** (section 5), built together with the
   recovery fix: recoveries are de-duplicated by the ship's persistentId,
   which KSP reuses when the same craft is relaunched, so the second flight's
   recovery was lost. Key them by launch instead.
10. **No more "× today's".** Phil: real numbers. Values are given for the size the
    page shows, like the "holds 400 units" row:
    - Tank empty weight in tonnes (e.g. "0.25 t").
    - Tank boil-off in % of fuel lost per hour (CryoTanks' own figure).
    - Intake airflow as intake area in m², and intake weight in tonnes.
    - Heat shield heat per ablator and solar durability, which have no clean
      unit, as plain percentages ("20% more heat per ablator", "30% tougher").
    - Also fix the overflow: long values were meant to shrink to fit, and
      "×1.40 today's tanks" was cut off instead.

11. **Size, length and in-line/side-mount per program** for every part type
    (section 14, "Size is chosen per program"). Found when "Test Short" and
    "Test Medium" tank programs came out identical: the window's size and
    length buttons were only a preview, and one program covered every size.
    The window's preview picker becomes the real design choice.

**Decided, not bugs:** the Mk1 pod sits in Start on plain KSP (Probes Before
Crew moves it in Phil's pack; PIP never touches crewed pods); stock shows TWR in
the staging list through the Δv app's display options. No decoupler type: there's
too little to put points into.

**Later the same day (fuels work, section 19):**
- **Plumes far too big** on small engines (Claude's flight test): the Goldfish's
  own plume is a vernier template, and swapping it 1:1 for Aerozine's main plume
  made a column meters wide. Swapped templates are now rescaled by their measured
  size per meter of engine (19.4). Fixed, re-flown.
- **"Pushing more GUIClips than popping" and "Value cannot be null, key",
  repeated a million times** (Phil, new career on the clean install): the
  program window's gimbal row asked R&D for the name of the free gimbal options'
  tech node (none) while liquid engines weren't researched. It now names the
  type's own node, a missing node has no name instead of an error, and any
  drawing error closes the window with one message (commit b13ccce). Fixed.
- **ClF5 cost 311,000 to fill a Jumbo-64** (Phil: "exorbitant") and was barely
  better than kerolox: fixed by 19.6.
- **Methalox and hydrazine/ClF5 plumes looked the same on the clean install**
  (Phil): expected, since that install has no Waterfall. In Phil's pack they
  differ.
- **"Are the separate fuel options for cryo and standard actually real?"**
  (Phil): half. The storables really are storable; kerolox counts as standard
  only because KSP's oxidizer never boils (real liquid oxygen is as cryogenic
  as methane). Led to 19.7. Sunlight-based boil-off and boiling oxygen were
  discussed and parked.

---

## First draft status (2026-11-03)

Tested in game. Test saves: `zz PIP Test` (career), `zz PIP Sandbox`.

**Works**
- **Game-start settings:** the PIP section shows up in the new-game settings.
- **Career locks and caps:** the career starts locked. Basic Rocketry opens
  1.25 m kerolox with a sea-level nozzle. General Rocketry adds the vacuum
  nozzle and raises the reliability cap to 50.
- **Starting a program:** the program window charges funds in career (20,347
  for 110 points) and is free in sandbox.
- **Program parts:** the program shows as its own part ("Kestrel Mk. 0") with
  the right price and description.
- **Hiding other engines:** other liquid engines are hidden; solid boosters
  stay.
- **Engine stats:** they reach the game, as the VAB readout shows (TWR, delta-v).
- **Launch checks:** a career launch passes KSP's pre-launch parts check.
- **Ignition:** the ignition check passed on launch.
- **Launch experience:** experience from a launch counts ("Harrier reached
  Mk. 1").
- **Recovery experience:** experience from a recovery counts ("Mk. 2"), and the
  stats improve as the rules say.

**Not tested yet**
- **Failures:** an actual failed ignition, and the light/off settings.
- **Recoveries:** StageRecovery boosters.
- **Retiring:** retiring a program.
- **Other models:** any fuel, size or nozzle besides 1.25 m kerolox with a
  sea-level nozzle.
- **Last fix:** the latest fix (program parts from another save hidden in
  sandbox).

**Gaps fixed and seen in game (second pass, 2026-11-03)**
- **Hover tooltip:** shows the program's mass and thrust at its current Mk
  (Titan Mk. 0: 33.033 t, 3,918.75 kN).
- **"More info" panel:** shows the program's thrust, Isp, failure chance,
  gimbal, propellant flow, dry mass and experience.
- **R&D screen:** stock liquid engines are hidden in R&D, and PIP's own
  unlock entries show in their nodes ("PIP: Liquid-Fuel Engines").
- **New-program window:** shows the "compared with" line against a familiar
  engine (for example the Reliant), read live from that engine, plus the
  one-line summary.
- **Retiring:** a retired program moves to "Retired" and leaves the VAB list.
- **Other saves:** a program from another save stays out of a sandbox's
  parts list.
- **Other models render:** 5 m (rescaled Cougar), 1.875 m hydrolox vacuum
  (Pavonis), 0.625 m aerospike (Dart), 3.75 m balanced (Etna). Not all 72 were
  looked at.

**Hiding other engines, tested in game**
- **Coverage:** 54 liquid rocket engines are hidden. This includes Near Future
  and CryoEngines engines, which file under the "Propulsion" category; that
  case was missed at first and is now fixed.
- **What stays:** the 47 engines still shown are all types PIP leaves alone:
  9 solids, 16 ion/Hall/plasma/MPD, 6 monopropellant, 6 jets including the
  RAPIER, and 10 nuclear.
- **Toggling:** switching the option off mid-game (Settings, then Difficulty
  Options, then PIP) brings them back in the VAB and R&D. Switching it on
  hides them again.

**Still open**
- **Failures:** an actual failed ignition, and the light/off settings.
- **StageRecovery boosters.**
