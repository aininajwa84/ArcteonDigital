# Preserve the supplied artwork while removing white backgrounds and blank margins.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$probe = [System.Drawing.Bitmap]::new(1, 1)
$probe.Dispose()
$drawingReferences = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { -not $_.IsDynamic -and $_.Location } | ForEach-Object Location
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class LogoAssetProcessor
{
    public static string ExportDensity(string path, int minimumWidth, int minimumHeight)
    {
        using (var original = new Bitmap(path))
        {
            double scale = Math.Max(1, Math.Max((double)minimumWidth / original.Width, (double)minimumHeight / original.Height));
            if (scale == 1) return System.IO.Path.GetFileName(path) + ": original resolution retained";
            using (var result = new Bitmap((int)Math.Ceiling(original.Width * scale), (int)Math.Ceiling(original.Height * scale), PixelFormat.Format32bppArgb))
            {
                result.SetResolution(144, 144);
                using (var graphics = Graphics.FromImage(result))
                using (var attributes = new ImageAttributes())
                {
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    attributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(original, new Rectangle(0, 0, result.Width, result.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
                // Release the input file before overwriting it.
                using (var stream = new System.IO.MemoryStream())
                {
                    result.Save(stream, ImageFormat.Png);
                    original.Dispose();
                    System.IO.File.WriteAllBytes(path, stream.ToArray());
                }
                return System.IO.Path.GetFileName(path) + ": " + result.Width + "x" + result.Height + " (resampled, not new source detail)";
            }
        }
    }
    public static string Process(string source, string destination, Rectangle? region, bool removeWhite)
    {
        using (var original = new Bitmap(source))
        {
            var crop = region ?? new Rectangle(0, 0, original.Width, original.Height);
            using (var working = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb))
            {
                int left = crop.Width, top = crop.Height, right = -1, bottom = -1;
                for (int y = 0; y < crop.Height; y++)
                for (int x = 0; x < crop.Width; x++)
                {
                    var pixel = original.GetPixel(crop.X + x, crop.Y + y);
                    int alpha = pixel.A;
                    int min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                    int max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
                    if (removeWhite && min >= 246) alpha = 0;
                    else if (removeWhite && min >= 238 && max - min <= 8)
                        alpha = alpha * (246 - min) / 8;
                    working.SetPixel(x, y, alpha == 0 ? Color.FromArgb(0, 0, 0, 0) : Color.FromArgb(alpha, pixel.R, pixel.G, pixel.B));
                    if (alpha > 8)
                    {
                        left = Math.Min(left, x); top = Math.Min(top, y);
                        right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                    }
                }
                if (right < left) throw new InvalidOperationException("Logo has no visible artwork.");
                const int padding = 4;
                int width = right - left + 1, height = bottom - top + 1;
                using (var result = new Bitmap(width + padding * 2, height + padding * 2, PixelFormat.Format32bppArgb))
                {
                    // Copy pixels directly: no interpolation, redrawing or colour changes.
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        result.SetPixel(x + padding, y + padding, working.GetPixel(x + left, y + top));
                    result.Save(destination, ImageFormat.Png);
                    return System.IO.Path.GetFileName(destination) + ": " + result.Width + "x" + result.Height;
                }
            }
        }
    }
}
'@

$repoRoot = Split-Path -Parent $PSScriptRoot
$assetRoot = Join-Path $repoRoot 'wwwroot/assets/img'
$productRoot = Join-Path $assetRoot 'products'
$sheet = Join-Path $productRoot 'product-reference.png'

[LogoAssetProcessor]::Process((Join-Path $assetRoot 'arcteon-logo-tagline.png'), (Join-Path $assetRoot 'arcteon-digital-tagline-transparent.png'), $null, $true)
[LogoAssetProcessor]::Process((Join-Path $productRoot 'arcteonmed-logo.png'), (Join-Path $productRoot 'arcteonmed-transparent.png'), $null, $true)
# The clock is intentional white artwork inside the coloured symbol; preserve it.
[LogoAssetProcessor]::Process($sheet, (Join-Path $productRoot 'workvibes-symbol-transparent.png'), [Drawing.Rectangle]::new(83, 98, 49, 50), $false)
[LogoAssetProcessor]::Process($sheet, (Join-Path $productRoot 'sep-transparent.png'), [Drawing.Rectangle]::new(377, 102, 83, 52), $true)
[LogoAssetProcessor]::Process($sheet, (Join-Path $productRoot 'qms-transparent.png'), [Drawing.Rectangle]::new(400, 276, 35, 12), $true)
[LogoAssetProcessor]::Process((Join-Path $assetRoot 'malaysia-digital-source.png'), (Join-Path $assetRoot 'malaysia-digital-logo.png'), $null, $false)

# Three-times display density. Large originals stay at their native resolution.
# Small source artwork is smoothly resampled, never generated or redrawn.
[LogoAssetProcessor]::ExportDensity((Join-Path $assetRoot 'arcteon-digital-tagline-transparent.png'), 1200, 0)
[LogoAssetProcessor]::ExportDensity((Join-Path $productRoot 'arcteonmed-transparent.png'), 600, 336)
[LogoAssetProcessor]::ExportDensity((Join-Path $productRoot 'workvibes-symbol-transparent.png'), 264, 264)
[LogoAssetProcessor]::ExportDensity((Join-Path $productRoot 'sep-transparent.png'), 540, 336)
[LogoAssetProcessor]::ExportDensity((Join-Path $productRoot 'qms-transparent.png'), 420, 210)
[LogoAssetProcessor]::ExportDensity((Join-Path $assetRoot 'malaysia-digital-logo.png'), 204, 0)
