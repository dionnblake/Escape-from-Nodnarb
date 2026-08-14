param(
    [string]$Serial,
    [string]$PackageId = "com.alphaleverage.escapefromnodnarb",
    [int]$Attempts = 3,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\..\Artifacts\device-production-sprint-15-cold-starts")
)

$ErrorActionPreference = "Stop"

$adbCommand = Get-Command adb -ErrorAction SilentlyContinue
$adbPath = if ($adbCommand) { $adbCommand.Source } else { $null }
if ([string]::IsNullOrWhiteSpace($adbPath)) {
    $sdkRoots = @($env:UNITY_ANDROID_SDK_ROOT, $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk")) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($sdkRoot in $sdkRoots) {
        $candidate = Join-Path $sdkRoot "platform-tools\adb.exe"
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $adbPath = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($adbPath)) {
    throw "adb was not found. Set UNITY_ANDROID_SDK_ROOT or put adb on PATH."
}

$deviceLines = @(& $adbPath devices)
$connected = $deviceLines | Where-Object { $_ -match "^(?<serial>\S+)\s+device$" } | ForEach-Object { $Matches.serial }
if ([string]::IsNullOrWhiteSpace($Serial)) {
    $Serial = $connected | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($Serial) -or $connected -notcontains $Serial) {
    Write-Output "NODNARB_DEVICE_SMOKE status=BLOCKED reason=no_connected_device"
    exit 2
}

if ($Attempts -lt 1) {
    throw "Attempts must be at least 1."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$passCount = 0
for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
    & $adbPath -s $Serial logcat -c
    & $adbPath -s $Serial shell am force-stop $PackageId
    & $adbPath -s $Serial shell monkey -p $PackageId 1 | Out-Null

    $ready = $false
    for ($poll = 0; $poll -lt 60; $poll++) {
        Start-Sleep -Milliseconds 500
        $log = (& $adbPath -s $Serial logcat -d -t 800 2>&1 | Out-String)
        if ($log -match "NODNARB_STARTUP stage=runtime_ready") {
            $ready = $true
            break
        }
    }

    $finalLog = (& $adbPath -s $Serial logcat -d -t 1200 2>&1 | Out-String)
    $logPath = Join-Path $OutputDirectory ("attempt-{0}-logcat.txt" -f $attempt)
    [System.IO.File]::WriteAllText($logPath, $finalLog)
    $fatalPattern = "(?i)\bANR in\b|Input dispatching timed out|FATAL EXCEPTION|SIGSEGV|Fatal signal \d+|is not responding\b"
    $appContextPattern = "(?i)" + [regex]::Escape($PackageId) + "|UnityPlayerGameActivity"
    $fatal = $false
    $logLines = @($finalLog -split "`r?`n")
    for ($lineIndex = 0; $lineIndex -lt $logLines.Count; $lineIndex++) {
        if ($logLines[$lineIndex] -notmatch $fatalPattern) {
            continue
        }

        $contextStart = [Math]::Max(0, $lineIndex - 3)
        $contextEnd = [Math]::Min($logLines.Count - 1, $lineIndex + 3)
        $context = ($logLines[$contextStart..$contextEnd] -join "`n")
        if ($context -match $appContextPattern) {
            $fatal = $true
            break
        }
    }
    $activity = (& $adbPath -s $Serial shell dumpsys activity activities 2>&1 | Out-String)
    $activityPath = Join-Path $OutputDirectory ("attempt-{0}-activity.txt" -f $attempt)
    [System.IO.File]::WriteAllText($activityPath, $activity)

    if ($ready -and -not $fatal) {
        $passCount++
        Write-Output ("NODNARB_DEVICE_SMOKE attempt={0} status=PASS runtime_ready=true fatal_markers=false" -f $attempt)
    }
    else {
        Write-Output ("NODNARB_DEVICE_SMOKE attempt={0} status=FAIL runtime_ready={1} fatal_markers={2}" -f $attempt, $ready, $fatal)
    }
}

if ($passCount -ne $Attempts) {
    exit 1
}

Write-Output ("NODNARB_DEVICE_SMOKE status=PASS serial={0} attempts={1}" -f $Serial, $Attempts)
