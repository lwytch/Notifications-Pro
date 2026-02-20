using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingText = System.Drawing.Text;

namespace NotificationsPro.Helpers;

public static class IconHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Drawing.Icon CreateTrayIcon()
    {
        using var bitmap = new Drawing.Bitmap(32, 32);
        using var g = Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

        // Neutral white badge background
        using var bgBrush = new Drawing.SolidBrush(Drawing.Color.White);
        FillRoundedRect(g, bgBrush, new Drawing.Rectangle(1, 1, 30, 30), 8);
        using var borderPen = new Drawing.Pen(Drawing.Color.Black, 1.2f);
        DrawRoundedRect(g, borderPen, new Drawing.Rectangle(1, 1, 30, 30), 8);

        // Black "N" letter
        using var font = new Drawing.Font("Segoe UI", 15, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var textBrush = new Drawing.SolidBrush(Drawing.Color.Black);
        var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        g.DrawString("N", font, textBrush, new Drawing.RectangleF(0, 1, 32, 32), format);

        var hIcon = bitmap.GetHicon();
        try
        {
            using var temporary = Drawing.Icon.FromHandle(hIcon);
            return (Drawing.Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public static System.Windows.Media.ImageSource CreateTrayIconImageSource(int size = 32)
    {
        using var icon = CreateTrayIcon();
        var source = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(size, size));
        source.Freeze();
        return source;
    }

    /// <summary>
    /// Creates a dimmed/muted variant of the tray icon (lower saturation + opacity).
    /// Used when notifications are paused.
    /// </summary>
    public static Drawing.Icon CreateDimmedTrayIcon()
    {
        using var bitmap = new Drawing.Bitmap(32, 32);
        using var g = Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

        // Dimmed monochrome variant
        using var bgBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(200, 230, 230, 230));
        FillRoundedRect(g, bgBrush, new Drawing.Rectangle(1, 1, 30, 30), 8);
        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(200, 80, 80, 80), 1.2f);
        DrawRoundedRect(g, borderPen, new Drawing.Rectangle(1, 1, 30, 30), 8);

        using var font = new Drawing.Font("Segoe UI", 15, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var textBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(210, 40, 40, 40));
        var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        g.DrawString("N", font, textBrush, new Drawing.RectangleF(0, 1, 32, 32), format);

        var hIcon = bitmap.GetHicon();
        try
        {
            using var temporary = Drawing.Icon.FromHandle(hIcon);
            return (Drawing.Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// Creates a tray icon with a notification count badge.
    /// </summary>
    public static Drawing.Icon CreateBadgedTrayIcon(int count)
    {
        using var bitmap = new Drawing.Bitmap(32, 32);
        using var g = Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

        // Neutral white background
        using var bgBrush = new Drawing.SolidBrush(Drawing.Color.White);
        FillRoundedRect(g, bgBrush, new Drawing.Rectangle(1, 1, 30, 30), 8);
        using var borderPen = new Drawing.Pen(Drawing.Color.Black, 1.2f);
        DrawRoundedRect(g, borderPen, new Drawing.Rectangle(1, 1, 30, 30), 8);

        // Black "N" letter
        using var font = new Drawing.Font("Segoe UI", 15, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var textBrush = new Drawing.SolidBrush(Drawing.Color.Black);
        var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        g.DrawString("N", font, textBrush, new Drawing.RectangleF(0, 1, 32, 32), format);

        // Badge circle in bottom-right
        if (count > 0)
        {
            var badgeText = count > 9 ? "9+" : count.ToString();
            using var badgeBg = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 80, 80));
            g.FillEllipse(badgeBg, 18, 18, 13, 13);
            using var badgeFont = new Drawing.Font("Segoe UI", 8, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            using var badgeTextBrush = new Drawing.SolidBrush(Drawing.Color.White);
            var badgeFormat = new Drawing.StringFormat
            {
                Alignment = Drawing.StringAlignment.Center,
                LineAlignment = Drawing.StringAlignment.Center
            };
            g.DrawString(badgeText, badgeFont, badgeTextBrush, new Drawing.RectangleF(18, 18, 13, 13), badgeFormat);
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            using var temporary = Drawing.Icon.FromHandle(hIcon);
            return (Drawing.Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static void FillRoundedRect(Drawing.Graphics g, Drawing.Brush brush, Drawing.Rectangle rect, int radius)
    {
        using var path = new Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRect(Drawing.Graphics g, Drawing.Pen pen, Drawing.Rectangle rect, int radius)
    {
        using var path = new Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }
}
