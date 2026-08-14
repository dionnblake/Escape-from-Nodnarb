param(
    [string]$Serial,
    [string]$PackageId = "com.alphaleverage.escapefromnodnarb",
    [int]$DurationSeconds = 30,
    [switch]$Launch,
    [switch]$RequireCombatMarker,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\..\Artifacts\device-production-sprint-17-performance")
)

$ErrorActionPreference = "Stop"

function Resolve-AdbPath {
    $command = Get-Command adb -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $sdkRoots = @($env:UNITY_ANDROID_SDK_ROOT, $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk")) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($sdkRoot in $sdkRoots) {
        $candidate = Join-Path $sdkRoot "platform-tools\adb.exe"
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    return $null
}

$adbPath = Resolve-AdbPath
if ([string]::IsNullOrWhiteSpace($adbPath)) { throw "adb was not found." }
if ($DurationSeconds -lt 1 -or $DurationSeconds -gt 600) { throw "DurationSeconds must be between 1 and 600." }

$connected = @(& $adbPath devices | Where-Object { $_ -match "^(?<serial>\S+)\s+device$" } | ForEach-Object { $Matches.serial })
if ([string]::IsNullOrWhiteSpace($Serial)) { $Serial = $connected | Select-Object -First 1 }
if ([string]::IsNullOrWhiteSpace($Serial) -or $connected -notcontains $Serial) {
    Write-Output "NODNARB_PERF_CAPTURE status=BLOCKED reason=no_connected_device"
    exit 2
}

$packagePath = (& $adbPath -s $Serial shell pm path $PackageId 2>&1 | Out-String).Trim()
if ($packagePath -notmatch "package:") {
    Write-Output ("NODNARB_PERF_CAPTURE status=BLOCKED reason=package_not_installed package={0}" -f $PackageId)
    exit 2
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& $adbPath -s $Serial logcat -c
& $adbPath -s $Serial shell dumpsys gfxinfo $PackageId reset | Out-Null
if ($Launch) {
    & $adbPath -s $Serial shell am force-stop $PackageId
    & $adbPath -s $Serial shell monkey -p $PackageId 1 | Out-Null
}
Start-Sleep -Seconds $DurationSeconds

$gfx = & $adbPath -s $Serial shell dumpsys gfxinfo $PackageId framestats 2>&1 | Out-String
$mem = & $adbPath -s $Serial shell dumpsys meminfo $PackageId 2>&1 | Out-String
$thermal = & $adbPath -s $Serial shell dumpsys thermalservice 2>&1 | Out-String
$log = & $adbPath -s $Serial logcat -d -t 3000 2>&1 | Out-String
$unityLog = (($log -split "`r?`n") | Where-Object { $_ -match "NODNARB_|/Unity\s" } | Out-String)
Set-Content -LiteralPath (Join-Path $OutputDirectory "gfxinfo-framestats.txt") -Value $gfx
Set-Content -LiteralPath (Join-Path $OutputDirectory "meminfo.txt") -Value $mem
Set-Content -LiteralPath (Join-Path $OutputDirectory "thermalservice.txt") -Value $thermal
Set-Content -LiteralPath (Join-Path $OutputDirectory "logcat.txt") -Value $log
Set-Content -LiteralPath (Join-Path $OutputDirectory "logcat-unity.txt") -Value $unityLog

$budgetMarker = $unityLog -match "NODNARB_BUDGET"
$stabilityMarker = $unityLog -match "NODNARB_STABILITY"
$combatStatus = if ($budgetMarker -and $stabilityMarker) { "PASS" } else { "OPEN" }
$captureStatus = if ($combatStatus -eq "PASS") { "PASS" } else { "BLOCKED" }
$line = "NODNARB_PERF_CAPTURE status=$captureStatus serial=$Serial duration_s=$DurationSeconds gfxinfo=true meminfo=true thermal=true combat_budget=$combatStatus"
Write-Output $line
if ($combatStatus -ne "PASS") {
    Write-Output "NODNARB_PERF_CAPTURE status=BLOCKED reason=missing_terminal_combat_markers"
    exit 2
}
