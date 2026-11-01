Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot
$src = [Drawing.Bitmap]::FromFile((Join-Path $root 'assets\logo.png'))

$minX = $src.Width; $minY = $src.Height; $maxX = 0; $maxY = 0
for ($y = 0; $y -lt $src.Height; $y += 2) { for ($x = 0; $x -lt $src.Width; $x += 2) {
  if ($src.GetPixel($x, $y).A -gt 40) { if ($x -lt $minX) {$minX=$x}; if ($x -gt $maxX) {$maxX=$x}; if ($y -lt $minY) {$minY=$y}; if ($y -gt $maxY) {$maxY=$y} } } }
$side = [Math]::Max($maxX - $minX, $maxY - $minY) + 2
$crop = New-Object Drawing.Rectangle $minX, $minY, $side, $side

function Render([int]$size, [bool]$gray) {
  $bmp = New-Object Drawing.Bitmap $size, $size
  $g = [Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'; $g.SmoothingMode = 'AntiAlias'
  $attr = New-Object Drawing.Imaging.ImageAttributes
  if ($gray) {
    $m = New-Object Drawing.Imaging.ColorMatrix
    $m.Matrix00 = 0.3; $m.Matrix01 = 0.3; $m.Matrix02 = 0.3
    $m.Matrix10 = 0.59; $m.Matrix11 = 0.59; $m.Matrix12 = 0.59
    $m.Matrix20 = 0.11; $m.Matrix21 = 0.11; $m.Matrix22 = 0.11
    $m.Matrix33 = 0.55; $m.Matrix44 = 1
    $attr.SetColorMatrix($m)
  }
  $g.DrawImage($src, (New-Object Drawing.Rectangle 0, 0, $size, $size), $crop.X, $crop.Y, $crop.Width, $crop.Height, 'Pixel', $attr)
  $g.Dispose()
  $ms = New-Object IO.MemoryStream; $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  return ,$ms.ToArray()
}

function WriteIco([string]$path, [int[]]$sizes, [bool]$gray) {
  $pngs = @(); foreach ($s in $sizes) { $pngs += ,(Render $s $gray) }
  $w = New-Object IO.BinaryWriter ([IO.File]::Create($path))
  $w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
  $offset = 6 + 16 * $sizes.Count
  for ($i = 0; $i -lt $sizes.Count; $i++) {
    $b = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$b); $w.Write([byte]$b); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$pngs[$i].Length); $w.Write([uint32]$offset)
    $offset += $pngs[$i].Length
  }
  foreach ($p in $pngs) { $w.Write($p) }
  $w.Close()
}

WriteIco (Join-Path $root 'src\app.ico') @(16, 20, 24, 32, 40, 48, 64, 256) $false
WriteIco (Join-Path $root 'src\tray-off.ico') @(16, 20, 24, 32, 40, 48, 64) $true
[IO.File]::WriteAllBytes((Join-Path $root 'src\logo.png'), (Render 128 $false))
$src.Dispose()
'icons ok'
