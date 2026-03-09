Add-Type -AssemblyName System.Drawing

$sizes = @{
    "SmallTile" = @{ w = 71; h = 71 }
    "Square44x44Logo" = @{ w = 44; h = 44 }
    "Square150x150Logo" = @{ w = 150; h = 150 }
    "Wide310x150Logo" = @{ w = 310; h = 150 }
    "LargeTile" = @{ w = 310; h = 310 }
    "StoreLogo" = @{ w = 50; h = 50 }
    "SplashScreen" = @{ w = 620; h = 300 }
}

$outputDir = Join-Path $PSScriptRoot "..\src\NotificationsPro.Package\Images"

if (-Not (Test-Path $outputDir)) {
    Write-Host "Images directory not found!"
    exit 1
}

$fontName = "Segoe UI"

foreach ($kv in $sizes.GetEnumerator()) {
    $name = $kv.Key
    $w = $kv.Value.w
    $h = $kv.Value.h
    
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    
    # White background
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillRectangle($bgBrush, 0, 0, $w, $h)
    
    # Dark N
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#1c1c1c"))
    
    # Calculate font size relative to height
    $fontSize = [math]::Min($w, $h) * 0.45
    $font = New-Object System.Drawing.Font($fontName, $fontSize, [System.Drawing.FontStyle]::Bold)
    
    $text = "N"
    $size = $g.MeasureString($text, $font)
    $x = ($w - $size.Width) / 2
    $y = ($h - $size.Height) / 2
    
    $g.DrawString($text, $font, $textBrush, $x, $y)
    
    $path = Join-Path $outputDir "$name.png"
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $g.Dispose()
    $bmp.Dispose()
    $bgBrush.Dispose()
    $textBrush.Dispose()
    $font.Dispose()
    
    Write-Host "Generated $name.png"
}
