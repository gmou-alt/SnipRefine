<#
    make-icon.ps1 - 生成应用图标资源

    原理：直接调用 SnipRefine.exe 内部用于绘制托盘/悬浮图标的 CreateAppBitmap()，
          因此生成的 .ico 与程序里显示的图标永远一致，不会出现两套图。

    用法： powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
    产物： assets\app.ico                多尺寸图标（16/24/32/48/64/128/256）
           docs\images\icon-preview.png  预览图（README 用）

    注意：需要先运行 scripts\build.cmd 生成 dist\SnipRefine.exe。
#>
$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$exe = Join-Path $root 'dist\SnipRefine.exe'
$icoOut = Join-Path $root 'assets\app.ico'
$pngOut = Join-Path $root 'docs\images\icon-preview.png'

if (-not (Test-Path $exe)) {
    throw "找不到 $exe，请先运行 scripts\build.cmd"
}

Add-Type -AssemblyName System.Drawing

# 用字节数组加载，避免 LoadFile 的“网络位置”沙箱限制，也不会锁住 exe
$asm = [Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($exe))
$type = $asm.GetType('SnipRefine.TrayApp')
if ($null -eq $type) { throw '在程序集里找不到 SnipRefine.TrayApp' }
$method = $type.GetMethod('CreateAppBitmap', [Reflection.BindingFlags]'Public,NonPublic,Static')
if ($null -eq $method) { throw '找不到 TrayApp.CreateAppBitmap(int)' }

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = @()

foreach ($size in $sizes) {
    $bmp = $method.Invoke($null, @($size))
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += , @{ Size = $size; Data = $ms.ToArray() }
    if ($size -eq 256) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $pngOut) | Out-Null
        $bmp.Save($pngOut, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $ms.Dispose()
    $bmp.Dispose()
}

# 组装 ICO 容器（Vista+ 支持 PNG 帧）
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $icoOut) | Out-Null
$fs = [System.IO.File]::Create($icoOut)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([UInt16]0)                 # reserved
$bw.Write([UInt16]1)                 # type = icon
$bw.Write([UInt16]$frames.Count)     # image count

$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $dim = if ($f.Size -ge 256) { 0 } else { $f.Size }   # 256 记作 0
    $bw.Write([Byte]$dim)            # width
    $bw.Write([Byte]$dim)            # height
    $bw.Write([Byte]0)               # palette
    $bw.Write([Byte]0)               # reserved
    $bw.Write([UInt16]1)             # color planes
    $bw.Write([UInt16]32)            # bits per pixel
    $bw.Write([UInt32]$f.Data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $f.Data.Length
}
foreach ($f in $frames) { $bw.Write($f.Data) }

$bw.Flush(); $bw.Close(); $fs.Close()

Write-Host ("[OK] {0}  ({1} 字节, {2} 个尺寸)" -f $icoOut, (Get-Item $icoOut).Length, $frames.Count)
Write-Host ("[OK] {0}" -f $pngOut)
