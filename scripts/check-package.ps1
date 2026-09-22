[CmdletBinding()]
param([string]$Version)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim() }
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version.' }
$archive = Join-Path $projectRoot "dist\TomatoFocus-$version-win-x64.zip"
$expected = @('Tomato.exe', 'Tomato.exe.config', 'README.md', 'LICENSE', 'CHANGELOG.md', 'VALIDATION.md', 'preview.png') | Sort-Object
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    $actual = @($zip.Entries | ForEach-Object { $_.FullName }) | Sort-Object
    if (Compare-Object $expected $actual) { throw 'Archive contents do not match the release allowlist.' }
} finally { $zip.Dispose() }
$recordedHash = ((Get-Content -LiteralPath ($archive + '.sha256') -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $recordedHash) { throw 'Archive checksum mismatch.' }
Write-Host 'PASS archive allowlist and SHA-256 checksum'
