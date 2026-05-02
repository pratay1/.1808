using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TopDownRacing;

public class Car
{
    public PointF Position;
    public float Angle; // degrees
    public float Speed;
    public float MaxSpeed = 8f;
    public float Acceleration = 0.2f;
    public float TurnRate = 4f; // degrees per frame at max speed
    public bool IsPlayer;
    public Color BodyColor;
    public readonly Size Size = new(40, 20);

    // AI waypoint data (optional)
    private readonly PointF[] _waypoints;
    private int _currentWaypoint;

    public Car(PointF startPos, bool isPlayer, Color bodyColor, PointF[]? waypoints = null)
    {
        Position = startPos;
        Angle = 0f;
        Speed = 0f;
        IsPlayer = isPlayer;
        BodyColor = bodyColor;
        _waypoints = waypoints ?? Array.Empty<PointF>();
        _currentWaypoint = 0;
    }

    // Update called each frame. Player input flags are passed; AI ignores them.
    public void Update(float dt, bool up, bool down, bool left, bool right)
    {
        if (IsPlayer)
        {
            // Acceleration / braking
            if (up) Speed = MathF.Min(Speed + Acceleration, MaxSpeed);
            else if (down) Speed = MathF.Max(Speed - Acceleration, -MaxSpeed / 2);
            else Speed *= 0.95f; // simple friction

            // Steering – only when moving
            if (Speed != 0)
            {
                if (left) Angle -= TurnRate * (Speed / MaxSpeed);
                if (right) Angle += TurnRate * (Speed / MaxSpeed);
            }
        }
        else
        {
            UpdateAI(dt);
        }

        // Move forward based on current angle and speed
        float rad = Angle * MathF.PI / 180f;
        Position.X += MathF.Cos(rad) * Speed;
        Position.Y += MathF.Sin(rad) * Speed;
    }

    // Simple waypoint‑following AI
    private void UpdateAI(float dt)
    {
        if (_waypoints.Length == 0) return;

        var target = _waypoints[_currentWaypoint];
        float dx = target.X - Position.X;
        float dy = target.Y - Position.Y;
        float targetAngle = MathF.Atan2(dy, dx) * 180f / MathF.PI;

        // Normalise angle difference to [-180,180]
        float diff = ((targetAngle - Angle + 540) % 360) - 180;

        // Turn towards target gradually
        if (MathF.Abs(diff) > 2f)
        {
            Angle += MathF.Sign(diff) * TurnRate * (Speed / MaxSpeed);
        }
        else
        {
            Angle = targetAngle; // snap when close
        }

        // Accelerate when roughly facing target, otherwise slow down
        if (MathF.Abs(diff) < 30f) Speed = MathF.Min(Speed + Acceleration, MaxSpeed);
        else Speed *= 0.94f;

        // Switch to next waypoint when close enough
        if (MathF.Sqrt(dx * dx + dy * dy) < 10f) _currentWaypoint = (_currentWaypoint + 1) % _waypoints.Length;
    }

    // Axis‑aligned bounding box (used for checkpoint detection)
    public RectangleF Bounds => new RectangleF(Position.X, Position.Y, Size.Width, Size.Height);

    // Returns the four corner points of the rotated car – used for precise barrier collision
    public PointF[] GetCorners()
    {
        float cx = Position.X + Size.Width / 2f;
        float cy = Position.Y + Size.Height / 2f;
        float rad = Angle * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        var offsets = new[]
        {
            new PointF(-Size.Width / 2f, -Size.Height / 2f), // top‑left
            new PointF( Size.Width / 2f, -Size.Height / 2f), // top‑right
            new PointF( Size.Width / 2f,  Size.Height / 2f), // bottom‑right
            new PointF(-Size.Width / 2f,  Size.Height / 2f)  // bottom‑left
        };
        var corners = new PointF[4];
        for (int i = 0; i < 4; i++)
        {
            float ox = offsets[i].X * cos - offsets[i].Y * sin;
            float oy = offsets[i].X * sin + offsets[i].Y * cos;
            corners[i] = new PointF(cx + ox, cy + oy);
        }
        return corners;
    }

        // Render the car – draws a stylized red car with a white front bar
        public void Render(Graphics g)
        {
            var saved = g.Transform;
            g.TranslateTransform(Position.X + Size.Width / 2f, Position.Y + Size.Height / 2f);
            g.RotateTransform(Angle);

            // Draw car body as a rounded rectangle (more polished look)
            using var bodyPath = new System.Drawing.Drawing2D.GraphicsPath();
            float radius = 4f;
            var rect = new RectangleF(-Size.Width / 2f, -Size.Height / 2f, Size.Width, Size.Height);
            bodyPath.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            bodyPath.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            bodyPath.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            bodyPath.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            bodyPath.CloseFigure();
            using var bodyBrush = new SolidBrush(BodyColor);
            g.FillPath(bodyBrush, bodyPath);

            // Simple roof shape – a smaller rectangle on top
            var roofRect = new RectangleF(-Size.Width * 0.2f, -Size.Height * 0.6f, Size.Width * 0.4f, Size.Height * 0.4f);
            using var roofBrush = new SolidBrush(Color.FromArgb(200, 200, 200)); // light gray roof
            g.FillRectangle(roofBrush, roofRect);

            // White front bar (2x2 pixels) placed at the front edge
            const int barSize = 2;
            float frontX = Size.Width / 2f - barSize / 2f;
            float frontY = -barSize / 2f;
            using var white = new SolidBrush(Color.White);
            g.FillRectangle(white, frontX, frontY, barSize, barSize);

            g.Transform = saved;
        }
}
