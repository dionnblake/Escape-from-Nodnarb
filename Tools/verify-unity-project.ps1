param(
    [string]$ProjectDirectory = (Split-Path $PSScriptRoot -Parent),
    [string]$TestResultsDirectory,
    [string[]]$TestResultPaths,
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ProjectDirectory -PathType Container)) {
    throw "Project directory does not exist: $ProjectDirectory"
}

$requiredFiles = @(
    "PROJECT_SPEC.md",
    "AGENTS.md",
    "Packages/manifest.json",
    "ProjectSettings/ProjectVersion.txt",
    "Assets/EscapeFromNodnarb/Scenes/Main.unity",
    "Assets/EscapeFromNodnarb/Runtime/Gameplay/NodnarbGame.cs",
    "Assets/EscapeFromNodnarb/Runtime/Core/LocalProgress.cs",
    "Assets/EscapeFromNodnarb/Tests/EditMode/RunModelTests.cs",
    "Assets/EscapeFromNodnarb/Tests/PlayMode/BootstrapSmokeTests.cs",
    "Tools/validate-asset-provenance.ps1",
    "Tools/Android/production-sprint-15-audit.ps1"
)

$missing = @(
    $requiredFiles | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $ProjectDirectory $_) -PathType Leaf)
    }
)

$versionPath = Join-Path $ProjectDirectory "ProjectSettings/ProjectVersion.txt"
$versionText = if (Test-Path -LiteralPath $versionPath) {
    Get-Content -LiteralPath $versionPath -Raw
} else {
    ""
}
$unityVersion = if ($versionText -match "m_EditorVersion:\s*(\S+)") { $Matches[1] } else { "unknown" }

$bootstrapPath = Join-Path $ProjectDirectory "Assets/EscapeFromNodnarb/Editor/ProjectBootstrapper.cs"
$bootstrapText = if (Test-Path -LiteralPath $bootstrapPath) {
    Get-Content -LiteralPath $bootstrapPath -Raw
} else {
    ""
}
$packageId = if ($bootstrapText -match 'AndroidApplicationId\s*=\s*"([^"]+)"') { $Matches[1] } else { "unknown" }
$projectSpecText = Get-Content -LiteralPath (Join-Path $ProjectDirectory "PROJECT_SPEC.md") -Raw -ErrorAction SilentlyContinue
$originalityBoundary = $projectSpecText -match "Hero Wars names|Hero Wars.*assets|original"

$testEvidence = "OPEN"
$testFiles = @()
$testFailures = @()

function Test-UnityTestResult {
    param([System.IO.FileInfo]$TestFile)

    try {
        $testXml = [xml](Get-Content -LiteralPath $TestFile.FullName -Raw)
    } catch {
        return $false
    }

    $testRun = $testXml.'test-run'
    if ($null -eq $testRun) {
        return $false
    }

    $total = 0
    $passed = 0
    $failed = 0
    $totalParsed = [int]::TryParse([string]$testRun.total, [ref]$total)
    $passedParsed = [int]::TryParse([string]$testRun.passed, [ref]$passed)
    $failedParsed = [int]::TryParse([string]$testRun.failed, [ref]$failed)
    return $totalParsed -and $passedParsed -and $failedParsed -and
        ([string]$testRun.result -eq "Passed") -and $total -gt 0 -and
        $passed -eq $total -and $failed -eq 0
}

function Test-RequiredUnityTestKinds {
    param([object[]]$Files)

    $kinds = @($Files | ForEach-Object {
        if ($_.Name -match "(?i)editmode") { "EditMode" }
        if ($_.Name -match "(?i)playmode") { "PlayMode" }
    })
    return $Files.Count -eq 2 -and $kinds -contains "EditMode" -and $kinds -contains "PlayMode"
}

if (-not [string]::IsNullOrWhiteSpace($TestResultsDirectory)) {
    if (Test-Path -LiteralPath $TestResultsDirectory -PathType Container) {
        $testFiles = @(Get-ChildItem -LiteralPath $TestResultsDirectory -Filter "*.xml" -File | Sort-Object LastWriteTime -Descending | Select-Object -First 2)
        foreach ($testFile in $testFiles) {
            if (-not (Test-UnityTestResult -TestFile $testFile)) {
                $testFailures += $testFile.Name
            }
        }

        if (Test-RequiredUnityTestKinds -Files $testFiles -and $testFailures.Count -eq 0) {
            $testEvidence = "PASS"
        } elseif ($testFiles.Count -gt 0) {
            $testEvidence = "FAIL"
        }
    }
}
if ($TestResultPaths -and $TestResultPaths.Count -gt 0) {
    $testFiles = @()
    foreach ($testResultPath in $TestResultPaths) {
        if (Test-Path -LiteralPath $testResultPath -PathType Leaf) {
            $testFiles += Get-Item -LiteralPath $testResultPath
        }
    }

    $testFailures = @()
    foreach ($testFile in $testFiles) {
        if (-not (Test-UnityTestResult -TestFile $testFile)) {
            $testFailures += $testFile.Name
        }
    }

    if (Test-RequiredUnityTestKinds -Files $testFiles -and $testFailures.Count -eq 0) {
        $testEvidence = "PASS"
    } elseif ($testFiles.Count -gt 0) {
        $testEvidence = "FAIL"
    }
}

$sourceStatus = if ($missing.Count -eq 0 -and $unityVersion -ne "unknown" -and $packageId -ne "unknown" -and $originalityBoundary) { "PASS" } else { "FAIL" }
$statusLine = "NODNARB_UNITY_VERIFY status=$sourceStatus unity=$unityVersion package=$packageId required_files=$($requiredFiles.Count) missing=$($missing.Count) test_evidence=$testEvidence release_gates=OPEN"
Write-Output $statusLine
Write-Output ("NODNARB_UNITY_VERIFY details originality_boundary={0} test_files={1} test_failures={2}" -f $originalityBoundary, $testFiles.Count, $testFailures.Count)

if ($missing.Count -gt 0) {
    Write-Output ("NODNARB_UNITY_VERIFY missing=" + ($missing -join ","))
}
if ($testFailures.Count -gt 0) {
    Write-Output ("NODNARB_UNITY_VERIFY failed_tests=" + ($testFailures -join ","))
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $parent = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value @(
        $statusLine
        ("NODNARB_UNITY_VERIFY details originality_boundary={0} test_files={1} test_failures={2}" -f $originalityBoundary, $testFiles.Count, $testFailures.Count)
    )
}

if ($sourceStatus -ne "PASS" -or $testEvidence -ne "PASS") {
    exit 1
}
