using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WslPathConverter
{
    internal static class MenuIcons
    {
        private static readonly Dictionary<string, Image> cache = new Dictionary<string, Image>();

        public static Image Get(string kind)
        {
            Image image;
            if (cache.TryGetValue(kind, out image))
                return image;
            image = Create(kind);
            cache[kind] = image;
            return image;
        }

        private static Image Create(string kind)
        {
            var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(96, 96, 96), 1.4f))
                using (var brush = new SolidBrush(Color.FromArgb(96, 96, 96)))
                {
                    if (kind == "hotkey") DrawHotkey(g, pen, brush);
                    else if (kind == "image") DrawImage(g, pen, brush);
                    else if (kind == "path") DrawPath(g, pen, brush);
                    else if (kind == "power") DrawPower(g, pen, brush);
                    else if (kind == "folder") DrawFolder(g, pen, brush);
                    else if (kind == "exit") DrawExit(g, pen, brush);
                }
            }
            return bitmap;
        }

        private static void DrawHotkey(Graphics g, Pen pen, Brush brush)
        {
            g.DrawRectangle(pen, 2, 4, 12, 8);
            g.FillRectangle(brush, 4, 6, 2, 2);
            g.FillRectangle(brush, 7, 6, 2, 2);
            g.FillRectangle(brush, 10, 6, 2, 2);
            g.FillRectangle(brush, 5, 9, 6, 2);
        }

        private static void DrawImage(Graphics g, Pen pen, Brush brush)
        {
            g.DrawRectangle(pen, 2, 3, 12, 10);
            g.FillEllipse(brush, 4, 5, 2, 2);
            g.DrawLines(pen, new[]
            {
                new PointF(3, 12), new PointF(7, 7), new PointF(9, 10),
                new PointF(11, 8), new PointF(13, 11)
            });
        }

        private static void DrawPath(Graphics g, Pen pen, Brush brush)
        {
            var curve = new GraphicsPath();
            curve.AddBezier(3, 12, 3, 5, 13, 11, 13, 4);
            g.DrawPath(pen, curve);
            curve.Dispose();
            g.FillEllipse(brush, 1.5f, 10.5f, 3, 3);
            g.FillEllipse(brush, 11.5f, 2.5f, 3, 3);
        }

        private static void DrawPower(Graphics g, Pen pen, Brush brush)
        {
            g.DrawArc(pen, 3, 3, 10, 10, 300, 300);
            g.DrawLine(pen, 8, 2, 8, 8);
        }

        private static void DrawFolder(Graphics g, Pen pen, Brush brush)
        {
            g.DrawLines(pen, new[]
            {
                new PointF(2, 13), new PointF(2, 4), new PointF(6, 4),
                new PointF(7, 6), new PointF(14, 6), new PointF(14, 13), new PointF(2, 13)
            });
        }

        private static void DrawExit(Graphics g, Pen pen, Brush brush)
        {
            g.DrawRectangle(pen, 2, 3, 7, 10);
            g.DrawLine(pen, 7, 8, 14, 8);
            g.DrawLines(pen, new[]
            {
                new PointF(11, 5), new PointF(14, 8), new PointF(11, 11)
            });
        }
    }
}
