# ---------------------------------------------------------------------
#  SnipRefine.ps1 - 免编译启动器（不生成 exe，直接跑同一份源码）
#
#  用 Windows PowerShell 自带的编译器把 src\*.cs 在内存里编译并启动，
#  与 dist\SnipRefine.exe 是同一份源码、同样的功能。
#
#  用法：
#    双击 scripts\SnipRefine.vbs（推荐，无黑框）
#    或在本文件上右键 ->「使用 PowerShell 运行」
# ---------------------------------------------------------------------
param([switch]$Background)
$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$srcDir = Join-Path $root 'src'

try {
if (-not ('SnapGrab.Program' -as [type])) {
    $sources = Get-ChildItem (Join-Path $srcDir '*.cs') | Sort-Object Name | ForEach-Object { $_.FullName }
    Add-Type -Path $sources -ReferencedAssemblies 'System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Security.dll', 'System.Web.Extensions.dll'
}

# 供程序里「开机自动启动」写入正确的启动命令
$scriptPath = Join-Path $here 'SnipRefine.ps1'
[SnapGrab.Program]::LauncherCommand = 'powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + $scriptPath + '"'

[SnapGrab.Program]::BackgroundStartup = $Background.IsPresent
[SnapGrab.Program]::Start()
if ([Environment]::ExitCode -ne 0) { exit [Environment]::ExitCode }

} catch {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show('源码启动失败。请完整解压后运行根目录 Start.cmd，保留窗口中的编译或启动错误。', 'SnipRefine 启动诊断', 'OK', 'Error') | Out-Null
    exit 1
}
