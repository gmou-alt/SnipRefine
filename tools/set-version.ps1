<#
    set-version.ps1 - 一次性更新所有版本号

    用法： powershell -ExecutionPolicy Bypass -File tools\set-version.ps1 -Version 1.12.2
    或：   scripts\bump-version.cmd 1.12.2

    会同步修改：
      src\Program.cs     -> public const string VersionText = "x.y.z";   （界面标题显示）
      scripts\pack.cmd   -> set "VERSION=x.y.z"                          （发布包文件名）

    提示：CHANGELOG.md 需要手工补一节，发布时记得打 v x.y.z 标签。
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+(\.\d+)?$') {
    throw "版本号格式应为 x.y.z，例如 1.12.2（收到：$Version）"
}

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$utf8 = New-Object System.Text.UTF8Encoding($false)

# 1) 源码里的显示版本
$programCs = Join-Path $root 'src\Program.cs'
$text = [System.IO.File]::ReadAllText($programCs)
$new = [regex]::Replace($text, 'VersionText = "[^"]*"', 'VersionText = "' + $Version + '"')
if ($new -eq $text) { throw "没有在 $programCs 里找到 VersionText，请检查文件" }
[System.IO.File]::WriteAllText($programCs, $new, $utf8)
Write-Host ("[OK] src\Program.cs        -> VersionText = " + $Version)

# 2) 打包脚本里的版本
$packCmd = Join-Path $root 'scripts\pack.cmd'
$text2 = [System.IO.File]::ReadAllText($packCmd)
$new2 = [regex]::Replace($text2, 'set "VERSION=[^"]*"', 'set "VERSION=' + $Version + '"')
if ($new2 -eq $text2) { throw "没有在 $packCmd 里找到 VERSION，请检查文件" }
[System.IO.File]::WriteAllText($packCmd, $new2, $utf8)
Write-Host ("[OK] scripts\pack.cmd       -> VERSION = " + $Version)

Write-Host ""
Write-Host "下一步："
Write-Host "  1) 在 CHANGELOG.md 顶部补一节 $Version 的变更"
Write-Host "  2) git add -A; git commit -m `"release: v$Version`""
Write-Host "  3) git tag -a v$Version -m `"SnipRefine v$Version`"; git push origin main --tags"
