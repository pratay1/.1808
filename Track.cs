using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TopDownRacing;

public class Track
{
    public Bitmap Background { get; private set; }
    private bool[,] _barrierMask; // true = barrier pixel
    public Size Size => Background.Size;

    // Procedural track: an oval road with a thick red border acting as barrier
    public Track(int width = 800, int height = 600)
    {
        Background = new Bitmap(width, height);
        using (var g = Graphics.FromImage(Background))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.DarkGreen);

            // Define outer bounds of the road (including border thickness)
            var outerRect = new Rectangle(100, 100, width - 200, height - 200);
            // Fill the road area (light gray interior)
            using var roadBrush = new SolidBrush(Color.LightGray);
            g.FillEllipse(roadBrush, outerRect);

            // Draw thick red border that will serve as barrier
            using var borderPen = new Pen(Color.Red, 20); // 20‑pixel-wide barrier
            g.DrawEllipse(borderPen, outerRect);
        }

        BuildBarrierMask();
    }

    private void BuildBarrierMask()
    {
        int w = Background.Width;
        int h = Background.Height;
        _barrierMask = new bool[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = Background.GetPixel(x, y);
                // Treat any pixel that is exactly red as a barrier
                if (c.R == 255 && c.G == 0 && c.B == 0) _barrierMask[x, y] = true;
            }
        }
    }

    // Checks whether a point collides with a barrier pixel (or is out of bounds)
    public bool IsBarrierAt(PointF p)
    {
        int x = (int)MathF.Round(p.X);
        int y = (int)MathF.Round(p.Y);
        if (x < 0 || y < 0 || x >= Background.Width || y >= Background.Height) return true;
        return _barrierMask[x, y];
    }

    // Checks whether any of the given points intersect a barrier.
    public bool CollidesWithBarrier(PointF[] points)
    {
        foreach (var pt in points)
        {
            if (IsBarrierAt(pt)) return true;
        }
        return false;
    }

    // Returns a set of evenly spaced waypoints around the inner edge of the track (used by AI)
    public PointF[] GetWaypoints(int count = 16)
    {
        // The inner ellipse is the outer ellipse reduced by half the border width (20/2 = 10)
        const int borderWidth = 20;
        int margin = 100;
        float innerWidth = Background.Width - 2 * margin - borderWidth;
        float innerHeight = Background.Height - 2 * margin - borderWidth;
        float cx = Background.Width / 2f;
        float cy = Background.Height / 2f;

        var pts = new PointF[count];
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2f * MathF.PI / count;
            float x = cx + (innerWidth / 2f) * MathF.Cos(angle);
            float y = cy + (innerHeight / 2f) * MathF.Sin(angle);
            pts[i] = new PointF(x, y);
        }
        return pts;
    }
}
