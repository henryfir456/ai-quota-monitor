$ErrorActionPreference = 'Stop'
$maximumInputLength = 1048576
$pipeName = 'AiQuotaMonitor.ClaudeStatusline.v1'

try {
    $rawInput = [Console]::In.ReadToEnd()
    if ($null -eq $rawInput -or $rawInput.Length -gt $maximumInputLength) {
        Write-Output 'Claude 額度：N/A'
        exit 0
    }

    $inputData = $rawInput | ConvertFrom-Json -ErrorAction Stop
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
        $usedValue = $sourceWindow.used_percentage
        $isNumber = $usedValue -is [double] -or $usedValue -is [decimal] -or $usedValue -is [int] -or $usedValue -is [long]
        if ($null -eq $usedValue -or -not $isNumber -or [double]::IsNaN([double]$usedValue) -or [double]::IsInfinity([double]$usedValue) -or [double]$usedValue -lt 0 -or [double]$usedValue -gt 100) { continue }

        $window = @{ used_percentage = [double]$usedValue }
        $resetValue = $sourceWindow.resets_at
        if ($null -ne $resetValue -and $resetValue -is [ValueType]) {
            $window.resets_at = $resetValue
        }
        $windows[$mapping.Target] = $window
        $remaining = 100 - [double]$usedValue
        $display += [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '{0} {1:0.#}%', $mapping.Label, $remaining)
    }

    if ($windows.Count -eq 0) {
        Write-Output 'Claude 額度：N/A'
        exit 0
    }

    $minimalPayload = @{ rate_limits = $windows } | ConvertTo-Json -Compress -Depth 5
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

    Write-Output ('Claude 額度：' + ($display -join ' · '))
    exit 0
} catch {
    Write-Output 'Claude 額度：N/A'
    exit 0
}
