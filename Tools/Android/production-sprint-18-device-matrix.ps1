param(
    [string[]]$Serials,
    [string]$PackageId = "com.alphaleverage.escapefromnodnarb",
    [int]$Attempts = 3,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\..\Artifacts\device-production-sprint-18-matrix")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

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

function Read-AdbProperty {
    param([string]$AdbPath, [string]$Serial, [string]$Property)
    return (& $AdbPath -s $Serial shell getprop $Property 2>&1 | Out-String).Trim()
}

$adbPath = Resolve-AdbPath
if ([string]::IsNullOrWhiteSpace($adbPath)) {
    throw "adb was not found. Set UNITY_ANDROID_SDK_ROOT or put adb on PATH."
}
if ($Attempts -lt 1) {
    throw "Attempts must be at least 1."
}

$connected = @(& $adbPath devices | Where-Object { $_ -match "^(?<serial>\S+)\s+device$" } | ForEach-Object { $Matches.serial })
$requested = @($Serials | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($requested.Count -eq 0) {
    $requested = $connected
}
if ($requested.Count -eq 0) {
    Write-Output "NODNARB_DEVICE_MATRIX status=BLOCKED reason=no_connected_device"
    exit 2
}

$gatePath = Join-Path $PSScriptRoot "production-sprint-17-device-gate.ps1"
if (-not (Test-Path -LiteralPath $gatePath -PathType Leaf)) {
    throw "Startup gate was not found: $gatePath"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$rows = @()
foreach ($serial in $requested) {
    if ($connected -notcontains $serial) {
        $rows += [pscustomobject]@{
            Serial = $serial
            Model = "unknown"
            Android = "unknown"
            DeviceKind = "NOT_CONNECTED"
            Startup = "BLOCKED"
        }
        continue
    }

    $model = Read-AdbProperty -AdbPath $adbPath -Serial $serial -Property "ro.product.model"
    $manufacturer = Read-AdbProperty -AdbPath $adbPath -Serial $serial -Property "ro.product.manufacturer"
    $android = Read-AdbProperty -AdbPath $adbPath -Serial $serial -Property "ro.build.version.release"
    $qemu = Read-AdbProperty -AdbPath $adbPath -Serial $serial -Property "ro.kernel.qemu"
    $deviceKind = if ($serial -match "(?i)^emulator-" -or $qemu -eq "1" -or $model -match "(?i)emulator|sdk_gphone|generic|aosp_x86|ranchu|goldfish") {
        "EMULATOR"
    } else {
        "PHYSICAL_CANDIDATE"
    }

    $safeSerial = $serial -replace "[^A-Za-z0-9_.-]", "_"
    $deviceOutput = Join-Path $OutputDirectory $safeSerial
    New-Item -ItemType Directory -Force -Path $deviceOutput | Out-Null
    $gateOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $gatePath `
        -Serial $serial -PackageId $PackageId -Attempts $Attempts `
        -OutputDirectory (Join-Path $deviceOutput "startup") 2>&1)
    $gateExitCode = $LASTEXITCODE
    $gateOutput | Set-Content -LiteralPath (Join-Path $deviceOutput "startup-summary.txt")
    $startup = if ($gateExitCode -eq 0 -and ($gateOutput -match "NODNARB_DEVICE_GATE status=PASS")) { "PASS" } else { "FAIL" }

    $rows += [pscustomobject]@{
        Serial = $serial
        Model = "$manufacturer $model".Trim()
        Android = $android
        DeviceKind = $deviceKind
        Startup = $startup
    }
}

$rows | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $OutputDirectory "device-matrix.csv")
$physicalRows = @($rows | Where-Object { $_.DeviceKind -eq "PHYSICAL_CANDIDATE" })
$startupPassRows = @($rows | Where-Object { $_.Startup -eq "PASS" })
$physicalStartupPassRows = @($physicalRows | Where-Object { $_.Startup -eq "PASS" })
$status = if ($physicalRows.Count -eq 0) {
    "BLOCKED"
} elseif ($physicalStartupPassRows.Count -lt $physicalRows.Count) {
    "FAIL"
} else {
    "OPEN"
}

$line = "NODNARB_DEVICE_MATRIX status=$status devices=$($rows.Count) physical_candidates=$($physicalRows.Count) startup_pass=$($startupPassRows.Count) physical_startup_pass=$($physicalStartupPassRows.Count) touch=OPEN lifecycle=OPEN performance=OPEN approval=REQUIRED"
Write-Output $line
$rows | ForEach-Object {
    Write-Output ("NODNARB_DEVICE_MATRIX_DEVICE serial={0} model={1} android={2} kind={3} startup={4}" -f $_.Serial, $_.Model, $_.Android, $_.DeviceKind, $_.Startup)
}
Set-Content -LiteralPath (Join-Path $OutputDirectory "matrix-summary.txt") -Value $line

if ($status -eq "BLOCKED") {
    exit 2
}
if ($status -ne "OPEN") {
    exit 1
}
exit 1
