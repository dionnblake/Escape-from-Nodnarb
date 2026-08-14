param(
    [string]$ApkPath = (Join-Path $PSScriptRoot "..\..\Builds\Android\EscapeFromNodnarb-debug.apk"),
    [string]$ExpectedPackageId = "com.alphaleverage.escapefromnodnarb",
    [string]$ExpectedVersionName = "0.1.0",
    [int]$ExpectedVersionCode = 1
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ApkPath -PathType Leaf)) {
    throw "APK was not found: $ApkPath"
}

$aapt2Command = Get-Command aapt2 -ErrorAction SilentlyContinue
$aapt2Path = if ($aapt2Command) { $aapt2Command.Source } else { $null }
if ([string]::IsNullOrWhiteSpace($aapt2Path)) {
    $sdkRoots = @($env:UNITY_ANDROID_SDK_ROOT, $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk")) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    foreach ($sdkRoot in $sdkRoots) {
        $buildToolsRoot = Join-Path $sdkRoot "build-tools"
        if (-not (Test-Path -LiteralPath $buildToolsRoot -PathType Container)) {
            continue
        }

        $candidate = Get-ChildItem -LiteralPath $buildToolsRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "aapt2.exe" } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
            Select-Object -First 1
        if ($candidate) {
            $aapt2Path = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($aapt2Path)) {
    throw "aapt2 was not found. Set UNITY_ANDROID_SDK_ROOT or put aapt2 on PATH."
}

$badging = (& $aapt2Path dump badging $ApkPath 2>&1 | Out-String)
if ($LASTEXITCODE -ne 0) {
    throw "aapt2 could not inspect the APK."
}

if ($badging -notmatch "package: name='(?<package>[^']+)'") {
    throw "APK package metadata was not found."
}

$packageId = $Matches.package
if ($packageId -ne $ExpectedPackageId) {
    throw "Unexpected package ID: $packageId"
}

if ($badging -notmatch "targetSdkVersion:'(?<target>\d+)'") {
    throw "Target SDK metadata was not found."
}

$targetSdk = [int]$Matches.target
if ($targetSdk -lt 36) {
    throw "Target SDK is below the current local release floor: $targetSdk"
}

if ($badging -notmatch "versionCode='(?<versionCode>[^']+)' versionName='(?<versionName>[^']+)'") {
    throw "APK version metadata was not found."
}

$versionCode = [int]$Matches.versionCode
$versionName = $Matches.versionName
if ($versionName -ne $ExpectedVersionName -or $versionCode -ne $ExpectedVersionCode) {
    throw ("Unexpected APK version: {0} ({1}); expected {2} ({3})." -f $versionName, $versionCode, $ExpectedVersionName, $ExpectedVersionCode)
}

$arm64 = $badging -match "native-code:.*arm64-v8a"
if (-not $arm64) {
    throw "APK does not advertise arm64-v8a native code."
}

$hash = (Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash
$bytes = (Get-Item -LiteralPath $ApkPath).Length
Write-Output ("NODNARB_ANDROID_AUDIT status=PASS apkBytes={0} sha256={1} package={2} version={3} version_code={4} target_sdk={5} arm64={6} aapt2={7}" -f $bytes, $hash, $packageId, $versionName, $versionCode, $targetSdk, $arm64.ToString().ToLowerInvariant(), $aapt2Path)
