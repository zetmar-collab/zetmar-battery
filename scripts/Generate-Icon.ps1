# Original geometric icon; no downloaded or third-party artwork.
Add-Type -AssemblyName System.Drawing
$iconPath = Join-Path $PSScriptRoot '..\src\Zetmar.Battery\Assets\battery.ico'
$frames = @()
foreach ($size in @(16,20,24,32,40,48,64,128,256)) {
    $bmp = [System.Drawing.Bitmap]::new($size,$size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $scale = $size / 64.0
    $g.ScaleTransform($scale,$scale)
    $bg = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#17352F'))
    $fg = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#79E6C4'))
    $pen = [System.Drawing.Pen]::new($fg,4)
    $g.FillEllipse($bg,0,0,63,63)
    $g.DrawRectangle($pen,14,20,33,24)
    $g.FillRectangle($fg,49,27,4,10)
    $pts = [System.Drawing.PointF[]]@([System.Drawing.PointF]::new(33,22),[System.Drawing.PointF]::new(23,34),[System.Drawing.PointF]::new(30,34),[System.Drawing.PointF]::new(27,42),[System.Drawing.PointF]::new(39,29),[System.Drawing.PointF]::new(32,29))
    $g.FillPolygon($fg,$pts)
    $stream = [System.IO.MemoryStream]::new()
    $bmp.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    $frames += ,@($size,$stream.ToArray())
    if ($size -eq 256) { $bmp.Save((Join-Path $PSScriptRoot '..\src\Zetmar.Battery\Assets\battery.png')) }
    $stream.Dispose(); $pen.Dispose(); $fg.Dispose(); $bg.Dispose(); $g.Dispose(); $bmp.Dispose()
}
$file = [System.IO.File]::Create($iconPath)
$writer = [System.IO.BinaryWriter]::new($file)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
    $n = if ($frame[0] -eq 256) { 0 } else { $frame[0] }
    $writer.Write([byte]$n); $writer.Write([byte]$n); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame[1].Length); $writer.Write([uint32]$offset)
    $offset += $frame[1].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame[1]) }
$writer.Dispose(); $file.Dispose()
