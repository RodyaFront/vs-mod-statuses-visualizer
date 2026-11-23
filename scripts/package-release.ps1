# Package Player Status Strip for ModDB: dotnet build Release + zip bin/Release/Mods/playerstatusstrip/* into release/.
#
# Entry names are always written with "/" separators. Windows PowerShell 5.1 Compress-Archive writes "\",
# which Vintage Story on Linux does not split on, so assets/lang never load there. Do not use Compress-Archive.
#
# Usage (from repository root):
#   .\scripts\package-release.ps1
#   .\scripts\package-release.ps1 -SkipBuild   # zip the existing build output as-is
# Requires: VINTAGE_STORY for dotnet build (same as .csproj) unless -SkipBuild.

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$modId = "playerstatusstrip"
$modRoot = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $modRoot "PlayerStatusStrip.csproj"
$buildOut = Join-Path (Join-Path (Join-Path (Join-Path $modRoot "bin") "Release") "Mods") $modId
$releaseDir = Join-Path $modRoot "release"

if (-not $SkipBuild) {
    Write-Host "Building Release..."
    dotnet build $csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$modinfo = Join-Path $buildOut "modinfo.json"
if (-not (Test-Path $modinfo)) {
    Write-Error "modinfo.json missing in build output: $buildOut"
}
$version = (Get-Content $modinfo -Raw | ConvertFrom-Json).version

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
$zipPath = Join-Path $releaseDir "${modId}_$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$rootPath = (Resolve-Path $buildOut).ProviderPath.TrimEnd('\', '/')
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -Path $rootPath -Recurse -File | ForEach-Object {
        $entryName = $_.FullName.Substring($rootPath.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $zip.Dispose()
}

$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName })
} finally {
    $zip.Dispose()
}

$badEntries = @($entries | Where-Object { $_.Contains('\') })
if ($badEntries.Count -gt 0) {
    Write-Error "Archive has entries with backslash separators (breaks Linux): $($badEntries -join ', ')"
}
if ($entries -notcontains "modinfo.json") {
    Write-Error "modinfo.json is not at the archive root: $zipPath"
}

Write-Host "Packaged $($entries.Count) files -> $zipPath (version $version)"
