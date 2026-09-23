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

Write-Host "PokeTokenBar $version installed:"
Write-Host "  exe      = $installedExe"
Write-Host "  shortcut = $shortcutPath"

if ($Launch) { Start-Process -FilePath $installedExe -WorkingDirectory $installDir }
