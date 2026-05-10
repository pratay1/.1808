using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace bumpercars;

public enum TrackLayout
{
    Arena,
    Oval,
    Figure8,
    Triangle
}

public class Track
{
    public Bitmap Background { get; private set; }
    private bool[,] _barrierMask = null!;
    public Size Size => Background.Size;
    public TrackLayout Layout { get; private set; }
    public int WallThickness { get; private set; } = 30;
    public int InnerMargin => WallThickness;

    /// <summary>Distance from window edge used when clamping car positions (layout-specific to reduce corner grinding on curved tracks).</summary>
    public int BoundsClampMargin { get; private set; }

    public Track(int width = 800, int height = 600, TrackLayout layout = TrackLayout.Arena)
    {
        Layout = layout;
        WallThickness = 30;
        Background = new Bitmap(width, height);

        switch (layout)
        {
            case TrackLayout.Arena:
                BuildArena(width, height);
                break;
            case TrackLayout.Oval:
                BuildOval(width, height);
                break;
            case TrackLayout.Figure8:
                BuildFigure8(width, height);
                break;
            case TrackLayout.Triangle:
                BuildTriangle(width, height);
                break;
        }

        BoundsClampMargin = WallThickness + layout switch
        {
            TrackLayout.Arena => 5,
            TrackLayout.Oval => 28,
            TrackLayout.Figure8 => 22,
            TrackLayout.Triangle => 26,
            _ => 5
        };

        BuildBarrierMask();
    }

    private void BuildArena(int width, int height)
    {
        using var g = Graphics.FromImage(Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(60, 60, 60));

        int wt = WallThickness;
        using var wallBrush = new SolidBrush(Color.FromArgb(20, 20, 20));

        g.FillRectangle(wallBrush, 0, 0, width, wt);
        g.FillRectangle(wallBrush, 0, height - wt, width, wt);
        g.FillRectangle(wallBrush, 0, 0, wt, height);
        g.FillRectangle(wallBrush, width - wt, 0, wt, height);

        g.FillRectangle(wallBrush, 0, 0, wt, wt);
        g.FillRectangle(wallBrush, width - wt, 0, wt, wt);
        g.FillRectangle(wallBrush, 0, height - wt, wt, wt);
        g.FillRectangle(wallBrush, width - wt, height - wt, wt, wt);

        int im = wt + 5;
        using var innerPen = new Pen(Color.FromArgb(40, 40, 40), 2);
        g.DrawRectangle(innerPen, im, im, width - 2 * im, height - 2 * im);
    }

    private void BuildOval(int width, int height)
    {
        using var g = Graphics.FromImage(Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(50, 50, 50));

        int wt = WallThickness;
        using var wallBrush = new SolidBrush(Color.FromArgb(20, 20, 20));

        int cx = width / 2;
        int cy = height / 2;
        int rx = width / 2 - wt;
        int ry = height / 2 - wt;

        // Outer oval
        g.FillEllipse(wallBrush, wt, wt, width - 2 * wt, height - 2 * wt);

        // Inner cutout (play area)
        int innerRx = rx - 60;
        int innerRy = ry - 60;
        using var floorBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
        g.FillEllipse(floorBrush, cx - innerRx, cy - innerRy, innerRx * 2, innerRy * 2);

        // Draw track lines
        using var linePen = new Pen(Color.FromArgb(50, 50, 50), 3);
        g.DrawEllipse(linePen, cx - innerRx, cy - innerRy, innerRx * 2, innerRy * 2);
    }

    private void BuildFigure8(int width, int height)
    {
        using var g = Graphics.FromImage(Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(50, 50, 50));

        int wt = WallThickness;
        using var wallBrush = new SolidBrush(Color.FromArgb(20, 20, 20));
        using var floorBrush = new SolidBrush(Color.FromArgb(70, 70, 70));

        g.FillRectangle(wallBrush, 0, 0, width, wt);
        g.FillRectangle(wallBrush, 0, height - wt, width, wt);
        g.FillRectangle(wallBrush, 0, 0, wt, height);
        g.FillRectangle(wallBrush, width - wt, 0, wt, height);

        int im = wt + 5;
        g.FillRectangle(wallBrush, im, im, width - 2 * im, height - 2 * im);

        float cx = width / 2f;
        float cy = height / 2f;
        float innerW = width - 2 * im;
        float innerH = height - 2 * im;
        float r = Math.Min(innerW, innerH) * 0.38f;
        float sep = r * 0.58f;

        using var peanut = new GraphicsPath();
        peanut.FillMode = FillMode.Winding;
        float d = r * 2f;
        peanut.AddEllipse(cx - sep - r, cy - r, d, d);
        peanut.AddEllipse(cx + sep - r, cy - r, d, d);
        g.FillPath(floorBrush, peanut);

        using var linePen = new Pen(Color.FromArgb(50, 50, 50), 3);
        g.DrawPath(linePen, peanut);
    }

    private void BuildTriangle(int width, int height)
    {
        using var g = Graphics.FromImage(Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(50, 50, 50));

        int wt = WallThickness;
        using var wallBrush = new SolidBrush(Color.FromArgb(20, 20, 20));
        using var floorBrush = new SolidBrush(Color.FromArgb(70, 70, 70));

        var points = new PointF[]
        {
            new PointF(width / 2, wt + 10),
            new PointF(wt + 10, height - wt - 10),
            new PointF(width - wt - 10, height - wt - 10)
        };

        // Draw triangular wall
        var wallPath = new System.Drawing.Drawing2D.GraphicsPath();
        wallPath.AddPolygon(points);
        g.FillPolygon(wallBrush, points);

        // Inner triangle for play area
        var innerPoints = new PointF[]
        {
            new PointF(width / 2, wt + 60),
            new PointF(wt + 50, height - wt - 50),
            new PointF(width - wt - 50, height - wt - 50)
        };
        g.FillPolygon(floorBrush, innerPoints);

        using var linePen = new Pen(Color.FromArgb(50, 50, 50), 3);
        g.DrawPolygon(linePen, innerPoints);
    }

    private void BuildBarrierMask()
    {
        int w = Background.Width;
        int h = Background.Height;
        _barrierMask = new bool[w, h];

        // Check pixel colors - walls are drawn in dark color (20, 20, 20)
        var wallColor = Color.FromArgb(20, 20, 20);
        int colorTolerance = 15;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var pixel = ((Bitmap)Background).GetPixel(x, y);
                if (IsSimilarColor(pixel, wallColor, colorTolerance))
                {
                    _barrierMask[x, y] = true;
                }
            }
        }
    }

    private bool IsSimilarColor(Color a, Color b, int tolerance)
    {
        return Math.Abs(a.R - b.R) <= tolerance &&
               Math.Abs(a.G - b.G) <= tolerance &&
               Math.Abs(a.B - b.B) <= tolerance;
    }

    public bool IsBarrierAt(PointF p)
    {
        int x = (int)MathF.Round(p.X);
        int y = (int)MathF.Round(p.Y);
        if (x < 0 || y < 0 || x >= Background.Width || y >= Background.Height) return true;
        return _barrierMask[x, y];
    }

    public bool IsShapeBarrierAt(PointF p, int margin = 4)
    {
        int x = (int)MathF.Round(p.X);
        int y = (int)MathF.Round(p.Y);

        int w = Background.Width;
        int h = Background.Height;

        if (x < margin || y < margin || x >= w - margin || y >= h - margin) return true;

        for (int dx = -margin; dx <= margin; dx++)
        {
            for (int dy = -margin; dy <= margin; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && py >= 0 && px < w && py < h)
                {
                    if (_barrierMask[px, py]) return true;
                }
            }
        }
        return false;
    }

    public float DistanceToWall(PointF p)
    {
        float minDist = float.MaxValue;

        int searchRange = 60;
        int x0 = Math.Max(0, (int)p.X - searchRange);
        int y0 = Math.Max(0, (int)p.Y - searchRange);
        int x1 = Math.Min(Background.Width - 1, (int)p.X + searchRange);
        int y1 = Math.Min(Background.Height - 1, (int)p.Y + searchRange);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (_barrierMask[x, y])
                {
                    float d = MathF.Sqrt((x - p.X) * (x - p.X) + (y - p.Y) * (y - p.Y));
                    if (d < minDist) minDist = d;
                }
            }
        }

        return minDist == float.MaxValue ? 500f : minDist;
    }

    public float GetWallProximityFactor(PointF p)
    {
        float dist = DistanceToWall(p);
        if (dist < 30f) return 1f - (dist / 30f);
        return 0f;
    }

    public bool CollidesWithBarrier(PointF[] points)
    {
        foreach (var pt in points)
        {
            if (IsBarrierAt(pt)) return true;
        }
        return false;
    }

    public PointF GetCenter() => new PointF(Background.Width / 2f, Background.Height / 2f);

    // Get the playable bounds (inside walls)
    public RectangleF GetPlayArea()
    {
        int im = WallThickness + 5;
        return new RectangleF(im, im,
            Background.Width - 2 * im,
            Background.Height - 2 * im);
    }
}