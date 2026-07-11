param(
    [string]$OutputDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "Assets")
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Save-PngIcon {
    param(
        [string]$Path,
        [switch]$Click
    )

    $bitmap = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $backgroundPath = New-RoundedRectanglePath 16 16 224 224 44
    $backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.RectangleF]::new(16, 16, 224, 224),
        [System.Drawing.Color]::FromArgb(255, 15, 23, 42),
        [System.Drawing.Color]::FromArgb(255, 37, 99, 235),
        45
    )
    $graphics.FillPath($backgroundBrush, $backgroundPath)

    $plateShadow = New-RoundedRectanglePath 43 75 170 104 14
    $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(75, 0, 0, 0))
    $graphics.FillPath($shadowBrush, $plateShadow)

    $platePath = New-RoundedRectanglePath 38 66 174 104 14
    $plateBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.RectangleF]::new(38, 66, 174, 104),
        [System.Drawing.Color]::FromArgb(246, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(225, 219, 234, 254),
        90
    )
    $graphics.FillPath($plateBrush, $platePath)

    $borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 31, 41, 55), 5)
    $graphics.DrawPath($borderPen, $platePath)

    $linePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(150, 31, 41, 55), 3)
    $graphics.DrawLine($linePen, 58, 92, 192, 92)
    $graphics.DrawLine($linePen, 58, 144, 192, 144)

    $fontFamily = [System.Drawing.FontFamily]::GenericSansSerif
    $font = [System.Drawing.Font]::new($fontFamily, 43, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 15, 23, 42))
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $graphics.DrawString("NPS", $font, $textBrush, [System.Drawing.RectangleF]::new(38, 87, 174, 64), $format)

    if ($Click) {
        $cursorPoints = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(151, 145),
            [System.Drawing.Point]::new(224, 187),
            [System.Drawing.Point]::new(192, 197),
            [System.Drawing.Point]::new(211, 234),
            [System.Drawing.Point]::new(190, 244),
            [System.Drawing.Point]::new(171, 207),
            [System.Drawing.Point]::new(148, 232)
        )
        $cursorShadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
        $cursorBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
        $cursorPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 15, 23, 42), 5)
        $graphics.TranslateTransform(5, 5)
        $graphics.FillPolygon($cursorShadow, $cursorPoints)
        $graphics.ResetTransform()
        $graphics.FillPolygon($cursorBrush, $cursorPoints)
        $graphics.DrawPolygon($cursorPen, $cursorPoints)

        $clickPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 250, 204, 21), 6)
        $graphics.DrawArc($clickPen, 181, 134, 46, 46, 210, 95)
    }

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)

    $format.Dispose()
    $font.Dispose()
    $textBrush.Dispose()
    $linePen.Dispose()
    $borderPen.Dispose()
    $plateBrush.Dispose()
    $shadowBrush.Dispose()
    $backgroundBrush.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

function Convert-PngToIco {
    param(
        [string]$PngPath,
        [string]$IcoPath
    )

    $pngBytes = [System.IO.File]::ReadAllBytes($PngPath)
    $stream = [System.IO.File]::Open($IcoPath, [System.IO.FileMode]::Create)
    $writer = [System.IO.BinaryWriter]::new($stream)

    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]1)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$pngBytes.Length)
    $writer.Write([UInt32]22)
    $writer.Write($pngBytes)

    $writer.Dispose()
    $stream.Dispose()
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$appPng = Join-Path $OutputDir "NamePlateStudio.png"
$clickPng = Join-Path $OutputDir "NamePlateStudioClick.png"
$appIco = Join-Path $OutputDir "NamePlateStudio.ico"
$clickIco = Join-Path $OutputDir "NamePlateStudioClick.ico"

Save-PngIcon -Path $appPng
Save-PngIcon -Path $clickPng -Click
Convert-PngToIco -PngPath $appPng -IcoPath $appIco
Convert-PngToIco -PngPath $clickPng -IcoPath $clickIco

Write-Host "Created: $appIco"
Write-Host "Created: $clickIco"
