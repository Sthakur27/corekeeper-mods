# Decode an NVIDIA HDR capture (.jxr, scRGB float) to raw little-endian float32 RGBA.
# Usage: powershell -File jxr_to_raw.ps1 <in.jxr> <out.raw>   (writes "<w> <h>" to stdout)
param([string]$In, [string]$Out)
Add-Type -AssemblyName PresentationCore
$fs = [IO.File]::OpenRead($In)
$dec = New-Object System.Windows.Media.Imaging.WmpBitmapDecoder($fs, 'PreservePixelFormat', 'OnLoad')
$src = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap($dec.Frames[0], [System.Windows.Media.PixelFormats]::Rgba128Float, $null, 0)
$w = $src.PixelWidth; $h = $src.PixelHeight; $stride = $w * 16
$buf = New-Object byte[] ($stride * $h)
$src.CopyPixels($buf, $stride, 0)
[IO.File]::WriteAllBytes($Out, $buf)
$fs.Close()
"$w $h"
