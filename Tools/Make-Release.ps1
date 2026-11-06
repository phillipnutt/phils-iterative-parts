# Builds a release's zips (see CHANGELOG.md):
#   PhilsIterativeParts-<version>.zip                    PIP alone (CKAN installs the rest)
#   PhilsIterativeParts-<version>-with-dependencies.zip  PIP plus the five mods it needs,
#       unmodified, from their own release zips, with credits (Tools/Bundled/BUNDLED.md)
# Build PIP first (MSBuild; /p:NoInstall=true is fine), then:
#   powershell -ExecutionPolicy Bypass -File Tools\Make-Release.ps1 -Version 0.2.0 -Out <folder> [-Deps <folder>]
# -Deps is a folder holding the five mods' own release zips, one each for Harmony2,
# ModuleManager, B9PartSwitch, CommunityTechTree and CommunityResourcePack (CKAN
# keeps them in its download cache). Without it, only the PIP-only zip is made.
# Their versions must match the table in Tools/Bundled/BUNDLED.md: update it first.

param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Out,
    [string]$Deps
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dll = Join-Path $root 'bin\Release\PhilsIterativeParts.dll'
if (-not (Test-Path $dll)) { throw 'Build PIP first: bin\Release\PhilsIterativeParts.dll is missing.' }
New-Item -ItemType Directory -Force $Out | Out-Null

function NewZip($path) {
    if (Test-Path $path) { Remove-Item $path }
    [IO.Compression.ZipFile]::Open($path, 'Create')
}
# Zip entry names always use forward slashes (Windows PowerShell 5.1's
# Compress-Archive writes backslashes, which other tools misread).
function AddFile($zip, $src, $entry) {
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $src, $entry, 'Optimal')
}
function AddPip($zip) {
    $gd = Join-Path $root 'GameData'
    Get-ChildItem (Join-Path $gd 'PIP') -Recurse -File | Where-Object { $_.FullName -notmatch '\\Plugins\\' } | ForEach-Object {
        AddFile $zip $_.FullName ('GameData/' + $_.FullName.Substring($gd.Length + 1).Replace('\', '/'))
    }
    AddFile $zip $dll 'GameData/PIP/Plugins/PhilsIterativeParts.dll'
    foreach ($d in 'LICENSE', 'NOTICE.md', 'README.md', 'CHANGELOG.md', 'FUELS.md') { AddFile $zip (Join-Path $root $d) "GameData/PIP/$d" }
}

$plain = Join-Path $Out "PhilsIterativeParts-$Version.zip"
$z = NewZip $plain
AddPip $z
$z.Dispose()
Write-Host "Wrote $plain"
if (-not $Deps) { return }

# Each required mod by its CKAN identifier, and the folder under GameData/PIP/Bundled
# that its license and readme go to when they sit outside its GameData folder.
$mods = [ordered]@{
    Harmony2 = 'Harmony'; ModuleManager = 'ModuleManager'; B9PartSwitch = 'B9PartSwitch'
    CommunityTechTree = 'CommunityTechTree'; CommunityResourcePack = 'CommunityResourcePack'
}
$bundle = Join-Path $Out "PhilsIterativeParts-$Version-with-dependencies.zip"
$z = NewZip $bundle
AddPip $z
foreach ($id in $mods.Keys) {
    $src = @(Get-ChildItem $Deps -File -Filter '*.zip' | Where-Object { $_.Name -match "(^|-)$id-" })
    if ($src.Count -ne 1) { throw "Need exactly one $id zip in $Deps, found $($src.Count)." }
    $in = [IO.Compression.ZipFile]::OpenRead($src[0].FullName)
    foreach ($e in $in.Entries) {
        $name = $e.FullName
        if ($name.EndsWith('/')) { continue }
        if ($name.StartsWith('GameData/')) { $to = $name }
        elseif ($name -notmatch '/' -and $name.EndsWith('.dll')) { $to = "GameData/$name" }   # Module Manager ships its DLL at the top
        elseif ($name -notmatch '/' -and $name -match '(?i)readme|licen') { $to = "GameData/PIP/Bundled/$($mods[$id])/$name" }
        else { continue }                                                                    # changelogs and modder notes stay out
        $entry = $z.CreateEntry($to, 'Optimal')
        $entry.LastWriteTime = $e.LastWriteTime
        $w = $entry.Open(); $r = $e.Open(); $r.CopyTo($w); $r.Dispose(); $w.Dispose()
    }
    $in.Dispose()
    Write-Host "  bundled $($src[0].Name)"
}
foreach ($f in 'BUNDLED.md', 'Harmony-LICENSE.txt') { AddFile $z (Join-Path $PSScriptRoot "Bundled\$f") "GameData/PIP/Bundled/$f" }
$z.Dispose()
Write-Host "Wrote $bundle"
