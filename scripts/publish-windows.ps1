param([switch]$Launch)

$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'src\Ui\PokeTokenBar.Ui.csproj'
$profile = Join-Path $repo 'src\Ui\Properties\PublishProfiles\win-x64.pubxml'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\PokeTokenBar'
$exeName = 'PokeTokenBar.exe'
$installedExe = Join-Path $installDir $exeName
$shortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\PokeTokenBar.lnk'
$staging = Join-Path $repo 'artifacts\publish\win-x64'

$csprojText = Get-Content -LiteralPath $project -Raw
$versionMatch = [regex]::Match($csprojText, '<Version>\s*([^<\s]+)\s*</Version>')
if (-not $versionMatch.Success) { throw "Ui csproj has no <Version>" }
$version = $versionMatch.Groups[1].Value

$running = @(Get-Process -Name PokeTokenBar -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $installedExe })
foreach ($process in $running)
{
    $process.Kill()
    $process.WaitForExit()
}

dotnet publish $project -c Release -p:PublishProfile="$profile"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$stagedExe = Join-Path $staging $exeName
if (-not (Test-Path -LiteralPath $stagedExe)) { throw "publish output missing $stagedExe" }
$builtVersion = (Get-Item $stagedExe).VersionInfo.ProductVersion
if (($builtVersion -split '\+')[0] -ne $version) { throw "staged exe version '$builtVersion' does not match csproj version '$version'" }

if (Test-Path -LiteralPath $installDir) { Remove-Item -LiteralPath $installDir -Recurse -Force }
New-Item -ItemType Directory -Path $installDir | Out-Null
Get-ChildItem -LiteralPath $staging -File | Where-Object { $_.Name -notlike '*.pdb' } |
    Copy-Item -Destination $installDir -Force

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExe
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$installedExe,0"
$shortcut.Save()

$releaseDir = Join-Path $repo 'artifacts\release'
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

$changelogPath = Join-Path $repo 'CHANGELOG.md'
if (-not (Test-Path -LiteralPath $changelogPath)) { throw "CHANGELOG.md not found — release notes are generated from it" }
$changelogLines = Get-Content -LiteralPath $changelogPath -Encoding UTF8
$bodyStart = -1
for ($i = 0; $i -lt $changelogLines.Count; $i++)
{
    if ($changelogLines[$i] -match '^## v') { $bodyStart = $i; break }
}
if ($bodyStart -lt 0 -or $changelogLines[$bodyStart] -notmatch "^## v$([regex]::Escape($version))(\s|$)")
{
    throw "CHANGELOG.md's newest section is '$($changelogLines[$bodyStart])' but the csproj version is $version — add the v$version section before releasing"
}

$zipPath = Join-Path $releaseDir "PokeTokenBar-$version-win-x64.zip"
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path $stagedExe -DestinationPath $zipPath

$notes = @(
    "# PokeTokenBar for Windows v$version",
    "",
    "Cumulative release: everything since v0.10.0, the full macOS-parity",
    "polish series. Newest changes first.",
    ""
) + $changelogLines[$bodyStart..($changelogLines.Count - 1)] + @(
    "",
    "## Install",
    "",
    "1. Download ``PokeTokenBar-$version-win-x64.zip`` below and unzip it anywhere.",
    "2. Run ``PokeTokenBar.exe``. State lives under ``%LOCALAPPDATA%\PokeTokenBar``;",
    "   unzipping or deleting the exe never touches your save.",
    "",
    "Upgrading from an earlier version: replace the old files (or unzip to a new",
    "folder) and launch — the save, settings and sprite cache are picked up",
    "automatically."
)
$notesPath = Join-Path $releaseDir "release-notes-v$version.md"
Set-Content -LiteralPath $notesPath -Value $notes -Encoding utf8

Write-Host "PokeTokenBar $version installed:"
Write-Host "  exe      = $installedExe"
Write-Host "  shortcut = $shortcutPath"
Write-Host "  zip      = $zipPath"
Write-Host "  notes    = $notesPath"

if ($Launch) { Start-Process -FilePath $installedExe -WorkingDirectory $installDir }
