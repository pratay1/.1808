using System;
using System.Drawing;
using System.Collections.Generic;

namespace bumpercars;

public static class AIController
{
    private static readonly Random _rng = new();

    public static void UpdateAI(Car car, List<Car> allCars, Track track, float dt)
    {
        if (!car.IsAI || car.IsDead) return;

        car.AIDecisionTimer += dt;
        if (car.AIDecisionTimer >= car.AIDecisionInterval)
        {
            car.AIDecisionTimer = 0;
            car.Target = SelectTarget(car, allCars, track);
        }

        bool up = true;
        bool down = false;
        bool left = false;
        bool right = false;
        bool drift = false;

        if (car.Target != null && !car.Target.IsDead)
        {
            ProcessAIBehavior(car, car.Target, track, dt, ref up, ref down, ref left, ref right, ref drift);
        }
        else
        {
            ProcessAIWander(car, track, dt, ref up, ref down, ref left, ref right, ref drift);
        }

        car.Update(dt, up, down, left, right, drift);
    }

    private static Car? SelectTarget(Car ai, List<Car> allCars, Track track)
    {
        Car? bestTarget = null;
        float bestScore = -10000f;
        var aiPos = ai.GetCenter();

        foreach (var other in allCars)
        {
            if (other == ai || other.IsDead) continue;

            float dist = Distance(aiPos, other.GetCenter());
            float score = -dist * 0.38f;

            if (IsNearBarrier(other, track))
            {
                score += 150f;
            }

            if (other.IsPlayer)
            {
                score += 110f;
            }
            else if (other.IsAI)
            {
                score += 35f;
            }

            score += (100f - other.Health) * 0.45f;

            score += (float)(_rng.NextDouble() * 55.0 - 27.5);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = other;
            }
        }

        return bestTarget;
    }

    private static void ProcessAIBehavior(Car ai, Car target, Track track, float dt, ref bool up, ref bool down, ref bool left, ref bool right, ref bool drift)
    {
        var aiPos = ai.GetCenter();
        var targetPos = target.GetCenter();

        float dx = targetPos.X - aiPos.X;
        float dy = targetPos.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 1f) dist = 1f;
        float targetAngle = MathF.Atan2(dy, dx) * 180f / MathF.PI + 90f;

        float currentAngle = ai.Angle;
        float angleDiff = NormalizeAngle(targetAngle - currentAngle);

        // Simple barrier check - if moving toward barrier, turn away
        var lookAhead = new PointF(
            aiPos.X + MathF.Cos((currentAngle - 90f) * MathF.PI / 180f) * 40f,
            aiPos.Y + MathF.Sin((currentAngle - 90f) * MathF.PI / 180f) * 40f
        );

        if (track.IsBarrierAt(lookAhead))
        {
            if (ai.Speed < 1.2f)
            {
                if (_rng.Next(2) == 0)
                {
                    left = true;
                    right = false;
                }
                else
                {
                    right = true;
                    left = false;
                }

                up = false;
                down = true;
                return;
            }

            if (_rng.Next(2) == 0)
            {
                left = true;
                right = false;
            }
            else
            {
                right = true;
                left = false;
            }

            up = true;
            down = false;
        }
        else
        {
            drift = MathF.Abs(angleDiff) > 45f && ai.Speed > ai.MaxSpeed * 0.4f;
            left = angleDiff > 8f;
            right = angleDiff < -8f;
            up = true;
            down = false;
        }
    }

    private static void ProcessAIWander(Car ai, Track track, float dt, ref bool up, ref bool down, ref bool left, ref bool right, ref bool drift)
    {
        var center = track.GetCenter();
        var aiPos = ai.GetCenter();

        float dx = center.X - aiPos.X;
        float dy = center.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 1f) dist = 1f;
        float targetAngle = MathF.Atan2(dy, dx) * 180f / MathF.PI + 90f;

        float angleDiff = NormalizeAngle(targetAngle - ai.Angle);

        left = angleDiff > 15f;
        right = angleDiff < -15f;
        drift = MathF.Abs(angleDiff) > 60f && ai.Speed > ai.MaxSpeed * 0.3f;

        up = true;
        down = false;
    }

    private static bool IsNearBarrier(Car car, Track track)
    {
        var center = car.GetCenter();
        int checkRadius = 30;
        for (int dx = -checkRadius; dx <= checkRadius; dx += 10)
        {
            for (int dy = -checkRadius; dy <= checkRadius; dy += 10)
            {
                var pt = new PointF(center.X + dx, center.Y + dy);
                if (track.IsBarrierAt(pt)) return true;
            }
        }
        return false;
    }

    private static PointF[] GetLookAheadPoints(Car car, float distance)
    {
        var center = car.GetCenter();
        float rad = (car.Angle - 90f) * MathF.PI / 180f;
        return new PointF[]
        {
            new PointF(center.X + MathF.Cos(rad) * distance, center.Y + MathF.Sin(rad) * distance),
            new PointF(center.X + MathF.Cos(rad) * distance * 0.7f + MathF.Cos(rad + 0.5f) * 20, center.Y + MathF.Sin(rad) * distance * 0.7f + MathF.Sin(rad + 0.5f) * 20),
            new PointF(center.X + MathF.Cos(rad) * distance * 0.7f + MathF.Cos(rad - 0.5f) * 20, center.Y + MathF.Sin(rad) * distance * 0.7f + MathF.Sin(rad - 0.5f) * 20),
        };
    }

    private static float RaycastClearance(Car car, float angleOffset, Track track)
    {
        var center = car.GetCenter();
        float baseAngle = (car.Angle - 90f) * MathF.PI / 180f;
        float rayAngle = baseAngle + angleOffset * MathF.PI / 180f;

        float clearDist = 0;
        float step = 10f;
        float maxDist = 150f;

        while (clearDist < maxDist)
        {
            var pt = new PointF(
                center.X + MathF.Cos(rayAngle) * clearDist,
                center.Y + MathF.Sin(rayAngle) * clearDist
            );
            if (track.IsBarrierAt(pt)) break;
            clearDist += step;
        }

        return clearDist;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}