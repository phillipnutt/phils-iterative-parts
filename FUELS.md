# Fuels

Liquid engine programs pick one of these fuels. The game numbers below are what
`GameData/PIP/Fuels.cfg` holds. `Tools/Generate-Fuels.ps1` writes that file from
the real data further down, by the rules in "How the game numbers are made", so a
fuel can be retuned or added by editing one line there.

Written by Claude (AI) from published sources, at Phil Nutt's direction. The real
data was gathered and cross-checked by an AI research pass; check the sources
before relying on it for anything but a game.

## In the game

Isp is at 100 Efficiency points, Mk. 5 (sea level / vacuum for the sea-level
nozzle; vacuum for the vacuum nozzle). Thrust, engine price and thrust-to-weight
are against kerolox's. "Fails to light" is a mature engine at 0 and 100
Reliability points. "A full tank" is the fuel cost of a full FL-T400-sized tank
(400 volume) at Community Resource Pack's prices.

| Code | Fuel | Propellants | Unlocks at | Isp SL/vac | Vac. nozzle | Thrust | TWR | Engine price | Fails to light | Mix kg/L | A full tank | Tank |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| K | Kerolox | LiquidFuel + Oxidizer | first liquid engines | 310/340 | 385 | 1 | 24.5 | 1 | 2% → 0.1% | 1.00 | 184 | standard |
| E | Ethalox | Ethanol75 + Oxidizer | first liquid engines | 288/317 | 358 | 0.9 | 21 | 0.7 | 1.5% → 0.08% | 0.92 | 158 | standard |
| N | Nitric acid | UDMH + IRFNA-III | General Rocketry | 281/306 | 346 | 1.05 | 23 | 0.85 | 0.8% → 0.03% | 1.30 | 1,061 | standard |
| P | Peroxide | LiquidFuel + HTP | Fuel Systems | 285/313 | 354 | 1 | 25 | 0.9 | 1.2% → 0.06% | 1.36 | 3,619 | standard |
| A | Aerozine | Aerozine50 + NTO | Adv. Fuel Systems | 298/324 | 367 | 1.05 | 25 | 1.1 | 0.6% → 0.02% | 1.20 | 1,474 | standard |
| H | Hydrolox | LqdHydrogen + Oxidizer | Adv. Fuel Systems | 380/425 | 475 | 0.9 | 22 | 1.3 | 2% → 0.1% | 0.30 | 97 | cryogenic |
| S | Syntin | Syntin + Oxidizer | Large Volume Containment | 315/345 | 391 | 1.05 | 25.5 | 1.4 | 2% → 0.1% | 0.95 | 321 | standard |
| M | Methalox | LqdMethane + Oxidizer | High-Performance Fuel Systems | 340/362 | 395 | 1.15 | 28 | 1.2 | 1.5% → 0.05% | 0.78 | 383 | cryogenic |
| F | FLOX | LiquidFuel + FLOX70 | Specialized Fuel Storage | 356/383 | 434 | 1.1 | 24 | 1.6 | 1.2% → 0.06% | 1.29 | 9,152 | cryogenic |
| C | ClF5 | Hydrazine + ClF5 | Exotic Fuel Storage | 324/347 | 393 | 1.15 | 24 | 1.8 | 0.8% → 0.03% | 1.53 | 19,432 | standard |
| L | Fluorine | LqdHydrogen + LqdFluorine | Extreme Fuel Storage | 425/455 | 515 | 0.95 | 20 | 2.2 | 1.2% → 0.06% | 0.46 | 6,652 | cryogenic |

Kerolox, hydrolox and methalox keep the numbers PIP had before the table; the
other eight are made from the real data. In KSP's own convention, `LiquidFuel` is
kerosene and `Oxidizer` is liquid oxygen, so the fuels that burn those use them.

**Things worth knowing before you pick:**
- The self-igniting fuels (nitric acid, Aerozine, ClF5) are the surest to light.
- Dense fuels (nitric acid, peroxide, Aerozine, ClF5) put more propellant in the
  same tank: a smaller stage for the same mass.
- Hydrogen fuels are bulky: a hydrolox tank holds 40% of the mass a kerolox tank of the same size does.
- **Propellant prices are Community Resource Pack's**, which are close to real
  prices per liter. Kerolox, ethalox, syntin and the cryogenic stock-style fuels
  are cheap to fill. Peroxide, FLOX, ClF5 and fluorine cost 20 to 100 times as
  much. That's realistic, but it may want tuning for KSP's economy.

### Boil-off

Fuels in **cryogenic** tanks boil off unless the tank's cooling is on and the
vessel has electric charge to run it. Insulation points cut both the loss and the
power.

| Resource | Lost per hour | EC/s per 1,000 units to cool |
|---|---|---|
| LqdHydrogen | 0.05% | 0.09 |
| LqdMethane | 0.005% | 0.045 |
| LqdFluorine | 0.005% | 0.045 |
| FLOX70 | 0.005% | 0.045 |

Hydrogen and methane are CryoTanks' rates. Fluorine and FLOX take methane's: per
liter, they need about as much heat to boil (fluorine about 259 MJ/m³, liquid
oxygen 243, methane 217, hydrogen 32), which is why hydrogen is the hard one.
`Oxidizer` doesn't boil off, as in stock KSP and CryoTanks. The rates are before
Insulation: at 0 points a tank loses twice them, at 100 points a fifth.

### Plumes

With Waterfall installed, a liquid engine shows its fuel's plume, whatever engine
model it wears. PIP names Waterfall's templates; it ships none.

| Fuel | Sea-level plume | Vacuum plume | Bell glow |
|---|---|---|---|
| Kerolox, syntin, FLOX | waterfall-kerolox-lower-1 | waterfall-kerolox-upper-1 | stock-kerolox-core |
| Ethalox | waterfall-alcolox-lower-1 | waterfall-ethalox-sustainer-1 | none |
| Nitric acid | waterfall-hypergolic-UDMH-NTO-lower-1 | waterfall-hypergolic-IRFNA-UDMH-upper-1 | stock-hypergolic-core |
| Peroxide | waterfall-keroxide-lower-1 | waterfall-keroxide-lower-1 | none |
| Aerozine | waterfall-hypergolic-aerozine50-lower-1 | waterfall-hypergolic-aerozine50-upper-1 | stock-hypergolic-core |
| Hydrolox, fluorine | waterfall-hydrolox-lower-1 | waterfall-hydrolox-upper-1 | stock-hydrolox-core-1 |
| Methalox | waterfall-methalox-lower-BE4-1 | waterfall-methalox-upper-1 | stock-methalox-core |
| ClF5 | waterfall-hypergolic-UDMH-NTO-lower-1 | waterfall-hypergolic-white-upper-1 | stock-hypergolic-core |

Syntin, FLOX, fluorine and ClF5 have no templates of their own and borrow the
nearest. A swapped template keeps its place on the engine and is rescaled by how
big each template is drawn per meter of engine, measured from the installed mods
that use it (DESIGN.md 19.4).

## How the game numbers are made

- **Isp:** kerolox's windows (per nozzle, sea level and vacuum at 0 and 100
  points) times the fuel's real Isp over kerolox's real Isp, sea level for sea
  level and vacuum for vacuum. This is Real Fuels Stockalike's rule. Both real
  numbers come from the same table, so the ratio is fair.
- **Engine ratios and tanks:** the real mixture ratio by mass, turned into units
  with each resource's density (stock's `LiquidFuel` and `Oxidizer` are 5 L units
  at 1 kg/L; Community Resource Pack's are 1 L units at their real density). The
  tank recipe holds the same mix, so both run out together.
- **Tank size and weight:** 5 liters per unit of tank volume (stock's), or 6.66
  for a hydrogen mix (CryoTanks'). The empty tank weighs the propellant's mass
  divided by 8 (an FL-T400 holds 8 times its own mass), 7.5 for a cryogenic mix,
  and 7.24 for a hydrogen mix (insulation weighs).
- **Thrust, weight, price, failures and tech node:** game choices, not physics.
  Denser fuels get a little more thrust; self-igniting ones fail to light less;
  exotic ones cost more and come late in the tree.

## Adding a fuel

Any `.cfg` file can add a fuel; PIP reads `PIP_FUEL` nodes when the game loads.
A fuel whose resources aren't installed is left out. PIP makes its engines and
looks, and adds its tank recipe to PIP's tanks.

```
PIP_FUEL
{
    code = X                 // letters only, no "-"; never change it once used (saves keep it)
    title = Example
    tech = advFuelSystems    // a tech node id; empty = with the first liquid engines
    note = One line for the new-program window.
    PROPELLANT { name = SomeFuel   ratio = 1 }
    PROPELLANT { name = SomeOxidizer   ratio = 1.2 }
    ISP                      // per nozzle: sea level at 0 and 100 points, vacuum at 0 and 100
    {
        S = 220, 310, 245, 340
        B = 190, 285, 270, 360
        V = 50, 100, 300, 385
        A = 240, 305, 280, 350
    }
    thrust = 1               // x kerolox's
    twr = 24.5               // engine thrust-to-weight at 100 weight points
    price = 1                // engine price, x kerolox's
    failHi = 0.02            // fails to light at 0 reliability points (mature)
    failLo = 0.001           // ... and at 100
    TANK
    {
        kind = S             // S standard, or C cryogenic (boils off)
        name = Example       // saved in craft files: never change it once used
        title = Ex/Ox
        tankType = MY-EXAMPLE-MIX   // a B9_TANK_TYPE you define
    }
}
PIP_BOILOFF { resource = SomeOxidizer   rate = 0.005   cooling = 0.045 }   // only if it boils
```

## The real data

One table for every row, so the numbers compare fairly: Wikipedia's bipropellant
table [W], which credits Rocketdyne's calculations in Huzel & Huang (NASA SP-125).
It assumes a 68 atm (1,000 psia) chamber and shifting equilibrium. Sea level is
expansion to 1 atm; vacuum is a 40:1 nozzle. Braeunig [B] uses the same chamber
pressure but gives sea level only; his figures run about 3% lower. "est." means
derived, as described under "Gaps".

| # | Fuel / oxidizer | O/F by mass (SL / vac) | Density kg/L (fuel / ox / mix) | Isp SL / vac (s) | Braeunig SL @ O/F | Notes | Era and engines | In PIP |
|---|---|---|---|---|---|---|---|---|
| 1 | Kerosene / LOX | 2.58 / 2.77 | 0.82 / 1.14 / 1.03 | 300 / 358 | 289 @ 2.29 | cryogenic oxidizer (−183 °C) | 1957+: YLR89, F-1, RD-180, Merlin | K |
| 2 | LH2 / LOX | 4.13 / 4.83 (engines run 5–6) | 0.071 / 1.14 / 0.29–0.32 | 389 / 455 | 381 @ 5.0 | both cryogenic (LH2 −252.9 °C) | 1962+: RL-10, J-2, SSME | H |
| 3 | Methane / LOX | 3.21 / 3.45 | 0.423 / 1.14 / 0.82 | 309 / 369 | 299 @ 2.77 | cryogenic (−161.6 °C), non-toxic | 2020s: Raptor, BE-4 | M |
| 4 | Aerozine 50 / NTO | 1.98 / 2.15 | 0.903 / 1.45 / 1.12–1.20 | 289 / 342 | 280 @ 1.59 | hypergolic, storable, toxic | 1962+: LR87, LR91, AJ10 (Apollo SPS) | A |
| 5 | MMH / NTO | 2.17 / 2.37 | 0.866 / 1.45 / 1.19 | 288 / 341 | 280 @ 1.73 | hypergolic, storable, toxic | 1964+: Gemini, R-4D, Shuttle OMS | — |
| 6 | UDMH / NTO | 2.10 [B] / 2.61 [A6] | 0.791 / 1.45 / 1.14–1.18 | est. 286 / 338 | 277 @ 2.10 | hypergolic, storable, toxic | late 1950s+: RD-253, YF-20A | — |
| 7 | Hydrazine / NTO | 1.36 / 1.42 | 1.004 / 1.45 / 1.21 | 292 / 344 | 286 @ 1.08 | hypergolic; hydrazine freezes at 1.4 °C | no production engines | — |
| 8 | UDMH / IRFNA-III | 3.13 / 3.31 | 0.791 / 1.55 / 1.26 | 272 / 322 | 266 @ 2.60 | hypergolic, storable, corrosive | 1954+: Agena, Able, Kosmos-3M | N |
| 9 | Ethanol 75% / LOX | 1.29 [B] / 1.43 [A9] | 0.87 / 1.14 / 0.98–1.01 | est. 279 / 333 | 269 @ 1.29 | cryogenic oxidizer | 1940s: V-2, RD-100, Redstone | E |
| 10 | Kerosene / HTP | 7.84 (85%) / 7.07 (98%) | 0.82 / 1.44 / 1.26–1.31 | est. 268 / 320 (85%); 276 / est. 329 (98%) | 258 @ 7.84 | storable, not hypergolic | 1950s–60s: Gamma 8 (Black Arrow), Stentor | P (98%) |
| 11 | Syntin / LOX | no source; ≈ kerosene | 0.851 / 1.14 / ~1.04 | est. 304 / 363 (+1–2%) | — | cryogenic oxidizer | 1982–95: Soyuz-U2, RD-58S | S |
| 12 | Hydyne / LOX | 1.73 | 0.86 / 1.14 / 1.02 | 306 / 359 (conditions unstated) | — | toxic fuel | 1957–59: Jupiter-C, Juno I | — |
| 13 | LH2 / fluorine | 7.94 / 9.74 | 0.071 / 1.50 / 0.46–0.52 | 412 / 479 | 400 @ 6.0 | cryogenic, hypergolic, extremely toxic | 1960s tests: RD-350 | L |
| 14 | Kerosene / FLOX-70 | 3.84 / 3.84 | 0.82 / 1.40 / 1.20 | 344 / 403 | 320 @ 3.80 | cryogenic, hypergolic, toxic | 1950s–60s Atlas tests, never flew | F |
| 15 | Hydrazine / LOX | 0.92 / 0.98 | 1.004 / 1.14 / 1.07 | 313 / 353 | 303 @ 0.74 | toxic fuel | 1950s tests | — |
| 16 | Hydrazine / ClF5 | 2.66 / 2.71 | 1.004 / 1.9 / 1.47 | 313 / 365 | 297 @ 2.12 | hypergolic; ClF5 boils at −13 °C; toxic | never flown | C |

MMH/NTO, UDMH/NTO and hydrazine/NTO are within a few seconds of Aerozine/NTO, and
hydyne and hydrazine/LOX within a few of kerolox, so PIP doesn't list them
separately; each would add a full set of engine parts for almost no difference.

More rows from [W], for anyone adding fuels:

| Fuel / oxidizer | O/F (SL / vac) | Mix kg/L | Isp SL / vac (s) |
|---|---|---|---|
| Ethane / LOX | 2.89 / 3.10 | 0.90 | 307 / 366 |
| Diborane / LOX | 1.96 / 2.06 | 0.74 | 342 / 410 |
| Pentaborane / LOX | 2.12 / 2.16 | 0.92 | 319 / 383 |
| Ammonia / fluorine | 3.32 / 3.35 | 1.12 | 360 / 423 |
| LH2 / OF2 | 5.92 / 7.37 | 0.44 | 409 / 477 |
| Hydrazine / N2F4 | 3.22 / 3.25 | 1.38 | 335 / 390 |
| MMH / MON-25 | 2.28 / 2.50 | 1.18 | 290 / 343 |
| UDMH / IRFNA-IV | 2.95 / 3.12 | 1.29 | 277 / 328 |
| Hydyne / IRFNA-III | 3.26 / 3.41 | 1.31 | 269 / 319 |
| Tonka-250 / AK-27 | — | — | 216–248 SL [WT] |

**How other mods scale Isp,** for comparison:
- Real Fuels Stockalike multiplies each engine's kerolox Isp: hydrolox 1.30 at sea
  level and 1.27 in vacuum, methalox 1.03, Aerozine/NTO and MMH/NTO 0.96/0.95,
  UDMH/NTO 0.95, ethalox 0.93 [RFSA]. These match [W]'s ratios almost exactly,
  which is why PIP uses the same rule.
- Chemical Propulsion gives each resource a multiplier and combines them by
  volume [CP][IG]. It is more generous than reality (methalox 1.2 against a real
  1.03).

### Gaps and disagreements

1. Braeunig's sea-level figures are 3–7% below [W] at the same pressure. He models
   kerosene as dodecane and runs hydrogen mixes leaner on purpose. PIP uses [W]
   throughout.
2. [W] has no vacuum figure for UDMH/NTO, ethanol, HTP, syntin or hydyne. Those
   are Braeunig's sea level, scaled by [W]/Braeunig for the nearest pair, then by
   that pair's vacuum/sea-level ratio. Check: UDMH/NTO comes out at 286 s against
   Astronautix's 285.
3. Mixture ratios disagree between sources for UDMH/NTO, nitric acid/UDMH,
   hydrazine/LOX, fluorine/hydrogen and ethanol. PIP uses ratios engines
   actually ran at: ethalox 1.3, nitric acid 3.0, peroxide 7.0, Aerozine 1.9,
   syntin 2.5, FLOX 3.84, ClF5 2.66, fluorine 8.
4. Syntin has no published theoretical figures, only "1–2% better than
   kerosene". Hydyne's are from Astronautix alone.
5. Hydrogen peroxide's strength varies by source (85%, 98%, 100%). Community
   Resource Pack's HTP density (1.431) is 98%, so PIP uses the 98% figures.
6. Community Resource Pack lists IRFNA-IV at 1.995 kg/L, well above every other
   nitric acid figure (1.51–1.66). PIP uses IRFNA-III (1.658).

### Sources

- [W] https://en.wikipedia.org/wiki/Liquid_rocket_propellant
- [B] http://www.braeunig.us/space/propel.htm
- [A1]–[A15] Astronautix propellant pages, e.g. https://astronautix.com/l/loxkerosene.html,
  https://astronautix.com/n/n2o4aerozine-50.html, https://astronautix.com/l/lf2lh2.html,
  https://astronautix.com/h/h2o2kerosene.html, https://astronautix.com/l/loxsintin.html
- [WS] https://en.wikipedia.org/wiki/Syntin
- [WH] https://en.wikipedia.org/wiki/Hydyne
- [WP] https://en.wikipedia.org/wiki/High-test_peroxide
- [WT] https://en.wikipedia.org/wiki/Tonka_(fuel)
- [CRP] Community Resource Pack's `CommonResources.cfg` (densities, prices)
- [CT] CryoTanks' tank types and boil-off rates
- [RFSA] Real Fuels Stockalike, `Stockalike_Squad.cfg`
- [CP] Chemical Propulsion's propellant configs; [IG] Ignition's `PropellantCombinationConfig.cs`
- [YA] https://yarchive.net/space/spacecraft/hydrogen_storage.html (LH2 and LOX boil-off in orbit, 1982 NASA study)
