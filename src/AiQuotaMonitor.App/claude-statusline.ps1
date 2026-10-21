$ErrorActionPreference = 'Stop'
$maximumInputLength = 1048576
$pipeName = 'AiQuotaMonitor.ClaudeStatusline.v1'

function Send-QuotaSnapshot($windows, $errorCode) {
    if ($null -ne $errorCode) {
        $minimalPayload = @{ monitor_error = $errorCode } | ConvertTo-Json -Compress -Depth 2
    } else {
        $minimalPayload = @{ rate_limits = $windows } | ConvertTo-Json -Compress -Depth 5
    }
    try {
        $pipe = [System.IO.Pipes.NamedPipeClientStream]::new('.', $pipeName, [System.IO.Pipes.PipeDirection]::Out)
        $pipe.Connect(100)
        $encoding = [System.Text.UTF8Encoding]::new($false)
        $writer = [System.IO.StreamWriter]::new($pipe, $encoding, 1024, $true)
        $writer.WriteLine($minimalPayload)
        $writer.Flush()
        $writer.Dispose()
        $pipe.Dispose()
    } catch {
        # Statusline output still works when the monitor is closed.
    }
}

try {
    Import-Module Microsoft.PowerShell.Utility -ErrorAction Stop
    $rawInput = [Console]::In.ReadToEnd()
    if ($null -eq $rawInput -or $rawInput.Length -gt $maximumInputLength) {
        Send-QuotaSnapshot @{} 'parse_failure'
        Write-Output 'Claude 額度：N/A'
        exit 0
    }

    try {
        $inputData = ConvertFrom-Json -InputObject $rawInput -ErrorAction Stop
    } catch {
        Send-QuotaSnapshot @{} 'parse_failure'
        Write-Output 'Claude 額度：N/A'
        exit 0
    }
    if ($null -eq $inputData -or $inputData -isnot [System.Management.Automation.PSCustomObject]) {
        Send-QuotaSnapshot @{} 'unsupported_statusline_data'
        Write-Output 'Claude 額度：N/A'
        exit 0
    }
    if ($null -ne $inputData.rate_limits -and $inputData.rate_limits -isnot [System.Management.Automation.PSCustomObject]) {
        Send-QuotaSnapshot @{} 'unsupported_statusline_data'
        Write-Output 'Claude 額度：N/A'
        exit 0
    }
    $windows = @{}
    $display = @()

    foreach ($mapping in @(
        @{ Source = 'five_hour'; Target = 'five_hour'; Label = '5h' },
        @{ Source = 'seven_day'; Target = 'seven_day'; Label = '7d' }
    )) {
        $sourceWindow = $null
        if ($null -ne $inputData.rate_limits) {
            $sourceWindow = $inputData.rate_limits.($mapping.Source)
        }

        if ($null -eq $sourceWindow) { continue }
        if ($sourceWindow -isnot [System.Management.Automation.PSCustomObject]) {
            $windows[$mapping.Target] = '__invalid_window_shape__'
            continue
        }
        $usedValue = $sourceWindow.used_percentage
        $isNumber = $usedValue -is [double] -or $usedValue -is [decimal] -or $usedValue -is [int] -or $usedValue -is [long]
        $validUsedValue = $isNumber -and
            -not [double]::IsNaN([double]$usedValue) -and
            -not [double]::IsInfinity([double]$usedValue) -and
            [double]$usedValue -ge 0 -and [double]$usedValue -le 100
        if ($validUsedValue) {
            $window = @{ used_percentage = [double]$usedValue }
        } else {
            # Send a fixed invalid-type marker so C# can report a precise reason
            # without forwarding arbitrary statusline data over the local pipe.
            $window = @{ used_percentage = '__invalid_used_percentage__' }
        }

        $resetValue = $sourceWindow.resets_at
        if ($null -eq $resetValue) {
            if ($sourceWindow.PSObject.Properties.Name -contains 'resets_at') {
                $window.resets_at = $null
            }
        } else {
            $isResetNumber = $resetValue -is [double] -or $resetValue -is [decimal] -or $resetValue -is [int] -or $resetValue -is [long]
            if ($isResetNumber -and -not [double]::IsNaN([double]$resetValue) -and -not [double]::IsInfinity([double]$resetValue) -and [math]::Truncate([double]$resetValue) -eq [double]$resetValue) {
                try {
                    $window.resets_at = [long]$resetValue
                } catch {
                    $window.resets_at = '__invalid_resets_at__'
                }
            } else {
                $window.resets_at = '__invalid_resets_at__'
            }
        }
        $windows[$mapping.Target] = $window
        if ($validUsedValue) {
            $remaining = 100 - [double]$usedValue
            $display += [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '{0} {1:0.#}%', $mapping.Label, $remaining)
        }
    }

    if ($windows.Count -eq 0) {
        Send-QuotaSnapshot @{}
        Write-Output 'Claude 額度：N/A'
        exit 0
    }

    Send-QuotaSnapshot $windows

    Write-Output ('Claude 額度：' + ($display -join ' · '))
    exit 0
} catch {
    Send-QuotaSnapshot @{} 'provider_failure'
    Write-Output 'Claude 額度：N/A'
    exit 0
}
