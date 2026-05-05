# Deploy Player Status Strip: dotnet build Release + copy FULL mod folder into Vintage Story Mods.
# Copies bin/Release/Mods/playerstatusstrip/* -> destination (flatten), never nests playerstatusstrip/playerstatusstrip.
#
# Destination:
#   $env:VS_MODS_TEST\playerstatusstrip   if VS_MODS_TEST is set (see repo docs/DEV_ENV.md),
#   else %AppData%\Roaming\VintagestoryData\Mods\playerstatusstrip
#
# Usage (from repository root):
#   .\PlayerStatusStrip\scripts\deploy-playerstatusstrip.ps1
# Or from this folder: .\deploy-playerstatusstrip.ps1 (set location to mod root via $PSScriptRoot)
# Requires: VINTAGE_STORY for dotnet build (same as .csproj).

$ErrorActionPreference = "Stop"

$modRoot = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $modRoot "PlayerStatusStrip.csproj"
$buildOut = Join-Path $modRoot "bin\Release\Mods\playerstatusstrip"

if (-not (Test-Path $csproj)) {
    Write-Error "Project not found: $csproj"
}

$dstRoot = if ($env:VS_MODS_TEST -and $env:VS_MODS_TEST.Trim().Length -gt 0) {
    Join-Path $env:VS_MODS_TEST.TrimEnd('\', '/') "playerstatusstrip"
} else {
    Join-Path $env:APPDATA "VintagestoryData\Mods\playerstatusstrip"
}

Write-Host "Building Release..."
dotnet build $csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path $buildOut)) {
    Write-Error "Build output missing: $buildOut"
}

Write-Host "Deploying to: $dstRoot"
New-Item -ItemType Directory -Path $dstRoot -Force | Out-Null
Remove-Item -Path (Join-Path $dstRoot "*") -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $buildOut "*") -Destination $dstRoot -Recurse -Force

$modinfo = Join-Path $dstRoot "modinfo.json"
if (Test-Path $modinfo) {
    $v = (Get-Content $modinfo -Raw | ConvertFrom-Json).version
    Write-Host "Done. modinfo version: $v"
} else {
    Write-Host "Done."
}
