param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Commit,
    [Parameter(Mandatory = $true)][string]$DotnetVersion,
    [Parameter(Mandatory = $true)][string]$PublishDirectory,
    [Parameter(Mandatory = $true)][string]$TestResultsPath,
    [Parameter(Mandatory = $true)][string]$DependencyReportPath,
    [Parameter(Mandatory = $true)][string]$WorkflowRunUrl,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$expectedVersion = 'v' + $Version
if ($env:GITHUB_REF_NAME -and $env:GITHUB_REF_NAME -ne $expectedVersion) {
    throw 'Release tag and application version do not match.'
}

$executablePath = Join-Path $PublishDirectory 'AiQuotaMonitor.exe'
if (-not (Test-Path -LiteralPath $executablePath)) {
    throw 'Windows x64 published executable is missing.'
}

if (Test-Path -LiteralPath $OutputDirectory) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stageDirectory = Join-Path $OutputDirectory 'portable'
New-Item -ItemType Directory -Path $stageDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDirectory '*') -Destination $stageDirectory -Recurse -Force

$documents = @(
    'README.md', 'PRIVACY.md', 'SECURITY.md', 'CHANGELOG.md',
    'docs/SECURITY_TEST_REPORT.md', 'docs/UAT_WINDOWS11.md', 'docs/DEPENDENCIES.md'
)
foreach ($relativePath in $documents) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $relativePath) -Destination $stageDirectory -Force
}

$depsPath = Join-Path $PublishDirectory 'AiQuotaMonitor.deps.json'
$assetsPath = Join-Path $repositoryRoot 'src/AiQuotaMonitor.App/obj/project.assets.json'
$sbomPath = Join-Path $stageDirectory 'SBOM.spdx.json'
& (Join-Path $PSScriptRoot 'New-Sbom.ps1') `
    -ProjectAssetsPath $assetsPath `
    -PublishDepsPath $depsPath `
    -OutputPath $sbomPath `
    -Version $Version `
    -Commit $Commit

$testSummary = 'No TRX result was found.'
$trxFiles = @(Get-ChildItem -Path (Join-Path $TestResultsPath '*.trx') -File -ErrorAction SilentlyContinue)
if ($trxFiles.Count -gt 0) {
    [xml]$trx = Get-Content -LiteralPath $trxFiles[0].FullName -Raw
    $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
    if ($null -ne $counters) {
        $testSummary = ('total={0}; passed={1}; failed={2}; notExecuted={3}' -f `
            $counters.total, $counters.passed, $counters.failed, $counters.notExecuted)
    }
}

$runTimestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$testReport = @"
# Windows Release Test Report

- Version: $Version
- Commit: $Commit
- Runner: GitHub Actions windows-latest (Windows Server runner; not Windows 11 device UAT)
- Workflow run: $WorkflowRunUrl
- Observed at: $runTimestamp
- Command: dotnet restore AiQuotaMonitor.sln; dotnet build AiQuotaMonitor.sln -c Release; dotnet test AiQuotaMonitor.sln -c Release
- Result: PASS (release workflow reached packaging)
- TRX: $testSummary
- Claude PowerShell helper parser/synthetic input: PASS (release workflow reached packaging)
- Windows 11 physical-device UAT: HUMAN_TEST_PENDING; see UAT_WINDOWS11.md
- Signed: No
"@
Set-Content -LiteralPath (Join-Path $stageDirectory 'TEST_REPORT.txt') -Value $testReport -Encoding utf8
Copy-Item -LiteralPath $DependencyReportPath -Destination (Join-Path $stageDirectory 'DEPENDENCIES.txt') -Force

$buildInfo = @"
Product: AI Quota Monitor
Version: $Version
Commit: $Commit
Target: Windows x64, .NET 8, self-contained WPF
SDK: $DotnetVersion
Workflow run: $WorkflowRunUrl
Built UTC: $runTimestamp
Signature: unsigned
UAT: HUMAN_TEST_PENDING
"@
Set-Content -LiteralPath (Join-Path $stageDirectory 'BUILD_INFO.txt') -Value $buildInfo -Encoding utf8

$insideSums = @()
foreach ($name in @('AiQuotaMonitor.exe', 'SBOM.spdx.json', 'DEPENDENCIES.txt')) {
    $path = Join-Path $stageDirectory $name
    if (Test-Path -LiteralPath $path) {
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        $insideSums += ($hash + '  ' + $name)
    }
}
Set-Content -LiteralPath (Join-Path $stageDirectory 'SHA256SUMS.txt') -Value ($insideSums -join "`n") -Encoding ascii

$releaseExeName = 'AiQuotaMonitor-' + $Version + '-win-x64.exe'
Copy-Item -LiteralPath $executablePath -Destination (Join-Path $OutputDirectory $releaseExeName) -Force
Copy-Item -LiteralPath (Join-Path $PublishDirectory 'claude-statusline.ps1') -Destination (Join-Path $OutputDirectory 'claude-statusline.ps1') -Force
Copy-Item -LiteralPath $sbomPath -Destination (Join-Path $OutputDirectory 'SBOM.spdx.json') -Force
Copy-Item -LiteralPath (Join-Path $stageDirectory 'DEPENDENCIES.txt') -Destination (Join-Path $OutputDirectory 'DEPENDENCIES.txt') -Force
Copy-Item -LiteralPath (Join-Path $stageDirectory 'TEST_REPORT.txt') -Destination (Join-Path $OutputDirectory 'TEST_REPORT.txt') -Force
Copy-Item -LiteralPath (Join-Path $stageDirectory 'UAT_WINDOWS11.md') -Destination (Join-Path $OutputDirectory 'UAT_WINDOWS11.md') -Force

$archivePath = Join-Path $OutputDirectory ('AiQuotaMonitor-' + $Version + '-win-x64-portable.zip')
Compress-Archive -Path (Join-Path $stageDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal
Remove-Item -LiteralPath $stageDirectory -Recurse -Force

$releaseNotes = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/RELEASE_NOTES_0.1.0.md') -Raw
$releaseNotes += "`n`nVersion: $Version`nCommit: $Commit`nWorkflow run: $WorkflowRunUrl`n`nWindows 11 physical-device UAT: HUMAN_TEST_PENDING."
Set-Content -LiteralPath (Join-Path $OutputDirectory 'RELEASE_NOTES.md') -Value $releaseNotes -Encoding utf8

$sumLines = @()
Get-ChildItem -LiteralPath $OutputDirectory -File | Where-Object Name -ne 'SHA256SUMS.txt' | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $sumLines += ($hash + '  ' + $_.Name)
}
Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Value ($sumLines -join "`n") -Encoding ascii
