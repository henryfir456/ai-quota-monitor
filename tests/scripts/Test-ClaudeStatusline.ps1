$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$helperPath = Join-Path $repositoryRoot 'src/AiQuotaMonitor.App/claude-statusline.ps1'
$scriptRoots = @(
    (Join-Path $repositoryRoot 'src/AiQuotaMonitor.App'),
    (Join-Path $repositoryRoot 'tests/scripts')
)
$scriptPaths = @(
    foreach ($scriptRoot in $scriptRoots) {
        Get-ChildItem -Path $scriptRoot -Filter '*.ps1' -File -Recurse
    }
)
foreach ($scriptFile in $scriptPaths) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptFile.FullName, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) {
        throw ('PowerShell parser failed for ' + $scriptFile.Name)
    }
}

$syntheticInput = '{"rate_limits":{"five_hour":{"used_percentage":23.5,"resets_at":1800000000},"seven_day":{"used_percentage":41.2,"resets_at":1800600000}},"session_id":"SYNTHETIC_SESSION_ONLY","transcript_path":"SYNTHETIC_PATH_ONLY"}'
$powerShell = (Get-Process -Id $PID).Path
$output = $syntheticInput | & $powerShell -NoProfile -File $helperPath
if ($LASTEXITCODE -ne 0 -or ($output -join "`n") -notmatch '5h 76\.5%.*7d 58\.8%') {
    throw 'Claude statusline helper did not produce the expected synthetic summary.'
}
if ($output -match 'SYNTHETIC_SESSION_ONLY|SYNTHETIC_PATH_ONLY') {
    throw 'Claude statusline helper leaked a synthetic session field.'
}

$malformedOutput = '{ malformed private-input }' | & $powerShell -NoProfile -File $helperPath
if ($LASTEXITCODE -ne 0 -or ($malformedOutput -join "`n") -notmatch 'N/A') {
    throw 'Claude statusline helper did not fail safely for invalid JSON.'
}
if ($malformedOutput -match 'malformed private-input') {
    throw 'Claude statusline helper exposed invalid input in its output.'
}
