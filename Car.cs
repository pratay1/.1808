using System;
using System.Drawing;

namespace bumpercars;

public class Car
{
    public PointF Position;
    public float Angle;
    public float VelocityX;
    public float VelocityY;
    public float Speed => MathF.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);
    public float MaxSpeed = 8f;
    public float Acceleration = 0.15f;
    public float TurnRate = 4f;
    public float Drag = 0.985f;
    public float Grip = 0.92f;
    public float DriftGrip = 0.15f;
    public bool IsPlayer;
    public bool IsDrifting;
    public float LastNonDriftAngle;
    public Color BodyColor;

    public readonly float Width = 40f;
    public readonly float Height = 20f;
    public Size Size => new Size((int)Width, (int)Height);

    public Car(PointF startPos, bool isPlayer, Color bodyColor)
    {
        Position = startPos;
        Angle = 0f;
        LastNonDriftAngle = 0f;
        VelocityX = 0f;
        VelocityY = 0f;
        IsPlayer = isPlayer;
        BodyColor = bodyColor;
    }

    public void Update(float dt, bool up, bool down, bool left, bool right, bool drift)
    {
        float speed = Speed;
        float maxSpeedThreshold = MaxSpeed * 0.2f;
        bool canPivotTurn = speed < maxSpeedThreshold && drift;

        IsDrifting = drift && !canPivotTurn;

        if (IsPlayer)
        {
            float rad = (Angle - 90f) * MathF.PI / 180f;

            if (up)
            {
                VelocityX += MathF.Cos(rad) * Acceleration;
                VelocityY += MathF.Sin(rad) * Acceleration;
            }

            if (canPivotTurn)
            {
                // Pivot turn while moving slow and holding drift
                if (left) Angle -= TurnRate;
                if (right) Angle += TurnRate;
            }
            else if (speed > 0.1f)
            {
                // Normal steering
                float turnFactor = Math.Min(speed / MaxSpeed, 1f);
                if (left) Angle -= TurnRate * turnFactor;
                if (right) Angle += TurnRate * turnFactor;
            }

            // Track last angle when not drifting for pivot return
            if (!drift)
            {
                LastNonDriftAngle = Angle;
            }
        }

        // Apply grip
        if (!canPivotTurn)
        {
            ApplyGrip(drift);
        }
        else
        {
            // When pivot turning, maintain velocity direction, very low grip
            float gripAmount = DriftGrip;
            VelocityX *= (1f - gripAmount) + gripAmount;
            VelocityY *= (1f - gripAmount) + gripAmount;
        }

        // Apply drag (higher after drift release)
        float dragAmount = (IsDrifting && !drift) ? 0.92f : Drag;
        VelocityX *= dragAmount;
        VelocityY *= dragAmount;

        // Clamp speed
        if (Speed > MaxSpeed)
        {
            float scale = MaxSpeed / Speed;
            VelocityX *= scale;
            VelocityY *= scale;
        }

        Position.X += VelocityX;
        Position.Y += VelocityY;
    }

    private void ApplyGrip(bool drift)
    {
        float speed = Speed;
        if (speed < 0.01f) return;

        float velAngle = MathF.Atan2(VelocityY, VelocityX) * 180f / MathF.PI;
        float carAngle = Angle - 90f;

        float angleDiff = carAngle - velAngle;
        while (angleDiff > 180) angleDiff -= 360;
        while (angleDiff < -180) angleDiff += 360;

        float diffRad = angleDiff * MathF.PI / 180f;
        float cosDiff = MathF.Cos(diffRad);
        float sinDiff = MathF.Sin(diffRad);

        float forwardSpeed = speed * cosDiff;
        float lateralSpeed = speed * sinDiff;

        float gripAmount = drift ? DriftGrip : Grip;
        lateralSpeed *= gripAmount;

        float newSpeed = MathF.Sqrt(forwardSpeed * forwardSpeed + lateralSpeed * lateralSpeed);
        float newAngle = velAngle + angleDiff * (1 - gripAmount);

        float newRad = newAngle * MathF.PI / 180f;
        VelocityX = newSpeed * MathF.Cos(newRad);
        VelocityY = newSpeed * MathF.Sin(newRad);
    }

    public PointF GetCenter()
    {
        return new PointF(Position.X + Width / 2f, Position.Y + Height / 2f);
    }

    public void Render(Graphics g)
    {
        var center = GetCenter();
        var saved = g.Transform;
        g.TranslateTransform(center.X, center.Y);
        g.RotateTransform(Angle - 90f);

        float hw = Width / 2f;
        float hh = Height / 2f;

        var sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
        g.FillRectangle(sb, (int)(-hw + 2), (int)(-hh + 3), (int)Width, (int)Height);

        var wb = new SolidBrush(Color.FromArgb(30, 30, 30));
        g.FillRectangle(wb, (int)(hw * 0.4f), (int)(-hh - 2), 8, 4);
        g.FillRectangle(wb, (int)(hw * 0.4f), (int)(hh - 2), 8, 4);
        g.FillRectangle(wb, (int)(-hw - 6), (int)(-hh - 2), 8, 4);
        g.FillRectangle(wb, (int)(-hw - 6), (int)(hh - 2), 8, 4);

        var bb = new SolidBrush(BodyColor);
        g.FillRectangle(bb, (int)(-hw), (int)(-hh), (int)Width, (int)Height);

        var wsb = new SolidBrush(Color.FromArgb(150, 200, 255));
        g.FillRectangle(wsb, (int)(hw * 0.15f), (int)(-hh + 2), (int)(hw * 0.35f), (int)(Height - 4));

        int rc = BodyColor.R + 35;
        int gc = BodyColor.G + 35;
        int bc = BodyColor.B + 35;
        var rb = new SolidBrush(Color.FromArgb(rc > 255 ? 255 : rc, gc > 255 ? 255 : gc, bc > 255 ? 255 : bc));
        g.FillRectangle(rb, (int)(-hw * 0.35f), (int)(-hh + 3), (int)(hw * 0.6f), (int)(Height - 6));

        var lb = new SolidBrush(Color.FromArgb(255, 255, 150));
        g.FillRectangle(lb, (int)(hw - 3), (int)(-hh + 3), 3, 4);
        g.FillRectangle(lb, (int)(hw - 3), (int)(hh - 7), 3, 4);

        g.Transform = saved;
    }
}