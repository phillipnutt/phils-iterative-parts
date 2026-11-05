# Writes GameData\PIP\Upgrades.cfg: the PARTUPGRADE entries that show PIP's
# unlocks inside their R&D nodes. The plugin reads research state itself
# (KSP\TechUnlocks.cs and each type's CapTechs in Core); keep the two in step.
# Run: powershell -ExecutionPolicy Bypass -File Tools\Generate-Upgrades.ps1

# node, name, icon chassis, short info, description
$u = @(
    # Liquid engines (DESIGN.md section 8)
    @('basicRocketry', 'Liquid-Fuel Engines', 'PIP-K-125-S', 'First liquid engine programs', 'Start liquid engine programs: 1.25 m, kerolox or ethalox, sea-level nozzle. Every category is capped at 25 points for now.'),
    @('generalRocketry', 'Altitude Nozzles', 'PIP-K-125-V', 'Vacuum nozzle', 'Liquid engine programs can use a vacuum nozzle: great in space, nearly useless at sea level.'),
    @('advRocketry', 'Turbopumps', 'PIP-K-125-S', 'Liquid engine caps 50', 'New liquid engine programs can put up to 50 points into every category.'),
    @('propulsionSystems', 'Miniaturized Combustion Chambers', 'PIP-K-0625-S', '0.625 m liquid engines', 'Liquid engine programs can be 0.625 m.'),
    @('heavyRocketry', '1.875 m Tooling', 'PIP-K-1875-S', '1.875 m', 'Rocket programs of every family can be 1.875 m.'),
    @('heavierRocketry', '2.5 m Tooling', 'PIP-K-25-S', '2.5 m', 'Rocket programs of every family can be 2.5 m.'),
    @('heavierRocketry', 'Staged Combustion', 'PIP-K-25-S', 'Liquid engine caps 75', 'New liquid engine programs can put up to 75 points into every category.'),
    @('precisionPropulsion', 'Sustainer Nozzles', 'PIP-K-125-B', 'Balanced nozzle', 'Liquid engine programs can use a balanced nozzle: decent low and high.'),
    @('advFuelSystems', 'Cryogenic Hydrogen', 'PIP-H-125-S', 'Hydrolox', 'Liquid engine programs can burn hydrolox: the best Isp, and bulky tanks.'),
    @('veryHeavyRocketry', '3.75 m Tooling', 'PIP-K-375-S', '3.75 m', 'Rocket programs of every family can be 3.75 m.'),
    @('highPerformanceFuelSystems', 'Methalox Combustion', 'PIP-M-125-S', 'Methalox', 'Liquid engine programs can burn methalox: the best thrust, weight and reliability.'),
    @('experimentalRocketry', 'Linear Aerospike', 'PIP-K-125-A', 'Aerospike nozzle', 'Liquid engine programs can use an aerospike: good at every altitude, best at none, no gimbal.'),
    @('experimentalRocketry', 'Full-Flow Staged Combustion', 'PIP-K-375-S', 'Liquid engine caps 100', 'New liquid engine programs can put up to 100 points into every category.'),
    @('giganticRocketry', '5 m Tooling', 'PIP-K-5-S', '5 m', 'Liquid engine programs can be 5 m.'),
    # Solid boosters
    @('start', 'Solid Rocket Motors', 'PIP-SOL-125-M', 'First solid booster programs', 'Start solid booster programs: 0.625 and 1.25 m, short and medium. Caps 25.'),
    @('generalRocketry', 'Segmented Casings', 'PIP-SOL-125-L', 'Long boosters; solid caps 50', 'Solid booster programs can be long, and new ones can put up to 50 points into every category.'),
    @('heavierRocketry', 'Composite Propellant', 'PIP-SOL-25-M', 'Solid caps 75', 'New solid booster programs can put up to 75 points into every category.'),
    @('experimentalRocketry', 'HTPB Propellant', 'PIP-SOL-375-L', 'Solid caps 100', 'New solid booster programs can put up to 100 points into every category.'),
    # Monopropellant engines
    @('propulsionSystems', 'Monopropellant Thrusters', 'PIP-MON-125', 'First monopropellant engine programs', 'Start monopropellant engine programs: 0.625 and 1.25 m. Caps 25.'),
    @('precisionPropulsion', 'Throttleable Catalysts', 'PIP-MON-125', 'Monopropellant caps 50', 'New monopropellant engine programs can put up to 50 points into every category.'),
    @('heavyLanding', 'Catalyst Beds', 'PIP-MON-25', 'Monopropellant caps 75', 'New monopropellant engine programs can put up to 75 points into every category.'),
    @('experimentalRocketry', 'Green Monopropellants', 'PIP-MON-375', 'Monopropellant engine and RCS caps 100', 'New monopropellant engine and RCS programs can put up to 100 points into every category.'),
    # Nuclear engines
    @('nuclearPropulsion', 'Solid-Core Reactors', 'PIP-NUC-S-125', 'First nuclear engine programs', 'Start nuclear engine programs: solid core, 1.25 m and any tooling size. Caps 25.'),
    @('improvedNuclearPropulsion', 'Compact Cores', 'PIP-NUC-S-0625', '0.625 m; nuclear caps 50', 'Nuclear engine programs can be 0.625 m, and new ones can put up to 50 points into every category.'),
    @('advNuclearPropulsion', 'Hot-Running Fuel Elements', 'PIP-NUC-S-25', 'Nuclear caps 75', 'New nuclear engine programs can put up to 75 points into every category.'),
    @('expNuclearPropulsion', 'Gas-Core Reactors', 'PIP-NUC-G-25', 'Gas core; nuclear caps 100', 'Nuclear engine programs can use a gas core, and new ones can put up to 100 points into every category.'),
    # Electric engines
    @('ionPropulsion', 'Electrostatic Thrusters', 'PIP-ELE-I-0625', 'First electric engine programs', 'Start electric engine programs: gridded ion and Hall, 0.625 m. Caps 25.'),
    @('advIonPropulsion', 'Larger Grids', 'PIP-ELE-I-125', '1.25 m; electric caps 50', 'Electric engine programs can be 1.25 m, and new ones can put up to 50 points into every category.'),
    @('plasmaPropulsion', 'Magnetoplasmadynamics', 'PIP-ELE-M-0625', 'MPD thrusters', 'Electric engine programs can be MPD thrusters: real thrust, a reactor''s worth of power.'),
    @('advGriddedThrusters', 'Carbon Grids', 'PIP-ELE-I-125', 'Electric caps 75', 'New electric engine programs can put up to 75 points into every category.'),
    @('advEMSystems', 'High-Power Thrusters', 'PIP-ELE-M-25', '2.5 m; electric caps 100', 'Electric engine programs can be 2.5 m, and new ones can put up to 100 points into every category.'),
    # Jet engines
    @('aviation', 'Jet Engines', 'PIP-JET-F-125', 'First jet engine programs', 'Start jet engine programs: turbofans, 0.625 and 1.25 m. Caps 25.'),
    @('supersonicFlight', 'Afterburners', 'PIP-JET-J-125', 'Turbojet; jet caps 50', 'Jet engine programs can be afterburning turbojets, and new ones can put up to 50 points into every category.'),
    @('highAltitudeFlight', 'High-Bypass Fans', 'PIP-JET-F-25', '1.875 and 2.5 m jets; jet caps 75', 'Jet engine programs can be 1.875 and 2.5 m, and new ones can put up to 75 points into every category.'),
    @('hypersonicFlight', 'Ramjets', 'PIP-JET-R-125', 'Ramjet', 'Jet engine programs can be ramjets: weak off the runway, strong around Mach 3.'),
    @('aerospaceTech', 'Combined Cycle', 'PIP-JET-C-125', 'Combined cycle; jet caps 100', 'Jet engine programs can be combined cycle (air-breathing, then a rocket), and new ones can put up to 100 points into every category.'),
    # RCS thrusters (caps 100 come with Green Monopropellants above)
    @('flightControl', 'Attitude Thrusters', 'PIP-RCS-M-4', 'First RCS programs', 'Start monopropellant RCS programs: single, 4-way or 5-way. Caps 25.'),
    @('advFlightControl', 'Pulse-Mode Valves', 'PIP-RCS-M-4', 'RCS caps 50', 'New RCS programs can put up to 50 points into every category.'),
    @('specializedControl', 'Bipropellant Thrusters', 'PIP-RCS-B-4', 'Bipropellant RCS; RCS caps 75', 'RCS programs can burn kerolox, and new ones can put up to 75 points into every category.'),
    # Batteries
    @('start', 'Rechargeable Cells', 'PIP-BAT', 'First battery program', 'Start a battery program: every size, picked on the part. Caps 25.'),
    @('electrics', 'Nickel-Cadmium', 'PIP-BAT', 'Battery caps 50', 'New battery programs can put up to 50 points into capacity.'),
    @('largeElectrics', 'Lithium-Ion', 'PIP-BAT', 'Battery caps 75', 'New battery programs can put up to 75 points into capacity.'),
    @('experimentalElectrics', 'Solid-State Cells', 'PIP-BAT', 'Battery caps 100', 'New battery programs can put up to 100 points into capacity.'),
    # Reaction wheels
    @('stability', 'Momentum Wheels', 'PIP-RWL', 'First reaction wheel program', 'Start a reaction wheel program: every size, picked on the part. Caps 25.'),
    @('flightControl', 'Brushless Motors', 'PIP-RWL', 'Reaction wheel caps 50', 'New reaction wheel programs can put up to 50 points into every category.'),
    @('advFlightControl', 'Magnetic Bearings', 'PIP-RWL', 'Reaction wheel caps 75', 'New reaction wheel programs can put up to 75 points into every category.'),
    @('specializedControl', 'Control Moment Gyroscopes', 'PIP-RWL', 'Reaction wheel caps 100', 'New reaction wheel programs can put up to 100 points into every category.'),
    # Heat shields
    @('survivability', 'Ablative Shields', 'PIP-HSH', 'First heat shield program', 'Start a heat shield program: 0.625 and 1.25 m, and bigger with the size tooling. Caps 25.'),
    @('enhancedSurvivability', 'Phenolic Resins', 'PIP-HSH', 'Heat shield caps 50', 'New heat shield programs can put up to 50 points into every category.'),
    @('heatManagementSystems', 'PICA', 'PIP-HSH', 'Heat shield caps 75', 'New heat shield programs can put up to 75 points into every category.'),
    @('advHeatManagement', 'Carbon-Carbon', 'PIP-HSH', 'Heat shield caps 100', 'New heat shield programs can put up to 100 points into every category.'),
    # Solar panels
    @('survivability', 'Photovoltaics', 'PIP-PV-F', 'First solar panel programs', 'Start fixed solar panel programs, small. Caps 25.'),
    @('electrics', 'Deployable Arrays', 'PIP-PV-D', 'Deployable panels; medium; solar caps 50', 'Solar programs can be deployable, medium panels are available, and new programs can put up to 50 points into every category.'),
    @('largeElectrics', 'Large Arrays', 'PIP-PV-D', 'Large panels', 'Deployable solar panels can be large.'),
    @('advSolarTech', 'Gallium Arsenide', 'PIP-PV-D', 'Solar caps 75', 'New solar panel programs can put up to 75 points into every category.'),
    @('cuttingEdgeSolarTech', 'Multi-Junction Cells', 'PIP-PV-D', 'Huge panels; solar caps 100', 'Deployable solar panels can be huge, and new programs can put up to 100 points into every category.'),
    # Antennas
    @('start', 'Radio', 'PIP-ANT-O', 'First antenna programs', 'Start omni antenna programs. Caps 25.'),
    @('basicScience', 'Directional Antennas', 'PIP-ANT-D', 'Short-range dishes', 'Antenna programs can be dishes, short-range class.'),
    @('precisionEngineering', 'Parabolic Dishes', 'PIP-ANT-D', 'Interplanetary dishes; antenna caps 50', 'Dishes can be interplanetary class, and new antenna programs can put up to 50 points into every category.'),
    @('unmannedTech', 'Deep Space Network', 'PIP-ANT-D', 'Deep-space dishes; antenna caps 75', 'Dishes can be deep-space class, and new antenna programs can put up to 75 points into every category.'),
    @('largeUnmanned', 'Phased Arrays', 'PIP-ANT-D', 'Antenna caps 100', 'New antenna programs can put up to 100 points into every category.'),
    # Probe cores
    @('start', 'Guidance Units', 'PIP-PRB', 'First probe core program', 'Start a probe core program: 0.625 m, more sizes later. Caps 25.'),
    @('basicScience', 'Larger Avionics Bays', 'PIP-PRB', '1.25 m probe cores', 'Probe cores can be 1.25 m.'),
    @('precisionEngineering', 'Digital Autopilots', 'PIP-PRB', 'Probe core caps 50', 'New probe core programs can put up to 50 points into every category.'),
    @('unmannedTech', '1.875 m Avionics', 'PIP-PRB', '1.875 m probe cores', 'Probe cores can be 1.875 m.'),
    @('advUnmanned', 'Maneuver Computers', 'PIP-PRB', 'Probe core caps 75', 'New probe core programs can put up to 75 points into every category: enough for maneuver-node autopilot.'),
    @('largeUnmanned', 'Heavy Avionics', 'PIP-PRB', '2.5 m probe cores; caps 100', 'Probe cores can be 2.5 m, and new programs can put up to 100 points into every category.'),
    # Fuel tanks
    @('basicRocketry', 'Welded Tanks', 'PIP-TNK-S', 'First fuel tank programs', 'Start standard fuel tank programs: kerolox, liquid fuel or oxidizer, size and length picked on the part. Caps 25.'),
    @('flightControl', 'Monopropellant Tanks', 'PIP-TNK-M', 'Monopropellant tanks', 'Fuel tank programs can hold monopropellant.'),
    @('fuelSystems', 'Stir-Welded Aluminum', 'PIP-TNK-S', 'Tank caps 50', 'New fuel tank programs can put up to 50 points into every category.'),
    @('advFuelSystems', 'Cryogenic Tanks', 'PIP-TNK-C', 'Cryogenic tanks', 'Fuel tank programs can be cryogenic: hydrogen, hydrolox, methane or methalox.'),
    @('largeVolumeContainment', 'Aluminum-Lithium Tanks', 'PIP-TNK-S', 'Tank caps 75', 'New fuel tank programs can put up to 75 points into every category.'),
    @('ionPropulsion', 'Pressurized Gas Tanks', 'PIP-TNK-E', 'Electric propellant tanks', 'Fuel tank programs can hold xenon, argon or lithium.'),
    @('specializedFuelStorage', 'Composite Tanks', 'PIP-TNK-S', 'Tank caps 100', 'New fuel tank programs can put up to 100 points into every category.'),
    # RTGs
    @('specializedElectrics', 'Radioisotope Power', 'PIP-RTG', 'First RTG program', 'Start an RTG program: small, large later. Caps 25.'),
    @('experimentalElectrics', 'Thermocouple Upgrades', 'PIP-RTG', 'RTG caps 50', 'New RTG programs can put up to 50 points into every category.'),
    @('highTechElectricalSystems', 'Stirling Converters', 'PIP-RTG', 'Large RTGs; caps 75', 'RTGs can be large, and new programs can put up to 75 points into every category.'),
    @('highPowerElectricalSystems', 'Advanced Isotopes', 'PIP-RTG', 'RTG caps 100', 'New RTG programs can put up to 100 points into every category.'),
    # Air intakes
    @('aviation', 'Air Intakes', 'PIP-INT-S', 'First intake programs', 'Start subsonic intake programs, 0.625 and 1.25 m. Caps 25.'),
    @('aerodynamicSystems', 'Variable Inlets', 'PIP-INT-S', 'Intake caps 50', 'New air intake programs can put up to 50 points into every category.'),
    @('highAltitudeFlight', 'Ramp Intakes', 'PIP-INT-P', 'Supersonic intakes; bigger sizes; caps 75', 'Intake programs can be supersonic, 1.875 and 2.5 m sizes are available, and new programs can put up to 75 points into every category.'),
    @('hypersonicFlight', 'Shock Cones', 'PIP-INT-H', 'Hypersonic intakes; caps 100', 'Intake programs can be hypersonic, and new ones can put up to 100 points into every category.')
)

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('// Generated by Tools\Generate-Upgrades.ps1. Do not edit by hand.')
[void]$sb.AppendLine('// Phil''s Iterative Parts: tech-tree unlocks. These entries show each unlock in its')
[void]$sb.AppendLine('// R&D node; the plugin reads the node''s research state (KSP\TechUnlocks.cs).')
[void]$sb.AppendLine()
foreach ($e in $u) {
    $node, $title, $icon, $info, $desc = $e
    $id = 'PIP-' + ($title -replace '[^A-Za-z0-9]', '')
    [void]$sb.AppendLine('PARTUPGRADE')
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine("`tname = $id")
    [void]$sb.AppendLine("`tpartIcon = $icon")
    [void]$sb.AppendLine("`ttechRequired = $node")
    [void]$sb.AppendLine("`tentryCost = 0")
    [void]$sb.AppendLine("`tcost = 0")
    [void]$sb.AppendLine("`ttitle = PIP: $title")
    [void]$sb.AppendLine("`tbasicInfo = $info")
    [void]$sb.AppendLine("`tmanufacturer = Phil's Iterative Parts")
    [void]$sb.AppendLine("`tdescription = $desc")
    [void]$sb.AppendLine('}')
}
$out = Join-Path $PSScriptRoot '..\GameData\PIP\Upgrades.cfg'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($out), $sb.ToString())
$dupes = $u | Group-Object { 'PIP-' + ($_[1] -replace '[^A-Za-z0-9]', '') } | Where-Object Count -gt 1
"Wrote $($u.Count) unlocks to $out" + $(if ($dupes) { " (DUPLICATE NAMES: $($dupes.Name -join ', '))" } else { '' })
