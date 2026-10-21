param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Commit,
    [Parameter(Mandatory = $true)][string]$DotnetVersion,
    [Parameter(Mandatory = $true)][string]$PublishDirectory,
    [Parameter(Mandatory = $true)][string]$DependencyReportPath,
    [Parameter(Mandatory = $true)][string]$WorkflowRunUrl,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$expectedVersion = 'v' + $Version
if ($Version -ne '0.1.1' -or ($env:GITHUB_REF_TYPE -eq 'tag' -and $env:GITHUB_REF_NAME -ne $expectedVersion)) {
    throw 'Only the reviewed v0.1.1 package may enter the Windows release gate.'
}

$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$executablePath = Join-Path $publish 'AiQuotaMonitor.exe'
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw 'Windows x64 published executable is missing.'
}
if (-not (Test-Path -LiteralPath $DependencyReportPath -PathType Leaf)) {
    throw 'Dependency report is missing.'
}

if (Test-Path -LiteralPath $OutputDirectory) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stageDirectory = Join-Path $OutputDirectory 'portable-stage'
New-Item -ItemType Directory -Path $stageDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $publish '*') -Destination $stageDirectory -Recurse -Force

$documents = @(
    'README.md', 'PRIVACY.md', 'SECURITY.md', 'CHANGELOG.md',
    'docs/SECURITY_TEST_REPORT.md', 'docs/UAT_WINDOWS11.md', 'docs/DEPENDENCIES.md'
)
foreach ($relativePath in $documents) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $relativePath) -Destination $stageDirectory -Force
}

$depsPath = Join-Path $publish 'AiQuotaMonitor.deps.json'
$assetsPath = Join-Path $repositoryRoot 'src/AiQuotaMonitor.App/obj/project.assets.json'
$sbomPath = Join-Path $stageDirectory 'SBOM.spdx.json'
& (Join-Path $PSScriptRoot 'New-Sbom.ps1') `
    -ProjectAssetsPath $assetsPath `
    -PublishDepsPath $depsPath `
    -OutputPath $sbomPath `
    -Version $Version `
    -Commit $Commit
Copy-Item -LiteralPath $DependencyReportPath -Destination (Join-Path $stageDirectory 'DEPENDENCIES.txt') -Force

$runTimestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$workflowInfo = @"
Product: AI Quota Monitor
Version: $Version
Commit: $Commit
Target: Windows x64, .NET 8, self-contained WPF
SDK: $DotnetVersion
Workflow run: $WorkflowRunUrl
Built UTC: $runTimestamp
Signature: unsigned
Startup verification: smoke gate runs on this exact package before Release
UAT: HUMAN_TEST_PENDING
"@
Set-Content -LiteralPath (Join-Path $stageDirectory 'BUILD_INFO.txt') -Value $workflowInfo -Encoding utf8

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
Copy-Item -LiteralPath (Join-Path $publish 'claude-statusline.ps1') -Destination (Join-Path $OutputDirectory 'claude-statusline.ps1') -Force
Copy-Item -LiteralPath $sbomPath -Destination (Join-Path $OutputDirectory 'SBOM.spdx.json') -Force
Copy-Item -LiteralPath (Join-Path $stageDirectory 'DEPENDENCIES.txt') -Destination (Join-Path $OutputDirectory 'DEPENDENCIES.txt') -Force
Copy-Item -LiteralPath (Join-Path $stageDirectory 'UAT_WINDOWS11.md') -Destination (Join-Path $OutputDirectory 'UAT_WINDOWS11.md') -Force

$archivePath = Join-Path $OutputDirectory ('AiQuotaMonitor-' + $Version + '-win-x64-portable.zip')
Compress-Archive -Path (Join-Path $stageDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal
Remove-Item -LiteralPath $stageDirectory -Recurse -Force

$releaseNotes = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/RELEASE_NOTES_0.1.1.md') -Raw
$releaseNotes += "`n`nVersion: $Version`nCommit: $Commit`nWorkflow run: $WorkflowRunUrl`n`nWindows 11 physical-device UAT: HUMAN_TEST_PENDING."
Set-Content -LiteralPath (Join-Path $OutputDirectory 'RELEASE_NOTES.md') -Value $releaseNotes -Encoding utf8

Write-Output "Packaged Windows x64 EXE and portable ZIP for $expectedVersion. Startup smoke test and final SHA-256 verification are the next required steps."
