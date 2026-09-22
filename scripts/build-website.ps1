[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'package.ps1')
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
$zipName = "TomatoFocus-$version-win-x64.zip"
$setupName = "TomatoFocus-$version-Setup.exe"
$zip = Join-Path $root "dist/$zipName"
$setup = Join-Path $root "dist/$setupName"
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
$html = $html -replace 'TomatoFocus-\d+\.\d+\.\d+-Setup\.exe', $setupName
$html = $html -replace 'TomatoFocus-\d+\.\d+\.\d+-win-x64\.zip', $zipName
$html = $html -replace 'v\d+\.\d+\.\d+ · Windows', "v$version · Windows"
[IO.File]::WriteAllText($htmlPath, $html, (New-Object Text.UTF8Encoding($false)))
# Package only public files: never include server configuration, source, or credentials.
$publicFiles = @('index.html', 'style.css', 'app.js', 'release.js', 'assets/tomato.png', 'assets/favicon.ico', 'downloads/SHA256SUMS.txt', "downloads/$setupName", "downloads/$zipName")
$manifest = foreach ($name in $publicFiles) { (Get-FileHash -LiteralPath (Join-Path $site $name) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $name }
$manifest | Set-Content -LiteralPath (Join-Path $site 'MANIFEST.sha256') -Encoding ASCII
$siteZip = Join-Path $root "dist/TomatoFocus-website-$version.zip"
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
Write-Host "PASS installer payload verified; website bundle created: $siteZip"
