# Writes GameData\PIP\Fuels.cfg: the fuels liquid engine programs can burn, their
# tank recipes, boil-off rates and R&D entries (DESIGN.md section 19, FUELS.md).
# Run: powershell -ExecutionPolicy Bypass -File Tools\Generate-Fuels.ps1
#
# The real numbers (Isp, mixture ratio, densities) are from one table, so they
# compare fairly: Wikipedia's "Liquid rocket propellant" table, from Rocketdyne's
# calculations in Huzel & Huang (NASA SP-125), 68 atm chamber, shifting
# equilibrium, sea level = expansion to 1 atm, vacuum = 40:1 nozzle. Where that table
# has no figure, FUELS.md says how it was estimated. The game numbers are made from
# them by fixed rules (below), so a fuel can be retuned by changing its line here.

$sb = New-Object System.Text.StringBuilder
function L([string]$line = '') { [void]$sb.AppendLine($line) }
$inv = [Globalization.CultureInfo]::InvariantCulture
function N([double]$v, [int]$d = 4) { [Math]::Round($v, $d).ToString($inv) }
function G([double]$v) { $v.ToString('G6', $inv) }

# Resources: liters per unit (stock's are 5 L), kg per liter, and price per unit
# (stock's, or Community Resource Pack's). In KSP's own convention LiquidFuel stands
# for kerosene and Oxidizer for liquid oxygen.
$res = @{
    LiquidFuel = @(5, 1.0, 0.8); Oxidizer = @(5, 1.0, 0.18)
    LqdHydrogen = @(1, 0.07085, 0.03675); LqdMethane = @(1, 0.42561, 0.45); LqdFluorine = @(1, 1.505, 9.03); FLOX70 = @(1, 1.3993, 6.184906)
    Ethanol75 = @(1, 0.84175, 0.12624); UDMH = @(1, 0.791, 0.791); 'IRFNA-III' = @(1, 1.658, 0.34818); HTP = @(1, 1.431, 2.1465)
    Aerozine50 = @(1, 0.9, 1.35); NTO = @(1, 1.45, 0.2175); Syntin = @(1, 0.851, 0.4255); Hydrazine = @(1, 1.004, 2.008); ClF5 = @(1, 1.9, 15.2)
}
# Prices PIP never changes: stock's, and the hydrogen and methane CryoTanks priced.
$keepPrice = @('LiquidFuel', 'Oxidizer', 'LqdHydrogen', 'LqdMethane')

# Kerolox's Isp windows (Core/Fuels.cs): per nozzle, sea level at 0 and 100 points,
# vacuum at 0 and 100 points. Every other fuel's are these times its real Isp over
# kerolox's real Isp (sea level for sea level, vacuum for vacuum): Real Fuels
# Stockalike's rule, which matches the table almost exactly.
$keroloxIsp = [ordered]@{ S = @(220, 310, 245, 340); B = @(190, 285, 270, 360); V = @(50, 100, 300, 385); A = @(240, 305, 280, 350) }
$keroloxReal = @(299.9, 357.9)

# One line per fuel.
#  code, title, tech node ('' = with the first liquid engines), one-line note
#  fuel resource, oxidizer resource, O/F by mass
#  real Isp sea level / vacuum (s)
#  thrust (x kerolox's), engine thrust-to-weight at 100 weight points, build price (x kerolox's),
#  fail to light at 0 / 100 reliability points (mature)
#  tank kind (S standard, C cryogenic) and recipe name; R&D entry name and title
#  fill: what filling an FL-T400-sized tank costs (kerolox 184), where Community
#  Resource Pack's prices would make it 6 to 100 times kerolox's (Phil, 11/5: price
#  should vary with how good a fuel is, "but surely not to that extent")
$fuels = @(
    @{ code = 'E'; title = 'Ethalox'; tech = ''; note = 'Alcohol and liquid oxygen, as in the V-2 and Redstone: cheap and forgiving, but weak.'
       fuel = 'Ethanol75'; ox = 'Oxidizer'; of = 1.3; real = @(279.1, 333.2)
       thrust = 0.9; twr = 21; price = 0.7; fail = @(0.015, 0.0008); tank = 'S'; tankName = 'Ethalox'; tankTitle = 'Eth/Ox' }
    @{ code = 'N'; title = 'Nitric acid'; tech = 'generalRocketry'; note = 'Nitric acid and UDMH, as in the Agena and Scud: storable and self-igniting, so it rarely fails to light. Low Isp.'
       fuel = 'UDMH'; ox = 'IRFNA-III'; of = 3.0; real = @(272.1, 321.9)
       thrust = 1.05; twr = 23; price = 0.85; fail = @(0.008, 0.0003); tank = 'S'; tankName = 'UDMH/IRFNA'; tankTitle = 'UDMH/IRFNA'; fill = 220
       rd = @('PIP-NitricAcid', 'Nitric acid', 'Liquid engine programs can burn nitric acid and UDMH: storable, self-igniting, low Isp.') }
    @{ code = 'P'; title = 'Peroxide'; tech = 'fuelSystems'; note = 'High-test peroxide and kerosene, as in Black Arrow: dense, and a catalyst lights it reliably. A little below kerolox.'
       fuel = 'LiquidFuel'; ox = 'HTP'; of = 7.0; real = @(276, 329)
       thrust = 1.0; twr = 25; price = 0.9; fail = @(0.012, 0.0006); tank = 'S'; tankName = 'LF/HTP'; tankTitle = 'LF/HTP'; fill = 260
       rd = @('PIP-Peroxide', 'Peroxide', 'Liquid engine programs can burn high-test peroxide and kerosene: dense and reliable to light.') }
    @{ code = 'A'; title = 'Aerozine'; tech = 'advFuelSystems'; note = 'Aerozine-50 and NTO, as in Titan II and the Apollo service engine: storable, self-igniting, the surest to light. A little below kerolox.'
       fuel = 'Aerozine50'; ox = 'NTO'; of = 1.9; real = @(288.7, 341.5)
       thrust = 1.05; twr = 25; price = 1.1; fail = @(0.006, 0.0002); tank = 'S'; tankName = 'A50/NTO'; tankTitle = 'A-50/NTO'; fill = 370
       rd = @('PIP-Aerozine', 'Aerozine', 'Liquid engine programs can burn Aerozine-50 and NTO: storable, self-igniting, the surest to light.') }
    @{ code = 'S'; title = 'Syntin'; tech = 'largeVolumeContainment'; note = 'Synthetic kerosene and liquid oxygen, as in Soyuz-U2: a little more Isp and thrust than kerolox, for a price.'
       fuel = 'Syntin'; ox = 'Oxidizer'; of = 2.5; real = @(304.4, 363.3)
       thrust = 1.05; twr = 25.5; price = 1.4; fail = @(0.02, 0.001); tank = 'S'; tankName = 'Syntin/O'; tankTitle = 'Syntin/Ox'
       rd = @('PIP-Syntin', 'Syntin', 'Liquid engine programs can burn syntin: a little better than kerolox, for a price.') }
    @{ code = 'F'; title = 'FLOX'; tech = 'specializedFuelStorage'; note = 'Kerosene and FLOX-70 (fluorine in liquid oxygen), tested for Atlas: methalox-beating Isp from kerosene. Cryogenic, costly.'
       fuel = 'LiquidFuel'; ox = 'FLOX70'; of = 3.84; real = @(344.4, 403.3)
       thrust = 1.1; twr = 24; price = 1.6; fail = @(0.012, 0.0006); tank = 'C'; tankName = 'LF/FLOX'; tankTitle = 'LF/FLOX'; fill = 550
       rd = @('PIP-Flox', 'FLOX', 'Liquid engine programs can burn kerosene and FLOX-70: better Isp than methalox, cryogenic and costly.') }
    @{ code = 'C'; title = 'ClF5'; tech = 'exoticFuelStorage'; note = 'Hydrazine and chlorine pentafluoride: dense, storable, self-igniting and efficient. Never flown; the densest and most thrust per engine, at a price.'
       fuel = 'Hydrazine'; ox = 'ClF5'; of = 2.66; real = @(313.0, 365.1)
       thrust = 1.15; twr = 24; price = 1.8; fail = @(0.008, 0.0003); tank = 'S'; tankName = 'N2H4/ClF5'; tankTitle = 'N2H4/ClF5'; fill = 650
       rd = @('PIP-Clf5', 'ClF5', 'Liquid engine programs can burn hydrazine and ClF5: the densest and punchiest storable fuel, at a price.') }
    @{ code = 'L'; title = 'Fluorine'; tech = 'extremeFuelStorage'; note = 'Liquid fluorine and hydrogen: the highest Isp of any chemical rocket. Both boil off, and it is costly.'
       fuel = 'LqdHydrogen'; ox = 'LqdFluorine'; of = 8.0; real = @(411.6, 479.0)
       thrust = 0.95; twr = 20; price = 2.2; fail = @(0.012, 0.0006); tank = 'C'; tankName = 'LH2/LF2'; tankTitle = 'LH2/LF2'; fill = 740
       rd = @('PIP-Fluorine', 'Fluorine', 'Liquid engine programs can burn fluorine and hydrogen: the highest Isp of any chemical rocket.') }
)

# Tank recipes hold the engine's mix, so both run out together.
# Liters per unit of tank volume (an FL-T400 is 400): stock's 5, or CryoTanks' 6.66
# for a hydrogen mix (hydrogen is bulky). The empty tank weighs what its size says,
# whatever is in it, as stock's do (Phil, 11/5): stock's 0.000625 t per unit of
# volume, or CryoTanks' 0.000278904 for a hydrogen mix. So a dense fuel carries more
# propellant per ton of tank, which is why real rockets use dense fuels: ClF5 12
# times its tank's mass, kerolox 8, ethalox 7.4.
function TankRecipe($f) {
    $fu = $res[$f.fuel]; $ox = $res[$f.ox]
    $lf = 1 / $fu[1]; $lo = $f.of / $ox[1]                    # liters per kg of fuel
    $hydrogen = $f.fuel -eq 'LqdHydrogen' -or $f.ox -eq 'LqdHydrogen'
    $litres = if ($hydrogen) { 6.66 } else { 5 }
    $mix = (1 + $f.of) / ($lf + $lo)                           # kg per liter
    $tankMass = if ($hydrogen) { 0.000278904 } else { 0.000625 }
    $fuelUnits = $litres * $lf / ($lf + $lo) / $fu[0]; $oxUnits = $litres * $lo / ($lf + $lo) / $ox[0]
    @{ fuelUnits = $fuelUnits; oxUnits = $oxUnits; tankMass = $tankMass; mix = $mix
       ratio = $litres * $mix / 1000 / $tankMass                 # propellant : empty tank, by mass
       fillAtCrp = 400 * ($fuelUnits * $fu[2] + $oxUnits * $ox[2]) }
}

# The new prices: a fuel's own resources (not stock's, hydrogen or methane) all
# scaled by one factor so that filling an FL-T400-sized tank costs its "fill".
# Returns name -> new price per unit.
function Prices($f) {
    $t = TankRecipe $f
    $parts = @(@($f.fuel, $t.fuelUnits), @($f.ox, $t.oxUnits))
    $fixed = 0; $scaled = 0
    foreach ($p in $parts) { $c = 400 * $p[1] * $res[$p[0]][2]; if ($keepPrice -contains $p[0]) { $fixed += $c } else { $scaled += $c } }
    if (-not $f.fill -or $scaled -le 0) { return @{} }
    $k = ($f.fill - $fixed) / $scaled
    if ($k -le 0) { throw "$($f.code): can't fill for $($f.fill) with $($f.fuel)/$($f.ox)" }
    $out = @{}
    foreach ($p in $parts) { if ($keepPrice -notcontains $p[0]) { $out[$p[0]] = $res[$p[0]][2] * $k } }
    $out
}

L '// Generated by Tools\Generate-Fuels.ps1. Edit that script, not this file.'
L '// The fuels liquid engine programs can burn (DESIGN.md section 19, FUELS.md).'
L '//'
L '// Add a fuel of your own with a PIP_FUEL node in any .cfg: code (letters, no "-"),'
L '// title, tech (a tech node id, or empty for the first liquid engines), note,'
L '// PROPELLANT nodes as on an engine, ISP with four numbers per nozzle (sea level at'
L '// 0 and 100 points, vacuum at 0 and 100 points), and thrust, twr, price, failHi and'
L '// failLo. A TANK node puts a B9_TANK_TYPE on PIP''s tanks: kind S (standard) or C'
L '// (cryogenic, which boils off). A PLUME node names Waterfall templates for its'
L '// engines by role (lower, upper, core, generator), best first, and "match": words'
L '// in a template name that mean it is already this fuel''s. A fuel whose resources'
L '// aren''t installed is left out.'
L ''
L '// Boil-off: % of the resource lost per hour, and electric charge per second per'
L '// 1,000 units to keep it cold (CryoTanks'' numbers for hydrogen and methane; the'
L '// fluorine oxidizers boil like methane, needing about as much heat per liter).'
foreach ($b in @(@('LqdHydrogen', 0.05, 0.09), @('LqdMethane', 0.005, 0.045), @('LqdFluorine', 0.005, 0.045), @('FLOX70', 0.005, 0.045))) {
    L 'PIP_BOILOFF'
    L '{'
    L "    resource = $($b[0])"
    L "    rate = $(G $b[1])"
    L "    cooling = $(G $b[2])"
    L '}'
}
L ''

function Fuel($code, $title, $tech, $note, $props, $isp, $thrust, $twr, $price, $fail, $tank) {
    L 'PIP_FUEL'
    L '{'
    L "    code = $code"
    L "    title = $title"
    L "    tech = $tech"
    L "    note = $note"
    foreach ($p in $props) {
        L '    PROPELLANT'
        L '    {'
        L "        name = $($p[0])"
        L "        ratio = $($p[1])"
        L '    }'
    }
    L '    ISP'
    L '    {'
    foreach ($k in $isp.Keys) { L "        $k = $($isp[$k] -join ', ')" }
    L '    }'
    L "    thrust = $(G $thrust)"
    L "    twr = $(G $twr)"
    L "    price = $(G $price)"
    L "    failHi = $(G $fail[0])"
    L "    failLo = $(G $fail[1])"
    if ($tank) {
        L '    TANK'
        L '    {'
        L "        kind = $($tank[0])"
        L "        name = $($tank[1])"
        L "        title = $($tank[2])"
        L "        tankType = $($tank[3])"
        L '    }'
    }
    $p = $plumes[$code]
    if ($p) {
        L '    PLUME'
        L '    {'
        foreach ($k in 'match', 'lower', 'upper', 'core', 'generator') { if ($p[$k]) { L "        $k = $($p[$k] -join ', ')" } }
        L '    }'
    }
    L '}'
}

# Waterfall plume templates per fuel (DESIGN.md 19.4), best first; the first one
# installed is used. "waterfall-*" come with Waterfall itself; "stock-*" with Stock
# Waterfall Effects. Fuels with no template of their own borrow the nearest look:
# syntin and FLOX kerolox's, fluorine hydrolox's, ClF5 the hypergolics'.
$kerolox = @{ match = @('kerolox'); lower = @('waterfall-kerolox-lower-1'); upper = @('waterfall-kerolox-upper-1'); core = @('stock-kerolox-core'); generator = @('stock-kerolox-generator', 'waterfall-kerolox-exhaust-1') }
$hydrolox = @{ match = @('hydrolox'); lower = @('waterfall-hydrolox-lower-1'); upper = @('waterfall-hydrolox-upper-1'); core = @('stock-hydrolox-core-1'); generator = @('stock-hydrolox-generator') }
$plumes = @{
    K = $kerolox; S = $kerolox; F = $kerolox
    H = $hydrolox; L = $hydrolox
    E = @{ match = @('alcolox', 'ethalox'); lower = @('waterfall-alcolox-lower-1'); upper = @('waterfall-ethalox-sustainer-1', 'waterfall-alcolox-lower-2') }
    N = @{ match = @('nitric', 'udmh'); lower = @('waterfall-hypergolic-UDMH-NTO-lower-1'); upper = @('waterfall-hypergolic-IRFNA-UDMH-upper-1'); core = @('stock-hypergolic-core') }
    P = @{ match = @('keroxide'); lower = @('waterfall-keroxide-lower-1'); upper = @('waterfall-keroxide-lower-1') }
    A = @{ match = @('aerozine'); lower = @('waterfall-hypergolic-aerozine50-lower-1', 'stock-aerozine50-lower-1'); upper = @('waterfall-hypergolic-aerozine50-upper-1', 'stock-aerozine50-upper-1'); core = @('stock-hypergolic-core') }
    M = @{ match = @('methalox'); lower = @('waterfall-methalox-lower-BE4-1', 'stock-methalox-lower-1'); upper = @('waterfall-methalox-upper-1', 'stock-methalox-upper-2'); core = @('stock-methalox-core') }
    C = @{ match = @('hypergolic'); lower = @('waterfall-hypergolic-UDMH-NTO-lower-1'); upper = @('waterfall-hypergolic-white-upper-1'); core = @('stock-hypergolic-core') }
}

# Kerolox, hydrolox and methalox: PIP's own numbers (Core/Fuels.cs). Their tanks are
# in Chassis/Parts.cfg.
$own = @{
    K = { Fuel 'K' 'Kerolox' '' 'Kerosene and liquid oxygen: the workhorse.' @(@('LiquidFuel', '0.9'), @('Oxidizer', '1.1')) $keroloxIsp 1 24.5 1 @(0.02, 0.001) $null }
    H = { Fuel 'H' 'Hydrolox' 'advFuelSystems' 'Liquid hydrogen and oxygen: the best Isp of the common fuels, bulky tanks, and hydrogen boils off.' @(@('LqdHydrogen', '1.5'), @('Oxidizer', '0.1')) ([ordered]@{ S = @(300, 380, 340, 425); B = @(270, 355, 370, 450); V = @(60, 120, 400, 475); A = @(310, 370, 360, 440) }) 0.9 22 1.3 @(0.02, 0.001) $null }
    M = { Fuel 'M' 'Methalox' 'highPerformanceFuelSystems' 'Liquid methane and oxygen: the best thrust, weight and reliability.' @(@('LqdMethane', '3'), @('Oxidizer', '1')) ([ordered]@{ S = @(280, 340, 300, 362); B = @(255, 320, 320, 378); V = @(50, 110, 340, 395); A = @(290, 332, 315, 370) }) 1.15 28 1.2 @(0.015, 0.0005) $null }
}

# The order programs list them in: roughly the order the tech tree reaches them.
$order = 'K', 'E', 'N', 'P', 'A', 'H', 'S', 'M', 'F', 'C', 'L'
$tankTypes = @()
foreach ($code in $order) {
    if ($own.ContainsKey($code)) { & $own[$code]; continue }
    $f = $fuels | Where-Object { $_.code -eq $code }
    $rs = $f.real[0] / $keroloxReal[0]; $rv = $f.real[1] / $keroloxReal[1]
    $isp = [ordered]@{}
    foreach ($k in $keroloxIsp.Keys) { $w = $keroloxIsp[$k]; $isp[$k] = @([Math]::Round($w[0] * $rs), [Math]::Round($w[1] * $rs), [Math]::Round($w[2] * $rv), [Math]::Round($w[3] * $rv)) }
    $t = TankRecipe $f
    # Engine ratios are the tank's units, so a full tank empties evenly.
    $props = @(@($f.fuel, (N $t.fuelUnits)), @($f.ox, (N $t.oxUnits)))
    $typeName = "PIP-$($f.code)-MIX"
    Fuel $f.code $f.title $f.tech $f.note $props $isp $f.thrust $f.twr $f.price $f.fail @($f.tank, $f.tankName, $f.tankTitle, $typeName)
    $tankTypes += , @($typeName, $t, $f)
}
L ''

L '// The fuels'' tank recipes, per unit of tank volume.'
foreach ($tt in $tankTypes) {
    $t = $tt[1]; $f = $tt[2]
    L 'B9_TANK_TYPE'
    L '{'
    L "    name = $($tt[0])"
    L "    tankMass = $(G $t.tankMass)"
    L '    tankCost = 0'
    L '    RESOURCE'
    L '    {'
    L "        name = $($f.fuel)"
    L "        unitsPerVolume = $(N $t.fuelUnits)"
    L '    }'
    L '    RESOURCE'
    L '    {'
    L "        name = $($f.ox)"
    L "        unitsPerVolume = $(N $t.oxUnits)"
    L '    }'
    L '}'
}
L ''

L '// Prices. Community Resource Pack''s are near real, which makes filling a tank with'
L '// the exotic fuels 6 to 100 times dearer than kerolox (ClF5: 19,432 for an'
L '// FL-T400). PIP sets these resources'' prices so a fuel costs what its'
L '// performance is worth (FUELS.md), everywhere in the game. Real Fuels keeps its own.'
$priced = @{}
foreach ($f in $fuels) {
    foreach ($kv in (Prices $f).GetEnumerator()) {
        if ($priced.ContainsKey($kv.Key)) { throw "$($kv.Key) is priced by two fuels" }
        $priced[$kv.Key] = $kv.Value
        L "@RESOURCE_DEFINITION[$($kv.Key)]:NEEDS[!RealFuels]:FINAL"
        L '{'
        L "    @unitCost = $($kv.Value.ToString('0.#####', $inv))"
        L '}'
    }
}
L ''

L '// R&D shows each fuel at its tech node.'
foreach ($f in $fuels) {
    if (-not $f.rd) { continue }
    L 'PARTUPGRADE'
    L '{'
    L "    name = $($f.rd[0])"
    L "    partIcon = PIP-$($f.code)-125-S"
    L "    techRequired = $($f.tech)"
    L '    entryCost = 0'
    L '    cost = 0'
    L "    title = PIP: $($f.rd[1]) engines"
    L "    basicInfo = $($f.rd[1])"
    L "    manufacturer = Phil's Iterative Parts"
    L "    description = $($f.rd[2])"
    L '}'
}

$out = Join-Path $PSScriptRoot '..\GameData\PIP\Fuels.cfg'
[IO.File]::WriteAllText($out, $sb.ToString().Replace("`r`n", "`n"), (New-Object Text.UTF8Encoding $false))
Write-Host "Wrote $($fuels.Count + 3) fuels to $out"

# The table for FUELS.md: game numbers at 100 points.
Write-Host ''
Write-Host '| Fuel | Tech | Isp SL/vac, sea-level nozzle | vacuum nozzle | mix kg/L | thrust | TWR | price | fails to light |'
foreach ($f in $fuels) {
    $rs = $f.real[0] / $keroloxReal[0]; $rv = $f.real[1] / $keroloxReal[1]; $t = TankRecipe $f
    Write-Host "| $($f.title) | $($f.tech) | $([Math]::Round(310 * $rs))/$([Math]::Round(340 * $rv)) | $([Math]::Round(385 * $rv)) | $(N $t.mix 2) | $($f.thrust) | $($f.twr) | $($f.price) | $($f.fail[0] * 100)% to $($f.fail[1] * 100)% |"
}

# Tanks and prices, for FUELS.md: propellant per ton of tank, and filling an FL-T400.
Write-Host ''
Write-Host '| Fuel | propellant : tank | fill at CRP prices | fill at PIP prices | new prices |'
foreach ($f in $fuels) {
    $t = TankRecipe $f; $p = Prices $f
    $now = if ($f.fill) { $f.fill } else { [Math]::Round($t.fillAtCrp) }
    Write-Host "| $($f.title) | $(N $t.ratio 1) | $([Math]::Round($t.fillAtCrp)) | $now | $(($p.GetEnumerator() | ForEach-Object { "$($_.Key) $($_.Value.ToString('0.###', $inv))" }) -join ', ') |"
}
