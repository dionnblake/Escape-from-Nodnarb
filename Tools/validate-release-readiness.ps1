param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root does not exist: $ProjectRoot"
}

$bootstrapPath = Join-Path $ProjectRoot "Assets\EscapeFromNodnarb\Editor\ProjectBootstrapper.cs"
$settingsPath = Join-Path $ProjectRoot "ProjectSettings\ProjectSettings.asset"
$bootstrapText = if (Test-Path -LiteralPath $bootstrapPath -PathType Leaf) { Get-Content -LiteralPath $bootstrapPath -Raw } else { "" }
$settingsText = if (Test-Path -LiteralPath $settingsPath -PathType Leaf) { Get-Content -LiteralPath $settingsPath -Raw } else { "" }
$packagePass = $bootstrapText -match 'AndroidApplicationId\s*=\s*"com\.alphaleverage\.escapefromnodnarb"'
$identityPass = $settingsText -match '(?m)^\s*companyName:\s*Alpha Leverage\s*$' -and
    $settingsText -match '(?m)^\s*productName:\s*Escape from Nodnarb\s*$'
$provisionalSigning = $settingsText -match '(?m)^\s*androidUseCustomKeystore:\s*1\s*$'
$releaseApkPath = Join-Path $ProjectRoot "Builds\Android\EscapeFromNodnarb-release-local.apk"
$releaseAabPath = Join-Path $ProjectRoot "Builds\Android\EscapeFromNodnarb-release-local.aab"
$releaseAuditPath = Join-Path $ProjectRoot "Tools\Android\production-sprint-15-audit.ps1"
$releaseAuditOutput = @()
$releaseApkPass = $false
if ((Test-Path -LiteralPath $releaseApkPath -PathType Leaf) -and (Test-Path -LiteralPath $releaseAuditPath -PathType Leaf)) {
    $releaseAuditOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $releaseAuditPath -ApkPath $releaseApkPath 2>&1)
    $releaseApkPass = $LASTEXITCODE -eq 0 -and ($releaseAuditOutput -match "NODNARB_ANDROID_AUDIT status=PASS")
}
$releaseAabPass = Test-Path -LiteralPath $releaseAabPath -PathType Leaf
$artifactPass = $releaseApkPass -and $releaseAabPass
$releaseApkHash = if (Test-Path -LiteralPath $releaseApkPath -PathType Leaf) { (Get-FileHash -LiteralPath $releaseApkPath -Algorithm SHA256).Hash } else { "missing" }
$releaseAabHash = if (Test-Path -LiteralPath $releaseAabPath -PathType Leaf) { (Get-FileHash -LiteralPath $releaseAabPath -Algorithm SHA256).Hash } else { "missing" }

$provenancePath = Join-Path $ProjectRoot "Tools\validate-asset-provenance.ps1"
$provenanceOutput = @()
$provenancePass = $false
if (Test-Path -LiteralPath $provenancePath -PathType Leaf) {
    $provenanceOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $provenancePath -ProjectRoot $ProjectRoot 2>&1)
    $provenancePass = $LASTEXITCODE -eq 0 -and ($provenanceOutput -match "NODNARB_ASSET_PROVENANCE status=PASS")
}

$replayPath = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot "Artifacts") -Filter "replay-evidence-validation-*.txt" -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Select-Object -ExpandProperty FullName
$replayStatus = if (-not [string]::IsNullOrWhiteSpace($replayPath) -and (Test-Path -LiteralPath $replayPath -PathType Leaf)) {
    $replayText = Get-Content -LiteralPath $replayPath -Raw
    if ($replayText -match "status=PASS") { "PASS" }
    elseif ($replayText -match "status=BLOCKED") { "BLOCKED" }
    else { "OPEN" }
} else {
    "OPEN"
}

$localBlock = -not $packagePass -or -not $identityPass -or -not $artifactPass -or -not $provisionalSigning -or -not $provenancePass -or $replayStatus -eq "BLOCKED"
$overallStatus = if ($localBlock) { "BLOCKED" } else { "OPEN" }
$line = "NODNARB_RELEASE_READINESS status=$overallStatus package=$([string]::Copy($(if ($packagePass) { 'PASS' } else { 'FAIL' }))) identity=$([string]::Copy($(if ($identityPass) { 'PROVISIONAL' } else { 'FAIL' }))) android_artifact=$([string]::Copy($(if ($artifactPass) { 'PASS' } else { 'OPEN' }))) release_apk=$([string]::Copy($(if ($releaseApkPass) { 'PASS' } else { 'OPEN' }))) release_aab=$([string]::Copy($(if ($releaseAabPass) { 'PASS' } else { 'OPEN' }))) signing=$([string]::Copy($(if ($provisionalSigning) { 'CONFIGURED' } else { 'OPEN' }))) provenance=$([string]::Copy($(if ($provenancePass) { 'PASS' } else { 'FAIL' }))) replay=$replayStatus rights=OPEN final_art=OPEN privacy_store=OPEN ios=BLOCKED generic_verifier=UNVERIFIED approval=REQUIRED"
Write-Output $line
$releaseAuditOutput | ForEach-Object { Write-Output ("NODNARB_RELEASE_READINESS release_audit=" + $_) }
Write-Output ("NODNARB_RELEASE_READINESS release_apk_sha256=" + $releaseApkHash)
Write-Output ("NODNARB_RELEASE_READINESS release_aab_sha256=" + $releaseAabHash)
$provenanceOutput | ForEach-Object { Write-Output ("NODNARB_RELEASE_READINESS provenance_detail=" + $_) }

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $parent = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $artifactLines = @($line) + @($releaseAuditOutput | ForEach-Object { "NODNARB_RELEASE_READINESS release_audit=" + $_ }) + @(
        "NODNARB_RELEASE_READINESS release_apk_sha256=$releaseApkHash",
        "NODNARB_RELEASE_READINESS release_aab_sha256=$releaseAabHash"
    ) + @($provenanceOutput | ForEach-Object { "NODNARB_RELEASE_READINESS provenance_detail=" + $_ })
    Set-Content -LiteralPath $OutputPath -Value $artifactLines
}

if ($localBlock) {
    exit 2
}
exit 1
