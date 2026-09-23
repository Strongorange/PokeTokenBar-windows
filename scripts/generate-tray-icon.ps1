$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot '..' | Resolve-Path
$outPath = Join-Path $outDir 'assets\pokeball.ico'

function New-PokeballBitmap([int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $margin = [int][Math]::Max(1, [Math]::Floor($size / 16))
        $diameter = [int]($size - 2 * $margin)
        $middle = [int]($margin + [Math]::Floor($diameter / 2))
        $red = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 51, 76))
        $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $black = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(33, 33, 33))
        $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(33, 33, 33), [float][Math]::Max(1, $size / 16))

        $rect = New-Object System.Drawing.Rectangle($margin, $margin, $diameter, $diameter)
        $graphics.FillEllipse($red, $rect)

        $circlePath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $circlePath.AddEllipse($rect)
        $graphics.SetClip($circlePath)
        $bottomHeight = [int]($size - $middle)
        $bottom = New-Object System.Drawing.Rectangle($margin, $middle, $diameter, $bottomHeight)
        $graphics.FillRectangle($white, $bottom)
        $bandHeight = [int][Math]::Max(2, [Math]::Floor($size / 9))
        $bandTop = [int]($middle - [Math]::Floor($bandHeight / 2))
        $band = New-Object System.Drawing.Rectangle($margin, $bandTop, $diameter, $bandHeight)
        $graphics.FillRectangle($black, $band)
        $graphics.ResetClip()

        $graphics.DrawEllipse($pen, $rect)
        $center = [int][Math]::Max(3, [Math]::Floor($size / 4.5))
        $centerTop = [int]($middle - [Math]::Floor($center / 2))
        $centerLeft = [int]($margin + [Math]::Floor($diameter / 2) - [Math]::Floor($center / 2))
        $centerRect = New-Object System.Drawing.Rectangle($centerLeft, $centerTop, $center, $center)
        $graphics.FillEllipse($white, $centerRect)
        $graphics.DrawEllipse($pen, $centerRect)
        $red.Dispose(); $white.Dispose(); $black.Dispose(); $pen.Dispose()
    }
    finally {
        $graphics.Dispose()
    }
    return $bitmap
}

$sizes = @(16, 32, 48)
$frames = @()
foreach ($size in $sizes) {
    $bitmap = New-PokeballBitmap $size
    try {
        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $frames += ,($stream.ToArray())
    }
    finally {
        $bitmap.Dispose()
    }
}

$ico = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($ico)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
for ($i = 0; $i -lt $frames.Count; $i++) {
    $dimension = $sizes[$i]
    $entrySize = [byte]$dimension
    $writer.Write($entrySize)
    $writer.Write($entrySize)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$frames[$i].Length)
    $writer.Write([uint32]$offset)
    $offset += $frames[$i].Length
}
foreach ($frame in $frames) {
    $writer.Write($frame)
}
$writer.Flush()

New-Item -ItemType Directory -Force -Path (Split-Path $outPath -Parent) | Out-Null
[System.IO.File]::WriteAllBytes($outPath, $ico.ToArray())
$writer.Dispose()

Write-Host "wrote $outPath ($((Get-Item $outPath).Length) bytes)"
