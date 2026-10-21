[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExecutablePath,
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [Parameter(Mandatory = $true)][string]$SourceCommit,
    [Parameter(Mandatory = $true)][string]$WorkflowRunUrl,
    [ValidateRange(10, 120)][int]$StartupTimeoutSeconds = 45,
    [ValidateRange(3, 30)][int]$StabilitySeconds = 5,
    [switch]$VerifyNegativeControl
)

$ErrorActionPreference = 'Stop'
$executable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$evidence = [IO.Path]::GetFullPath($EvidencePath)
$exeName = [IO.Path]::GetFileName($executable)

function Get-ApplicationCrashEvents {
    param([DateTimeOffset]$StartedAt, [string]$ApplicationName)

    try {
        $records = @(Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $StartedAt.UtcDateTime } -MaxEvents 200 -ErrorAction Stop)
    }
    catch {
        if ($_.FullyQualifiedErrorId -like 'NoMatchingEventsFound*') {
            return @()
        }

        throw 'Unable to read the Windows Application event log; smoke result is UNKNOWN.'
    }

    $escapedName = [regex]::Escape($ApplicationName)
    return @($records | Where-Object {
        $_.Id -in 1000, 1026 -and
        $_.ProviderName -in 'Application Error', '.NET Runtime' -and
        $_.TimeCreated -ge $StartedAt.UtcDateTime -and
        $_.Message -match $escapedName
    } | ForEach-Object {
        [ordered]@{
            EventId = $_.Id
            Provider = $_.ProviderName
            TimeCreatedUtc = $_.TimeCreated.ToUniversalTime().ToString('o')
        }
    })
}

function Invoke-ApplicationStartup {
    param(
        [string]$Scenario,
        [switch]$InjectFailure
    )

    $runDirectory = Join-Path ([IO.Path]::GetDirectoryName($evidence)) ('smoke-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
    $readyFile = Join-Path $runDirectory 'ui-ready.json'
    $exceptionFile = Join-Path $runDirectory 'unhandled-exception.json'
    $settingsFile = Join-Path $runDirectory 'isolated-settings.json'
    $startedAt = [DateTimeOffset]::UtcNow.AddSeconds(-1)
    $initialHash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
    $initialPackageHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $executable
    $startInfo.WorkingDirectory = $runDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Normal
    foreach ($argument in @(
        '--smoke-test',
        '--smoke-ready-file', $readyFile,
        '--smoke-exception-file', $exceptionFile,
        '--smoke-settings-file', $settingsFile
    )) {
        [void]$startInfo.ArgumentList.Add($argument)
    }
    if ($InjectFailure) {
        [void]$startInfo.ArgumentList.Add('--smoke-test-force-failure')
    }
    foreach ($credentialVariable in @(
        'ANTHROPIC_API_KEY', 'ANTHROPIC_AUTH_TOKEN', 'CLAUDE_CODE_OAUTH_TOKEN',
        'OPENAI_API_KEY', 'CODEX_API_KEY'
    )) {
        [void]$startInfo.Environment.Remove($credentialVariable)
    }

    $process = $null
    $failure = $null
    $ready = $null
    $eventErrors = @()
    try {
        $process = [Diagnostics.Process]::Start($startInfo)
        if ($null -eq $process) {
            throw 'process-start-returned-null'
        }
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds($StartupTimeoutSeconds)
        $expectedExceptionObserved = $false

        while ([DateTimeOffset]::UtcNow -lt $deadline) {
            $process.Refresh()
            if (Test-Path -LiteralPath $exceptionFile) {
                $expectedExceptionObserved = $true
            }

            if ($InjectFailure -and $process.HasExited) {
                break
            }

            if (Test-Path -LiteralPath $readyFile) {
                $ready = Get-Content -LiteralPath $readyFile -Raw | ConvertFrom-Json
                break
            }

            if ($process.HasExited) {
                $failure = 'process-exited-before-ui-ready'
                break
            }

            Start-Sleep -Milliseconds 250
        }

        $process.Refresh()
        if ($InjectFailure) {
            if (-not $process.HasExited) {
                $failure = 'injected-failure-process-did-not-exit'
            }
            elseif (-not $expectedExceptionObserved -or -not (Test-Path -LiteralPath $exceptionFile)) {
                $failure = 'injected-failure-was-not-recorded'
            }
            elseif (Test-Path -LiteralPath $readyFile) {
                $failure = 'injected-failure-reached-ui-ready'
            }
            else {
                $exception = Get-Content -LiteralPath $exceptionFile -Raw | ConvertFrom-Json
                if ($exception.ExceptionType -ne 'System.Windows.Markup.XamlParseException' -or $process.ExitCode -eq 0) {
                    $failure = 'injected-failure-did-not-produce-the-expected-startup-rejection'
                }
            }

            if ($null -eq $failure) {
                return [ordered]@{
                    Scenario = $Scenario
                    GateResult = 'REJECTED_AS_EXPECTED'
                    ProcessExitCode = $process.ExitCode
                    ExceptionType = 'System.Windows.Markup.XamlParseException'
                    ReadyMarkerPresent = $false
                    ApplicationCrashEventCount = 0
                }
            }

            throw $failure
        }

        if ($null -ne $failure) {
            throw $failure
        }
        if ($null -eq $ready) {
            if ($process.HasExited) {
                throw 'process-exited-before-ui-ready'
            }

            throw 'startup-timeout-before-ui-ready'
        }
        if ($null -ne $ready.ExceptionType -or (Test-Path -LiteralPath $exceptionFile)) {
            throw 'unhandled-exception-during-startup'
        }
        if ($ready.Ready -ne $true -or $ready.ProcessId -ne $process.Id -or
            $ready.MainWindowVisible -ne $true -or $ready.WindowTitle -ne 'AI 額度監控器' -or
            $ready.ProviderCardCount -ne 2 -or $ready.QuotaProgressBarCount -ne 4 -or
            @($ready.ProgressValueBindingModes).Count -ne 4 -or
            @($ready.ProgressValueBindingModes | Where-Object { $_ -ne 'OneWay' }).Count -ne 0) {
            throw 'ui-ready-marker-did-not-match-the-expected-window-and-controls'
        }

        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero -or $process.MainWindowTitle -ne 'AI 額度監控器') {
            throw 'external-window-check-failed'
        }

        $stableUntil = [DateTimeOffset]::UtcNow.AddSeconds($StabilitySeconds)
        while ([DateTimeOffset]::UtcNow -lt $stableUntil) {
            Start-Sleep -Milliseconds 250
            $process.Refresh()
            if ($process.HasExited -or (Test-Path -LiteralPath $exceptionFile)) {
                throw 'process-exited-or-reported-an-exception-during-stability-window'
            }
            if ($process.MainWindowHandle -eq [IntPtr]::Zero -or $process.MainWindowTitle -ne 'AI 額度監控器') {
                throw 'main-window-disappeared-during-stability-window'
            }
        }

        $eventErrors = @(Get-ApplicationCrashEvents -StartedAt $startedAt -ApplicationName $exeName)
        if ($eventErrors.Count -gt 0) {
            throw 'Windows Application log contains a .NET Runtime or Application Error event for the executable.'
        }

        $finalHash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
        $finalPackageHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($finalHash -ne $initialHash -or $finalPackageHash -ne $initialPackageHash) {
            throw 'Executable or package SHA-256 changed during the smoke test.'
        }

        if (-not $process.CloseMainWindow() -or -not $process.WaitForExit(5000) -or $process.ExitCode -ne 0) {
            throw 'smoke-test-process-did-not-close-cleanly'
        }

        return [ordered]@{
            Scenario = $Scenario
            GateResult = 'PASS'
            ProcessId = $process.Id
            WindowTitle = $ready.WindowTitle
            ProviderCardCount = $ready.ProviderCardCount
            QuotaProgressBarCount = $ready.QuotaProgressBarCount
            ProgressValueBindingModes = @($ready.ProgressValueBindingModes)
            ProcessAliveForSeconds = $StabilitySeconds
            ApplicationCrashEventCount = $eventErrors.Count
            ExecutableSha256 = $finalHash
            PackageSha256 = $finalPackageHash
        }
    }
    finally {
        if ($null -ne $process) {
            $process.Refresh()
            if (-not $process.HasExited) {
                try { $process.Kill($true) } catch { }
                try { $process.WaitForExit(5000) | Out-Null } catch { }
            }
            $process.Dispose()
        }

        Remove-Item -LiteralPath $runDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$normalResult = $null
$negativeResult = $null
$failureCode = $null
try {
    $normalResult = Invoke-ApplicationStartup -Scenario 'offline-no-account-no-token'
    if ($VerifyNegativeControl) {
        $negativeResult = Invoke-ApplicationStartup -Scenario 'deliberate-xaml-parse-startup-failure' -InjectFailure
    }
}
catch {
    $failureCode = $_.Exception.GetType().FullName
}

$outcome = if ($null -eq $failureCode -and $null -ne $normalResult -and (-not $VerifyNegativeControl -or $null -ne $negativeResult)) { 'PASS' } else { 'FAIL' }
$evidenceDirectory = Split-Path -Parent $evidence
New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
$packageHash = $null
if ($null -ne $normalResult) { $packageHash = $normalResult.PackageSha256 }
$report = [ordered]@{
    SchemaVersion = 1
    Outcome = $outcome
    ObservedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    RunnerOS = $env:RUNNER_OS
    RunnerWindowsVersion = [Environment]::OSVersion.Version.ToString()
    SourceCommit = $SourceCommit
    WorkflowRunUrl = $WorkflowRunUrl
    ExecutableName = $exeName
    ExecutableSha256 = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
    PackageSha256 = $packageHash
    StartupTimeoutSeconds = $StartupTimeoutSeconds
    StabilitySeconds = $StabilitySeconds
    PositiveStartup = $normalResult
    NegativeControl = $negativeResult
    FailureCode = $failureCode
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $evidence -Encoding utf8

if ($outcome -ne 'PASS') {
    throw 'Windows executable smoke gate failed; release must remain blocked.'
}

Write-Output "Windows executable smoke gate: PASS ($exeName, SHA-256 $($report.ExecutableSha256))."
Write-Output "Visible window and expected UI initialized; process remained alive for $StabilitySeconds seconds; no matching Windows crash events."
if ($VerifyNegativeControl) {
    Write-Output 'Negative control: PASS (injected XamlParseException was rejected before UI readiness).'
}
