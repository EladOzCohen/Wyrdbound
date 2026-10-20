# Builds everything players download, in one go:
#   1. the game (Unity batch build)            -> Builds\Release\Wyrdbound\Wyrdbound.exe
#   2. the installer (Inno Setup)              -> Builds\Release\Wyrdbound-Setup-<version>.exe
#   3. a portable zip (no install needed)      -> Builds\Release\Wyrdbound-<version>-Windows-Portable.zip
# Usage (from the project folder):  powershell -ExecutionPolicy Bypass -File tools\make-release.ps1
# The version comes from Assets\Wyrdbound\Editor\ReleaseTools.cs (ReleaseTools.Version).

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe",
    [string]$Iscc = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$versionLine = Select-String -Path "Assets\Wyrdbound\Editor\ReleaseTools.cs" -Pattern 'Version = "([^"]+)"' | Select-Object -First 1
$version = $versionLine.Matches[0].Groups[1].Value
Write-Host "== Wyrdbound $version release =="

if (-not (Test-Path $Unity)) { throw "Unity editor not found at $Unity (pass -Unity <path>)" }
if ($Iscc -eq "") {
    foreach ($c in @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $c) { $Iscc = $c; break }
    }
}
if ($Iscc -eq "" -or -not (Test-Path $Iscc)) { throw "Inno Setup not found. Install it with: winget install JRSoftware.InnoSetup" }

New-Item -ItemType Directory -Force "Builds\Release" | Out-Null

Write-Host "[1/3] Building the game with Unity (a few minutes)..."
$log = Join-Path $root "Builds\Release\build.log"
$p = Start-Process -FilePath $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-nographics", "-projectPath", "`"$root`"",
    "-executeMethod", "Wyrdbound.EditorTools.ReleaseTools.BuildRelease", "-logFile", "`"$log`"")
if ($p.ExitCode -ne 0 -or -not (Test-Path "Builds\Release\Wyrdbound\Wyrdbound.exe")) { throw "Unity build failed (see $log)" }

Write-Host "[2/3] Building the installer with Inno Setup..."
& $Iscc "/Qp" "/DAppVersion=$version" "installer\Wyrdbound.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }

Write-Host "[3/3] Packing the portable zip..."
$zip = "Builds\Release\Wyrdbound-$version-Windows-Portable.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "Builds\Release\Wyrdbound" -DestinationPath $zip -CompressionLevel Optimal

Write-Host ""
Write-Host "Done:"
foreach ($f in @("Builds\Release\Wyrdbound-Setup-$version.exe", $zip)) {
    $i = Get-Item $f
    $h = (Get-FileHash $f -Algorithm SHA256).Hash
    Write-Host ("  {0}  {1:N0} MB  sha256 {2}" -f $i.Name, ($i.Length / 1MB), $h)
}
