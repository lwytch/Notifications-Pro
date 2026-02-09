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

        // Purple rounded background
        using var bgBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(124, 92, 252));
        FillRoundedRect(g, bgBrush, new Drawing.Rectangle(1, 1, 30, 30), 8);

        // White "N" letter
        using var font = new Drawing.Font("Segoe UI", 15, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var textBrush = new Drawing.SolidBrush(Drawing.Color.White);
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
}
