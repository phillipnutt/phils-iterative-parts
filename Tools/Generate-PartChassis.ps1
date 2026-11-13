# Writes GameData\PIP\Chassis\Parts.cfg: the hidden chassis parts for PIP's
# non-engine part types (DESIGN.md section 14). Where the size is picked on the
# part, one chassis carries a B9PartSwitch "pipSize" module: each subtype scales
# the model, moves the attach nodes, and tells ModulePipPart which size it is.
# Run: powershell -ExecutionPolicy Bypass -File Tools\Generate-PartChassis.ps1

$sb = New-Object System.Text.StringBuilder
function L([string]$line = '') { [void]$sb.AppendLine($line) }
$inv = [Globalization.CultureInfo]::InvariantCulture
function N([double]$v) { [Math]::Round($v, 6).ToString($inv) }

# B9 "upgradeRequired" names for sizes that need research (see Generate-Upgrades.ps1).
$tooling = @{ heavyRocketry = 'PIP-1875mTooling'; heavierRocketry = 'PIP-25mTooling'; veryHeavyRocketry = 'PIP-375mTooling'; giganticRocketry = 'PIP-5mTooling' }
$diameter = @{ '0625' = 0.625; '125' = 1.25; '1875' = 1.875; '25' = 2.5; '375' = 3.75; '5' = 5 }
$nodeSize = @{ '0625' = 0; '125' = 1; '1875' = 1; '25' = 2; '375' = 3; '5' = 4 }
$sizeLabel = @{ '0625' = '0.625 m'; '125' = '1.25 m'; '1875' = '1.875 m'; '25' = '2.5 m'; '375' = '3.75 m'; '5' = '5 m' }

function Begin($name, $title, $base, [double]$cost, [string[]]$strip) {
    $script:chassisBase = $base
    L "+PART[$base]:FINAL"
    L '{'
    L "    @name = $name"
    L "    @title = PIP chassis: $title"
    L "    %manufacturer = Phil's Iterative Parts"
    L '    %category = none'
    L '    %TechHidden = True'
    L '    %TechRequired = start'
    L '    %entryCost = 0'
    L "    %cost = $([Math]::Round($cost))"
    foreach ($m in @('TweakScale', 'ModuleTestSubject', 'Reliability') + $strip) { L "    !MODULE[$m],* {}" }
    # Paint variants stay, paint only, as on looks (DESIGN.md 29.1): some models (ReStock's)
    # hold overlapping meshes that only a variant switches off. No node moves, mass or cost.
    L '    @MODULE[ModulePartVariants],*'
    L '    {'
    L '        @VARIANT,*'
    L '        {'
    L '            !NODES,* {}'
    L '            !mass = delete'
    L '            !cost = delete'
    L '        }'
    L '    }'
    # Name every model so B9PartSwitch can scale it (it can't see the part's own "model" root).
    L '    @MODEL,*'
    L '    {'
    L '        %name = PIPModel'
    L '    }'
}

function PipModule($typeId, $model) {
    L '    MODULE'
    L '    {'
    L '        name = ModulePipPart'
    L "        typeId = $typeId"
    L "        model = $model"
    # The part this chassis copies: its standard look (KSP names turn '_' into '.').
    L "        chassisSource = $($script:chassisBase.Replace('_', '.'))"
    L '    }'
}

# $subtypes: list of hashtables { code; title; scale (x/z); scaleY; upgrade; nodes = @{ id = y }; volume; default }
function SizeSwitch($subtypes, [string]$description = 'Size', [string]$parent = $null) {
    L '    MODULE'
    L '    {'
    L '        name = ModuleB9PartSwitch'
    L '        moduleID = pipSize'
    L "        switcherDescription = $description"
    if ($parent) { L "        parentID = $parent" }
    foreach ($s in $subtypes) {
        $sy = $(if ($s.scaleY) { $s.scaleY } else { $s.scale })
        L '        SUBTYPE'
        L '        {'
        L "            name = $($s.code)"
        L "            title = $($s.title)"
        if ($s.default) { L '            defaultSubtypePriority = 1' }
        if ($s.upgrade) { L "            upgradeRequired = $($s.upgrade)" }
        if ($s.volume) { L "            volumeAddedToParent = $(N $s.volume)" }
        L '            TRANSFORM'
        L '            {'
        L '                name = PIPModel'
        L "                scaleOffset = $(N $s.scale), $(N $sy), $(N $s.scale)"
        L '            }'
        if ($s.nodes) {
            foreach ($id in $s.nodes.Keys) {
                L '            NODE'
                L '            {'
                L "                name = $id"
                L "                position = 0, $(N ($s.nodes[$id] * $sy)), 0"
                if ($null -ne $s.nodeSize) { L "                size = $($s.nodeSize)" }
                L '            }'
            }
        }
        L '            MODULE'
        L '            {'
        L '                IDENTIFIER'
        L '                {'
        L '                    name = ModulePipPart'
        L '                }'
        L '                DATA'
        L '                {'
        L "                    variant = $($s.code)"
        L '                }'
        L '            }'
        L '        }'
    }
    L '    }'
}

# Stack sizes scaled from a base model of diameter $baseD.
function Sizes([string[]]$codes, [double]$baseD, $nodes, [string]$default = '125', $extraUpgrade = @{}) {
    foreach ($c in $codes) {
        $s = $diameter[$c] / $baseD
        $n = @{}; foreach ($k in $nodes.Keys) { $n[$k] = $nodes[$k] }
        $up = $(if ($extraUpgrade.ContainsKey($c)) { $extraUpgrade[$c] } else { $null })
        if (-not $up) { foreach ($tech in $tooling.Keys) { if (($c -eq '1875' -and $tech -eq 'heavyRocketry') -or ($c -eq '25' -and $tech -eq 'heavierRocketry') -or ($c -eq '375' -and $tech -eq 'veryHeavyRocketry') -or ($c -eq '5' -and $tech -eq 'giganticRocketry')) { $up = $tooling[$tech] } } }
        @{ code = $c; title = $sizeLabel[$c]; scale = $s; nodes = $n; nodeSize = $nodeSize[$c]; upgrade = $up; default = ($c -eq $default) }
    }
}

L '// Generated by Tools\Generate-PartChassis.ps1. Do not edit by hand.'
L '// Hidden chassis parts for Phil''s Iterative Parts (part types that are not engines).'
L ''

# --- RCS: propellant x layout (layout is a program choice: thruster transforms differ by model)
$rcs = @(
    @('M-1', 'linearRcs', 'single monopropellant', $false), @('M-4', 'RCSBlock_v2', '4-way monopropellant', $false), @('M-5', 'rcsblock-orbital-5way-1', '5-way monopropellant', $false),
    @('B-1', 'nflv-rcs-heavy-1x-2', 'single bipropellant', $true), @('B-4', 'nflv-rcs-heavy-4x-2', '4-way bipropellant', $true), @('B-5', 'rcsblock-orbital-5way-1', '5-way bipropellant', $true)
)
# Stock stand-ins for the Near Future blocks (DESIGN.md section 17): the Vernor and
# the RV-105 burning LF/Ox. Five-nozzle blocks have no stock stand-in: a padlock.
$rcsStandIns = @{ 'B-1' = 'vernierEngine'; 'B-4' = 'RCSBlock_v2' }
$rcsNeeds = @{ 'M-5' = 'Near Future Spacecraft'; 'B-5' = 'Near Future Spacecraft' }
$rcsAll = @()
foreach ($r in $rcs) {
    $rcsAll += , $r
    if ($rcsStandIns.ContainsKey($r[0])) { $rcsAll += , @("$($r[0])", $rcsStandIns[$r[0]], "$($r[2]) (stock stand-in)", $r[3], '-stock') }
    if ($rcsNeeds.ContainsKey($r[0])) {
        L 'PIP_NEEDS'
        L '{'
        L "    chassis = PIP-RCS-$($r[0])"
        L "    needs = $($rcsNeeds[$r[0]])"
        L '}'
    }
}
foreach ($r in $rcsAll) {
    Begin "PIP-RCS-$($r[0])$($r[4])" "$($r[2]) RCS" $r[1] 50 @()
    L '    !MODULE[ModuleB9PartSwitch]:HAS[#moduleID[RCS]] {}'
    if ($r[3]) {
        L '    @MODULE[ModuleRCS*]'
        L '    {'
        L '        !PROPELLANT,* {}'
        L '        %resourceName = LiquidFuel'
        L '        PROPELLANT'
        L '        {'
        L '            name = LiquidFuel'
        L '            ratio = 0.9'
        L '        }'
        L '        PROPELLANT'
        L '        {'
        L '            name = Oxidizer'
        L '            ratio = 1.1'
        L '        }'
        L '    }'
    }
    PipModule 'rcs' $r[0]
    L '}'
}

# --- Batteries: one chassis, size on the part (scaled Z-1k)
Begin 'PIP-BAT' 'battery' 'batteryBank' 880 @()
SizeSwitch (Sizes @('0625', '125', '1875', '25', '375') 1.25 @{ top = 0.1108553; bottom = -0.1108553 })
PipModule 'battery' ''
L '}'

# --- Reaction wheels (scaled Advanced Inline Stabilizer)
Begin 'PIP-RWL' 'reaction wheel' 'advSasModule' 1100 @()
SizeSwitch (Sizes @('0625', '125', '1875', '25', '375') 1.25 @{ top = 0.1990267; bottom = -0.1990267 })
PipModule 'wheel' ''
L '}'

# --- Heat shields (scaled 1.25 m shield)
Begin 'PIP-HSH' 'heat shield' 'HeatShield1' 300 @()
SizeSwitch (Sizes @('0625', '125', '1875', '25', '375', '5') 1.25 @{ direct = 0; bottom = -0.17; top = 0.022 })
PipModule 'shield' ''
L '}'

# --- Solar panels: fixed (OX-STAT-XL) and deployable (Gigantor), size class on the part
Begin 'PIP-PV-F' 'fixed solar panel' 'LgRadialSolarPanel' 600 @()
SizeSwitch @(
    @{ code = 'S'; title = 'Small'; scale = 0.5 },
    @{ code = 'M'; title = 'Medium'; scale = 1; upgrade = 'PIP-DeployableArrays'; default = $true }) 'Size class'
PipModule 'solar' 'F'
L '}'
Begin 'PIP-PV-D' 'deployable solar panel' 'largeSolarPanel' 3000 @()
SizeSwitch @(
    @{ code = 'S'; title = 'Small'; scale = 0.3 },
    @{ code = 'M'; title = 'Medium'; scale = 0.55; default = $true },
    @{ code = 'L'; title = 'Large'; scale = 1; upgrade = 'PIP-LargeArrays' },
    @{ code = 'H'; title = 'Huge'; scale = 2.2; upgrade = 'PIP-MultiJunctionCells' }) 'Size class'
PipModule 'solar' 'D'
L '}'

# --- Antennas: omni (Communotron 16) and dish (HG-55), dish class on the part
Begin 'PIP-ANT-O' 'omni antenna' 'longAntenna' 300 @()
PipModule 'antenna' 'O'
L '}'
Begin 'PIP-ANT-D' 'dish antenna' 'HighGainAntenna' 2500 @()
SizeSwitch @(
    @{ code = 'S'; title = 'Wide Dish'; scale = 0.6; default = $true },
    @{ code = 'I'; title = 'Focused Dish'; scale = 1; upgrade = 'PIP-ParabolicDishes' },
    @{ code = 'D'; title = 'Focused Dish, large'; scale = 2; upgrade = 'PIP-DeepSpaceNetwork' }) 'Class'   # D: old deep-space dishes only (DESIGN.md 29.2)
PipModule 'antenna' 'D'
L '}'

# --- Probe cores (scaled OKTO2, plus a reaction wheel for the Torque category)
Begin 'PIP-PRB' 'probe core' 'probeCoreOcto2_v2' 1000 @()
# The program sets the autopilot level (ModulePipPart.ApplyProbe); the OKTO2's own
# level 2 would otherwise offer seven SAS modes on any program (DESIGN.md 27.3).
L '    @MODULE[ModuleSAS]'
L '    {'
L '        @SASServiceLevel = 0'
L '    }'
L '    MODULE'
L '    {'
L '        name = ModuleReactionWheel'
L '        PitchTorque = 1'
L '        YawTorque = 1'
L '        RollTorque = 1'
L '        RESOURCE'
L '        {'
L '            name = ElectricCharge'
L '            rate = 0.03'
L '        }'
L '    }'
SizeSwitch (Sizes @('0625', '125', '1875', '25') 0.625 @{ top = 0.0610621; bottom = -0.0610621 } '125' @{ '125' = 'PIP-LargerAvionicsBays'; '1875' = 'PIP-1875mAvionics'; '25' = 'PIP-HeavyAvionics' })
PipModule 'probe' ''
L '}'

# --- Fuel tanks (scaled FL-T400): kind is the program; size, length and propellant on the part.
# PIP's own fuel switch (moduleID fuelSwitch) holds the kind's propellants; the size
# switch adds its volume to it. Whatever the FL-T400 came with (its resources, another
# mod's fuel switch or boil-off) is taken off. Other fuels' tanks (Fuels.cfg, PIP_FUEL
# TANK nodes) are added to these switches by FuelLoader when the game loads.
$kinds = @(
    # kind, title, propellant subtypes (name, title, tank type), boil-off module: 'cool',
    # 'leak' (no insulation or cooling) or none. Standard tanks hold every mix and leak
    # cryogenic fuels; cryogenic tanks insulate and cool (DESIGN.md 19.7).
    @('S', 'standard', @(@('LF/O', 'LF/Ox', 'PIP-LFOX'), @('LiquidFuel', 'LF', 'PIP-LF'), @('Oxidizer', 'Ox', 'PIP-OX'),
        @('LH2/O', 'LH2/Ox', 'PIP-LH2O'), @('LH2', 'LH2', 'PIP-LH2'), @('Methalox', 'LCH4/Ox', 'PIP-LMOX'), @('Methane', 'LCH4', 'PIP-LM')), 'leak'),
    @('C', 'cryogenic', @(@('LH2/O', 'LH2/Ox', 'PIP-LH2O'), @('LH2', 'LH2', 'PIP-LH2'), @('Methalox', 'LCH4/Ox', 'PIP-LMOX'), @('Methane', 'LCH4', 'PIP-LM')), 'cool'),
    @('M', 'monopropellant', @(, @('Monopropellant', 'Monoprop', 'PIP-MONO')), ''),
    @('E', 'electric propellant', @(@('Xenon', 'Xenon', 'PIPXenon'), @('Argon', 'Argon', 'PIPArgon'), @('Lithium', 'Lithium', 'PIPLithium')), '')
)

# PIP's tank types, under PIP's own names so they never clash with anyone's. The
# chemical ones carry CryoTanks' numbers (MIT licensed, by Nertea; see NOTICE.md),
# monopropellant B9PartSwitch's, electric propellants Near Future's. Hydrogen,
# methane, argon and lithium are Community Resource Pack resources.
#            name            tankMass        tankCost  resources (name, units per volume)
$tankTypes = @(
    @('PIP-LFOX', 0.000625, 0, @(@('LiquidFuel', 0.45), @('Oxidizer', 0.55))),
    @('PIP-LF', 0.000625, 0.3, @(, @('LiquidFuel', 1))),
    @('PIP-OX', 0.000625, 0, @(, @('Oxidizer', 1))),
    @('PIP-LH2O', 0.000278904, 0, @(@('LqdHydrogen', 4.995), @('Oxidizer', 0.333))),
    @('PIP-LH2', 0.000106275, 0.15, @(, @('LqdHydrogen', 7.5))),
    @('PIP-LMOX', 0.000523628, 0, @(@('LqdMethane', 1.875), @('Oxidizer', 0.625))),
    @('PIP-LM', 0.000354675, 0.12, @(, @('LqdMethane', 5))),
    @('PIP-MONO', 0.0004, 0.75, @(, @('MonoPropellant', 1.5))),
    @('PIPXenon', 0.00123046875, 4.5, @(, @('XenonGas', 15.75))),
    @('PIPArgon', 0.000594667, 2.5, @(, @('ArgonGas', 1000))),
    @('PIPLithium', 0.00089, 9.5625, @(, @('Lithium', 5))),
    # An empty adapter (DESIGN.md 20.2): a plain structural adapter, a quarter of a tank's weight.
    @('PIP-EMPTY', 0.00015625, 0, @())
)
foreach ($t in $tankTypes) {
    L 'B9_TANK_TYPE'
    L '{'
    L "    name = $($t[0])"
    L "    tankMass = $(([double]$t[1]).ToString('R', $inv))"
    L "    tankCost = $(N $t[2])"
    foreach ($r in $t[3]) {
        L '    RESOURCE'
        L '    {'
        L "        name = $($r[0])"
        L "        unitsPerVolume = $(N $r[1])"
        L '    }'
    }
    L '}'
}
$lengths = @(@('S', 'short', 0.25), @('M', 'medium', 0.5), @('L', 'long', 1), @('X', 'extra long', 2))
# A tank chassis's propellants: PIP's fuel switch for kind $k, and its boil-off.
function TankInsides($k) {
    L '    %mass = 0.001'
    L '    !RESOURCE,* {}'
    L '    !MODULE[ModuleB9PartSwitch]:HAS[#moduleID[FreeIvaSwitch]] {}'
    L '    !MODULE[ModuleB9PartSwitch]:HAS[#moduleID[fuelSwitch]] {}'
    L '    MODULE'
    L '    {'
    L '        name = ModuleB9PartSwitch'
    L '        moduleID = fuelSwitch'
    L '        switcherDescription = Propellant'
    L '        baseVolume = 0'
    foreach ($f in $k[2]) {
        L '        SUBTYPE'
        L '        {'
        L "            name = $($f[0])"
        L "            title = $($f[1])"
        L "            tankType = $($f[2])"
        L '        }'
    }
    L '    }'
    if ($k[3]) {
        L '    MODULE'
        L '    {'
        L '        name = ModulePipBoiloff'
        if ($k[3] -eq 'leak') { L '        canCool = False' }
        L '    }'
    }
}
$tankStrip = @('ModuleCryoTank', 'ModuleFuelTanks', 'FSfuelSwitch', 'InterstellarFuelSwitch')
foreach ($k in $kinds) {
    Begin "PIP-TNK-$($k[0])" "$($k[1]) tank" 'fuelTank' 300 $tankStrip
    TankInsides $k
    $subs = foreach ($c in '0625', '125', '1875', '25', '375', '5') {
        $s = $diameter[$c] / 1.25
        foreach ($len in $lengths) {
            $up = $null
            switch ($c) { '1875' { $up = $tooling.heavyRocketry } '25' { $up = $tooling.heavierRocketry } '375' { $up = $tooling.veryHeavyRocketry } '5' { $up = $tooling.giganticRocketry } }
            @{ code = "$c-$($len[0])"; title = "$($sizeLabel[$c]) $($len[1])"; scale = $s; scaleY = $s * $len[2]; upgrade = $up
               nodes = @{ top = 0.981725; bottom = -0.9125 }; nodeSize = $nodeSize[$c]; volume = 400 * $s * $s * $s * $len[2]; default = ($c -eq '125' -and $len[0] -eq 'L') }
        }
    }
    SizeSwitch $subs 'Size and length' 'fuelSwitch'
    PipModule 'tank' $k[0]
    L '}'
}

# --- Plane fuselages (DESIGN.md 21.2): the Mk2 and Mk3 rocket fuel fuselages, one
# chassis per kind and section, stretched along their length (never widened).
#            section, base part, its length, node size, lengths (code, title, length m, volume)
$sections = @(
    @('2', 'mk2FuselageLongLFO', 3.75, 1, @(@('2-S', 'Short', 1.875, 400), @('2-L', 'Long', 3.75, 800))),
    @('3', 'mk3FuselageLFO_50', 5.0, 3, @(@('3-S', 'Short', 2.5, 2500), @('3-M', 'Medium', 5.0, 5000), @('3-L', 'Long', 10.0, 10000)))
)
foreach ($k in $kinds) {
    foreach ($sec in $sections) {
        Begin "PIP-PLN-$($k[0])-$($sec[0])" "Mk$($sec[0]) $($k[1]) fuselage" $sec[1] 300 $tankStrip
        TankInsides $k
        $half = $sec[2] / 2
        $subs = foreach ($len in $sec[4]) {
            @{ code = $len[0]; title = $len[1]; scale = 1; scaleY = $len[2] / $sec[2]; nodes = @{ top = $half; bottom = -$half }
               nodeSize = $sec[3]; volume = $len[3]; default = ($len[2] -eq $sec[2]) }
        }
        SizeSwitch $subs 'Length' 'fuelSwitch'
        PipModule 'plane' "$($k[0])-$($sec[0])"
        L '}'
    }
}

# --- Decouplers (DESIGN.md 21.1): the TD-12 and TS-12, scaled from 1.25 m. Radial
# decoupler programs are always made from a look, so they need no chassis here.
foreach ($d in @(@('D', 'decoupler', 'Decoupler_1', 200), @('S', 'separator', 'Separator_1', 275))) {
    Begin "PIP-DEC-$($d[0])" "stack $($d[1])" $d[2] $d[3] @()
    SizeSwitch (Sizes @('0625', '125', '1875', '25', '375', '5') 1.25 @{ top = 0.05; bottom = -0.05 } '125')
    PipModule 'decoupler' $d[0]
    L '}'
}

# --- RTGs (scaled PB-NUK)
Begin 'PIP-RTG' 'RTG' 'rtg' 4000 @()
SizeSwitch @(
    @{ code = 'S'; title = 'Small'; scale = 0.6; nodes = @{ top = 0.3268025; bottom = -0.3268025 }; default = $true },
    @{ code = 'L'; title = 'Large'; scale = 1.6; nodes = @{ top = 0.3268025; bottom = -0.3268025 }; upgrade = 'PIP-StirlingConverters' }) 'Size class'
PipModule 'rtg' ''
L '}'

# --- Air intakes: subsonic (circular), supersonic (ramp), hypersonic (shock cone); size on the part
$intakes = @(@('S', 'subsonic', 'CircularIntake', @{ bottom01 = 0 }), @('P', 'supersonic', 'ramAirIntake', @{ bottom01 = 0 }), @('H', 'hypersonic', 'shockConeIntake', @{ bottom = -0.625 }))
foreach ($i in $intakes) {
    Begin "PIP-INT-$($i[0])" "$($i[1]) intake" $i[2] 300 @()
    SizeSwitch (Sizes @('0625', '125', '1875', '25') 1.25 $i[3] '125' @{ '1875' = 'PIP-RampIntakes'; '25' = 'PIP-RampIntakes' })
    PipModule 'intake' $i[0]
    L '}'
}

$out = Join-Path $PSScriptRoot '..\GameData\PIP\Chassis\Parts.cfg'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($out), $sb.ToString())
"Wrote $([regex]::Matches($sb.ToString(), '\+PART').Count) chassis parts to $out"
