using System;
using System.Drawing;
using System.Linq;

namespace TopDownRacing;

public class Track
{
    public Bitmap Background { get; private set; }
    private bool[,] _barrierMask; // true = barrier pixel
    public Size Size => Background.Size;

    // Simple procedural track: green field with a red rectangular barrier loop
    public Track(int width = 800, int height = 600)
    {
        Background = new Bitmap(width, height);
        using (var g = Graphics.FromImage(Background))
        {
            g.Clear(Color.DarkGreen);
            // Draw a simple rectangular road (light gray) inside the green field
            var roadRect = new Rectangle(100, 100, width - 200, height - 200);
            g.FillRectangle(Brushes.LightGray, roadRect);
            // Draw outer barriers (red) around the road
            using var pen = new Pen(Color.Red, 10);
            g.DrawRectangle(pen, roadRect);
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

    // Checks whether the given rectangle collides with any barrier pixel.
    public bool CollidesWithBarrier(RectangleF rect)
    {
        int left = (int)Math.Floor(rect.Left);
        int top = (int)Math.Floor(rect.Top);
        int right = (int)Math.Ceiling(rect.Right);
        int bottom = (int)Math.Ceiling(rect.Bottom);

        // Clamp to bitmap bounds
        left = Math.Max(left, 0);
        top = Math.Max(top, 0);
        right = Math.Min(right, Background.Width - 1);
        bottom = Math.Min(bottom, Background.Height - 1);

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                if (_barrierMask[x, y])
                {
                    // Simple pixel‑level collision – if any barrier pixel is inside the car bounds, treat as collision
                    return true;
                }
            }
        }
        return false;
    }
}
