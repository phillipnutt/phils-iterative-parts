# Checks PIP's rules against the spec (DESIGN.md), outside KSP.
# Build first (dotnet/msbuild with /p:NoInstall=true is fine), then:
#   powershell -ExecutionPolicy Bypass -File Tools\Test-Core.ps1

$ksp = 'C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program'
$managed = Join-Path $ksp 'KSP_x64_Data\Managed'
[AppDomain]::CurrentDomain.add_AssemblyResolve({
    param($s, $e)
    $n = ($e.Name -split ',')[0]
    foreach ($p in (Join-Path $managed "$n.dll"), (Join-Path $ksp "GameData\000_Harmony\$n.dll")) { if (Test-Path $p) { return [Reflection.Assembly]::LoadFrom($p) } }
    $null
})
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\Release\PhilsIterativeParts.dll'))
function T($name) { $asm.GetType("PIP.$name") }
$Progress = T 'Progress'; $Pricing = T 'Pricing'; $Program = T 'Program'; $LevelT = T 'FailureLevel'; $PriceT = T 'PriceLevel'
$Types = T 'PartTypes'; $Liquid = T 'LiquidEngines'; $Nuclear = T 'NuclearEngines'
function TypeOf($name) { $Types.GetField($name).GetValue($null) }
$liq = TypeOf 'Liquid'; $sol = TypeOf 'Solid'; $mon = TypeOf 'Monoprop'; $nuc = TypeOf 'Nuclear'; $ele = TypeOf 'Electric'; $jet = TypeOf 'Jet'

$script:failed = 0; $script:passed = 0
trap { Write-Host "  ERROR $_" -ForegroundColor Red; $script:failed++; continue }
function Check($label, $actual, $expected, [double]$tol = 1e-6) {
    $ok = if ($expected -is [double] -or $expected -is [int]) { [Math]::Abs([double]$actual - [double]$expected) -le $tol } else { "$actual" -eq "$expected" }
    if ($ok) { $script:passed++ } else { Write-Host "  FAIL $label = $actual (expected $expected)" -ForegroundColor Red; $script:failed++ }
}
function Call($type, $name, [object[]]$a) { $type.GetMethods() | Where-Object { $_.Name -eq $name -and $_.GetParameters().Count -eq $a.Count } | Select-Object -First 1 | ForEach-Object { $_.Invoke($null, $a) } }
function Level($l) { [Enum]::Parse($LevelT, $l) }
function M([string]$code) { , [string[]]($code -split '-') }   # the comma stops PowerShell unrolling the array
function Stat($type, [string]$model, [int[]]$pts, [int]$mk, $key, $level = 'Normal') { ($type.Compute((M $model), $pts, $mk, (Level $level))).Item($key) }

Write-Host 'Mk. 1 to 5 (section 18.2)'
Check 'experience to Mk. 2..5' "$((2..5 | ForEach-Object { Call $Progress XpForMk @([int]$_) }) -join ',')" '2,7,14,25'
Check 'programs start at Mk. 1' (Call $Progress Mk @(0)) 1
Check 'Mk at 6 xp' (Call $Progress Mk @(6)) 2
Check 'Mk at 7 xp' (Call $Progress Mk @(7)) 3
Check 'Mk at 25 xp' (Call $Progress Mk @(25)) 5
Check 'Mk never passes 5' (Call $Progress Mk @(5000)) 5
Check 'a flight 1, a recovery 3' "$($Progress.GetField('LaunchXp').GetValue($null)),$($Progress.GetField('RecoveryXp').GetValue($null))" '1,3'
Check 'Mk. 1 delivers half the points' ($Progress.GetMethod('Effective', [type[]]@([int], [int])).Invoke($null, [object[]]@([int]60, [int]1))) 30
Check 'Mk. 5 delivers all of them' ($Progress.GetMethod('Effective', [type[]]@([int], [int])).Invoke($null, [object[]]@([int]60, [int]5))) 60
Check 'a later version starts from the one before' ($Progress.GetMethod('Effective', [type[]]@([int], [double], [int])).Invoke($null, [object[]]@([int]35, [double]25, [int]1))) 25
Check 'front-loaded: Mk. 2 is about half the way' ($Progress.GetMethod('Curve').Invoke($null, [object[]]@([int]2))) 0.4602 1e-3

Write-Host 'Program cost: 1,000 to a ceiling by type (section 18.5)'
$normal = [Enum]::Parse($PriceT, 'Normal')
function PC([double]$ceiling, [double]$share, $lvl = $normal) { $Pricing.GetMethod('ProgramCost').Invoke($null, @($ceiling, $share, $lvl)) }
Check 'no points spent' (PC 1.5e6 0) 1000
Check 'liquid at a quarter' (PC 1.5e6 0.25) 6223.3 1
Check 'liquid at half' (PC 1.5e6 0.5) 38729.8 1
Check 'liquid maxed' (PC 1.5e6 1) 1500000 1
Check 'tank at half' (PC 1e5 0.5) 10000 1e-6
Check 'PIP prices: double' (PC 1e5 0.5 ([Enum]::Parse($PriceT, 'Double'))) 20000 1e-6
Check 'ceilings by type' "$($sol.CostCeiling),$($mon.CostCeiling),$($jet.CostCeiling),$($liq.CostCeiling),$($ele.CostCeiling),$($nuc.CostCeiling)" '500000,500000,1000000,1500000,2000000,3000000'
Check 'a successor costs a tenth' ($Pricing.GetField('SuccessorShare').GetValue($null)) 0.1
Check 'share over the sliders used' ($liq.PointsShare((M 'K-125-S'), [int[]]@(25, 25, 25, 25, 25))) 0.25 1e-9
Check 'unused sliders don''t count' ((TypeOf 'Tank').PointsShare((M 'S'), [int[]]@(50, 50, 0, 50))) 0.5 1e-9

Write-Host 'Family caps'
Check 'liquid cap nodes' ($liq.CapTechs -join ',') 'basicRocketry,advRocketry,heavierRocketry,experimentalRocketry'
Check 'solid cap nodes' ($sol.CapTechs -join ',') 'start,generalRocketry,heavierRocketry,experimentalRocketry'
Check 'liquid 2.5 m needs shared tooling' (($liq.Techs((M 'K-25-S')) | Select-Object -Unique) -join ',') 'basicRocketry,heavierRocketry'

Write-Host 'Liquid engines'
$k125 = M 'K-125-S'
Check 'thrust 1.25 m, 0 points' (Call $Liquid ThrustAt @($k125, [double]0)) 90
Check 'thrust 1.25 m, 100 points' (Call $Liquid ThrustAt @($k125, [double]100)) 550
Check 'methalox thrust ceiling x1.15' (Call $Liquid ThrustAt @((M 'M-125-S'), [double]100)) 632.5
Check 'vacuum nozzle thrust window' (Call $Liquid ThrustAt @((M 'K-125-V'), [double]100)) 150
Check 'Isp sea level, 0 points' (Call $Liquid IspSLAt @($k125, [double]0)) 220
Check 'Isp vacuum nozzle, 100 points' (Call $Liquid IspVacAt @((M 'K-125-V'), [double]100)) 385
Check 'vacuum nozzle stays bad at sea level' (Call $Liquid IspSLAt @((M 'K-125-V'), [double]100)) 100
Check 'weight: 25 thrust, 25 weight' (Call $Liquid MassAt @($k125, 25, [double]25)) 1.4932 1e-3
function GStat($type, [string]$model, [int[]]$pts, [int]$mk, $key, [int]$gimbal) { ($type.Compute((M $model), $pts, $mk, (Level 'Normal'), $null, $null, $gimbal)).Item($key) }
Check 'gimbal is a pick, not points' "$($liq.HasGimbalPick) $(GStat $liq 'K-125-S' @(0, 0, 0, 0, 0) 1 'gimbal' 10)" 'True 10'
Check 'gimbal options and research' ((($Types.Assembly.GetType('PIP.PartType')).GetField('GimbalOptions').GetValue($null) | ForEach-Object { "$($_.Code):$($_.Tech)" }) -join ',') '0:,5:,10:generalRocketry,20:advRocketry'
Check 'aerospike has no gimbal' (GStat $liq 'K-125-A' @(0, 0, 0, 0, 0) 1 'gimbal' 20) 0
Check 'no Gimbal slider left' ((@($liq, $sol, $mon) | ForEach-Object { ($_.Categories | Where-Object { $_.Label -eq 'Gimbal' }).Count }) -join ',') '0,0,0'
Check 'failure at Mk. 0' ($liq.FailureAt($k125, 50, 0, (Level Normal))) 0.10
Check 'failure at Mk. 10, 50 points' ($liq.FailureAt($k125, 50, 10, (Level Normal))) 0.004472 1e-5
Check 'methalox mature failure, 100 points' ($liq.FailureAt((M 'M-125-S'), 100, 10, (Level Normal))) 0.0005
Check 'light failures are half' ($liq.FailureAt($k125, 50, 0, (Level Light))) 0.05
Check 'every type has Build cost last' ((@($liq, $jet, (TypeOf 'Tank')) | ForEach-Object { $_.Categories[$_.CostIndex].Label }) -join ',') 'Build cost,Build cost,Build cost'
# Build price: 1,200 base x ambition (80 of 400 other points: x0.9) x cost factor (1.5 at 0 points, 0.5 at 100).
Check 'build price, Build cost 0' ($liq.BuildPriceAt($k125, [int[]]@(20, 20, 20, 20, 0), 1)) 1620
Check 'build price, Build cost 100 at Mk. 5' ($liq.BuildPriceAt($k125, [int[]]@(20, 20, 20, 20, 100), 5)) 540
Check 'build price falls as it matures' ($liq.BuildPriceAt($k125, [int[]]@(20, 20, 20, 20, 100), 1)) 1080
Check 'aerospike builds at x2' ($liq.BuildPriceAt((M 'K-125-A'), [int[]]@(20, 20, 20, 20, 0), 1)) 3240
$pts = [int[]]@(25, 25, 25, 25, 25)
Check 'Mk. 1 thrust uses half the points' (Stat $liq 'K-125-S' $pts 1 'thrustVac') 147.5
Check 'Mk. 5 thrust uses all of them' (Stat $liq 'K-125-S' $pts 5 'thrustVac') 205
Check 'Mk. 1 weight' (Stat $liq 'K-125-S' $pts 1 'mass') 1.5999 1e-3
Check 'failures start over each version: 10% at Mk. 1' ($liq.FailureAt($k125, 50, 1, (Level Normal))) 0.10

Write-Host 'Solid boosters'
$max = [int[]]@(100, 100, 100, 100, 100); $zero = [int[]]@(0, 0, 0, 0, 0)
Check '1.25 m medium, 100 points' (Stat $sol '125-M' $max 10 'thrustVac') 480
Check '1.25 m long is x2' (Stat $sol '125-L' $max 10 'thrustVac') 960
Check '1.25 m short is x0.6' (Stat $sol '125-S' $zero 10 'thrustVac') 72
Check 'Thumper-size propellant' (Stat $sol '125-M' $zero 0 'propellant') 6.15
Check 'casing at 0 points' (Stat $sol '125-M' $zero 0 'mass') (6.15 * 0.27) 1e-6
Check 'casing at 100 points' (Stat $sol '125-M' $max 10 'mass') (6.15 * 0.13) 1e-6
Check 'Isp window' "$(Stat $sol '125-M' $zero 0 'ispSL')/$(Stat $sol '125-M' $max 10 'ispVac')" '150/265'
Check 'solid gimbal is a pick' (GStat $sol '125-M' $max 5 'gimbal' 5) 5
Check 'mature failure 0.05%' (Stat $sol '125-M' $max 10 'fail') 0.0005
Check 'burn time' (Stat $sol '125-M' $max 10 'burn') (6.15 * 265 * 9.80665 / 480) 1e-6
Check 'chassis name' ($sol.ChassisPart((M '375-L'))) 'PIP-SOL-375-L'

Write-Host 'Monopropellant engines'
Check '1.25 m, 100 points' (Stat $mon '125' $max 10 'thrustVac') 170
Check 'Isp vacuum at 100' (Stat $mon '125' $max 10 'ispVac') 345
Check 'weight at 100 points (TWR 25)' (Stat $mon '125' $max 10 'mass') (170 / (25 * 9.80665)) 1e-6

Write-Host 'Nuclear engines'
$Nuclear.GetField('Kerbalism').SetValue($null, $true)
Check 'gas core has no 0.625 m' ($nuc.Valid((M 'G-0625'))) $false
Check 'solid core 1.25 m, 100 points' (Stat $nuc 'S-125' $max 10 'thrustVac') 120
Check 'gas core Isp at 100' (Stat $nuc 'G-25' $max 10 'ispVac') 3000
Check 'radiation at 0 points (Nerv level)' (Stat $nuc 'S-125' $zero 0 'radiation') 0.25
Check 'radiation at 100 points is a tenth' (Stat $nuc 'S-125' $max 10 'radiation') 0.025 1e-9
Check 'gas core radiates twice' (Stat $nuc 'G-125' $zero 0 'radiation') 0.5
Check 'fifth category is Shielding' ($nuc.Categories[4].Label) 'Shielding'
Check 'gimbal is a pick with Kerbalism too' (GStat $nuc 'S-125' $zero 1 'gimbal' 10) 10
$Nuclear.GetField('Kerbalism').SetValue($null, $false)
Check 'without Kerbalism: four sliders and Build cost' (($nuc.Categories | ForEach-Object { $_.Label }) -join ',') 'Thrust,Efficiency,Weight,Reliability,Build cost'

Write-Host 'Electric engines'
Check 'ion 0.625 m, 100 points' (Stat $ele 'I-0625' $max 10 'thrustVac') 5
Check 'power: kN x Isp/1000 x 2.5 at 100' (Stat $ele 'I-0625' $max 10 'ecRate') (5 * 12 * 2.5) 1e-6
Check 'power: factor 6 at 0' (Stat $ele 'H-0625' $zero 0 'ecRate') (1 * 1.8 * 6) 1e-6
Check 'MPD 2.5 m ceiling' (Stat $ele 'M-25' $max 10 'thrustVac') 320

Write-Host 'Jet engines'
$jmax = [int[]]@(100, 100, 100, 100); $jzero = [int[]]@(0, 0, 0, 0)
Check 'four categories and Build cost' $jet.Categories.Length 5
Check 'jets never fail' (Stat $jet 'F-125' $jmax 0 'fail') 0
Check 'turbojet dry thrust is two-thirds' (Stat $jet 'J-125' $jmax 10 'thrustDry') (200 * 2 / 3) 1e-6
Check 'combined rocket thrust x1.7' (Stat $jet 'C-125' $jmax 10 'rocketThrust') 340
Check 'ramjet top speed at 0 points' (Stat $jet 'R-125' $jzero 0 'topMach') 4.4 1e-9
Check 'ramjet top speed at 100 points' (Stat $jet 'R-125' $jmax 10 'topMach') 6.6 1e-9

Write-Host 'Part types (size per program)'
function PStat($name, [string]$model, [int[]]$pts, [int]$mk, $key, $variant) { $t = TypeOf $name; $m = $(if ($model) { M $model } else { , [string[]]@() }); ($t.Compute($m, $pts, $mk, (Level 'Normal'), $variant)).Item($key) }
Check 'eighteen types' $Types.GetField('All').GetValue($null).Length 18
Check 'battery: stock capacity at ~45 points' (PStat 'Battery' '' @(45) 10 'capacity' '125') 20100
Check 'battery: 1.25 m holds 600 EC at 0 points' (PStat 'Battery' '' @(0) 0 'ec' '125') 600
Check 'battery: 1.25 m holds 1,500 EC at 100' (PStat 'Battery' '' @(100) 10 'ec' '125') 1500
Check 'battery: Capacity and Build cost' (((TypeOf 'Battery').Categories | ForEach-Object { $_.Label }) -join ',') 'Capacity,Build cost'
Check 'wheel: 1.25 m torque at 100' (PStat 'Wheel' '' @(100, 0, 0) 10 'torque' '125') 25
Check 'wheel: 3.75 m torque at 100' (PStat 'Wheel' '' @(100, 0, 0) 10 'torque' '375') 100
Check 'heat shield: 5 m ablator' (PStat 'Shield' '' @(0, 0, 0) 0 'ablator' '5') 3200
Check 'heat shield: max temperature at 100' (PStat 'Shield' '' @(0, 0, 100) 10 'maxTemp' '125') 3600
Check 'solar: huge deployable at 100' (PStat 'Solar' 'D' @(100, 0, 0) 10 'ecRate' 'H') 300
Check 'solar: folds up from 50 designed points' (PStat 'Solar' 'D' @(0, 0, 50) 0 'retractable' 'M') 1
Check 'solar: fixed has no durability' ((TypeOf 'Solar').CategoryUsed((M 'F'), 2)) $false
Check 'antenna: short dish at 50 points ~22 Mm' ([Math]::Round((PStat 'Antenna' 'D' @(50, 0, 0) 10 'range' 'S') / 1e6, 1)) 22.4
Check 'antenna: interplanetary dish at 50 points 20 Gm' ((PStat 'Antenna' 'D' @(50, 0, 0) 10 'range' 'I') / 1e9) 20 1e-6
Check 'antenna: omni at 100 points 6 Mm' ((PStat 'Antenna' 'O' @(100, 0, 0) 10 'range' $null) / 1e6) 6 1e-6
Check 'probe: SAS steps' "$((0..4 | ForEach-Object { [PIP.ProbeCores]::SasLevel(@(0, 10, 25, 50, 75)[$_]) }) -join ',')" '-1,0,1,2,3'
Check 'probe: 50-point core at Mk. 0 has level 1' (PStat 'Probe' '' @(50, 0, 0, 0) 0 'sas' '125') 1
Check 'probe: 50-point core at Mk. 10 has level 2' (PStat 'Probe' '' @(50, 0, 0, 0) 10 'sas' '125') 2
Check 'tank: FL-T400 volume' ([PIP.FuelTanks]::Volume('125-L')) 400
Check 'tank: long 2.5 m is an X200-32' ([PIP.FuelTanks]::Volume('25-L')) 3200
Check 'tank: weight factor at 100' (PStat 'Tank' 'S' @(100, 0, 0) 10 'weightFactor' '125-L') 0.8
Check 'tank: empty FL-T400 at factor 1 is stock' (0.001 + 400 * 0.000625) 0.251
Check 'tank: empty weight in tonnes at 100' (PStat 'Tank' 'S' @(100, 0, 0) 10 'mass' '125-L') 0.201 1e-9
Check 'tank: units held by kind, FL-T400 size' "$([PIP.FuelTanks]::Units((M 'S'), '125-L')),$([PIP.FuelTanks]::Units((M 'M'), '125-L')),$([PIP.FuelTanks]::Units((M 'E'), '125-L'))" '400,600,6300'
Check 'tank: a short 0.625 m holds 12.5' ([PIP.FuelTanks]::Units((M 'S'), '0625-S')) 12.5
Check 'tank: hydrogen boil-off at 0 insulation' (PStat 'Tank' 'C' @(0, 0, 0) 10 'boiloffRate' '125-L') 0.1 1e-9
Check 'plain percentages' "$([PIP.PartKind]::Percent(1.2, 'more', 'less'))|$([PIP.PartKind]::Percent(0.6, 'more', 'less'))|$([PIP.PartKind]::Percent(1, 'more', 'less'))" '20% more|40% less|as stock'
Check 'tank: insulation only for cryogenic' "$((TypeOf 'Tank').CategoryUsed((M 'S'), 2)),$((TypeOf 'Tank').CategoryUsed((M 'C'), 2))" 'False,True'
# Section 19.7: standard tanks hold every mix and leak cryogenic fuels at 4x;
# cryogenic tanks come later, with Insulation and cooling.
Check 'tank: standard tanks leak at 4x the base rate' "$(PStat 'Tank' 'S' @(0, 0, 0) 10 'boiloff' '125-L'),$(PStat 'Tank' 'S' @(0, 0, 0) 10 'boiloffRate' '125-L')" '4,0.2'
Check 'tank: cryogenic tanks unlock at Large Volume Containment' ((TypeOf 'Tank').Choices[0].Find('C').Tech) 'largeVolumeContainment'
$partsCfg = Get-Content (Join-Path $PSScriptRoot '..\GameData\PIP\Chassis\Parts.cfg') -Raw
function TankBlock($k) { [regex]::Match($partsCfg, "(?s)@name = PIP-TNK-$k\b.*?(?=\+PART\[|\z)").Value }
$std = TankBlock 'S'; $cry = TankBlock 'C'
Check 'tank: standard holds every mix' (([regex]::Matches($std, 'tankType = (\S+)') | ForEach-Object { $_.Groups[1].Value }) -join ',') 'PIP-LFOX,PIP-LF,PIP-OX,PIP-LH2O,PIP-LH2,PIP-LMOX,PIP-LM'
Check 'tank: standard boils off but can''t cool, cryogenic cools' "$($std -match 'name = ModulePipBoiloff\s+canCool = False'),$($cry -match 'name = ModulePipBoiloff\s+\}')" 'True,True'
Check 'tank: 24 size-and-length variants' ((TypeOf 'Tank').Variants((M 'S')).Length) 24
Check 'rtg: large output at 100' (PStat 'Rtg' '' @(100, 0) 10 'ecRate' 'L') 4.5 1e-9
Check 'intake: hypersonic top speed at 100' (PStat 'Intake' 'H' @(0, 100, 0) 10 'topMach' '125') 9.6 1e-9
Check 'rcs: 4-way mono weight at 100' (PStat 'Rcs' 'M-4' @(100, 0, 100) 10 'mass' $null) (12 / (12.5 * 9.80665)) 1e-9
Check 'part types have no failures' (PStat 'Rcs' 'M-4' @(100, 0, 100) 0 'fail' $null) 0

Write-Host 'Side-mount parts and looks (sections 15, 16)'
$bat = TypeOf 'Battery'; $whl = TypeOf 'Wheel'; $int = TypeOf 'Intake'; $rtg = TypeOf 'Rtg'
Check 'side-mount types' "$($bat.HasSideMount),$($whl.HasSideMount),$($int.HasSideMount),$($rtg.HasSideMount),$($liq.HasSideMount),$((TypeOf 'Tank').HasSideMount)" 'True,True,True,False,False,True'
Check 'battery side sizes' (($bat.SideVariants((M '')) | ForEach-Object { $_.Code }) -join ',') 'R-S,R-M,R-L'
# Section 20.1: radial tanks, five sizes by volume, scaled by volume.
$tk = TypeOf 'Tank'
Check 'radial tanks: side-mount, five sizes' "$($tk.HasSideMount)|$(($tk.SideVariants((M 'S')) | ForEach-Object { $_.Code }) -join ',')" 'True|R-T,R-S,R-M,R-L,R-H'
Check 'radial tank volumes' "$((@('R-T','R-S','R-M','R-L','R-H') | ForEach-Object { [PIP.FuelTanks]::Volume($_) }) -join ',')" '5,25,50,200,1600'
Check 'radial unlocks: Propulsion Systems, tooling for large and huge' (($tk.SideVariants((M 'S')) | ForEach-Object { $_.Tech }) -join ',') 'propulsionSystems,propulsionSystems,propulsionSystems,heavyRocketry,heavierRocketry'
Check 'radial looks scale by volume, in-line by size and length' "$($tk.ScaleBasis($true, 'F')),$($tk.ScaleBasis($false, 'F')),$($tk.NominalVolume('R-M'))" 'Volume,Tank,50'
Check 'a medium radial tank weighs as 50 units of tank' (PStat 'Tank' 'S' @(100, 0, 0) 10 'mass' 'R-M') (0.001 + 50 * 0.000625 * 0.8) 1e-9
Check 'standard radial looks by kind' "$($tk.SideLookPreference((M 'S'))[0]),$($tk.SideLookPreference((M 'M'))[0]),$($tk.SideLookPreference((M 'E'))[0])" 'externalTankCapsule,rcsTankRadialLong,xenonTankRadial'
Check 'a radial program is labelled with its size' ($tk.VariantLabel((M 'S'), 'R-L')) 'Side-mount, large'
# Section 20.2: adapter tanks, nine size pairs, short and long, by taper ratio.
$ad = $tk.AdapterVariants((M 'S'))
Check 'adapters: nine pairs, short and long' "$($tk.HasAdapters)|$($ad.Count)|$($ad[0].Code)|$($ad[-1].Code)" 'True|18|A-125-0625-S|A-5-375-L'
Check 'an adapter is labelled with its ends' ($tk.VariantLabel((M 'S'), 'A-25-125-L')) 'Adapter, 2.5 to 1.25 m long'
Check 'adapter codes are neither in-line nor side-mount' "$([PIP.PartType]::MountOf('A-25-125-L')),$([PIP.PartType]::MountOf('R-M')),$([PIP.PartType]::MountOf('125-L'))" 'A,R,'
Check 'adapter tapers' "$((@('A-125-0625-L','A-1875-125-L','A-25-1875-L','A-1875-0625-L','A-5-375-S') | ForEach-Object { $tk.AdapterKind($_) }) -join ',')" 'A2,A32,A43,A3,A43'
# Each ratio holds what its stock adapter holds (C7 800, ADTP-2-3 3000, FL-A215 1200, FL-A150 160), by size cubed; short holds half.
Check 'long 2.5 to 1.25 holds 800, as the C7' ([PIP.FuelTanks]::Volume('A-25-125-L')) 800 1e-9
Check 'short 2.5 to 1.25 holds 400' ([PIP.FuelTanks]::Volume('A-25-125-S')) 400 1e-9
Check '1.25 to 0.625 is the C7 at half size' ([PIP.FuelTanks]::Volume('A-125-0625-L')) 100 1e-9
Check '3.75 to 2.5 holds 3000, as the ADTP-2-3' ([PIP.FuelTanks]::Volume('A-375-25-L')) 3000 1e-9
Check '2.5 to 1.875 holds 1200, as the FL-A215' ([PIP.FuelTanks]::Volume('A-25-1875-L')) 1200 1e-9
Check '5 to 3.75 is the FL-A215 at twice the size' ([PIP.FuelTanks]::Volume('A-5-375-L')) 9600 1e-9
Check '1.875 to 0.625 holds 160, as the FL-A150' ([PIP.FuelTanks]::Volume('A-1875-0625-L')) 160 1e-9
Check 'adapter heights: C7 2.5 m, half when short, ADTP-2-3 2.25 m' "$($tk.NominalHeight('A-25-125-L')),$($tk.NominalHeight('A-25-125-S')),$([Math]::Round($tk.NominalHeight('A-375-25-L'), 3))" '2.5,1.25,2.25'
Check 'adapter unlocks: Fuel Systems, then tooling' "$(($tk.VariantOption((M 'S'), 'A-125-0625-L').Techs) -join '+')|$(($tk.VariantOption((M 'S'), 'A-375-25-L').Techs) -join '+')" 'fuelSystems|fuelSystems+heavierRocketry+veryHeavyRocketry'
Check 'a program needs every node its adapter does' (($tk.Techs((M 'S'), 'A-25-1875-L') | Select-Object -Unique) -join ',') 'basicRocketry,fuelSystems,heavyRocketry,heavierRocketry'
Check 'adapter looks: scaled as adapters; serve every kind' "$($tk.ScaleBasis($false, 'A2')),$(($tk.LookKeys('A2')) -join '')" 'Adapter,SCME'
Check 'standard adapter looks' "$($tk.AdapterLookPreference('A2')[0]),$($tk.AdapterLookPreference('A32')[0]),$($tk.AdapterLookPreference('A43')[0]),$($tk.AdapterLookPreference('A3')[0])" 'adapterSize2-Size1,Size3To2Adapter.v2,Size1p5.Size2.Adapter.01,Size1p5.Size0.Adapter.01'
Check 'adapter tapers by their ends' "$([PIP.FuelTanks]::AdapterKindOf(2.5, 1.25)),$([PIP.FuelTanks]::AdapterKindOf(7.5, 5)),$([PIP.FuelTanks]::AdapterKindOf(5, 1.25))" 'A2,A32,'
Check 'kept tank-shaped parts' "$([PIP.FuelTanks]::KeptTank('noseConeAdapter', 'NCS Adapter')),$([PIP.FuelTanks]::KeptTank('nfex-fueltank-stack-medium-1', 'MLT-240 Multipurpose Fuel Tank Cluster')),$([PIP.FuelTanks]::KeptTank('adapter-25-125', 'Rockomax Skeletal Structural Adapter')),$([PIP.FuelTanks]::KeptTank('adapterSize2-Size1', 'C7 Brand Adapter - 2.5m to 1.25m'))" 'True,True,True,False'
Check 'a long 2.5 to 1.25 adapter weighs as 800 units of tank' (PStat 'Tank' 'S' @(100, 0, 0) 10 'mass' 'A-25-125-L') (0.001 + 800 * 0.000625 * 0.8) 1e-9
# Section 21.1: decouplers. Today's TD-12 (100 kN, 0.04 t) sits at 25 points of ejection, 50 of weight.
$dec = TypeOf 'Decoupler'
Check 'decouplers: kinds, sizes, side sizes' "$(($dec.Choices[0].Options | ForEach-Object { $_.Code }) -join ',')|$(($dec.Variants((M 'D')) | ForEach-Object { $_.Code }) -join ',')|$(($dec.SideVariants((M 'D')) | ForEach-Object { $_.Code }) -join ',')" 'D,S|0625,125,1875,25,375,5|R-S,R-M,R-L'
Check 'separators have no side-mount' ($null -eq $dec.SideVariants((M 'S'))) $true
Check 'TD-12 ejection at 25 points' (PStat 'Decoupler' 'D' @(25, 50, 25, 25) 10 'eject' '125') 100 1e-9
Check '0.625 m ejects about as the TD-06' (PStat 'Decoupler' 'D' @(25, 50, 25, 25) 10 'eject' '0625') (100 * [Math]::Pow(0.5, 0.6)) 1e-9
Check 'TD-12 weight at 50 points' (PStat 'Decoupler' 'D' @(25, 50, 25, 25) 10 'mass' '125') 0.048 1e-9
Check 'a separator weighs a quarter more' (PStat 'Decoupler' 'S' @(25, 50, 25, 25) 10 'mass' '125') 0.06 1e-9
Check 'a 2.5 m decoupler weighs by diameter squared' (PStat 'Decoupler' 'D' @(25, 100, 25, 25) 10 'mass' '25') (0.16 * 0.8) 1e-9
Check 'a medium radial decoupler ejects as the TT-38K' (PStat 'Decoupler' 'D' @(25, 50, 25, 25) 10 'eject' 'R-M') 250 1e-9
Check 'decoupler looks: radial ones dress decouplers only' "$(($dec.LookKeys('R')) -join '')|$(($dec.LookKeys('')) -join '')|$($dec.ScaleBasis($true, 'R')),$($dec.ScaleBasis($false, ''))" 'D|DS|Mass,Diameter'
Check 'a 2.5 m separator needs its node and tooling' (($dec.Techs((M 'S'), '25') | Select-Object -Unique) -join ',') 'engineering101,generalConstruction,heavierRocketry'
Check 'large radial decouplers at Specialized Construction' (($dec.Techs((M 'D'), 'R-L') | Select-Object -Unique) -join ',') 'engineering101,specializedConstruction'
Check 'a radial decoupler program is labelled' ($dec.VariantLabel((M 'D'), 'R-M')) 'Side-mount, medium'
# Section 21.2: plane fuselages, a type of their own, tanks in every other way.
$pln = TypeOf 'Plane'
Check 'fuselages: kinds as tanks, Mk2 and Mk3' "$(($pln.Choices[0].Options | ForEach-Object { $_.Code }) -join ',')|$(($pln.Choices[1].Options | ForEach-Object { $_.Code }) -join ',')" 'S,C,M,E|2,3'
Check 'Mk2 short and long, Mk3 short, medium and long' "$(($pln.Variants((M 'S-2')) | ForEach-Object { $_.Code }) -join ',')|$(($pln.Variants((M 'S-3')) | ForEach-Object { $_.Code }) -join ',')" '2-S,2-L|3-S,3-M,3-L'
Check 'fuselages hold what stock ones do' "$((@('2-S','2-L','3-S','3-M','3-L') | ForEach-Object { PStat 'Plane' 'S-3' @(25, 25, 0) 10 'volume' $_ }) -join ',')" '400,800,2500,5000,10000'
Check 'a fuselage weighs as a tank of its volume' (PStat 'Plane' 'S-3' @(100, 0, 0) 10 'mass' '3-M') (0.001 + 5000 * 0.000625 * 0.8) 1e-9
Check 'and as the same tank does' (PStat 'Plane' 'S-2' @(40, 60, 0) 10 'mass' '2-L') (PStat 'Tank' 'S' @(40, 60, 0) 10 'mass' 'A-25-125-L') 1e-9
Check 'a cryogenic fuselage uses Insulation' "$($pln.CategoryUsed((M 'C-2'), 2)),$($pln.CategoryUsed((M 'S-2'), 2))" 'True,False'
Check 'fuselage labels' "$($pln.Label((M 'S-2')))|$($pln.Label((M 'C-3')))|$($pln.VariantLabel((M 'S-2'), '2-L'))" 'Mk2 standard fuselages|Mk3 cryogenic fuselages|Long'
Check 'a long Mk3 fuselage needs its nodes' (($pln.Techs((M 'S-3'), '3-L') | Select-Object -Unique) -join ',') 'supersonicFlight,heavyAerodynamics,experimentalAerodynamics'
Check 'Mk2 lift goes with length, as stock' "$([PIP.PlaneFuselages]::Length('2-S') * [PIP.PlaneFuselages]::Mk2LiftPerMeter),$([PIP.PlaneFuselages]::Length('2-L') * [PIP.PlaneFuselages]::Mk2LiftPerMeter)" '0.35,0.7'
Check 'fuselage looks: by section, stretched only' "$($pln.LookFits((M 'S-3'), '3')),$($pln.LookFits((M 'S-3'), '2')),$($pln.ScaleBasis($false, '2')),$($pln.NominalLength('3-L'))" 'True,False,Length,10'
Check 'fuselage chassis by kind and section' ($pln.ChassisPart((M 'M-3'))) 'PIP-PLN-M-3'
Check 'battery side default is small' ($bat.DefaultSideVariant((M ''))) 'R-S'
Check 'battery: side medium holds 400 EC at stock capacity' (PStat 'Battery' '' @(45) 10 'ec' 'R-M') 402 1e-9
Check 'battery: side small weighs 0.005 t' (PStat 'Battery' '' @(45) 10 'mass' 'R-S') 0.005
Check 'battery: inline unchanged by side sizes' (PStat 'Battery' '' @(0) 0 'ec' '125') 600
Check 'battery: nominal mass, side large' ($bat.NominalMass('R-L')) 0.04
Check 'wheel: side torque 0.5 to 3' "$(PStat 'Wheel' '' @(0, 0, 0) 0 'torque' 'R'),$(PStat 'Wheel' '' @(100, 0, 0) 10 'torque' 'R')" '0.5,3'
Check 'wheel: inline torque unchanged' (PStat 'Wheel' '' @(100, 0, 0) 10 'torque' '125') 25
Check 'intake: 1.25 m subsonic area at 0 points' (PStat 'Intake' 'S' @(0, 0, 0) 0 'area' '125') 0.0036 1e-12
Check 'intake: 2.5 m area is four times' ((PStat 'Intake' 'S' @(0, 0, 0) 0 'area' '25') / (PStat 'Intake' 'S' @(0, 0, 0) 0 'area' '125')) 4 1e-9
Check 'intake: subsonic side area at 100' (PStat 'Intake' 'S' @(100, 0, 0) 10 'area' 'R') 0.00465 1e-12
Check 'intake: shock cone weight at 0 points' (PStat 'Intake' 'H' @(0, 0, 0) 0 'mass' '125') 0.192 1e-12
Check 'intake: standard look by speed type' "$($int.DefaultLook((M 'S'))),$($int.DefaultLook((M 'H')))" 'CircularIntake,shockConeIntake'
Check 'intake: side looks prefer the XM-G50 for subsonic' ($int.SideLookPreference((M 'S'))[0]) 'airScoop'
Check 'rtg: scaled by mass' "$($rtg.ScaleBasis($false, '')),$($rtg.NominalMass('L'))" 'Mass,0.3'
Check 'types with looks' (($Types.GetField('All').GetValue($null) | Where-Object { $_.HasLooks } | ForEach-Object { $_.Id }) -join ',') 'liquid,solid,monoprop,nuclear,electric,jet,rcs,battery,wheel,shield,solar,antenna,probe,tank,plane,rtg,intake,decoupler'

Write-Host 'Looks step 2: solar, antennas, tanks, RCS'
$sol = TypeOf 'Solar'; $ant = TypeOf 'Antenna'; $tnk = TypeOf 'Tank'; $rcsT = TypeOf 'Rcs'
Check 'solar: fixed look only for fixed programs' "$($sol.LookFits((M 'F'), 'F')),$($sol.LookFits((M 'F'), 'D')),$($sol.LookFits((M 'D'), 'D'))" 'True,False,True'
Check 'solar: scaled by output' ($sol.ScaleBasis($false, 'D')) 'Output'
Check 'solar: large class middle output' ($sol.NominalOutput('L')) 30
Check 'solar: standard looks' "$($sol.DefaultLook((M 'F'))),$($sol.DefaultLook((M 'D')))" 'LgRadialSolarPanel,largeSolarPanel'
Check 'antenna: dish look only for dish programs' "$($ant.LookFits((M 'D'), 'D')),$($ant.LookFits((M 'D'), 'O'))" 'True,False'
Check 'antenna: dishes scale by weight, omnis not at all' "$($ant.ScaleBasis($false, 'D')),$($ant.ScaleBasis($false, 'O'))" 'Mass,None'
Check 'antenna: deep-space dish nominal weight' ($ant.NominalMass('D')) ([Math]::Sqrt(0.15)) 1e-12
Check 'tank: one look part per kind' "$($tnk.LookKey((M 'C')))|$($tnk.LookKeys('F') -join ',')|$($tnk.LookKeys('E') -join ',')|$($tnk.LookKeys('M') -join ',')" 'C|S,C|E|M'
Check 'tank: scaled by size and length' ($tnk.ScaleBasis($false, 'F')) 'Tank'
Check 'tank: FL-T400 aspect' ([PIP.FuelTanks]::LongAspect) 1.51538 1e-5
Check 'rcs: look part per propellant' "$($rcsT.LookKey((M 'B-4')))|$($rcsT.LookKeys('') -join ',')" 'B|M,B'
Check 'rcs: nozzles must match the layout' "$($rcsT.LookFits((M 'M-4'), '4')),$($rcsT.LookFits((M 'M-4'), '5'))" 'True,False'
Check 'rcs: standard looks' "$($rcsT.DefaultLook((M 'M-4'))),$($rcsT.DefaultLook((M 'B-1'))),$($rcsT.DefaultLook((M 'B-5')))" 'RCSBlock.v2,nflv-rcs-heavy-1x-2,rcsblock-orbital-5way-1'

Write-Host 'Looks step 3: engines, at their own size only'
Check 'every type has looks now' (($Types.GetField('All').GetValue($null) | Where-Object { -not $_.HasLooks }).Count) 0
Check 'engines are never scaled' (($Types.GetField('Engines').GetValue($null) | ForEach-Object { $_.ScaleBasis($false, '') }) -join ',') 'None,None,None,None,None,None'
Check 'liquid: a look per fuel' "$($liq.LookKey((M 'H-25-V')))|$($liq.LookKeys('25') -join ',')" 'H|K,H,M'
Check 'liquid: only its own size' "$($liq.LookFits((M 'K-25-S'), '25')),$($liq.LookFits((M 'K-25-S'), '125'))" 'True,False'
Check 'liquid: aerospike looks for aerospikes only' "$($liq.LookFits((M 'K-125-A'), '125-A')),$($liq.LookFits((M 'K-125-A'), '125')),$($liq.LookFits((M 'K-125-S'), '125-A'))" 'True,False,False'
Check 'liquid: standard looks' "$($liq.DefaultLook((M 'K-25-S'))),$($liq.DefaultLook((M 'H-125-V'))),$($liq.DefaultLook((M 'K-375-A')))" 'liquidEngineMainsail.v2,liquidEngine3.v2,toroidalAerospike'
$srb = TypeOf 'Solid'
Check 'solid: size and length' "$($srb.LookFits((M '125-M'), '125-M')),$($srb.LookFits((M '125-M'), '125-L'))" 'True,False'
Check 'solid: length by nearest load' "$([PIP.SolidMotors]::LengthOf('125', 820)),$([PIP.SolidMotors]::LengthOf('125', 140)),$([PIP.SolidMotors]::LengthOf('125', 2600)),$([PIP.SolidMotors]::LengthOf('25', 8000))" 'M,S,L,M'
Check 'solid: a look gets the program load' ([PIP.SolidMotors]::UnitsFor('1875', 'M')) 2767.5
Check 'monoprop: own size' "$($mon.LookFits((M '125'), '125')),$($mon.DefaultLook((M '375')))" 'True,orbital-engine-375'
Check 'nuclear: either core at the size' "$($nuc.LookFits((M 'G-25'), '25')),$($nuc.LookFits((M 'S-25'), '25')),$($nuc.LookFits((M 'S-25'), '125'))" 'True,True,False'
Check 'electric: gas decides the type' "$([PIP.ElectricEngines]::TypeForGas('XenonGas')),$([PIP.ElectricEngines]::TypeForGas('ArgonGas')),$([PIP.ElectricEngines]::TypeForGas('Lithium'))" 'I,H,M'
Check 'electric: type and size' "$($ele.LookFits((M 'H-0625'), 'H-0625')),$($ele.LookFits((M 'H-0625'), 'I-0625'))" 'True,False'
Check 'jet: modes must match' "$($jet.LookFits((M 'J-125'), '125-2')),$($jet.LookFits((M 'F-125'), '125-1')),$($jet.LookFits((M 'R-125'), '125-1')),$($jet.LookFits((M 'C-125'), '125-1'))" 'True,True,True,False'
Check 'step-1 types keep one look part' "[$($bat.LookKey((M '')))][$($bat.LookKeys('') -join ',')]" '[][]'

Write-Host 'Programs and saves'
$p = Call $Program Create @('abc123', 'Kestrel', $liq, $k125, [int[]]@(25, 120, -5, 50, 10), [double]0, [double]18000)
Check 'points are clamped to 0..100' ($p.Points -join ',') '25,100,0,50,10'
Check 'part name' $p.PartName 'PIP.abc123'
Check 'chassis for a liquid' $p.ChassisPart 'PIP-K-125-S'
function Key([uint32]$launch) { $Program.GetMethod('FlightKey').Invoke($null, @($launch)) }
Check 'a flight counts once, however many parts' "$($p.RecordLaunch((Key 7))),$($p.RecordLaunch((Key 7)))" 'True,False'
Check 'a relaunch recovers again (same vessel, new launch)' "$($p.RecordRecovery((Key 8))),$($p.RecordRecovery((Key 9))),$($p.RecordRecovery((Key 9)))" 'True,True,False'
[void]$p.RecordLaunch((Key 8)); [void]$p.RecordLaunch((Key 9))
Check 'xp: 3 flights x1, 2 recoveries x3' $p.Xp 9
Check 'display name with version' $p.DisplayName() 'Kestrel I Mk. 3'
$p.Retired = $true
$values = $p.ToValues(); $map = @{}; foreach ($kv in $values) { $map[$kv.Key] = $kv.Value }
$back = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $map[$k] }))
Check 'save keeps the type' $back.Type.Id 'liquid'
Check 'save keeps the model' $back.ModelCode 'K-125-S'
Check 'save keeps points' ($back.Points -join ',') '25,100,0,50,10'
Check 'save keeps experience' $back.Xp 9
Check 'save keeps counted launches' ($back.RecordLaunch((Key 7))) $false
Check 'save keeps counted recoveries' ($back.RecordRecovery((Key 9))) $false
Check 'save keeps retired' $back.Retired $true
$map.Remove('type')
$old = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $map[$k] }))
Check 'a save from before types loads as liquid' $old.Type.Id 'liquid'
$j = Call $Program Create @('j1', 'Swift', $jet, (M 'J-125'), [int[]]@(50, 50, 50, 50), [double]0, [double]0)
$jm = @{}; foreach ($kv in $j.ToValues()) { $jm[$kv.Key] = $kv.Value }
$jb = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $jm[$k] }))
Check 'a jet program round-trips' "$($jb.Type.Id) $($jb.ModelCode) $($jb.Points -join ',')" 'jet J-125 50,50,50,50,0'

Write-Host 'Versions (section 18.1)'
$v1 = Call $Program Create @('v1', 'Kestrel', $liq, $k125, [int[]]@(25, 25, 25, 25, 25), [double]0, [double]0)
$v1.Gimbal = '5'
Check 'a first version is I' "$($v1.Title) $($v1.Version)" 'Kestrel I 1'
Check 'next points: +10 each' ($v1.NextPoints(100) -join ',') '35,35,35,35,35'
Check 'never past the research cap' ($v1.NextPoints(25) -join ',') '25,25,25,25,25'
Check 'at the cap: waiting on research' "$($v1.CanImprove(25)) $($v1.WaitingOnResearch(25))" 'False True'
Check 'never past 100' (((Call $Program Create @('x', 'X', $liq, $k125, [int[]]@(95, 100, 0, 50, 50), [double]0, [double]0)).NextPoints(100)) -join ',') '100,100,10,60,60'
$v2 = $v1.NextVersion('v2', 100, [double]0, [double]0)
Check 'the next version is II' "$($v2.Title) $($v2.Version) $($v2.PredecessorId)" 'Kestrel II 2 v1'
Check 'it keeps the design and gimbal' "$($v2.ModelCode) $($v2.Gimbal)" 'K-125-S 5'
Check 'II at Mk. 1 = I at Mk. 5' ($v2.Stats(1, (Level Normal)).Item('thrustVac')) ($v1.Stats(5, (Level Normal)).Item('thrustVac')) 1e-9
Check 'failures start over' ($v2.Stats(1, (Level Normal)).Item('fail')) 0.10
Check 'II at Mk. 1 weighs what I did at Mk. 5' ($v2.Stats(1, (Level Normal)).Item('mass')) ($v1.Stats(5, (Level Normal)).Item('mass')) 1e-9
Check 'II at Mk. 1 costs what I did at Mk. 5' ($v2.Stats(1, (Level Normal)).Item('price')) ($v1.Stats(5, (Level Normal)).Item('price')) 1e-6
Check 'II at Mk. 5 uses its own points' ($v2.Stats(5, (Level Normal)).Item('thrustVac')) (Call $Liquid ThrustAt @($k125, [double]35))
$vm = Call $Program Create @('vm', 'Kestrel', $liq, $k125, [int[]]@(100, 100, 100, 100, 100), [double]0, [double]0)
Check 'every slider at 100 is Max' "$($vm.Title) $($vm.IsMax) $($vm.CanImprove(100))" 'Kestrel Max True False'
Check 'roman numerals' "$([PIP.Program]::Roman(4)) $([PIP.Program]::Roman(9)) $([PIP.Program]::Roman(12))" 'IV IX XII'
$vmap = @{}; foreach ($kv in $v2.ToValues()) { $vmap[$kv.Key] = $kv.Value }
$vb = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $vmap[$k] }))
Check 'a version round-trips' "$($vb.Version) $($vb.StartPoints -join ',') $($vb.PredecessorId) $($vb.Gimbal)" '2 25,25,25,25,25 v1 5'
$b = Call $Program Create @('b1', 'Volta', $bat, (M ''), [int[]]@(40), [double]0, [double]0)
Check 'side part name' $b.SidePartName 'PIP.b1.side'
$b.Look = 'battery-125'; $b.SideLook = 'ksp.r.largeBatteryPack'
$bm = @{}; foreach ($kv in $b.ToValues()) { $bm[$kv.Key] = $kv.Value }
$bb = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $bm[$k] }))
Check 'a save keeps the looks' "$($bb.Look) $($bb.SideLook)" 'battery-125 ksp.r.largeBatteryPack'
Check 'a program with no look saves none' ($j.ToValues() | Where-Object { $_.Key -like '*ook' }).Count 0
Check 'a save from before looks has none' "[$($jb.Look)][$($jb.SideLook)]" '[][]'
Check 'side stats use the side size' ($bb.Stats(10, (Level Normal), 'R-L').Item('mass')) 0.04
Check 'an older program covers every size' "$($b.FixedSize) $($b.Label)" 'False Batteries, every size'
$b.Variant = 'R-M'
$bm = @{}; foreach ($kv in $b.ToValues()) { $bm[$kv.Key] = $kv.Value }
$bs = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $bm[$k] }))
Check 'a program keeps its size' "$($bs.Variant) $($bs.FixedSize) $($bs.IsSide)" 'R-M True True'
Check 'a sized program is labelled with it' $bs.Label 'Batteries, side-mount, medium'
Check 'a sized program computes at its size' ($bs.Stats(10, (Level Normal)).Item('mass')) 0.02
$bm['size'] = 'nonsense'
$bx = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $bm[$k] }))
Check 'an unknown size is dropped' $bx.FixedSize $false
Check 'a size needs its research' (($bat.Techs((M ''), '1875')) -join ',') 'start,heavyRocketry'
Check 'tank label with size' "$((TypeOf 'Tank').VariantLabel((M 'S'), '25-X'))" '2.5 m extra long'
Check 'a bad model does not load' ($Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) @{ id = 'x'; type = 'nuclear'; model = 'G-0625' }[$k] }))) $null

Write-Host 'Tooltip and summary text'
$PartInfo = T 'PartInfo'; $Comparison = T 'Comparison'
$pts = [int[]]@(25, 25, 25, 25, 10)
$s0 = $liq.Compute($k125, $pts, 0, (Level Normal))
$plain = "Mass: 1.5 t`nThrust (ASL): 167.969 kN`nThrust (Vac.): 215.0 kN"
$out = $PartInfo.GetMethod('Stats').Invoke($null, @($plain, $s0))
Check 'tooltip mass rewritten' ($out -match 'Mass: 1\.6 t') $true
Check 'tooltip sea-level thrust rewritten' ($out -match 'Thrust \(ASL\): 132\.786 kN') $true
Check 'tooltip vacuum thrust rewritten' ($out -match 'Thrust \(Vac\.\): 147\.5 kN') $true
$sum = $Comparison.GetMethod('Summary').Invoke($null, @('Kestrel', $liq, $k125, [int[]]@(25, 25, 25, 25, 10), (Level Normal)))
Check 'summary line' $sum 'Kestrel: Built for the pad. No standout strengths. Under 3% failures from Mk. 4.'
$sum2 = $Comparison.GetMethod('Summary').Invoke($null, @('Talon', $liq, (M 'K-125-V'), [int[]]@(0, 60, 0, 25, 0), (Level Off)))
Check 'summary for a vacuum engine' $sum2 'Talon: An upper-stage engine, nearly useless at sea level. Strong on efficiency. Neglects weight. Failures are off in this game.'
$sum3 = $Comparison.GetMethod('Summary').Invoke($null, @('Swift', $jet, (M 'F-125'), [int[]]@(60, 25, 0, 25), (Level Normal)))
Check 'summary for a jet (no failure line)' $sum3 'Swift: Efficient and subsonic. Strong on thrust. Neglects weight.'


Write-Host 'Fuel table (section 19)'
$FuelTable = T 'FuelTable'; $FuelDef = T 'FuelDef'
Check 'default fuels' (($FuelTable::All | ForEach-Object { $_.Code }) -join ',') 'K,H,M'
Check 'stock mixes are liquid engines' "$($FuelTable::IsLiquidMix([string[]]@('LiquidFuel', 'Oxidizer'))),$($FuelTable::IsLiquidMix([string[]]@('LqdHydrogen', 'Oxidizer', 'ElectricCharge')))" 'True,True'
Check 'monopropellant and a lone fuel are not' "$($FuelTable::IsLiquidMix([string[]]@('MonoPropellant'))),$($FuelTable::IsLiquidMix([string[]]@('LqdHydrogen')))" 'False,False'
Check 'an unknown mix is not, until a fuel burns it' $FuelTable::IsLiquidMix([string[]]@('Aerozine50', 'NTO')) $false
Check 'a fuel is found by its resources, pumps aside' $FuelTable::ForResources([string[]]@('Oxidizer', 'LqdMethane', 'ElectricCharge')).Code 'M'

$a = [Activator]::CreateInstance($FuelDef)
$a.Code = 'A'; $a.Title = 'Aerozine'; $a.Tech = 'advFuelSystems'
$a.Resources = [string[]]@('Aerozine50', 'NTO'); $a.Ratios = [double[]]@(2.29, 2.71)
$a.Isp = [double[][]]@([double[]]@(212, 298, 234, 324), [double[]]@(183, 274, 258, 343), [double[]]@(48, 96, 286, 367), [double[]]@(231, 294, 267, 334))
$a.Thrust = 1.05; $a.Twr = 25; $a.Price = 1.1; $a.FailHi = 0.006; $a.FailLo = 0.0002
$bad = [Activator]::CreateInstance($FuelDef); $bad.Code = 'X-1'
$list = [Activator]::CreateInstance([Collections.Generic.List`1].MakeGenericType($FuelDef))
foreach ($f in @($FuelTable::Defaults()) + $a + $bad) { $list.Add($f) }
$FuelTable::Set($list)
Check 'a fuel joins the table; a code with "-" is refused' (($FuelTable::All | ForEach-Object { $_.Code }) -join ',') 'K,H,M,A'
Check 'the engine fuel choice follows the table' (($liq.Choices[0].Options | ForEach-Object { $_.Code }) -join ',') 'K,H,M,A'
Check 'its tech node' ($liq.Choices[0].Find('A').Tech) 'advFuelSystems'
Check 'a look per fuel, the new one too' ($liq.LookKeys('125') -join ',') 'K,H,M,A'
Check 'its mix is now a liquid engine' $FuelTable::IsLiquidMix([string[]]@('NTO', 'Aerozine50')) $true
Check 'Isp from its windows' "$(Call $Liquid IspSLAt @((M 'A-125-S'), [double]100)),$(Call $Liquid IspVacAt @((M 'A-125-V'), [double]100))" '298,367'
Check 'thrust from its factor' (Call $Liquid ThrustAt @((M 'A-125-S'), [double]100)) 577.5
Check 'price from its factor' ($liq.BuildPriceAt((M 'A-125-S'), [int[]]@(0, 0, 0, 0), 5) / $liq.BuildPriceAt($k125, [int[]]@(0, 0, 0, 0), 5)) 1.1 1e-9
Check 'label keeps a name with capitals' "$($liq.Label((M 'A-125-S')))|$((T 'Fmt')::Lower('FLOX')),$((T 'Fmt')::Lower('ClF5')),$((T 'Fmt')::Lower('Nitric acid'))" '1.25 m aerozine, sea level nozzle|FLOX,ClF5,nitric acid'
Check 'an unknown code falls back to kerolox' (Call $Liquid IspSLAt @((M 'Q-125-S'), [double]100)) 310
$FuelTable::Reset()
Check 'reset to the defaults' (($liq.Choices[0].Options | ForEach-Object { $_.Code }) -join ',') 'K,H,M'

# Fuels.cfg (Tools\Generate-Fuels.ps1) agrees with the code's defaults and with itself.
$cfg = Get-Content (Join-Path $PSScriptRoot '..\GameData\PIP\Fuels.cfg') -Raw
$blocks = [regex]::Matches($cfg, '(?ms)^PIP_FUEL\s*\{(.*?)^\}') | ForEach-Object { $_.Groups[1].Value }
function Val($b, $k) { if ($b -match "(?m)^\s*$k = (.*)$") { $Matches[1].Trim() } }
Check 'Fuels.cfg: fuels in order' (($blocks | ForEach-Object { Val $_ 'code' }) -join ',') 'K,E,N,P,A,H,S,M,F,C,L'
$types = [regex]::Matches($cfg, '(?m)^B9_TANK_TYPE\s*\{\s*name = (\S+)') | ForEach-Object { $_.Groups[1].Value }
$problems = @()
foreach ($b in $blocks) {
    $code = Val $b 'code'
    foreach ($n in 'S', 'B', 'V', 'A') { $w = (Val $b $n) -split ',\s*'; if ($w.Count -ne 4 -or [double]$w[1] -lt [double]$w[0] -or [double]$w[3] -lt [double]$w[2]) { $problems += "$code ISP $n" } }
    if ($b -match 'tankType = (\S+)' -and $types -notcontains $Matches[1]) { $problems += "$code tank $($Matches[1])" }
    $def = $FuelTable::Defaults() | Where-Object { $_.Code -eq $code }
    if ($def) {
        $isp = ('S', 'B', 'V', 'A' | ForEach-Object { Val $b $_ }) -join ' | '
        $mine = ($def.Isp | ForEach-Object { $_ -join ', ' }) -join ' | '
        if ($isp -ne $mine -or [double](Val $b 'twr') -ne $def.Twr -or [double](Val $b 'price') -ne $def.Price -or [double](Val $b 'thrust') -ne $def.Thrust) { $problems += "$code differs from Core/Fuels.cs" }
    }
}
Check 'Fuels.cfg: windows rise with points, tanks exist, K/H/M match the code' ($problems -join '; ') ''

# Section 19.6: tanks weigh by size, and filling a tank costs at most ~4x kerolox.
$tanks = @{}
foreach ($m in [regex]::Matches($cfg, '(?ms)^B9_TANK_TYPE\s*\{\s*name = (\S+)\s*tankMass = (\S+)(.*?)^\}')) {
    $units = @{}; foreach ($r in [regex]::Matches($m.Groups[3].Value, 'name = (\S+)\s*unitsPerVolume = (\S+)')) { $units[$r.Groups[1].Value] = [double]$r.Groups[2].Value }
    $tanks[$m.Groups[1].Value] = @{ mass = [double]$m.Groups[2].Value; units = $units }
}
$prices = @{ LiquidFuel = 0.8; Oxidizer = 0.18; LqdHydrogen = 0.03675; LqdMethane = 0.45; Ethanol75 = 0.12624; Syntin = 0.4255 }
foreach ($m in [regex]::Matches($cfg, '(?ms)^@RESOURCE_DEFINITION\[(\S+)\]:NEEDS\[!RealFuels\]:FINAL\s*\{\s*@unitCost = (\S+)')) { $prices[$m.Groups[1].Value] = [double]$m.Groups[2].Value }
function Fill($type) { $s = 0; foreach ($kv in $tanks[$type].units.GetEnumerator()) { $s += 400 * $kv.Value * $prices[$kv.Key] }; [Math]::Round($s) }
Check 'storable tanks weigh by size, like stock' (($tanks.Keys | Where-Object { $_ -ne 'PIP-L-MIX' } | ForEach-Object { $tanks[$_].mass } | Sort-Object -Unique) -join ',') '0.000625'
Check 'a hydrogen mix weighs as CryoTanks hydrolox' $tanks['PIP-L-MIX'].mass 0.000278904
Check 'ClF5 carries 12 tons per ton of tank' ((2.0785 * 1.004 + 2.9215 * 1.9) * 5 / 5 / 0.625 / 1) 12.2 0.05
Check 'filling an FL-T400: nitric, peroxide, Aerozine, FLOX, ClF5, fluorine' "$(Fill 'PIP-N-MIX'),$(Fill 'PIP-P-MIX'),$(Fill 'PIP-A-MIX'),$(Fill 'PIP-F-MIX'),$(Fill 'PIP-C-MIX'),$(Fill 'PIP-L-MIX')" '220,260,370,550,650,740'
Check 'nothing costs more than ~4x kerolox to fill' (($tanks.Keys | ForEach-Object { Fill $_ } | Measure-Object -Maximum).Maximum -le 4.1 * 184) $true
Check 'a Jumbo-64 of ClF5' ((Fill 'PIP-C-MIX') * 16) 10400

Write-Host 'Plumes (section 19.4)'
$Plumes = T 'Plumes'; $PlumeSet = T 'PlumeSet'
function Set-Plume($code, [string[]]$match, $roles) {
    $s = [Activator]::CreateInstance($PlumeSet); $s.Match = $match
    foreach ($k in $roles.Keys) { $s.Roles[$k] = [string[]]$roles[$k] }
    $Plumes::ByFuel[$code] = $s
}
$Plumes::ByFuel.Clear()
Set-Plume 'H' @('hydrolox') @{ lower = @('waterfall-hydrolox-lower-1'); upper = @('waterfall-hydrolox-upper-1'); core = @('stock-hydrolox-core-1') }
Set-Plume 'K' @('kerolox') @{ lower = @('waterfall-kerolox-lower-1'); upper = @('waterfall-kerolox-upper-1'); core = @('stock-kerolox-core') }
Set-Plume 'E' @('alcolox', 'ethalox') @{ lower = @('waterfall-alcolox-lower-1') }
$all = [Func[string, bool]] { param($n) $true }
$noSwe = [Func[string, bool]] { param($n) -not $n.StartsWith('stock-') }
function SW($t, $f, $i) { $r = $Plumes::Swap($t, $f, $i); if ($null -eq $r) { "<keep>" } elseif ($r -eq "") { "<drop>" } else { $r } }
Check 'roles' "$($Plumes::Role('waterfall-kerolox-lower-5-SWE')),$($Plumes::Role('waterfall-kerolox-upper-1')),$($Plumes::Role('stock-kerolox-core')),$($Plumes::Role('stock-kerolox-generator')),$($Plumes::Role('waterfall-kerolox-exhaust-1'))" 'lower,upper,core,generator,generator'
Check 'Mainsail plume, burning hydrolox' (SW 'waterfall-kerolox-lower-5-SWE' 'H' $all) 'waterfall-hydrolox-lower-1'
Check 'its bell glow too' (SW 'stock-kerolox-core' 'H' $all) 'stock-hydrolox-core-1'
Check 'a vacuum plume stays vacuum' (SW 'waterfall-kerolox-upper-1' 'H' $all) 'waterfall-hydrolox-upper-1'
Check 'already its own fuel: kept' (SW 'waterfall-kerolox-lower-5-SWE' 'K' $all) "<keep>"
Check 'no glow template installed: the glow goes' (SW 'stock-kerolox-core' 'H' $noSwe) "<drop>"
Check 'no template for a role: the original stays' (SW 'stock-kerolox-generator' 'H' $all) "<keep>"
Check 'lights and other effects untouched' (SW 'waterfall-floodlight-1' 'H' $all) "<keep>"
Check 'aerospikes keep theirs' (SW 'stock-hydrolox-aerospike' 'K' $all) "<keep>"
Check 'a fuel with no PLUME node changes nothing' (SW 'waterfall-kerolox-lower-1' 'Q' $all) "<keep>"
Check 'a fuel with several match words' (SW 'waterfall-ethalox-sustainer-1' 'E' $all) "<keep>"
# Sizes: a vernier template is drawn ~4x bigger per meter than a main plume (the
# Goldfish's own plume swapped 1:1 came out several meters wide, 2026-11-05).
$Plumes::SizePerMeter.Clear()
Check 'a vernier swapped to a main plume shrinks' ($Plumes::Rescale('waterfall-kerolox-vernier-2', 'waterfall-hypergolic-aerozine50-lower-1')) (0.55 / 2.4) 1e-9
$Plumes::SizePerMeter['waterfall-kerolox-lower-5'] = 1.0; $Plumes::SizePerMeter['waterfall-hydrolox-lower-3'] = 0.293
Check 'measured sizes win over typical ones' ($Plumes::Rescale('waterfall-kerolox-lower-5', 'waterfall-hydrolox-lower-3')) 0.293 1e-9
Check 'a vacuum plume typical size' ($Plumes::Size('waterfall-anything-upper-9')) 0.7
$l = New-Object 'System.Collections.Generic.List[double]'; foreach ($x in 0.4, 2.4, 0.8, 0.6) { $l.Add($x) }
Check 'median of measured sizes' ($Plumes::Median($l)) 0.7 1e-9
$Plumes::SizePerMeter.Clear()
$Plumes::ByFuel.Clear()

# Fuels.cfg names a plume for every fuel, and every role it names is one PIP knows.
$plumeBlocks = [regex]::Matches($cfg, '(?ms)^\s+PLUME\s*\{(.*?)^\s+\}') | ForEach-Object { $_.Groups[1].Value }
Check 'Fuels.cfg: a plume per fuel' $plumeBlocks.Count 11
$keys = $plumeBlocks | ForEach-Object { [regex]::Matches($_, '(?m)^\s*(\w+) =') | ForEach-Object { $_.Groups[1].Value } } | Sort-Object -Unique
Check 'Fuels.cfg: plume keys' ($keys -join ',') 'core,generator,lower,match,upper'
Write-Host ''
if ($script:failed -eq 0) { Write-Host "All $script:passed checks passed." -ForegroundColor Green } else { Write-Host "$script:failed failed, $script:passed passed." -ForegroundColor Red; exit 1 }
