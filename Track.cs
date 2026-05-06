using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace bumpercars;

public class Track
{
    public Bitmap Background { get; private set; }
    private bool[,] _barrierMask = null!; // true = barrier pixel
    public Size Size => Background.Size;

    // Arena settings
    public int WallThickness { get; private set; } = 30;
    public int InnerMargin => WallThickness;

    // Square arena with black walls and dark grey floor
    public Track(int width = 800, int height = 600)
    {
        WallThickness = 30;
        Background = new Bitmap(width, height);
        using (var g = Graphics.FromImage(Background))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Dark grey floor (arcade bumper car style)
            g.Clear(Color.FromArgb(60, 60, 60));

            // Draw black walls on all four sides with rounded corners
            int wt = WallThickness;
            using var wallBrush = new SolidBrush(Color.FromArgb(20, 20, 20));

            // Top wall
            g.FillRectangle(wallBrush, 0, 0, width, wt);
            // Bottom wall
            g.FillRectangle(wallBrush, 0, height - wt, width, wt);
            // Left wall
            g.FillRectangle(wallBrush, 0, 0, wt, height);
            // Right wall
            g.FillRectangle(wallBrush, width - wt, 0, wt, height);

            // Corner patches to fill gaps
            g.FillRectangle(wallBrush, 0, 0, wt, wt);
            g.FillRectangle(wallBrush, width - wt, 0, wt, wt);
            g.FillRectangle(wallBrush, 0, height - wt, wt, wt);
            g.FillRectangle(wallBrush, width - wt, height - wt, wt, wt);

            // Inner play area outline (subtle)
            int im = wt + 5;
            using var innerPen = new Pen(Color.FromArgb(40, 40, 40), 2);
            g.DrawRectangle(innerPen, im, im, width - 2 * im, height - 2 * im);
        }

        BuildBarrierMask();
    }

    private void BuildBarrierMask()
    {
        int w = Background.Width;
        int h = Background.Height;
        _barrierMask = new bool[w, h];
        int wt = WallThickness;

        // Mark wall pixels as barriers
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // All pixels within wall thickness are barriers
                if (x < wt || x >= w - wt || y < wt || y >= h - wt)
                {
                    _barrierMask[x, y] = true;
                }
            }
        }
    }

    public bool IsBarrierAt(PointF p)
    {
        int x = (int)MathF.Round(p.X);
        int y = (int)MathF.Round(p.Y);
        if (x < 0 || y < 0 || x >= Background.Width || y >= Background.Height) return true;
        return _barrierMask[x, y];
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