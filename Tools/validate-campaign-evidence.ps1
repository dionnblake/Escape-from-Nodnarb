param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$PlayModeResultPath,
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root does not exist: $ProjectRoot"
}

$catalogPath = Join-Path $ProjectRoot "Assets\EscapeFromNodnarb\Tests\EditMode\CampaignCatalogTests.cs"
$runtimePath = Join-Path $ProjectRoot "Assets\EscapeFromNodnarb\Tests\PlayMode\BootstrapSmokeTests.cs"
if ([string]::IsNullOrWhiteSpace($PlayModeResultPath)) {
    $resultDirectory = Join-Path $ProjectRoot "Artifacts\TestResults"
    $PlayModeResultPath = Get-ChildItem -LiteralPath $resultDirectory -Filter "*playmode*.xml" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Select-Object -ExpandProperty FullName
}

$catalogText = if (Test-Path -LiteralPath $catalogPath -PathType Leaf) { Get-Content -LiteralPath $catalogPath -Raw } else { "" }
$runtimeText = if (Test-Path -LiteralPath $runtimePath -PathType Leaf) { Get-Content -LiteralPath $runtimePath -Raw } else { "" }
$catalogPass = $catalogText.Contains("CampaignHasTenOrderedAuthoredStages") -and
    $catalogText.Contains("CampaignAuthorsVariedWavesCardPathsAndBossBehaviors") -and
    $catalogText.Contains("CampaignHasDistinctAlienEcologyAndStoryboardBeats")
$runtimePass = $runtimeText.Contains("AllCampaignLevelsOpenTheirRuntimeStoryAndCombatPath") -and
    $runtimeText.Contains("AllCampaignLevelsCompleteTheirRuntimeTerminalPath")

$runtimeEvidencePass = $false
if (-not [string]::IsNullOrWhiteSpace($PlayModeResultPath) -and (Test-Path -LiteralPath $PlayModeResultPath -PathType Leaf)) {
    try {
        [xml]$testXml = Get-Content -LiteralPath $PlayModeResultPath -Raw
        $testCases = @($testXml.SelectNodes("//test-case"))
        $runtimeEvidencePass = @($testCases | Where-Object {
            $_.result -eq "Passed" -and ($_.name -match "AllCampaignLevels(OpenTheirRuntimeStoryAndCombatPath|CompleteTheirRuntimeTerminalPath)")
        }).Count -eq 2
    } catch {
        $runtimeEvidencePass = $false
    }
}

$status = if ($catalogPass -and $runtimePass -and $runtimeEvidencePass) { "OPEN" } else { "BLOCKED" }
$line = "NODNARB_CAMPAIGN_EVIDENCE status=$status catalog_source=$([string]::Copy($(if ($catalogPass) { 'PASS' } else { 'FAIL' }))) runtime_source=$([string]::Copy($(if ($runtimePass) { 'PASS' } else { 'FAIL' }))) runtime_test=$([string]::Copy($(if ($runtimeEvidencePass) { 'PASS' } else { 'OPEN' }))) ten_stage_hardware=BLOCKED replay_gate=OPEN approval=REQUIRED"
Write-Output $line

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $parent = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value $line
}

if ($status -ne "OPEN") {
    exit 2
}
exit 1
