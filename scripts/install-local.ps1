[CmdletBinding()]
param([switch]$Launch, [switch]$PrepareOnly, [string]$Version)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($Launch -and $PrepareOnly) { throw 'Choose either Launch or PrepareOnly.' }
$projectRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = (Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim() }
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version.' }
& (Join-Path $PSScriptRoot 'check-package.ps1') -Version $Version
$installRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs\TomatoFocus'))
$target = [IO.Path]::GetFullPath((Join-Path $installRoot $version))
if (!(($target + '\').StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase))) { throw 'Install target is outside TomatoFocus.' }
$executable = Join-Path $target 'Tomato.exe'
$running = @(Get-Process -Name Tomato -ErrorAction SilentlyContinue)
if ($running.Count -gt 0 -and !$PrepareOnly) {
    throw '请先右键任务栏通知区域的番茄图标，选择“退出朱果”，再重新运行安装。退出会保存计时；安装程序不会强制结束进程。'
}
if ($PrepareOnly -and @($running | Where-Object { $_.Path -eq $executable }).Count -gt 0) {
    throw 'The target version is running. Exit it from the tray before updating its files.'
}

$archive = Join-Path $projectRoot "dist\TomatoFocus-$version-win-x64.zip"
$source = Join-Path $projectRoot "dist\TomatoFocus-$version-win-x64"
$files = @('Tomato.exe', 'Tomato.exe.config', 'README.md', 'LICENSE', 'CHANGELOG.md', 'VALIDATION.md', 'preview.png')
$previous = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'dist'), $installRoot -Filter Tomato.exe -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -ne $executable -and [Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion -ne "$version.0" } | ForEach-Object { $_.FullName })
$backup = Join-Path $installRoot ('backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
$state = Join-Path $env:LOCALAPPDATA 'TomatoFocus\state.xml'
if (Test-Path -LiteralPath $state) { Copy-Item -LiteralPath $state -Destination (Join-Path $backup 'state.xml') }

# Extract the verified, allowlisted ZIP, then verify every extracted byte before switching shortcuts.
if (Test-Path -LiteralPath $target) {
    $existing = @(Get-ChildItem -LiteralPath $target -File | ForEach-Object { $_.Name })
    if (Compare-Object ($files | Sort-Object) ($existing | Sort-Object)) { throw 'Existing installation differs; it was preserved. Use a new version directory.' }
}
New-Item -ItemType Directory -Path $target -Force | Out-Null
Expand-Archive -LiteralPath $archive -DestinationPath $target -Force
foreach ($name in $files) {
    $actual = Get-FileHash -LiteralPath (Join-Path $target $name) -Algorithm SHA256
    $expected = Get-FileHash -LiteralPath (Join-Path $source $name) -Algorithm SHA256
    if ($actual.Hash -ne $expected.Hash) { throw "Installed file did not verify: $name. Shortcuts were not changed." }
}
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($executable).FileVersion -ne "$version.0") { throw 'Installed executable version mismatch.' }

$shell = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath('DesktopDirectory')
$programs = [Environment]::GetFolderPath('Programs')
$links = @((Join-Path $desktop '朱果番茄钟.lnk'), (Join-Path $programs '朱果番茄钟.lnk'))
$oldLinks = @()
foreach ($folder in @($desktop, $programs)) {
    foreach ($file in @(Get-ChildItem -LiteralPath $folder -Filter *.lnk -ErrorAction SilentlyContinue)) {
        $link = $shell.CreateShortcut($file.FullName)
        if ($previous -contains $link.TargetPath -or $link.TargetPath -eq $executable -or $links -contains $file.FullName) {
            $oldLinks += [pscustomobject]@{ Path = $file.FullName; Target = $link.TargetPath; Arguments = $link.Arguments; WorkingDirectory = $link.WorkingDirectory; IconLocation = $link.IconLocation }
            $links += $file.FullName
        }
    }
}
$oldLinks | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $backup 'shortcuts.json') -Encoding UTF8
foreach ($path in @($links | Select-Object -Unique)) {
    $link = $shell.CreateShortcut($path)
    $link.TargetPath = $executable
    $link.WorkingDirectory = $target
    $link.Arguments = ''
    $link.IconLocation = $executable + ',0'
    $link.Description = "朱果番茄钟 $version"
    $link.Save()
    if ($shell.CreateShortcut($path).TargetPath -ne $executable) { throw "Shortcut did not verify: $path" }
}

$rollback = @(
    "朱果 $version 本地升级记录",
    "安装路径：$target",
    "设置备份：$backup",
    '回退：先从托盘退出朱果，再打开下面保留的旧版本 Tomato.exe。新设置兼容旧版，一般不需要恢复备份。',
    '如必须恢复备份，先备份当前 state.xml，再复制本目录的 state.xml 到 LocalAppData/TomatoFocus/state.xml。恢复旧备份会恢复当时的截止时间，请确认后操作。',
    '旧版本路径：'
) + $previous
$rollback | Set-Content -LiteralPath (Join-Path $backup 'rollback.txt') -Encoding UTF8
$result = [ordered]@{ Version = $version; Executable = $executable; DesktopShortcut = $links[0]; Backup = $backup; PreviousExecutables = $previous; Launched = $false; WaitingForPreviousExit = ($running.Count -gt 0) }
if ($Launch) {
    $process = Start-Process -FilePath $executable -WorkingDirectory $target -WindowStyle Hidden -PassThru
    Start-Sleep -Seconds 3
    $process.Refresh()
    if ($process.HasExited) { throw "新版启动后退出，退出码 $($process.ExitCode)。旧版本和备份已保留；请检查实际安全软件告警或本地错误日志。" }
    if ($process.MainModule.FileName -ne $executable) { throw 'Running process path mismatch.' }
    $result.Launched = $true
    $result.ProcessId = $process.Id
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $installRoot 'latest-install.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 4
