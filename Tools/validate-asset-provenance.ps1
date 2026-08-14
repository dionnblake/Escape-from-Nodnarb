param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$licensePath = Join-Path $ProjectRoot "LICENSES.md"
if (-not (Test-Path -LiteralPath $licensePath -PathType Leaf)) {
    throw "LICENSES.md was not found: $licensePath"
}

$requiredFiles = @(
    "Assets\EscapeFromNodnarb\Runtime\Resources\Captain\Captain_Unity.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Captain\captain_color_atlas.png",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Enemies\Rusher.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Enemies\Spitter.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Enemies\Blocker.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Enemies\Carrier.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Enemies\CrewSoldier.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\World\ExtractionBeacon.fbx",
    "Assets\EscapeFromNodnarb\Runtime\Resources\World\SporeArch.fbx",
    "blender escape from nodnarb\nodnarb_world_props_blender.py",
    "Assets\EscapeFromNodnarb\Runtime\Resources\Art\title-screen.png"
)

$missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $ProjectRoot $_) -PathType Leaf) })
if ($missingFiles.Count -gt 0) {
    throw ("Required provenance files are missing: " + ($missingFiles -join ", "))
}

$inventory = Get-Content -Raw -LiteralPath $licensePath
$requiredInventoryRows = @(
    "Captain FBX, color atlas, and emission mask",
    "Rusher, Spitter, Blocker, Carrier, and CrewSoldier FBX assets",
    "CrashedEngine, SnowArch, RelayBeacon, CanyonDebris, CrystalCluster, HiveGrowth, SporeArch, RuinGate, HiveObelisk, and ExtractionBeacon FBX assets",
    "Runtime/Resources/Art/title-screen.png",
    "Runtime UI fallback font"
)
$missingRows = @($requiredInventoryRows | Where-Object { $inventory.IndexOf($_, [System.StringComparison]::Ordinal) -lt 0 })
if ($missingRows.Count -gt 0) {
    throw ("LICENSES.md is missing provenance rows: " + ($missingRows -join "; "))
}

$licenseHash = (Get-FileHash -LiteralPath $licensePath -Algorithm SHA256).Hash
Write-Output ("NODNARB_ASSET_PROVENANCE status=PASS files_checked={0} inventory_rows_checked={1} licenses_sha256={2} rights_review=OPEN" -f $requiredFiles.Count, $requiredInventoryRows.Count, $licenseHash)
