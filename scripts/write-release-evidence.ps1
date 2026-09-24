[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][ValidateSet('windows-x64', 'macos-universal')][string]$Platform,
    [Parameter(Mandatory)][ValidateSet('stable', 'preview')][string]$Channel,
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string[]]$Assets
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must use x.y.z format.' }
if ([string]::IsNullOrWhiteSpace($Tag)) { throw 'Release tag is required.' }
if (!$Assets.Count) { throw 'At least one release asset is required.' }

$assetRecords = foreach ($path in $Assets) {
    $item = Get-Item -LiteralPath $path
    if ($item.PSIsContainer) { throw "Release asset must be a file: $path" }
    [ordered]@{
        name = $item.Name
        bytes = $item.Length
        sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

foreach ($checksum in @($Assets | Where-Object { $_ -like '*.sha256' })) {
    $target = $checksum.Substring(0, $checksum.Length - '.sha256'.Length)
    if (!(Test-Path -LiteralPath $target -PathType Leaf)) { throw "Checksum target is missing: $target" }
    $recorded = ((Get-Content -LiteralPath $checksum -Raw).Trim() -split '\s+')[0]
    $actual = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    if ($recorded -ne $actual) { throw "Checksum mismatch: $checksum" }
}

$repository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { 'Dante9k/tommi' }
$runUrl = if ($env:GITHUB_RUN_ID) { "https://github.com/$repository/actions/runs/$($env:GITHUB_RUN_ID)" } else { $null }
$record = [ordered]@{
    schema_version = 1
    product = 'Tomato Focus'
    version = $Version
    platform = $Platform
    channel = $Channel
    tag = $Tag
    commit = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { (git rev-parse HEAD).Trim() }
    repository = "https://github.com/$repository"
    workflow_run = $runUrl
    generated_at_utc = [DateTime]::UtcNow.ToString('o')
    assets = @($assetRecords)
}

$parent = Split-Path -Parent $Output
if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
[IO.File]::WriteAllText($Output, ($record | ConvertTo-Json -Depth 5) + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Release evidence written: $Output"
