# PIP: Phil's Iterative Parts — Engine Spec (v1)

Status: **first draft built and flight-tested**, 2026-11-03 (see "First draft
status" at the end). This replaces the old "Iterative Development" design
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
- **Not built yet.** The build still counts once per program per launch, and
  recoveries are de-duplicated by the ship's ID, which KSP reuses when the same
  craft is relaunched, so a relaunch's recovery is lost. Both are fixed together:
  count per part, keyed by launch.

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

- **Size goes on the part, not the program, wherever the categories don't
  depend on size.** One battery program covers every battery size; you pick
  the size (and a tank's length) on the part in the VAB. Engines keep size in
  the program, because their thrust windows are set by size. This is the big
  call. It means far fewer programs to buy, and one PIP part replacing dozens
  of stock ones in the parts list. Windows below are given for each size, or
  as "× today's part of that size".
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
the first draft: a local backup.

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

## Looks and side-mount build (2026-11-04)

Built from sections 15 and 16 after Phil's "Yes, build it". This is Looks
step 1: batteries, reaction wheels, air intakes (with side-mount), RTGs, heat
shields and probe cores. Backup: a local backup.

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

Solar panels, antennas, fuel tanks and RCS (Phil: "Yes, step 2"). Backup:
a local backup.

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

Phil: "Yes, step 3". Backup: a local backup.

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

Phil: "Yes, flight test". zz PIP Sandbox only. Backup of the save:
a local backup.

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
Phil: "Yes, build it".
Backup of the code and DLL before it: a local backup.

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
