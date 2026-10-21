param(
    [Parameter(Mandatory = $true)][string]$ProjectAssetsPath,
    [Parameter(Mandatory = $true)][string]$PublishDepsPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Commit
)

$ErrorActionPreference = 'Stop'
$packageRecords = @{}

function Add-LibraryRecords($manifestPath) {
    if (-not (Test-Path -LiteralPath $manifestPath)) { return }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $libraries = $manifest.libraries
    if ($null -eq $libraries) { return }

    foreach ($entry in $libraries.PSObject.Properties) {
        if ($entry.Value.type -notin @('package', 'Package', 'RuntimePackage', 'RuntimePack', 'runtimepack')) { continue }
        $parts = $entry.Name.Split([char[]]@('/'), 2)
        if ($parts.Length -ne 2) { continue }
        $packageRecords[($parts[0].ToLowerInvariant() + '@' + $parts[1])] = [pscustomobject]@{
            Name = $parts[0]
            Version = $parts[1]
        }
    }
}

Add-LibraryRecords $ProjectAssetsPath
Add-LibraryRecords $PublishDepsPath

$packages = @()
$relationships = @()
foreach ($key in @($packageRecords.Keys | Sort-Object)) {
    $record = $packageRecords[$key]
    $safeId = ($record.Name + '-' + $record.Version) -replace '[^A-Za-z0-9.-]', '-'
    $spdxId = 'SPDXRef-Package-' + $safeId
    $packages += [ordered]@{
        name = $record.Name
        SPDXID = $spdxId
        versionInfo = $record.Version
        downloadLocation = 'NOASSERTION'
        filesAnalyzed = $false
        licenseConcluded = 'NOASSERTION'
        licenseDeclared = 'NOASSERTION'
        copyrightText = 'NOASSERTION'
        externalRefs = @(@{
            referenceCategory = 'PACKAGE-MANAGER'
            referenceType = 'purl'
            referenceLocator = ('pkg:nuget/' + $record.Name.ToLowerInvariant() + '@' + $record.Version)
        })
    }
    $relationships += [ordered]@{
        spdxElementId = 'SPDXRef-DOCUMENT'
        relationshipType = 'DESCRIBES'
        relatedSpdxElement = $spdxId
    }
}

$created = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$document = [ordered]@{
    spdxVersion = 'SPDX-2.3'
    dataLicense = 'CC0-1.0'
    SPDXID = 'SPDXRef-DOCUMENT'
    name = ('AI Quota Monitor ' + $Version + ' win-x64 portable')
    documentNamespace = ('https://github.com/henryfir456/ai-quota-monitor/commit/' + $Commit + '/sbom/' + $Version)
    creationInfo = @{
        created = $created
        creators = @('Tool: AiQuotaMonitor-SBOM-Generator')
    }
    packages = $packages
    relationships = $relationships
}

$json = $document | ConvertTo-Json -Depth 12
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
[System.IO.File]::WriteAllText($OutputPath, $json, [System.Text.UTF8Encoding]::new($false))
