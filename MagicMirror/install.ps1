$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ModManifest.json') -Raw | ConvertFrom-Json
$destination = 'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods\MagicMirror'
$overhaul = Join-Path (Split-Path $destination) 'SidsOverhaul\ModManifest.json'
if (Test-Path -LiteralPath $overhaul) {
    $installed = Get-Content -LiteralPath $overhaul -Raw | ConvertFrom-Json
    if ($installed.files.path -match '^Scripts/MagicMirror/') { throw 'Installed Overhaul already includes Magic Mirror. Use only one copy.' }
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($file in $manifest.files) {
    $target = Join-Path $destination $file.path
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file.path) -Destination $target -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ModManifest.json') -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $destination -Force
Write-Host 'Installed Magic Mirror. Restart Core Keeper to load it.'
