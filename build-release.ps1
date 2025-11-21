# Build-Script für Jellyfin Plugin Release
# Erstellt ZIP-Paket und aktualisiert manifest.json mit Checksum

param(
    [string]$Version = "1.0.0.0",
    [string]$TargetAbi = "10.11.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Jellyfin Plugin Release Builder ===" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Target ABI: $TargetAbi"

# Pfade
$ProjectDir = "$PSScriptRoot\Jellyfin.Plugin.RequestsBridge"
$OutputDir = "$PSScriptRoot\releases"
$ReleaseDir = "$OutputDir\v$Version"
$ZipName = "Jellyfin.Plugin.RequestsBridge.zip"
$ZipPath = "$ReleaseDir\$ZipName"

# 1. Build
Write-Host "`n[1/4] Building Release..." -ForegroundColor Yellow
dotnet build "$ProjectDir" -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# 2. Release-Ordner erstellen
Write-Host "`n[2/4] Creating release folder..." -ForegroundColor Yellow
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 3. ZIP erstellen (nur die DLL)
Write-Host "`n[3/4] Creating ZIP package..." -ForegroundColor Yellow
$DllPath = "$ProjectDir\bin\Release\net9.0\Jellyfin.Plugin.RequestsBridge.dll"
if (-not (Test-Path $DllPath)) { throw "DLL not found: $DllPath" }

# Temporärer Ordner für ZIP-Inhalt
$TempDir = "$OutputDir\temp"
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
Copy-Item $DllPath $TempDir

Compress-Archive -Path "$TempDir\*" -DestinationPath $ZipPath -Force
Remove-Item $TempDir -Recurse -Force

# 4. MD5 Checksum berechnen
Write-Host "`n[4/4] Calculating checksum..." -ForegroundColor Yellow
$Hash = (Get-FileHash $ZipPath -Algorithm MD5).Hash.ToLower()
Write-Host "Checksum: $Hash" -ForegroundColor Green

# 5. manifest.json aktualisieren
Write-Host "`nUpdating manifest.json..." -ForegroundColor Yellow
$ManifestPath = "$PSScriptRoot\manifest.json"
$Manifest = Get-Content $ManifestPath | ConvertFrom-Json

$Timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

# Prüfen ob Version existiert, sonst hinzufügen
$ExistingVersion = $Manifest[0].versions | Where-Object { $_.version -eq $Version -and $_.targetAbi -eq $TargetAbi }
if ($ExistingVersion) {
    $ExistingVersion.checksum = $Hash
    $ExistingVersion.timestamp = $Timestamp
} else {
    $NewVersion = @{
        version = $Version
        changelog = "Update"
        targetAbi = $TargetAbi
        sourceUrl = "https://github.com/Serekay/jellyfin-requests-bridge/releases/download/v$Version/$ZipName"
        checksum = $Hash
        timestamp = $Timestamp
    }
    $Manifest[0].versions = @($NewVersion) + $Manifest[0].versions
}

$Manifest | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath -Encoding UTF8

Write-Host "`n=== Release Complete ===" -ForegroundColor Green
Write-Host "ZIP: $ZipPath"
Write-Host "Checksum: $Hash"
Write-Host "`nNächste Schritte:"
Write-Host "1. Commit & Push zu GitHub"
Write-Host "2. GitHub Release erstellen mit Tag 'v$Version'"
Write-Host "3. ZIP-Datei zum Release hochladen"
Write-Host "4. manifest.json URL in Jellyfin hinzufügen:"
Write-Host "   https://raw.githubusercontent.com/Serekay/jellyfin-requests-bridge/main/manifest.json" -ForegroundColor Cyan
