<#
.SYNOPSIS
One-time generator for assets/pokemon-snapshot.json (M6 bundled pokemon data).

Fetches every Gen 1-5 species plus its evolution chain from PokeAPI v2 REST and
emits the sanitized, deterministic snapshot the Windows port bundles as an
embedded resource:

- bases:  hatch pool (evolution-line starters, id <= 649, Ditto excluded)
- lines:  per-base evolution chain tree + rarity inputs (capture rate, legendary,
          mythical flags); includes the Ditto line for disguise reveals
- names:  species names limited to the app-supported language codes

Requires network access to pokeapi.co. Re-runnable; output is sorted so runs are
byte-stable for identical upstream data.
#>
[CmdletBinding()]
param(
    [string]$OutPath = "$PSScriptRoot\..\assets\pokemon-snapshot.json",
    [int]$MaxId = 649,
    [int]$ThrottleLimit = 8
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$langs = @('ko', 'en', 'ja', 'ja-hrkt', 'es', 'fr', 'pt', 'pt-br', 'de')

function Get-WithRetry([string]$Uri) {
    for ($try = 1; $try -le 4; $try++) {
        try {
            return Invoke-RestMethod -Uri $Uri -TimeoutSec 30
        }
        catch {
            if ($try -eq 4) { throw }
            Start-Sleep -Milliseconds (400 * $try)
        }
    }
}

Write-Host "Fetching species 1..$MaxId (throttle $ThrottleLimit)..."
$speciesRaw = 1..$MaxId | ForEach-Object -Parallel {
    $uri = "https://pokeapi.co/api/v2/pokemon-species/$_"
    for ($try = 1; $try -le 4; $try++) {
        try {
            $s = Invoke-RestMethod -Uri $uri -TimeoutSec 30
            $chainId = 0
            if ($s.evolution_chain.url -match '/(\d+)/?$') { $chainId = [int]$Matches[1] }
            [pscustomobject]@{
                id          = [int]$s.id
                capture     = [int]$s.capture_rate
                legendary   = [bool]$s.is_legendary
                mythical    = [bool]$s.is_mythical
                isBase      = ($null -eq $s.evolves_from_species)
                chainId     = $chainId
                names       = $s.names
            }
            break
        }
        catch {
            if ($try -eq 4) { throw }
            Start-Sleep -Milliseconds (400 * $try)
        }
    }
} -ThrottleLimit $ThrottleLimit

$speciesById = @{}
foreach ($s in $speciesRaw) { $speciesById[$s.id] = $s }

$chainIds = $speciesRaw | ForEach-Object { $_.chainId } | Sort-Object -Unique
Write-Host "Fetching $($chainIds.Count) evolution chains..."
$chains = $chainIds | ForEach-Object -Parallel {
    $uri = "https://pokeapi.co/api/v2/evolution-chain/$_"
    for ($try = 1; $try -le 4; $try++) {
        try {
            $c = Invoke-RestMethod -Uri $uri -TimeoutSec 30
            [pscustomobject]@{ id = [int]$_; chain = $c.chain }
            break
        }
        catch {
            if ($try -eq 4) { throw }
            Start-Sleep -Milliseconds (400 * $try)
        }
    }
} -ThrottleLimit $ThrottleLimit
$chainById = @{}
foreach ($c in $chains) { $chainById[$c.id] = $c.chain }

function Convert-ChainNode($link) {
    $id = 0
    if ($link.species.url -match '/(\d+)/?$') { $id = [int]$Matches[1] }
    $node = [System.Text.Json.Nodes.JsonArray]::new()
    [void]$node.Add($id)
    $children = [System.Text.Json.Nodes.JsonArray]::new()
    foreach ($child in @($link.evolves_to)) {
        if ($null -ne $child) { [void]$children.Add((Convert-ChainNode $child)) }
    }
    [void]$node.Add($children)
    ,$node
}

function Collect-TreeIds($node) {
    [int]$node[0]
    foreach ($child in @($node[1])) { Collect-TreeIds $child }
}

function Find-Node($node, [int]$id) {
    if ([int]$node[0] -eq $id) { return ,$node }
    foreach ($child in @($node[1])) {
        $found = Find-Node $child $id
        if ($found) { return ,$found }
    }
    return $null
}

function Single-Node([int]$id) {
    $node = [System.Text.Json.Nodes.JsonArray]::new()
    [void]$node.Add($id)
    [void]$node.Add([System.Text.Json.Nodes.JsonArray]::new())
    ,$node
}

$lineSpeciesIds = [System.Collections.Generic.HashSet[int]]::new()
$lines = @()
foreach ($s in ($speciesRaw | Where-Object { $_.isBase } | Sort-Object id)) {
    $chainRoot = $chainById[$s.chainId]
    if ($null -eq $chainRoot) { throw "chain $($s.chainId) missing for base $($s.id)" }
    $found = Find-Node (Convert-ChainNode $chainRoot) $s.id
    if ($null -eq $found) { $tree = Single-Node $s.id } else { $tree = $found.DeepClone() }
    Collect-TreeIds $tree | ForEach-Object { [void]$lineSpeciesIds.Add($_) }
    $lines += [pscustomobject]@{
        base      = $s.id
        capture   = $s.capture
        legendary = $s.legendary
        mythical  = $s.mythical
        tree      = $tree
    }}

Write-Host "Building snapshot ($($lines.Count) lines, $($lineSpeciesIds.Count) species)..."
$names = [ordered]@{}
foreach ($id in ($lineSpeciesIds | Sort-Object)) {
    $byLang = [ordered]@{}
    foreach ($n in $speciesById[$id].names) {
        $code = ([string]$n.language.name).Trim().ToLowerInvariant()
        if ($langs -contains $code -and -not $byLang.Contains($code)) {
            $name = ([string]$n.name).Trim()
            if ($name.Length -gt 0) { $byLang[$code] = $name }
        }
    }
    $names[[string]$id] = $byLang
}

$bases = @()
foreach ($s in ($speciesRaw | Where-Object { $_.isBase -and $_.id -ne 132 } | Sort-Object id)) {
    $bases += [ordered]@{ id = $s.id; captureRate = $s.capture }
}

$linesNode = [System.Text.Json.Nodes.JsonArray]::new()
foreach ($line in $lines) {
    $o = [System.Text.Json.Nodes.JsonObject]::new()
    $o['base'] = $line.base
    $o['captureRate'] = $line.capture
    $o['legendary'] = $line.legendary
    $o['mythical'] = $line.mythical
    $o['tree'] = $line.tree
    [void]$linesNode.Add($o)
}

$namesNode = [System.Text.Json.Nodes.JsonObject]::new()
foreach ($key in $names.Keys) {
    $byLang = [System.Text.Json.Nodes.JsonObject]::new()
    foreach ($code in $names[$key].Keys) { $byLang[$code] = $names[$key][$code] }
    $namesNode[$key] = $byLang
}

$basesNode = [System.Text.Json.Nodes.JsonArray]::new()
foreach ($b in $bases) {
    $o = [System.Text.Json.Nodes.JsonObject]::new()
    $o['id'] = $b['id']
    $o['captureRate'] = $b['captureRate']
    [void]$basesNode.Add($o)
}

$root = [System.Text.Json.Nodes.JsonObject]::new()
$root['format'] = 'poketokenbar.pokemon-snapshot'
$root['schema'] = 1
$root['generatedAt'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')
$root['source'] = 'PokeAPI v2 REST (pokeapi.co); names limited to app languages'
$root['bases'] = $basesNode
$root['lines'] = $linesNode
$root['names'] = $namesNode

$out = [System.IO.Path]::GetFullPath($OutPath)
$outDir = Split-Path -Parent $out
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$options = [System.Text.Json.JsonSerializerOptions]::new()
$options.WriteIndented = $true
$options.Encoder = [System.Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping
[System.IO.File]::WriteAllText($out, $root.ToJsonString($options))
$sizeKb = [math]::Round((Get-Item $out).Length / 1KB)
Write-Host "Wrote $out ($sizeKb KB, $($bases.Count) bases, $($lines.Count) lines)"
