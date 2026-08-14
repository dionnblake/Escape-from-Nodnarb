param(
    [Parameter(Mandatory = $true)]
    [string]$CsvPath,
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$requiredColumns = @(
    "Tester ID",
    "Device model",
    "Android OS version",
    "Completed Level 1?",
    "Replayed without being asked?",
    "Furthest level reached"
)
$forbiddenColumns = @("name", "email", "phone", "address", "contact")

if (-not (Test-Path -LiteralPath $CsvPath -PathType Leaf)) {
    Write-Output "NODNARB_REPLAY_EVIDENCE status=BLOCKED reason=missing_csv"
    exit 2
}

$rows = @(Import-Csv -LiteralPath $CsvPath)
if ($rows.Count -eq 0) {
    Write-Output "NODNARB_REPLAY_EVIDENCE status=BLOCKED reason=no_rows"
    exit 2
}

$columns = @($rows[0].PSObject.Properties.Name)
$missingColumns = @($requiredColumns | Where-Object { $columns -notcontains $_ })
$forbiddenFound = @($columns | Where-Object {
    $column = $_.ToLowerInvariant()
    $forbiddenColumns | Where-Object { $column -eq $_ -or $column.Contains($_) }
})
if ($missingColumns.Count -gt 0 -or $forbiddenFound.Count -gt 0) {
    Write-Output ("NODNARB_REPLAY_EVIDENCE status=FAIL rows={0} missing_columns={1} forbidden_columns={2}" -f $rows.Count, ($missingColumns -join ","), ($forbiddenFound -join ","))
    exit 1
}

function Read-Yes([object]$value) {
    return $value -match "^(?i:yes|y|true|1)$"
}

$completed = @($rows | Where-Object { Read-Yes $_.'Completed Level 1?' }).Count
$replayed = @($rows | Where-Object { Read-Yes $_.'Replayed without being asked?' }).Count
$furthestInvalid = @($rows | Where-Object {
    $value = 0
    -not [int]::TryParse($_.'Furthest level reached', [ref]$value) -or $value -lt 0 -or $value -gt 10
}).Count

$status = if ($rows.Count -eq 10 -and $completed -ge 7 -and $replayed -ge 5 -and $furthestInvalid -eq 0) { "PASS" } else { "OPEN" }
$line = "NODNARB_REPLAY_EVIDENCE status=$status rows=$($rows.Count) level1_completed=$completed/10 voluntary_replays=$replayed/10 invalid_levels=$furthestInvalid approval=REQUIRED"
Write-Output $line

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $parent = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value $line
}

if ($furthestInvalid -gt 0) {
    exit 1
}
