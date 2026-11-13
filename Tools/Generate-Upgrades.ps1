# Writes GameData\PIP\Upgrades.cfg: the PARTUPGRADE entries that show PIP's
# unlocks inside their R&D nodes. The plugin reads research state itself
# (KSP\TechUnlocks.cs and each type's CapTechs in Core); keep the two in step.
# With KSP's entry purchase on, each entry is bought like a stock part (DESIGN.md
# 27.4): $sells says what it opens; PIP sets its price at load.
# Run: powershell -ExecutionPolicy Bypass -File Tools\Generate-Upgrades.ps1

# node, name, icon chassis, short info, description
$u = @(
    # Liquid engines (DESIGN.md section 8)
    @('basicRocketry', 'Liquid-Fuel Engines', 'PIP-K-125-S', 'First liquid engine programs', 'Start liquid engine programs: 1.25 m, kerolox or ethalox, sea-level nozzle. Every category is capped at 25 points for now.'),
    @('generalRocketry', 'Altitude Nozzles', 'PIP-K-125-V', 'Vacuum nozzle', 'Liquid engine programs can use a vacuum nozzle: great in space, nearly useless at sea level.'),
    @('advRocketry', 'Turbopumps', 'PIP-K-125-S', 'Liquid engine caps 50', 'New liquid engine programs can put up to 50 points into every category.'),
    @('propulsionSystems', 'Miniaturized Combustion Chambers', 'PIP-K-0625-S', '0.625 m liquid engines', 'Liquid engine programs can be 0.625 m.'),
    @('heavyRocketry', '1.875 m Tooling', 'PIP-K-1875-S', '1.875 m', 'Programs of every type that comes in 1.875 m can be 1.875 m.'),
    @('heavierRocketry', '2.5 m Tooling', 'PIP-K-25-S', '2.5 m', 'Programs of every type that comes in 2.5 m can be 2.5 m.'),
    @('heavierRocketry', 'Staged Combustion', 'PIP-K-25-S', 'Liquid engine caps 75', 'New liquid engine programs can put up to 75 points into every category.'),
    @('precisionPropulsion', 'Sustainer Nozzles', 'PIP-K-125-B', 'Balanced nozzle', 'Liquid engine programs can use a balanced nozzle: decent low and high.'),
    @('advFuelSystems', 'Cryogenic Hydrogen', 'PIP-H-125-S', 'Hydrolox', 'Liquid engine programs can burn hydrolox: the best Isp, and bulky tanks. Standard tanks lose hydrogen fast; cryogenic tanks come with Large Volume Containment.'),
    @('veryHeavyRocketry', '3.75 m Tooling', 'PIP-K-375-S', '3.75 m', 'Programs of every type that comes in 3.75 m can be 3.75 m.'),
    @('highPerformanceFuelSystems', 'Methalox Combustion', 'PIP-M-125-S', 'Methalox', 'Liquid engine programs can burn methalox: the best thrust, weight and reliability.'),
    @('experimentalRocketry', 'Linear Aerospike', 'PIP-K-125-A', 'Aerospike nozzle', 'Liquid engine programs can use an aerospike: good at every altitude, best at none, no gimbal.'),
    @('experimentalRocketry', 'Full-Flow Staged Combustion', 'PIP-K-375-S', 'Liquid engine caps 100', 'New liquid engine programs can put up to 100 points into every category.'),
    @('giganticRocketry', '5 m Tooling', 'PIP-K-5-S', '5 m', 'Liquid engine, fuel tank, heat shield and decoupler programs can be 5 m.'),
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
    @('basicScience', 'Directional Antennas', 'PIP-ANT-D', 'Wide dishes', 'Antenna programs can be wide dishes: shorter reach, a wider cone.'),
    @('precisionEngineering', 'Parabolic Dishes', 'PIP-ANT-D', 'Focused dishes; antenna caps 50', 'Antenna programs can be focused dishes: the longest reach, a narrow cone. New antenna programs can put up to 50 points into every category.'),
    @('unmannedTech', 'Deep Space Network', 'PIP-ANT-D', 'Antenna caps 75', 'New antenna programs can put up to 75 points into every category.'),
    @('largeUnmanned', 'Phased Arrays', 'PIP-ANT-D', 'Antenna caps 100', 'New antenna programs can put up to 100 points into every category.'),
    # Probe cores
    @('start', 'Guidance Units', 'PIP-PRB', 'First probe core program', 'Start a probe core program: 0.625 m, more sizes later. Caps 25: SAS holds attitude (from 10 autopilot points).'),
    @('basicScience', 'Larger Avionics Bays', 'PIP-PRB', '1.25 m probe cores', 'Probe cores can be 1.25 m.'),
    @('precisionEngineering', 'Digital Autopilots', 'PIP-PRB', 'Probe core caps 50: prograde, retrograde', 'New probe core programs can put up to 50 points into every category. At 50 autopilot points, SAS also holds prograde and retrograde.'),
    @('unmannedTech', '1.875 m Avionics', 'PIP-PRB', '1.875 m probe cores', 'Probe cores can be 1.875 m.'),
    @('advUnmanned', 'Star Trackers', 'PIP-PRB', 'Probe core caps 75: normal, radial, target', 'New probe core programs can put up to 75 points into every category. At 75 autopilot points, SAS also holds normal, radial and target.'),
    @('largeUnmanned', 'Heavy Avionics', 'PIP-PRB', '2.5 m probe cores; caps 100: maneuver node', 'Probe cores can be 2.5 m, and new programs can put up to 100 points into every category. At 100 autopilot points, SAS also follows the maneuver node: maneuver computers.'),
    # Fuel tanks
    @('basicRocketry', 'Welded Tanks', 'PIP-TNK-S', 'First fuel tank programs', 'Start standard fuel tank programs: any fuel, size and length picked on the part. No insulation, so cryogenic fuels boil off. Caps 25.'),
    @('flightControl', 'Monopropellant Tanks', 'PIP-TNK-M', 'Monopropellant tanks', 'Fuel tank programs can hold monopropellant.'),
    @('propulsionSystems', 'Radial Tanks', 'PIP-TNK-S', 'Radial tanks', 'Fuel tank programs can be side-mount: tiny, small or medium radial tanks; large with 1.875 m tooling, huge with 2.5 m.'),
    @('fuelSystems', 'Adapter Tanks', 'PIP-TNK-S', 'Adapter tanks', 'Fuel tank programs can be adapters between two sizes, short or long, or empty structural adapters. Each size needs its tooling; 4:3 and 3:1 tapers need Making History, ReStock+ or Near Future Launch Vehicles.'),
    @('fuelSystems', 'Stir-Welded Aluminum', 'PIP-TNK-S', 'Tank caps 50', 'New fuel tank programs can put up to 50 points into every category.'),
    @('largeVolumeContainment', 'Cryogenic Tanks', 'PIP-TNK-C', 'Cryogenic tanks', 'Fuel tank programs can be cryogenic: insulated and cooled, so hydrogen, methane and fluorine keep.'),
    @('largeVolumeContainment', 'Aluminum-Lithium Tanks', 'PIP-TNK-S', 'Tank caps 75', 'New fuel tank programs can put up to 75 points into every category.'),
    @('ionPropulsion', 'Pressurized Gas Tanks', 'PIP-TNK-E', 'Electric propellant tanks', 'Fuel tank programs can hold xenon, argon or lithium.'),
    @('specializedFuelStorage', 'Composite Tanks', 'PIP-TNK-S', 'Tank caps 100', 'New fuel tank programs can put up to 100 points into every category.'),
    # Plane fuselages (DESIGN.md 21.2)
    @('supersonicFlight', 'Mk2 Fuselages', 'PIP-PLN-S-2', 'First plane fuselage programs', 'Start Mk2 fuselage programs: short; long at High Altitude Flight. Any fuel, as tanks. Caps 25. Short adapter tanks to Mk2 need it too.'),
    @('highAltitudeFlight', 'Long Mk2 Fuselages', 'PIP-PLN-S-2', 'Long Mk2 fuselages; caps 50', 'Mk2 fuselages can be long, and new fuselage programs can put up to 50 points into every category. Long adapter tanks to Mk2 need it too.'),
    @('heavyAerodynamics', 'Mk3 Fuselages', 'PIP-PLN-S-3', 'Mk3 fuselages; caps 75', 'Fuselage programs can be Mk3, short or medium, and new ones can put up to 75 points into every category. Adapter tanks from Mk3 need it too.'),
    @('experimentalAerodynamics', 'Long Mk3 Fuselages', 'PIP-PLN-S-3', 'Long Mk3 fuselages; caps 100', 'Mk3 fuselages can be long, and new fuselage programs can put up to 100 points into every category. The Mk3 to 3.75 m adapter tank needs it too.'),
    # Decouplers (DESIGN.md 21.1)
    @('engineering101', 'Pyrotechnic Bolts', 'PIP-DEC-D', 'First decoupler programs', 'Start stack decoupler programs: 0.625 and 1.25 m, larger sizes with their tooling. Caps 25.'),
    @('stability', 'Radial Decouplers', 'PIP-DEC-D', 'Radial decouplers', 'Decoupler programs can be side-mount: small or medium radial decouplers; large at Specialized Construction.'),
    @('generalConstruction', 'Stack Separators', 'PIP-DEC-S', 'Separators; decoupler caps 50', 'Decoupler programs can be separators, which let go of both sides, and new ones can put up to 50 points into every category.'),
    @('advConstruction', 'Frangible Joints', 'PIP-DEC-D', 'Decoupler caps 75', 'New decoupler programs can put up to 75 points into every category.'),
    @('specializedConstruction', 'Hydraulic Separation', 'PIP-DEC-D', 'Large radial decouplers; caps 100', 'Radial decouplers can be large, and new decoupler programs can put up to 100 points into every category.'),
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

# What each entry sells (DESIGN.md 27.4): "type:what" items, read by Core\Entries.cs.
# what: cap (first programs, or a caps step), size, or Choice=Code. * = every type.
$sells = @{
    'Liquid-Fuel Engines' = 'liquid:cap'; 'Altitude Nozzles' = 'liquid:Nozzle=V'; 'Turbopumps' = 'liquid:cap'
    'Miniaturized Combustion Chambers' = 'liquid:size'
    '1.875 m Tooling' = '*:size'; '2.5 m Tooling' = '*:size'; '3.75 m Tooling' = '*:size'; '5 m Tooling' = '*:size'
    'Staged Combustion' = 'liquid:cap'; 'Sustainer Nozzles' = 'liquid:Nozzle=B'; 'Cryogenic Hydrogen' = 'liquid:Fuel=H'
    'Methalox Combustion' = 'liquid:Fuel=M'; 'Linear Aerospike' = 'liquid:Nozzle=A'; 'Full-Flow Staged Combustion' = 'liquid:cap'
    'Solid Rocket Motors' = 'solid:cap'; 'Segmented Casings' = 'solid:Length=L, solid:cap'; 'Composite Propellant' = 'solid:cap'; 'HTPB Propellant' = 'solid:cap'
    'Monopropellant Thrusters' = 'monoprop:cap'; 'Throttleable Catalysts' = 'monoprop:cap'; 'Catalyst Beds' = 'monoprop:cap'
    'Green Monopropellants' = 'monoprop:cap, rcs:cap'
    'Solid-Core Reactors' = 'nuclear:cap'; 'Compact Cores' = 'nuclear:size, nuclear:cap'; 'Hot-Running Fuel Elements' = 'nuclear:cap'
    'Gas-Core Reactors' = 'nuclear:Core=G, nuclear:cap'
    'Electrostatic Thrusters' = 'electric:cap'; 'Larger Grids' = 'electric:size, electric:cap'; 'Magnetoplasmadynamics' = 'electric:Type=M'
    'Carbon Grids' = 'electric:cap'; 'High-Power Thrusters' = 'electric:size, electric:cap'
    'Jet Engines' = 'jet:cap'; 'Afterburners' = 'jet:Type=J, jet:cap'; 'High-Bypass Fans' = 'jet:size, jet:cap'; 'Ramjets' = 'jet:Type=R'
    'Combined Cycle' = 'jet:Type=C, jet:cap'
    'Attitude Thrusters' = 'rcs:cap'; 'Pulse-Mode Valves' = 'rcs:cap'; 'Bipropellant Thrusters' = 'rcs:Propellant=B, rcs:cap'
    'Rechargeable Cells' = 'battery:cap'; 'Nickel-Cadmium' = 'battery:cap'; 'Lithium-Ion' = 'battery:cap'; 'Solid-State Cells' = 'battery:cap'
    'Momentum Wheels' = 'wheel:cap'; 'Brushless Motors' = 'wheel:cap'; 'Magnetic Bearings' = 'wheel:cap'; 'Control Moment Gyroscopes' = 'wheel:cap'
    'Ablative Shields' = 'shield:cap'; 'Phenolic Resins' = 'shield:cap'; 'PICA' = 'shield:cap'; 'Carbon-Carbon' = 'shield:cap'
    'Photovoltaics' = 'solar:cap'; 'Deployable Arrays' = 'solar:Kind=D, solar:size, solar:cap'; 'Large Arrays' = 'solar:size'
    'Gallium Arsenide' = 'solar:cap'; 'Multi-Junction Cells' = 'solar:size, solar:cap'
    'Radio' = 'antenna:cap'; 'Directional Antennas' = 'antenna:Kind=D'; 'Parabolic Dishes' = 'antenna:size, antenna:cap'
    'Deep Space Network' = 'antenna:cap'; 'Phased Arrays' = 'antenna:cap'
    'Guidance Units' = 'probe:cap'; 'Larger Avionics Bays' = 'probe:size'; 'Digital Autopilots' = 'probe:cap'; '1.875 m Avionics' = 'probe:size'
    'Star Trackers' = 'probe:cap'; 'Heavy Avionics' = 'probe:size, probe:cap'
    'Welded Tanks' = 'tank:cap'; 'Monopropellant Tanks' = 'tank:Kind=M, plane:Kind=M'; 'Radial Tanks' = 'tank:size'; 'Adapter Tanks' = 'tank:size'
    'Stir-Welded Aluminum' = 'tank:cap'; 'Cryogenic Tanks' = 'tank:Kind=C, plane:Kind=C'; 'Aluminum-Lithium Tanks' = 'tank:cap'
    'Pressurized Gas Tanks' = 'tank:Kind=E, plane:Kind=E'; 'Composite Tanks' = 'tank:cap'
    'Mk2 Fuselages' = 'plane:cap, tank:size'; 'Long Mk2 Fuselages' = 'plane:size, plane:cap, tank:size'
    'Mk3 Fuselages' = 'plane:Section=3, plane:cap, tank:size'; 'Long Mk3 Fuselages' = 'plane:size, plane:cap, tank:size'
    'Pyrotechnic Bolts' = 'decoupler:cap'; 'Radial Decouplers' = 'decoupler:size'; 'Stack Separators' = 'decoupler:Kind=S, decoupler:cap'
    'Frangible Joints' = 'decoupler:cap'; 'Hydraulic Separation' = 'decoupler:size, decoupler:cap'
    'Radioisotope Power' = 'rtg:cap'; 'Thermocouple Upgrades' = 'rtg:cap'; 'Stirling Converters' = 'rtg:size, rtg:cap'; 'Advanced Isotopes' = 'rtg:cap'
    'Air Intakes' = 'intake:cap'; 'Variable Inlets' = 'intake:cap'; 'Ramp Intakes' = 'intake:Speed type=P, intake:size, intake:cap'
    'Shock Cones' = 'intake:Speed type=H, intake:cap'
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('// Generated by Tools\Generate-Upgrades.ps1. Do not edit by hand.')
[void]$sb.AppendLine('// Phil''s Iterative Parts: tech-tree unlocks. These entries show each unlock in its')
[void]$sb.AppendLine('// R&D node; the plugin reads the node''s research state (KSP\TechUnlocks.cs).')
[void]$sb.AppendLine('// With entry purchase on, each is bought in R&D: pipUnlocks says what it opens')
[void]$sb.AppendLine('// (Core\Entries.cs), and PIP sets entryCost at load (30 funds per science point).')
[void]$sb.AppendLine()
foreach ($e in $u) {
    $node, $title, $icon, $info, $desc = $e
    $id = 'PIP-' + ($title -replace '[^A-Za-z0-9]', '')
    if (-not $sells.ContainsKey($title)) { throw "No `$sells line for $title" }
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
    [void]$sb.AppendLine("`tpipUnlocks = $($sells[$title])")
    [void]$sb.AppendLine('}')
}
$out = Join-Path $PSScriptRoot '..\GameData\PIP\Upgrades.cfg'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($out), $sb.ToString())
$dupes = $u | Group-Object { 'PIP-' + ($_[1] -replace '[^A-Za-z0-9]', '') } | Where-Object Count -gt 1
"Wrote $($u.Count) unlocks to $out" + $(if ($dupes) { " (DUPLICATE NAMES: $($dupes.Name -join ', '))" } else { '' })
