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
            g.Clear(Color.DarkGreen); // grass

            // Parameters for the track
            int margin = 80; // distance from window edge to outer road edge
            int roadWidth = 150; // total width of the road (including both lanes)
            int borderWidth = 20; // red barrier thickness

            // Outer ellipse defines the outer edge of the road (including half of the border)
            var outerRect = new Rectangle(margin, margin, width - 2 * margin, height - 2 * margin);

            // Inner ellipse defines the inner edge of the road (leaving space for the inside curb)
            var innerRect = Rectangle.Inflate(outerRect, -roadWidth, -roadWidth);

            // Build a donut‑shaped path for the road surface
            using var roadPath = new System.Drawing.Drawing2D.GraphicsPath();
            roadPath.AddEllipse(outerRect);
            roadPath.AddEllipse(innerRect);
            roadPath.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

            // Fill the road with a dark‑gray asphalt color
            using var asphaltBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
            g.FillPath(asphaltBrush, roadPath);

            // Draw lane markings – a dashed white line roughly in the centre of the road
            var midRect = Rectangle.Inflate(outerRect, -(roadWidth / 2), -(roadWidth / 2));
            using var lanePen = new Pen(Color.White, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawEllipse(lanePen, midRect);

            // Draw a thick red border that will act as a barrier (centered on the outer ellipse edge)
            using var borderPen = new Pen(Color.Red, borderWidth);
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
                // Treat any pixel that is close to pure red as a barrier (tolerate antialiasing)
                if (c.R > 200 && c.G < 50 && c.B < 50) _barrierMask[x, y] = true;
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

    // Returns the geometric centre of the track (used for collision normal calculations)
    public PointF GetCenter() => new PointF(Background.Width / 2f, Background.Height / 2f);

    public PointF[] GetWaypoints(int count = 16)
    {
        // Re‑use the same geometry as the track constructor
        int margin = 80; // same as above
        int roadWidth = 150;
        int borderWidth = 20;
        // Outer ellipse bounds (same as used for drawing)
        var outerRect = new Rectangle(margin, margin, Background.Width - 2 * margin, Background.Height - 2 * margin);
        // Inner ellipse is the drivable area (outerRect contracted by roadWidth)
        var innerRect = Rectangle.Inflate(outerRect, -roadWidth, -roadWidth);

        float cx = outerRect.X + outerRect.Width / 2f;
        float cy = outerRect.Y + outerRect.Height / 2f;
        float a = innerRect.Width / 2f; // semi‑major axis
        float b = innerRect.Height / 2f; // semi‑minor axis

        var pts = new PointF[count];
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2f * MathF.PI / count;
            float x = cx + a * MathF.Cos(angle);
            float y = cy + b * MathF.Sin(angle);
            pts[i] = new PointF(x, y);
        }
        return pts;
    }

}
