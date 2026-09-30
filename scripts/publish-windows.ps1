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

function Get-ChangelogBody($path, $version)
{
    if (-not (Test-Path -LiteralPath $path)) { throw "$path not found — release notes are generated from the changelogs" }
    $lines = Get-Content -LiteralPath $path -Encoding UTF8
    $bodyStart = -1
    for ($i = 0; $i -lt $lines.Count; $i++)
    {
        if ($lines[$i] -match '^## v') { $bodyStart = $i; break }
    }
    if ($bodyStart -lt 0 -or $lines[$bodyStart] -notmatch "^## v$([regex]::Escape($version))(\s|$)")
    {
        throw "$(Split-Path $path -Leaf)'s newest section is '$($lines[$bodyStart])' but the csproj version is $version — add the v$version section before releasing"
    }
    return ,$lines[$bodyStart..($lines.Count - 1)]
}

$enBody = Get-ChangelogBody (Join-Path $repo 'CHANGELOG.md') $version
$koBody = Get-ChangelogBody (Join-Path $repo 'CHANGELOG.ko.md') $version

$zipPath = Join-Path $releaseDir "PokeTokenBar-$version-win-x64.zip"
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path $stagedExe -DestinationPath $zipPath

$enNotes = @(
    "# PokeTokenBar for Windows v$version",
    "",
    "Cumulative release: everything since v0.10.0, the full macOS-parity",
    "polish series. Newest changes first.",
    ""
) + $enBody + @(
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
$enNotesPath = Join-Path $releaseDir "release-notes-v$version.md"
Set-Content -LiteralPath $enNotesPath -Value $enNotes -Encoding utf8

$koNotes = @(
    "# PokeTokenBar for Windows v$version",
    "",
    "v0.10.0 이후의 모든 변화를 담은 누적 릴리스입니다. 새 변화가 위에",
    "옵니다.",
    ""
) + $koBody + @(
    "",
    "## 설치",
    "",
    "1. 아래 ``PokeTokenBar-$version-win-x64.zip``를 내려받아 아무 곳에나",
    "   압축을 풉니다.",
    "2. ``PokeTokenBar.exe``를 실행합니다. 상태는",
    "   ``%LOCALAPPDATA%\PokeTokenBar``에 저장됩니다. exe를 압축 해제하거나",
    "   지워도 저장 데이터에는 영향이 없습니다.",
    "",
    "이전 버전에서 업그레이드: 기존 파일을 교체하거나(또는 새 폴더에 압축을",
    "풀어) 실행하면 저장 데이터, 설정, 스프라이트 캐시를 자동으로 이어",
    "받습니다."
)
$koNotesPath = Join-Path $releaseDir "release-notes-v$version.ko.md"
Set-Content -LiteralPath $koNotesPath -Value $koNotes -Encoding utf8

Write-Host "PokeTokenBar $version installed:"
Write-Host "  exe      = $installedExe"
Write-Host "  shortcut = $shortcutPath"
Write-Host "  zip      = $zipPath"
Write-Host "  notes    = $enNotesPath"
Write-Host "  notes ko = $koNotesPath"

if ($Launch) { Start-Process -FilePath $installedExe -WorkingDirectory $installDir }
