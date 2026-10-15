$ErrorActionPreference = 'Stop'
$modRoot = $PSScriptRoot
$installRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods\ConnectedStorage'
$manifest = Get-Content -LiteralPath (Join-Path $modRoot 'ModManifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.files) {
    if (-not (Test-Path -LiteralPath (Join-Path $modRoot $entry.path))) { throw "Missing runtime file: $($entry.path)" }
}
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'ModManifest.json') -Destination (Join-Path $installRoot 'ModManifest.json') -Force
foreach ($entry in $manifest.files) {
    $destination = Join-Path $installRoot $entry.path
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $modRoot $entry.path) -Destination $destination -Force
}
Write-Host "Installed Connected Storage to $installRoot. Restart Core Keeper to load it."
