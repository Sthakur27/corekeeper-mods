$ErrorActionPreference = 'Stop'
$destination = 'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods\MushroomSlayer'
# Migration only: the old test build must not run alongside the renamed mod.
$legacy = 'C:\Program Files (x86)\Steam\steamapps\common\Core Keeper\CoreKeeper_Data\StreamingAssets\Mods\BruteArmor'
if (Test-Path -LiteralPath $legacy) {
    $manifest = Get-Content -LiteralPath (Join-Path $legacy 'ModManifest.json') -Raw | ConvertFrom-Json
    if ($manifest.guid -ne '419d9a524d614982b615fdd5fb97ddb3') { throw 'Unexpected legacy mod identity; leaving it untouched.' }
    $backupRoot = 'C:\Users\Sid\CoreKeeperMods\build\mushroomslayer\legacy-installs'
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $backup = Join-Path $backupRoot ([guid]::NewGuid().ToString('N'))
    if ((Resolve-Path -LiteralPath $legacy).Path -ne $legacy -or -not $backup.StartsWith($backupRoot + '\')) { throw 'Unexpected migration paths' }
    Move-Item -LiteralPath $legacy -Destination $backup
}
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ModManifest.json') -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Scripts') -Destination $destination -Recurse -Force
Write-Host 'Mushroom Slayer installed. Requires CoreLib and standalone Mod Options. Restart Core Keeper to load it.'
