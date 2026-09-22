$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1') -Test
$render = Start-Process -FilePath (Join-Path $projectRoot 'build\Tomato.Verify.exe') -ArgumentList '--render-preview' -WindowStyle Hidden -Wait -PassThru
if ($render.ExitCode -ne 0) { throw 'Preview rendering failed.' }

$version = (Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain a semantic version.' }
$releaseName = "TomatoFocus-$version-win-x64"
$releasePath = Join-Path $projectRoot "dist\$releaseName"
New-Item -ItemType Directory -Force -Path $releasePath | Out-Null
foreach ($name in @('Tomato.exe', 'Tomato.exe.config')) { Copy-Item -LiteralPath (Join-Path $projectRoot "build\$name") -Destination $releasePath -Force }
foreach ($name in @('README.md', 'LICENSE', 'CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $releasePath -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\VALIDATION.md') -Destination $releasePath -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'build\preview.png') -Destination $releasePath -Force

# Explicit allowlist excludes developer tools, state files and debug symbols.
$releaseFiles = @('Tomato.exe', 'Tomato.exe.config', 'README.md', 'LICENSE', 'CHANGELOG.md', 'VALIDATION.md', 'preview.png')
$archivePath = Join-Path $projectRoot "dist\$releaseName.zip"
Compress-Archive -LiteralPath @($releaseFiles | ForEach-Object { Join-Path $releasePath $_ }) -DestinationPath $archivePath -Force
$hash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
($hash.Hash + "  $releaseName.zip") | Set-Content -LiteralPath ($archivePath + '.sha256') -Encoding ASCII
Write-Host "Package created: dist/$releaseName.zip"
