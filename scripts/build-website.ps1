[CmdletBinding()]
param([switch]$UseExistingApplicationPackage)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
if (-not $UseExistingApplicationPackage) { & (Join-Path $PSScriptRoot 'package.ps1') }
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
$zipName = "Tommi-$version-win-x64.zip"
$setupName = "Tommi-$version-Setup.exe"
$zip = Join-Path $root "dist/$zipName"
$setup = Join-Path $root "dist/$setupName"
if ($UseExistingApplicationPackage) {
    # Website-only changes can reuse an already built release, after checking both files.
    foreach ($artifact in @($zip, $setup)) {
        $recorded = ((Get-Content -LiteralPath ($artifact + '.sha256') -Raw).Trim() -split '\s+')[0]
        if ($recorded -notmatch '^[a-fA-F0-9]{64}$' -or (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash -ne $recorded) { throw "Application package checksum mismatch: $artifact" }
    }
}
$site = Join-Path $root 'website'
New-Item -ItemType Directory -Force -Path (Join-Path $site 'assets') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'assets/tomato-cute.png') -Destination (Join-Path $site 'assets/tomato.png') -Force
Copy-Item -LiteralPath (Join-Path $root 'assets/brand/tomato-focus.ico') -Destination (Join-Path $site 'assets/favicon.ico') -Force
$downloads = Join-Path $site 'downloads'
New-Item -ItemType Directory -Force -Path $downloads | Out-Null
Copy-Item -LiteralPath $setup,$zip -Destination $downloads -Force
$checksums = foreach ($name in @($setupName, $zipName)) { (Get-FileHash -LiteralPath (Join-Path $downloads $name) -Algorithm SHA256).Hash + '  ' + $name }
$checksums | Set-Content -LiteralPath (Join-Path $downloads 'SHA256SUMS.txt') -Encoding ASCII
$release = [ordered]@{ version = $version; installer = "downloads/$setupName"; portable = "downloads/$zipName"; installerBytes = (Get-Item -LiteralPath $setup).Length }
('window.TOMATO_RELEASE = ' + ($release | ConvertTo-Json -Compress) + ';') | Set-Content -LiteralPath (Join-Path $site 'release.js') -Encoding UTF8
$htmlPath = Join-Path $site 'index.html'
$html = [IO.File]::ReadAllText($htmlPath)
$html = $html -replace 'Tommi-\d+\.\d+\.\d+-Setup\.exe', $setupName
$html = $html -replace 'Tommi-\d+\.\d+\.\d+-win-x64\.zip', $zipName
$html = $html -replace 'v\d+\.\d+\.\d+ · Windows', "v$version · Windows"
[IO.File]::WriteAllText($htmlPath, $html, (New-Object Text.UTF8Encoding($false)))
# Package only public files: never include server configuration, source, or credentials.
$publicFiles = @('index.html', 'style.css', 'app.js', 'release.js', 'assets/tomato.png', 'assets/favicon.ico', 'downloads/SHA256SUMS.txt', "downloads/$setupName", "downloads/$zipName")
$publicFiles += @('media/tomato.webp', 'media/timer-edit.webp', 'media/timer-focus.webp', 'media/film-zh.webp', 'media/film-en.webp', 'media/film-zh.mp4', 'media/film-en.mp4')
$manifest = foreach ($name in $publicFiles) { (Get-FileHash -LiteralPath (Join-Path $site $name) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $name }
$manifest | Set-Content -LiteralPath (Join-Path $site 'MANIFEST.sha256') -Encoding ASCII
$siteZip = Join-Path $root "dist/Tommi-website-$version.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($siteZip, [IO.FileMode]::Create)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in ($publicFiles + 'MANIFEST.sha256')) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $site $name), $name) | Out-Null
    }
} finally { $archive.Dispose(); $stream.Dispose() }
((Get-FileHash -LiteralPath $siteZip -Algorithm SHA256).Hash + '  ' + [IO.Path]::GetFileName($siteZip)) | Set-Content -LiteralPath ($siteZip + '.sha256') -Encoding ASCII
& (Join-Path $PSScriptRoot 'check-website-package.ps1')
Write-Host "PASS public website bundle and file checksums verified: $siteZip"
