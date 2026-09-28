<#
.SYNOPSIS
Generator for assets/pokemon-snapshot.json (M6 bundled base data, M10 combat details).

Fetches every Gen 1-5 species plus its evolution chain and default-form combat
metadata from PokeAPI v2 REST and emits the sanitized, deterministic snapshot
the Windows port bundles as an embedded resource:

- bases:         hatch pool (evolution-line starters, id <= 649, Ditto excluded)
- lines:         per-base evolution chain tree + rarity inputs (capture rate, legendary,
                 mythical flags); includes the Ditto line for disguise reveals
- names:         species names limited to the app-supported language codes
- details:       (schema 2) per-species default-form combat data: height, weight,
                 base experience, gender rate, types, base stats, abilities, and
                 every move learnable in the "black-2-white-2" version group,
                 encoded as [slug, method, level, method, level, ...] arrays
- resourceNames: localized display names for every referenced type/ability/move
                 slug in the app-supported language codes

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
$statOrder = @('hp', 'attack', 'defense', 'special-attack', 'special-defense', 'speed')

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
                genderRate  = [int]$s.gender_rate
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

$detailIds = @($lineSpeciesIds | Sort-Object)
Write-Host "Building snapshot ($($lines.Count) lines, $($detailIds.Count) species)..."
Write-Host "Fetching pokemon combat details for $($detailIds.Count) species..."
$pokemonRaw = $detailIds | ForEach-Object -Parallel {
    $uri = "https://pokeapi.co/api/v2/pokemon/$_"
    for ($try = 1; $try -le 4; $try++) {
        try {
            $p = Invoke-RestMethod -Uri $uri -TimeoutSec 30
            $baseStats = @{}
            foreach ($stat in @($p.stats)) {
                $baseStats[[string]$stat.stat.name] = [int]$stat.base_stat
            }
            $moves = @()
            foreach ($move in @($p.moves)) {
                $methods = @()
                foreach ($row in @($move.version_group_details)) {
                    if ([string]$row.version_group.name -ne 'black-2-white-2') { continue }
                    $methods += [string]$row.move_learn_method.name
                    $methods += [int]$row.level_learned_at
                }
                if ($methods.Count -eq 0) { continue }
                $moves += [pscustomobject]@{
                    name    = [string]$move.move.name
                    methods = $methods
                }
            }
            [pscustomobject]@{
                id             = [int]$p.id
                name           = [string]$p.name
                height         = [int]$p.height
                weight         = [int]$p.weight
                baseExperience = $p.base_experience
                types          = @($p.types | Sort-Object slot | ForEach-Object { [string]$_.type.name })
                baseStats      = $baseStats
                abilities      = @($p.abilities | Sort-Object slot | ForEach-Object {
                    [pscustomobject]@{
                        name   = [string]$_.ability.name
                        slot   = [int]$_.slot
                        hidden = [bool]$_.is_hidden
                    }
                })
                moves          = $moves
            }
            break
        }
        catch {
            if ($try -eq 4) { throw }
            Start-Sleep -Milliseconds (400 * $try)
        }
    }
} -ThrottleLimit $ThrottleLimit

$pokemonById = @{}
foreach ($p in $pokemonRaw) { $pokemonById[$p.id] = $p }

$names = [ordered]@{}
foreach ($id in $detailIds) {
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

$detailsNode = [System.Text.Json.Nodes.JsonObject]::new()
$typeSlugs = [System.Collections.Generic.SortedSet[string]]::new()
$abilitySlugs = [System.Collections.Generic.SortedSet[string]]::new()
$moveSlugs = [System.Collections.Generic.SortedSet[string]]::new()
foreach ($id in $detailIds) {
    $p = $pokemonById[$id]
    if ($null -eq $p) { throw "pokemon details missing for species $id" }
    $o = [System.Text.Json.Nodes.JsonObject]::new()
    $o['name'] = $p.name
    $o['height'] = $p.height
    $o['weight'] = $p.weight
    if ($null -ne $p.baseExperience) { $o['baseExperience'] = [int]$p.baseExperience }
    $o['genderRate'] = $speciesById[$id].genderRate
    $typesNode = [System.Text.Json.Nodes.JsonArray]::new()
    foreach ($type in $p.types) {
        [void]$typesNode.Add($type)
        [void]$typeSlugs.Add($type)
    }
    $o['types'] = $typesNode
    $statsNode = [System.Text.Json.Nodes.JsonObject]::new()
    foreach ($stat in $statOrder) {
        if ($null -ne $p.baseStats[$stat]) { $statsNode[$stat] = [int]$p.baseStats[$stat] }
    }
    $o['baseStats'] = $statsNode
    $abilitiesNode = [System.Text.Json.Nodes.JsonArray]::new()
    foreach ($ability in $p.abilities) {
        $a = [System.Text.Json.Nodes.JsonObject]::new()
        $a['name'] = $ability.name
        $a['slot'] = $ability.slot
        $a['hidden'] = $ability.hidden
        [void]$abilitiesNode.Add($a)
        [void]$abilitySlugs.Add($ability.name)
    }
    $o['abilities'] = $abilitiesNode
    $movesNode = [System.Text.Json.Nodes.JsonArray]::new()
    foreach ($move in ($p.moves | Sort-Object { $_.name } -Culture ([Globalization.CultureInfo]::InvariantCulture))) {
        $entry = [System.Text.Json.Nodes.JsonArray]::new()
        [void]$entry.Add($move.name)
        foreach ($value in $move.methods) { [void]$entry.Add($value) }
        [void]$movesNode.Add($entry)
        [void]$moveSlugs.Add($move.name)
    }
    $o['moves'] = $movesNode
    $detailsNode[[string]$id] = $o
}

function Get-ResourceNames([string]$Kind, [string[]]$Slugs) {
    $bySlug = @{}
    foreach ($slug in $Slugs) {
        $resource = Get-WithRetry "https://pokeapi.co/api/v2/$Kind/$slug"
        $byLang = [ordered]@{}
        foreach ($n in @($resource.names)) {
            $code = ([string]$n.language.name).Trim().ToLowerInvariant()
            if ($langs -contains $code -and -not $byLang.Contains($code)) {
                $name = ([string]$n.name).Trim()
                if ($name.Length -gt 0) { $byLang[$code] = $name }
            }
        }
        $bySlug[$slug] = $byLang
    }
    $bySlug
}

Write-Host "Fetching localized names for $($typeSlugs.Count) types, $($abilitySlugs.Count) abilities, $($moveSlugs.Count) moves..."
$resourceNames = @{
    type    = Get-ResourceNames 'type' @($typeSlugs)
    ability = Get-ResourceNames 'ability' @($abilitySlugs)
    move    = Get-ResourceNames 'move' @($moveSlugs)
}
$resourceNamesNode = [System.Text.Json.Nodes.JsonObject]::new()
$resourceKinds = @(
    @{ kind = 'type'; slugs = @($typeSlugs) },
    @{ kind = 'ability'; slugs = @($abilitySlugs) },
    @{ kind = 'move'; slugs = @($moveSlugs) }
)
foreach ($entry in $resourceKinds) {
    $kindNode = [System.Text.Json.Nodes.JsonObject]::new()
    foreach ($slug in $entry.slugs) {
        $byLang = [System.Text.Json.Nodes.JsonObject]::new()
        foreach ($code in $resourceNames[$entry.kind][$slug].Keys) {
            $byLang[$code] = $resourceNames[$entry.kind][$slug][$code]
        }
        $kindNode[$slug] = $byLang
    }
    $resourceNamesNode[$entry.kind] = $kindNode
}

$root = [System.Text.Json.Nodes.JsonObject]::new()
$root['format'] = 'poketokenbar.pokemon-snapshot'
$root['schema'] = 2
$root['generatedAt'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')
$root['source'] = 'PokeAPI v2 REST (pokeapi.co); names limited to app languages; moves filtered to black-2-white-2'
$root['bases'] = $basesNode
$root['lines'] = $linesNode
$root['names'] = $namesNode
$root['details'] = $detailsNode
$root['resourceNames'] = $resourceNamesNode

$out = [System.IO.Path]::GetFullPath($OutPath)
$outDir = Split-Path -Parent $out
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$options = [System.Text.Json.JsonSerializerOptions]::new()
$options.WriteIndented = $true
$options.Encoder = [System.Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping
[System.IO.File]::WriteAllText($out, $root.ToJsonString($options))
$sizeKb = [math]::Round((Get-Item $out).Length / 1KB)
Write-Host "Wrote $out ($sizeKb KB, $($bases.Count) bases, $($lines.Count) lines, $($detailIds.Count) detail species)"
