[CmdletBinding()]
param([switch]$Test)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path $PSScriptRoot -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Windows x64 with .NET Framework 4.8 is required.' }
& (Join-Path $PSScriptRoot 'build-brand-icon.ps1')

$output = Join-Path $projectRoot 'build'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @('System.dll', 'System.Core.dll', 'System.Xml.dll', 'System.Xaml.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll', 'WPF\UIAutomationTypes.dll', 'WPF\UIAutomationProvider.dll')
$common = @('/nologo', '/platform:x64', '/optimize+', '/debug-', '/warnaserror+', '/utf8output')
foreach ($reference in $references) { $common += '/reference:' + (Join-Path $framework $reference) }
$application = Join-Path $projectRoot 'src\Tomato.Focus'
$appArguments = $common + @(
    '/target:winexe',
    ('/win32manifest:' + (Join-Path $application 'app.manifest')),
    ('/win32icon:' + (Join-Path $projectRoot 'assets\brand\tomato-focus.ico')),
    ('/resource:' + (Join-Path $projectRoot 'assets\tomato-cute.png') + ',Tomato.Texture.png'),
    ('/out:' + (Join-Path $output 'Tomato.exe'))
)
foreach ($index in 0..2) {
    $name = "impact-soft-$index.wav"
    $appArguments += '/resource:' + (Join-Path $projectRoot "assets\audio\$name") + ',Tomato.Audio.' + $name
}
$appArguments += @(Get-ChildItem -LiteralPath $application -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' } | Sort-Object FullName | ForEach-Object { $_.FullName })
& $compiler $appArguments
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
Copy-Item -LiteralPath (Join-Path $application 'App.config') -Destination (Join-Path $output 'Tomato.exe.config') -Force

$testArguments = $common + @('/target:exe', ('/reference:' + (Join-Path $output 'Tomato.exe')), ('/out:' + (Join-Path $output 'Tomato.Verify.exe')))
$testArguments += @(Get-ChildItem (Join-Path $projectRoot 'tests\Tomato.Focus.Verification') -Filter *.cs | Sort-Object FullName | ForEach-Object { $_.FullName })
& $compiler $testArguments
if ($LASTEXITCODE -ne 0) { throw 'Verification compilation failed.' }
Copy-Item -LiteralPath (Join-Path $application 'App.config') -Destination (Join-Path $output 'Tomato.Verify.exe.config') -Force
Write-Host 'Build succeeded: build/Tomato.exe'

if ($Test) {
    $verification = Start-Process -FilePath (Join-Path $output 'Tomato.Verify.exe') -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
    if ($verification.ExitCode -ne 0) { throw 'Verification failed. See build/test-results.txt.' }
    Get-Content -LiteralPath (Join-Path $output 'test-results.txt') -Encoding UTF8
}
