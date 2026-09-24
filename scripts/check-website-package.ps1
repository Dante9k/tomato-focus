$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version.' }
$path = Join-Path $root "dist/Tommi-website-$version.zip"
$recorded = ((Get-Content -LiteralPath ($path + '.sha256') -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $recorded) { throw 'Website archive checksum mismatch.' }
$expected = @('index.html', 'style.css', 'app.js', 'release.js', 'assets/tomato.png', 'assets/favicon.ico', 'downloads/SHA256SUMS.txt', "downloads/Tommi-$version-Setup.exe", "downloads/Tommi-$version-win-x64.zip")
$expected += @('media/tomato.webp', 'media/timer-edit.webp', 'media/timer-focus.webp', 'media/film-zh.webp', 'media/film-en.webp', 'media/film-zh.mp4', 'media/film-en.mp4')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($path)
try {
    $names = @($zip.Entries | ForEach-Object FullName)
    if ($names.Count -ne $expected.Count + 1 -or (Compare-Object ($expected + 'MANIFEST.sha256') $names)) { throw 'Website archive differs from the public allowlist.' }
    $reader = [IO.StreamReader]::new($zip.GetEntry('MANIFEST.sha256').Open())
    try { $lines = $reader.ReadToEnd().Trim() -split '\r?\n' } finally { $reader.Dispose() }
    $manifest = @{}
    foreach ($line in $lines) {
        if ($line -cnotmatch '^([a-f0-9]{64})  (.+)$') { throw 'Invalid manifest entry.' }
        if ($manifest.ContainsKey($Matches[2])) { throw 'Duplicate manifest entry.' }
        $manifest[$Matches[2]] = $Matches[1]
    }
    if (Compare-Object $expected @($manifest.Keys)) { throw 'Incomplete manifest.' }
    foreach ($name in $expected) {
        $stream = $zip.GetEntry($name).Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $actual = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() } finally { $stream.Dispose(); $sha.Dispose() }
        if ($actual -ne $manifest[$name]) { throw "Website file checksum mismatch: $name" }
    }
} finally { $zip.Dispose() }
Write-Host 'PASS website allowlist, archive checksum and per-file manifest'
