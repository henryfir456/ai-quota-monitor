param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Commit,
    [Parameter(Mandatory = $true)][string]$UnitTestSummary,
    [Parameter(Mandatory = $true)][string]$SmokeEvidencePath,
    [Parameter(Mandatory = $true)][string]$WorkflowRunUrl,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
if ($Version -ne '0.1.2' -or ($env:GITHUB_REF_TYPE -eq 'tag' -and $env:GITHUB_REF_NAME -ne ('v' + $Version))) {
    throw 'Release finalization is restricted to v0.1.2.'
}

$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$exePath = Join-Path $output ('AiQuotaMonitor-' + $Version + '-win-x64.exe')
$zipPath = Join-Path $output ('AiQuotaMonitor-' + $Version + '-win-x64-portable.zip')
$evidencePath = (Resolve-Path -LiteralPath $SmokeEvidencePath).Path
foreach ($requiredFile in @($exePath, $zipPath, $evidencePath)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw 'A packaged release artifact or smoke evidence file is missing.'
    }
}

$evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
if ($evidence.Outcome -ne 'PASS' -or
    $evidence.PositiveStartup.GateResult -ne 'PASS' -or
    $evidence.NegativeControl.GateResult -ne 'REJECTED_AS_EXPECTED' -or
    $evidence.NegativeControl.ExceptionType -ne 'System.Windows.Markup.XamlParseException' -or
    $evidence.RunnerOS -ne 'Windows' -or
    $evidence.SourceCommit -ne $Commit -or
    $evidence.WorkflowRunUrl -ne $WorkflowRunUrl) {
    throw 'Windows startup smoke evidence is FAIL, UNKNOWN, or incomplete.'
}

$exeHash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash.ToLowerInvariant()
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($exeHash -ne $evidence.ExecutableSha256 -or $zipHash -ne $evidence.PackageSha256) {
    throw 'Release EXE or ZIP differs from the exact package tested by the Windows smoke gate.'
}

$temporaryExe = Join-Path ([IO.Path]::GetTempPath()) ('AiQuotaMonitor-embedded-' + [guid]::NewGuid().ToString('N') + '.exe')
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $entry = $archive.Entries | Where-Object FullName -eq 'AiQuotaMonitor.exe' | Select-Object -First 1
        $sumEntry = $archive.Entries | Where-Object FullName -eq 'SHA256SUMS.txt' | Select-Object -First 1
        if ($null -eq $entry -or $null -eq $sumEntry) {
            throw 'Portable ZIP is missing its executable or internal checksum list.'
        }

        $entryStream = $entry.Open()
        try {
            $destination = [IO.File]::Create($temporaryExe)
            try { $entryStream.CopyTo($destination) } finally { $destination.Dispose() }
        }
        finally { $entryStream.Dispose() }

        $sumStream = $sumEntry.Open()
        try {
            $reader = [IO.StreamReader]::new($sumStream)
            try { $internalSums = $reader.ReadToEnd() } finally { $reader.Dispose() }
        }
        finally { $sumStream.Dispose() }
    }
    finally { $archive.Dispose() }

    $embeddedHash = (Get-FileHash -LiteralPath $temporaryExe -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($embeddedHash -ne $exeHash -or $internalSums -notmatch ('(?m)^' + [regex]::Escape($exeHash) + '\s+AiQuotaMonitor\.exe\s*$')) {
        throw 'Standalone EXE, portable ZIP executable, and embedded SHA-256 do not match.'
    }
}
finally {
    Remove-Item -LiteralPath $temporaryExe -Force -ErrorAction SilentlyContinue
}

if ([string]::IsNullOrWhiteSpace($UnitTestSummary) -or $UnitTestSummary -notmatch 'failed=0; notExecuted=0$') {
    throw 'Windows unit test summary is missing or does not report zero failures.'
}

$evidenceDestination = Join-Path $output 'WINDOWS_SMOKE_EVIDENCE.json'
Copy-Item -LiteralPath $evidencePath -Destination $evidenceDestination -Force
$positive = $evidence.PositiveStartup
$negative = $evidence.NegativeControl
$testReport = @"
# Windows Release Test Report

- Version: $Version
- Source commit: $Commit
- Runner: GitHub Actions $($evidence.RunnerOS) runner, Windows version $($evidence.RunnerWindowsVersion) (not Windows 11 device UAT)
- Workflow run: $WorkflowRunUrl
- Observed at: $($evidence.ObservedAtUtc)
- Release order: restore/build/test → package → final EXE smoke test → package SHA-256 verification → Release
- Windows unit tests: $UnitTestSummary
- Smoke command: `tests/scripts/Test-WindowsExecutableSmoke.ps1 -ExecutablePath <packaged EXE> -PackagePath <portable ZIP> -VerifyNegativeControl`
- Smoke result: PASS; visible main window `$($positive.WindowTitle)`; two provider cards and four progress bars initialized with OneWay bindings.
- Process check: PASS; process remained alive for $($positive.ProcessAliveForSeconds) seconds; window handle/title stayed available; closed cleanly.
- Windows crash events: $($positive.ApplicationCrashEventCount) matching .NET Runtime 1026 / Application Error 1000 events.
- Offline/no-account/no-token: PASS; isolated settings path, credential environment variables removed from child process, and smoke mode skips all Provider refresh / CLI / account / token use.
- Reverse control: PASS; injected $($negative.ExceptionType) exited nonzero before the ready marker and was rejected by the same gate.
- Tested EXE SHA-256: $exeHash
- Tested portable ZIP SHA-256: $zipHash
- Unsigned: Yes
- Windows 11 physical-device UAT: HUMAN_TEST_PENDING; see UAT_WINDOWS11.md.
"@
Set-Content -LiteralPath (Join-Path $output 'TEST_REPORT.txt') -Value $testReport -Encoding utf8

$sumLines = @()
Get-ChildItem -LiteralPath $output -File | Where-Object Name -ne 'SHA256SUMS.txt' | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $sumLines += ($hash + '  ' + $_.Name)
}
if ($sumLines.Count -eq 0) {
    throw 'No release files are available to checksum.'
}
Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Value ($sumLines -join "`n") -Encoding ascii

Write-Output "Verified smoke-tested EXE SHA-256: $exeHash"
Write-Output "Verified smoke-tested portable ZIP SHA-256: $zipHash"
Write-Output 'Final release asset SHA-256 manifest created after the Windows smoke gate.'
