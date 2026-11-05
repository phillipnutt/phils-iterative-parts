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

Write-Host 'Mk progression'
Check 'xp for Mk. 1' (Call $Progress XpForMk @(1)) 1
Check 'xp for Mk. 4' (Call $Progress XpForMk @(4)) 7
Check 'xp for Mk. 10' (Call $Progress XpForMk @(10)) 143
Check 'Mk at 6 xp' (Call $Progress Mk @(6)) 3
Check 'Mk at 7 xp' (Call $Progress Mk @(7)) 4
Check 'Mk at 143 xp' (Call $Progress Mk @(143)) 10
Check 'Mk never passes 10' (Call $Progress Mk @(5000)) 10
Check 'share at Mk. 0' (Call $Progress Share @(0)) 0.5
Check 'share at Mk. 4' (Call $Progress Share @(4)) 0.8832 1e-3
Check 'share at Mk. 10' (Call $Progress Share @(10)) 1.0

Write-Host 'Program cost: 1,000 and 1% per point'
$normal = [Enum]::Parse($PriceT, 'Normal')
Check 'bare program' (Call $Pricing ProgramCost @(0, [double]1, $normal)) 1000
Check 'start caps (125)' (Call $Pricing ProgramCost @(125, [double]1, $normal)) 3468.7 1
Check 'stock level (250)' (Call $Pricing ProgramCost @(250, [double]1, $normal)) 12032.3 1
Check 'maxed (500)' (Call $Pricing ProgramCost @(500, [double]1, $normal)) 144772.7 1
Check '70 points doubles it' ((Call $Pricing ProgramCost @(170, [double]1, $normal)) / (Call $Pricing ProgramCost @(100, [double]1, $normal))) 2.0068 1e-3
Check 'solid factor x0.5' (Call $Pricing ProgramCost @(0, [double]0.5, $normal)) 500
Check 'PIP prices: quarter' (Call $Pricing ProgramCost @(250, [double]1, [Enum]::Parse($PriceT, 'Quarter'))) 3008.1 1
Check 'PIP prices: double' (Call $Pricing ProgramCost @(0, [double]2, [Enum]::Parse($PriceT, 'Double'))) 4000
Check 'type factors' "$($sol.PriceFactor),$($mon.PriceFactor),$($liq.PriceFactor),$($jet.PriceFactor),$($ele.PriceFactor),$($nuc.PriceFactor)" '0.5,0.5,1,1,1.5,2'

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
Check 'gimbal 10 points' (Call $Liquid GimbalAt @($k125, [double]10)) 4
Check 'gimbal 100 points' (Call $Liquid GimbalAt @($k125, [double]100)) 20
Check 'aerospike has no gimbal' (Call $Liquid GimbalAt @((M 'K-125-A'), [double]100)) 0
Check 'failure at Mk. 0' ($liq.FailureAt($k125, 50, 0, (Level Normal))) 0.10
Check 'failure at Mk. 10, 50 points' ($liq.FailureAt($k125, 50, 10, (Level Normal))) 0.004472 1e-5
Check 'methalox mature failure, 100 points' ($liq.FailureAt((M 'M-125-S'), 100, 10, (Level Normal))) 0.0005
Check 'light failures are half' ($liq.FailureAt($k125, 50, 0, (Level Light))) 0.05
Check 'build price, 100 points, Mk. 0' ($liq.BuildPriceAt($k125, [int[]]@(20, 20, 20, 20, 20), 0)) 1080
Check 'build price, 100 points, Mk. 10' ($liq.BuildPriceAt($k125, [int[]]@(20, 20, 20, 20, 20), 10)) 1134
Check 'aerospike builds at x2' ($liq.BuildPriceAt((M 'K-125-A'), [int[]]@(20, 20, 20, 20, 20), 0)) 2160
$pts = [int[]]@(25, 25, 25, 25, 10)
Check 'Mk. 0 thrust uses half the points' (Stat $liq 'K-125-S' $pts 0 'thrustVac') 147.5
Check 'Mk. 10 thrust uses all of them' (Stat $liq 'K-125-S' $pts 10 'thrustVac') 205
Check 'Mk. 0 weight' (Stat $liq 'K-125-S' $pts 0 'mass') 1.5999 1e-3
Check 'aerospike gimbal category unused' ($liq.CategoryUsed((M 'K-125-A'), 4)) $false

Write-Host 'Solid boosters'
$max = [int[]]@(100, 100, 100, 100, 100); $zero = [int[]]@(0, 0, 0, 0, 0)
Check '1.25 m medium, 100 points' (Stat $sol '125-M' $max 10 'thrustVac') 480
Check '1.25 m long is x2' (Stat $sol '125-L' $max 10 'thrustVac') 960
Check '1.25 m short is x0.6' (Stat $sol '125-S' $zero 10 'thrustVac') 72
Check 'Thumper-size propellant' (Stat $sol '125-M' $zero 0 'propellant') 6.15
Check 'casing at 0 points' (Stat $sol '125-M' $zero 0 'mass') (6.15 * 0.27) 1e-6
Check 'casing at 100 points' (Stat $sol '125-M' $max 10 'mass') (6.15 * 0.13) 1e-6
Check 'Isp window' "$(Stat $sol '125-M' $zero 0 'ispSL')/$(Stat $sol '125-M' $max 10 'ispVac')" '150/265'
Check 'gimbal fixed at 0 points' (Stat $sol '125-M' $zero 0 'gimbal') 0
Check 'gimbal 8 degrees at 100' (Stat $sol '125-M' $max 10 'gimbal') 8
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
Check 'gimbal fixed at 3 with Kerbalism' (Stat $nuc 'S-125' $zero 0 'gimbal') 3
Check 'fifth category is Shielding' ($nuc.Categories[4].Label) 'Shielding'
$Nuclear.GetField('Kerbalism').SetValue($null, $false)
Check 'fifth category is Gimbal without Kerbalism' ($nuc.Categories[4].Label) 'Gimbal'
Check 'gimbal from points without Kerbalism' (Stat $nuc 'S-125' $max 10 'gimbal') 20

Write-Host 'Electric engines'
Check 'ion 0.625 m, 100 points' (Stat $ele 'I-0625' $max 10 'thrustVac') 5
Check 'power: kN x Isp/1000 x 2.5 at 100' (Stat $ele 'I-0625' $max 10 'ecRate') (5 * 12 * 2.5) 1e-6
Check 'power: factor 6 at 0' (Stat $ele 'H-0625' $zero 0 'ecRate') (1 * 1.8 * 6) 1e-6
Check 'MPD 2.5 m ceiling' (Stat $ele 'M-25' $max 10 'thrustVac') 320

Write-Host 'Jet engines'
$jmax = [int[]]@(100, 100, 100, 100); $jzero = [int[]]@(0, 0, 0, 0)
Check 'four categories' $jet.Categories.Length 4
Check 'jets never fail' (Stat $jet 'F-125' $jmax 0 'fail') 0
Check 'turbojet dry thrust is two-thirds' (Stat $jet 'J-125' $jmax 10 'thrustDry') (200 * 2 / 3) 1e-6
Check 'combined rocket thrust x1.7' (Stat $jet 'C-125' $jmax 10 'rocketThrust') 340
Check 'ramjet top speed at 0 points' (Stat $jet 'R-125' $jzero 0 'topMach') 4.4 1e-9
Check 'ramjet top speed at 100 points' (Stat $jet 'R-125' $jmax 10 'topMach') 6.6 1e-9

Write-Host 'Part types (size on the part)'
function PStat($name, [string]$model, [int[]]$pts, [int]$mk, $key, $variant) { $t = TypeOf $name; $m = $(if ($model) { M $model } else { , [string[]]@() }); ($t.Compute($m, $pts, $mk, (Level 'Normal'), $variant)).Item($key) }
Check 'sixteen types' $Types.GetField('All').GetValue($null).Length 16
Check 'battery: stock capacity at ~45 points' (PStat 'Battery' '' @(45) 10 'capacity' '125') 20100
Check 'battery: 1.25 m holds 600 EC at 0 points' (PStat 'Battery' '' @(0) 0 'ec' '125') 600
Check 'battery: 1.25 m holds 1,500 EC at 100' (PStat 'Battery' '' @(100) 10 'ec' '125') 1500
Check 'battery: one category, maxed costs 2.7k' (Call $Pricing ProgramCost @(100, [double]1, $normal)) 2704.8 1
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
Check 'tank: insulation only for cryogenic' "$((TypeOf 'Tank').CategoryUsed((M 'S'), 2)),$((TypeOf 'Tank').CategoryUsed((M 'C'), 2))" 'False,True'
Check 'tank: 24 size-and-length variants' ((TypeOf 'Tank').Variants((M 'S')).Length) 24
Check 'rtg: large output at 100' (PStat 'Rtg' '' @(100, 0) 10 'ecRate' 'L') 4.5 1e-9
Check 'intake: hypersonic top speed at 100' (PStat 'Intake' 'H' @(0, 100, 0) 10 'topMach' '125') 9.6 1e-9
Check 'rcs: 4-way mono weight at 100' (PStat 'Rcs' 'M-4' @(100, 0, 100) 10 'mass' $null) (12 / (12.5 * 9.80665)) 1e-9
Check 'part types have no failures' (PStat 'Rcs' 'M-4' @(100, 0, 100) 0 'fail' $null) 0

Write-Host 'Side-mount parts and looks (sections 15, 16)'
$bat = TypeOf 'Battery'; $whl = TypeOf 'Wheel'; $int = TypeOf 'Intake'; $rtg = TypeOf 'Rtg'
Check 'side-mount types' "$($bat.HasSideMount),$($whl.HasSideMount),$($int.HasSideMount),$($rtg.HasSideMount),$($liq.HasSideMount)" 'True,True,True,False,False'
Check 'battery side sizes' (($bat.SideVariants((M '')) | ForEach-Object { $_.Code }) -join ',') 'R-S,R-M,R-L'
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
Check 'types with looks' (($Types.GetField('All').GetValue($null) | Where-Object { $_.HasLooks } | ForEach-Object { $_.Id }) -join ',') 'liquid,solid,monoprop,nuclear,electric,jet,rcs,battery,wheel,shield,solar,antenna,probe,tank,rtg,intake'

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
Check 'first launch counts' ($p.RecordLaunch([uint32]7)) $true
Check 'same launch counts once' ($p.RecordLaunch([uint32]7)) $false
[void]$p.RecordRecovery([uint32]99); [void]$p.RecordLaunch([uint32]8); [void]$p.RecordLaunch([uint32]9); [void]$p.RecordLaunch([uint32]10)
Check 'xp from 4 launches and 1 recovery' $p.Xp 5
Check 'display name' $p.DisplayName() 'Kestrel Mk. 3'
$p.Retired = $true
$values = $p.ToValues(); $map = @{}; foreach ($kv in $values) { $map[$kv.Key] = $kv.Value }
$back = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $map[$k] }))
Check 'save keeps the type' $back.Type.Id 'liquid'
Check 'save keeps the model' $back.ModelCode 'K-125-S'
Check 'save keeps points' ($back.Points -join ',') '25,100,0,50,10'
Check 'save keeps experience' $back.Xp 5
Check 'save keeps counted launches' ($back.RecordLaunch([uint32]8)) $false
Check 'save keeps retired' $back.Retired $true
$map.Remove('type')
$old = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $map[$k] }))
Check 'a save from before types loads as liquid' $old.Type.Id 'liquid'
$j = Call $Program Create @('j1', 'Swift', $jet, (M 'J-125'), [int[]]@(50, 50, 50, 50), [double]0, [double]0)
$jm = @{}; foreach ($kv in $j.ToValues()) { $jm[$kv.Key] = $kv.Value }
$jb = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $jm[$k] }))
Check 'a jet program round-trips' "$($jb.Type.Id) $($jb.ModelCode) $($jb.Points -join ',')" 'jet J-125 50,50,50,50'
$b = Call $Program Create @('b1', 'Volta', $bat, (M ''), [int[]]@(40), [double]0, [double]0)
Check 'side part name' $b.SidePartName 'PIP.b1.side'
$b.Look = 'battery-125'; $b.SideLook = 'ksp.r.largeBatteryPack'
$bm = @{}; foreach ($kv in $b.ToValues()) { $bm[$kv.Key] = $kv.Value }
$bb = $Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) $bm[$k] }))
Check 'a save keeps the looks' "$($bb.Look) $($bb.SideLook)" 'battery-125 ksp.r.largeBatteryPack'
Check 'a program with no look saves none' ($j.ToValues() | Where-Object { $_.Key -like '*ook' }).Count 0
Check 'a save from before looks has none' "[$($jb.Look)][$($jb.SideLook)]" '[][]'
Check 'side stats use the side size' ($bb.Stats(10, (Level Normal), 'R-L').Item('mass')) 0.04
Check 'a bad model does not load' ($Program.GetMethod('FromValues').Invoke($null, @([Func[string, string]] { param($k) @{ id = 'x'; type = 'nuclear'; model = 'G-0625' }[$k] }))) $null

Write-Host 'Tooltip and summary text'
$PartInfo = T 'PartInfo'; $Comparison = T 'Comparison'
$s0 = $liq.Compute($k125, $pts, 0, (Level Normal))
$plain = "Mass: 1.5 t`nThrust (ASL): 167.969 kN`nThrust (Vac.): 215.0 kN"
$out = $PartInfo.GetMethod('Stats').Invoke($null, @($plain, $s0))
Check 'tooltip mass rewritten' ($out -match 'Mass: 1\.6 t') $true
Check 'tooltip sea-level thrust rewritten' ($out -match 'Thrust \(ASL\): 132\.786 kN') $true
Check 'tooltip vacuum thrust rewritten' ($out -match 'Thrust \(Vac\.\): 147\.5 kN') $true
$sum = $Comparison.GetMethod('Summary').Invoke($null, @('Kestrel', $liq, $k125, [int[]]@(25, 25, 25, 25, 10), (Level Normal)))
Check 'summary line' $sum 'Kestrel: Built for the pad. No standout strengths. Under 3% failures from Mk. 5.'
$sum2 = $Comparison.GetMethod('Summary').Invoke($null, @('Talon', $liq, (M 'K-125-V'), [int[]]@(0, 60, 0, 25, 0), (Level Off)))
Check 'summary for a vacuum engine' $sum2 'Talon: An upper-stage engine, nearly useless at sea level. Strong on efficiency. Neglects weight. Failures are off in this game.'
$sum3 = $Comparison.GetMethod('Summary').Invoke($null, @('Swift', $jet, (M 'F-125'), [int[]]@(60, 25, 0, 25), (Level Normal)))
Check 'summary for a jet (no failure line)' $sum3 'Swift: Efficient and subsonic. Strong on thrust. Neglects weight.'

Write-Host ''
if ($script:failed -eq 0) { Write-Host "All $script:passed checks passed." -ForegroundColor Green } else { Write-Host "$script:failed failed, $script:passed passed." -ForegroundColor Red; exit 1 }
