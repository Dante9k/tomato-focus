[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$roslyn = Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll'
if (!(Test-Path -LiteralPath $roslyn)) { throw 'Formatting requires PowerShell 7 with its bundled Roslyn assemblies.' }
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path $roslyn
$method = [Microsoft.CodeAnalysis.SyntaxNodeExtensions].GetMethods() | Where-Object { $_.Name -eq 'NormalizeWhitespace' -and $_.GetParameters().Count -eq 4 } | Select-Object -First 1
$normalize = $method.MakeGenericMethod([Microsoft.CodeAnalysis.SyntaxNode])
$utf8 = [System.Text.UTF8Encoding]::new($false)
$changed = @()
$files = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src'), (Join-Path $projectRoot 'tests') -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
foreach ($file in $files) {
    $source = [System.IO.File]::ReadAllText($file.FullName)
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source)
    $syntaxRoot = $tree.GetRoot()
    $formatted = $normalize.Invoke($null, @($syntaxRoot, '    ', "`n", $false)).ToFullString() + "`n"
    if ($source -ne $formatted) {
        $changed += $file.Name
        if (!$Check) { [System.IO.File]::WriteAllText($file.FullName, $formatted, $utf8) }
    }
}
if ($Check -and $changed.Count -gt 0) { throw ('Formatting required: ' + ($changed -join ', ')) }
Write-Host ('C# formatting verified: ' + $files.Count + ' files')
