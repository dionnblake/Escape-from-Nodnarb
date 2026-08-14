param(
    [string]$Serial,
    [string]$PackageId = "com.alphaleverage.escapefromnodnarb",
    [int]$Attempts = 3,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\..\Artifacts\device-production-sprint-17-startup")
)

$ErrorActionPreference = "Stop"

function Resolve-AdbPath {
    $command = Get-Command adb -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $sdkRoots = @($env:UNITY_ANDROID_SDK_ROOT, $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk")) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($sdkRoot in $sdkRoots) {
        $candidate = Join-Path $sdkRoot "platform-tools\adb.exe"
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    return $null
}

function Test-AppFatalMarkers {
    param(
        [string]$LogText,
        [string]$PackageId
    )

    $fatalPattern = "(?i)\bANR in\b|Input dispatching timed out|Application Not Responding|FATAL EXCEPTION|SIGSEGV|Fatal signal \d+|Focus.*timeout|timeout.*focus|is not responding\b"
    $appContextPattern = "(?i)" + [regex]::Escape($PackageId) + "|UnityPlayerGameActivity"
    $logLines = @($LogText -split "`r?`n")
    for ($lineIndex = 0; $lineIndex -lt $logLines.Count; $lineIndex++) {
        if ($logLines[$lineIndex] -notmatch $fatalPattern) {
            continue
        }

        $contextStart = [Math]::Max(0, $lineIndex - 3)
        $contextEnd = [Math]::Min($logLines.Count - 1, $lineIndex + 3)
        $context = ($logLines[$contextStart..$contextEnd] -join "`n")
        if ($context -match $appContextPattern) {
            return $true
        }
    }

    return $false
}

$adbPath = Resolve-AdbPath
if ([string]::IsNullOrWhiteSpace($adbPath)) {
    throw "adb was not found. Set UNITY_ANDROID_SDK_ROOT or put adb on PATH."
}
if ($Attempts -lt 1) {
    throw "Attempts must be at least 1."
}

$connected = @(& $adbPath devices | Where-Object { $_ -match "^(?<serial>\S+)\s+device$" } | ForEach-Object { $Matches.serial })
if ([string]::IsNullOrWhiteSpace($Serial)) {
    $Serial = $connected | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($Serial) -or $connected -notcontains $Serial) {
    Write-Output "NODNARB_DEVICE_GATE status=BLOCKED reason=no_connected_device"
    exit 2
}

$packagePath = (& $adbPath -s $Serial shell pm path $PackageId 2>&1 | Out-String).Trim()
if ($packagePath -notmatch "package:") {
    Write-Output ("NODNARB_DEVICE_GATE status=BLOCKED reason=package_not_installed serial={0} package={1}" -f $Serial, $PackageId)
    exit 2
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$passCount = 0
for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
    & $adbPath -s $Serial logcat -c
    & $adbPath -s $Serial shell am force-stop $PackageId
    # Unity 6's generated launcher activity is discoverable by monkey even on
    # emulator images where package-scoped am start cannot resolve the intent.
    $savedErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $startOutput = & $adbPath -s $Serial shell monkey -p $PackageId 1 2>&1 | Out-String
        $startExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    $ready = $false
    for ($poll = 0; $poll -lt 60; $poll++) {
        Start-Sleep -Milliseconds 500
        $pollLog = (& $adbPath -s $Serial logcat -d -t 1000 2>&1 | Out-String)
        if ($pollLog -match "NODNARB_STARTUP stage=runtime_ready") {
            $ready = $true
            break
        }
        if (Test-AppFatalMarkers -LogText $pollLog -PackageId $PackageId) {
            break
        }
    }

    $finalLog = (& $adbPath -s $Serial logcat -d -t 1600 2>&1 | Out-String)
    $windowState = (& $adbPath -s $Serial shell dumpsys window windows 2>&1 | Out-String)
    $activityState = (& $adbPath -s $Serial shell dumpsys activity activities 2>&1 | Out-String)
    $displaySize = (& $adbPath -s $Serial shell wm size 2>&1 | Out-String).Trim()
    $displayDensity = (& $adbPath -s $Serial shell wm density 2>&1 | Out-String).Trim()
    $fatalMarkers = Test-AppFatalMarkers -LogText $finalLog -PackageId $PackageId
    $escapedPackageId = [regex]::Escape($PackageId)
    $windowFocusOwned = $windowState -match ("(?im)mCurrentFocus=.*" + $escapedPackageId + "|mFocusedApp=.*" + $escapedPackageId)
    $activityOwned = $activityState -match ("(?im)(ResumedActivity|topResumedActivity|topActivity)=?.*" + $escapedPackageId)
    # Android can transition the transient window-focus line after the Unity
    # activity is already resumed. Treat a package-owned resumed activity as
    # foreground ownership while retaining the raw window dump as evidence.
    $focusOwned = $windowFocusOwned -or $activityOwned
    $attemptStatus = $startExitCode -eq 0 -and $ready -and $focusOwned -and $activityOwned -and -not $fatalMarkers

    Set-Content -LiteralPath (Join-Path $OutputDirectory ("attempt-{0}-start.txt" -f $attempt)) -Value $startOutput
    Set-Content -LiteralPath (Join-Path $OutputDirectory ("attempt-{0}-logcat.txt" -f $attempt)) -Value $finalLog
    Set-Content -LiteralPath (Join-Path $OutputDirectory ("attempt-{0}-window.txt" -f $attempt)) -Value @($windowState, $activityState, $displaySize, $displayDensity)

    if ($attemptStatus) {
        $passCount++
    }
    Write-Output ("NODNARB_DEVICE_GATE attempt={0} status={1} launch_exit={2} runtime_ready={3} focus_owned={4} activity_owned={5} fatal_markers={6} display=({7})" -f $attempt, $(if ($attemptStatus) { "PASS" } else { "FAIL" }), $startExitCode, $ready, $focusOwned, $activityOwned, $fatalMarkers, $displaySize.Trim())
}

$overall = $passCount -eq $Attempts
Write-Output ("NODNARB_DEVICE_GATE status={0} serial={1} package={2} attempts={3} startup_only=true gameplay_marker=OPEN" -f $(if ($overall) { "PASS" } else { "FAIL" }), $Serial, $PackageId, $Attempts)
if (-not $overall) {
    exit 1
}
