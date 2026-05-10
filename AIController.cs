using System;
using System.Drawing;
using System.Collections.Generic;

namespace bumpercars;

public static class AIController
{
    private static readonly Random _rng = new();
    private const float RayCount = 9;
    private const float ShortRayLength = 70f;
    private const float LongRayLength = 140f;
    private const float RaySpread = 130f;

    public static void UpdateAI(Car car, List<Car> allCars, Track track, float dt, List<PowerUp> powerUps, GameState gameState)
    {
        if (!car.IsAI || car.IsDead) return;

        car.AIDecisionTimer += dt;
        if (car.AIDecisionTimer >= car.AIDecisionInterval)
        {
            car.AIDecisionTimer = 0;
            UpdateAIState(car, allCars, track, powerUps);
        }

        if (car.AIState == AIState.Repulsing && car.RepulsorCharge >= GameState.RepulsorChargeTime)
        {
            car.RepulsorCharge = 0f;
            car.RepulsorCooldownTimer = 0.5f;
            if (gameState != null)
                gameState.TriggerRepulsor(car);
        }

        ExecuteBehavior(car, track, dt);
    }

    private static void UpdateAIState(Car car, List<Car> allCars, Track track, List<PowerUp> powerUps)
    {
        float healthRatio = car.Health / car.MaxHealth;

        if (car.LastHitByCarTime > 0)
        {
            car.RageTimer = 6f;
            car.RageTarget = car.LastHitBy;
        }

        if (car.RageTimer > 0)
        {
            car.AIState = AIState.Raging;
            car.Target = car.RageTarget != null && !car.RageTarget.IsDead ? car.RageTarget : null;
            if (car.Target == null)
                car.AIState = AIState.Chasing;
            car.TargetPowerUp = null;
            return;
        }

        if (car.RepulsorCharge >= GameState.RepulsorChargeTime && !car.IsStunned)
        {
            int nearbyEnemies = 0;
            var carPos = car.GetCenter();
            foreach (var other in allCars)
            {
                if (other == car || other.IsDead) continue;
                float dist = Distance(carPos, other.GetCenter());
                if (dist < GameState.RepulsorRadius)
                    nearbyEnemies++;
            }

            if (nearbyEnemies >= 2 || (nearbyEnemies >= 1 && healthRatio < 0.5f))
            {
                car.AIState = AIState.Repulsing;
                car.Target = null;
                car.TargetPowerUp = null;
                return;
            }
        }

        bool needsShield = !car.HasShield && healthRatio < 0.4f;
        bool needsSpeed = car.MaxSpeed < 12f && healthRatio > 0.3f;

        if (needsShield || needsSpeed)
        {
            var nearestPowerUp = FindBestPowerUp(car, powerUps, needsShield ? PowerUpType.Shield : PowerUpType.SpeedBoost);
            if (nearestPowerUp != null)
            {
                car.AIState = AIState.SeekingPowerUp;
                car.TargetPowerUp = nearestPowerUp;
                car.Target = null;
                return;
            }
        }

        car.TargetPowerUp = null;
        car.SafeZone = new PointF(0, 0);
        car.Target = SelectTacticalTarget(car, allCars, track);
        car.AIState = car.Target != null ? AIState.Chasing : AIState.Wandering;
    }

    private static void ExecuteBehavior(Car car, Track track, float dt)
    {
        PointF desiredDir = new PointF(0, 0);
        float desiredSpeed = car.MaxSpeed;
        float aggressionBoost = 1f;

        switch (car.AIState)
        {
            case AIState.Chasing:
                if (car.Target != null && !car.Target.IsDead)
                {
                    desiredDir = ComputeChaseDirection(car, car.Target);
                    desiredSpeed = car.MaxSpeed * 1.15f;
                    aggressionBoost = 1.3f;
                }
                else
                {
                    desiredDir = ComputeWanderDirection(car, track);
                }
                break;

            case AIState.Raging:
                if (car.Target != null && !car.Target.IsDead)
                {
                    desiredDir = ComputeRageDirection(car, car.Target);
                    desiredSpeed = car.MaxSpeed * 1.3f;
                    aggressionBoost = 2.5f;
                }
                else
                {
                    desiredDir = ComputeWanderDirection(car, track);
                }
                break;

            case AIState.SeekingPowerUp:
                if (car.TargetPowerUp != null)
                {
                    desiredDir = ComputePowerUpDirection(car, car.TargetPowerUp);
                    desiredSpeed = car.MaxSpeed * 1.1f;
                }
                else
                {
                    desiredDir = ComputeWanderDirection(car, track);
                }
                break;

            case AIState.Wandering:
            default:
                desiredDir = ComputeWanderDirection(car, track);
                desiredSpeed = car.MaxSpeed * 0.7f;
                break;

            case AIState.Repulsing:
                desiredDir = new PointF(0, 0);
                desiredSpeed = 0;
                break;
        }

        var steering = ComputeSteering(car, desiredDir, track, dt, isRepulsing: car.AIState == AIState.Repulsing);
        ApplySteering(car, steering, desiredSpeed, dt, aggressionBoost, car.AIState == AIState.Repulsing);
    }

    private static PointF ComputeRageDirection(Car car, Car target)
    {
        var aiPos = car.GetCenter();
        var targetPos = target.GetCenter();

        float dx = targetPos.X - aiPos.X;
        float dy = targetPos.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 1f) dist = 1f;

        float targetRad = MathF.Atan2(target.VelocityY, target.VelocityX);
        float toTargetAngle = MathF.Atan2(dy, dx);
        float angleDiff = toTargetAngle - targetRad;
        while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
        while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;

        float idealAngle;
        if (angleDiff > 0)
            idealAngle = targetRad + 0.2f;
        else
            idealAngle = targetRad - 0.2f;

        float idealX = MathF.Cos(idealAngle);
        float idealY = MathF.Sin(idealAngle);

        float toTargetX = dx / dist;
        float toTargetY = dy / dist;

        float blend = Math.Clamp(1f - dist / 200f, 0f, 1f);
        return new PointF(
            toTargetX * (1f - blend) + idealX * blend,
            toTargetY * (1f - blend) + idealY * blend
        );
    }

    private static PointF ComputeChaseDirection(Car car, Car target)
    {
        var aiPos = car.GetCenter();
        var targetPos = target.GetCenter();

        float aiVelMag = car.Speed;
        float targetVelMag = target.Speed;

        float dx = targetPos.X - aiPos.X;
        float dy = targetPos.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 1f) dist = 1f;

        if (dist > 40f)
        {
            float interceptTime = dist / Math.Max(aiVelMag + targetVelMag, 1f);
            interceptTime = Math.Clamp(interceptTime, 0.1f, 1f);

            PointF targetFuture = new PointF(
                targetPos.X + target.VelocityX * interceptTime * 8f,
                targetPos.Y + target.VelocityY * interceptTime * 8f
            );

            dx = targetFuture.X - aiPos.X;
            dy = targetFuture.Y - aiPos.Y;
            dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 1f) dist = 1f;
        }

        PointF idealApproach = ComputeIdealApproachAngle(car, target);

        float blendFactor = dist < 80f ? 0.2f : 0.6f;
        float toTargetX = dx / dist;
        float toTargetY = dy / dist;

        float resultX = toTargetX * (1 - blendFactor) + idealApproach.X * blendFactor;
        float resultY = toTargetY * (1 - blendFactor) + idealApproach.Y * blendFactor;

        float mag = MathF.Sqrt(resultX * resultX + resultY * resultY);
        if (mag > 0.01f)
        {
            resultX /= mag;
            resultY /= mag;
        }

        return new PointF(resultX, resultY);
    }

    private static PointF ComputeIdealApproachAngle(Car car, Car target)
    {
        var aiPos = car.GetCenter();
        var targetPos = target.GetCenter();

        float dx = targetPos.X - aiPos.X;
        float dy = targetPos.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 1f) dist = 1f;

        float targetRad = MathF.Atan2(target.VelocityY, target.VelocityX);
        float perpX = -MathF.Sin(targetRad);
        float perpY = MathF.Cos(targetRad);

        float toTargetAngle = MathF.Atan2(dy, dx);
        float angleDiff = toTargetAngle - targetRad;
        while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
        while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;

        bool hitFromBehind = angleDiff > 0;

        float idealAngle;
        if (car.Speed > target.Speed * 1.1f)
        {
            idealAngle = targetRad + (hitFromBehind ? 0.25f : -0.25f);
        }
        else
        {
            float lateralOffset = hitFromBehind ? 0.9f : -0.9f;
            idealAngle = targetRad + lateralOffset;
        }

        float baseAngle = MathF.Atan2(dy, dx);
        idealAngle = baseAngle * 0.35f + idealAngle * 0.65f;

        return new PointF(MathF.Cos(idealAngle), MathF.Sin(idealAngle));
    }

    private static PointF ComputePowerUpDirection(Car car, PowerUp pu)
    {
        var aiPos = car.GetCenter();
        float dx = pu.Position.X - aiPos.X;
        float dy = pu.Position.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 1f) dist = 1f;
        return new PointF(dx / dist, dy / dist);
    }

    private static PointF ComputeWanderDirection(Car car, Track track)
    {
        var aiPos = car.GetCenter();
        var center = track.GetCenter();

        float dx = center.X - aiPos.X;
        float dy = center.Y - aiPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 1f) dist = 1f;

        float baseAngle = MathF.Atan2(dy, dx);
        float wobble = (float)Math.Sin(DateTime.Now.Ticks / 10000000.0 + car.GetHashCode()) * 0.6f;
        float wanderAngle = baseAngle + wobble;

        return new PointF(MathF.Cos(wanderAngle), MathF.Sin(wanderAngle));
    }

    private static PointF ComputeSteering(Car car, PointF desiredDir, Track track, float dt, bool isRepulsing = false)
    {
        float rad = (car.Angle - 90f) * MathF.PI / 180f;
        PointF forward = new PointF(MathF.Cos(rad), MathF.Sin(rad));
        PointF right = new PointF(-forward.Y, forward.X);

        float forwardComp = desiredDir.X * forward.X + desiredDir.Y * forward.Y;
        float rightComp = desiredDir.X * right.X + desiredDir.Y * right.Y;

        var wallAvoid = ComputeWallAvoidance(car, track);
        float wallRightComp = wallAvoid.X * right.X + wallAvoid.Y * right.Y;
        float wallForwardComp = wallAvoid.X * forward.X + wallAvoid.Y * forward.Y;

        float totalRight = rightComp * 2.5f + wallRightComp * 1.8f;
        float totalForward = Math.Max(forwardComp * 2f + wallForwardComp * 0.5f, 0.1f);

        var carAvoid = ComputeCarAvoidance(car, track);
        float carRightComp = carAvoid.X * right.X + carAvoid.Y * right.Y;
        float carForwardComp = carAvoid.X * forward.X + carAvoid.Y * forward.Y;

        totalRight += carRightComp * 1f;
        totalForward += carForwardComp * 0.6f;

        if (!isRepulsing)
        {
            var centerSeek = ComputeCenterSeeking(car, track);
            float centerRightComp = centerSeek.X * right.X + centerSeek.Y * right.Y;
            float centerForwardComp = centerSeek.X * forward.X + centerSeek.Y * forward.Y;

            float wallProximity = track.GetWallProximityFactor(car.GetCenter());
            float centerWeight = wallProximity * 3f;

            totalRight += centerRightComp * centerWeight;
            totalForward += centerForwardComp * centerWeight * 0.5f;
        }

        return new PointF(totalRight, totalForward);
    }

    private static PointF ComputeCenterSeeking(Car car, Track track)
    {
        var carPos = car.GetCenter();
        var center = track.GetCenter();

        float dx = center.X - carPos.X;
        float dy = center.Y - carPos.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        float wallDist = track.DistanceToWall(carPos);
        if (wallDist < 40f && dist > 50f)
        {
            float urgency = 1f - (wallDist / 40f);
            urgency = urgency * urgency;
            return new PointF(dx / (dist + 1f) * urgency, dy / (dist + 1f) * urgency);
        }

        return new PointF(0, 0);
    }

    private static void ApplySteering(Car car, PointF steering, float desiredSpeed, float dt, float aggressionBoost, bool isRepulsing)
    {
        if (isRepulsing)
        {
            car.Update(dt, false, true, false, false, false);
            return;
        }

        float speed = car.Speed;
        float maxSteer = car.TurnRate * 2.5f;
        float steerX = Math.Clamp(steering.X, -maxSteer, maxSteer);

        bool left = false, right = false, up = true, down = false, drift = false;

        if (steerX > 0.05f)
            right = true;
        else if (steerX < -0.05f)
            left = true;

        float speedError = desiredSpeed - speed;
        if (speedError > 2f)
            down = false;
        else if (speedError < -2f)
            down = true;
        else if (speed > desiredSpeed * 1.1f)
            down = true;

        float driftThreshold = 0.6f - (aggressionBoost - 1f) * 0.1f;
        if (MathF.Abs(steerX) > driftThreshold && speed > car.MaxSpeed * 0.25f)
        {
            drift = true;
        }

        car.Update(dt, up, down, left, right, drift);
    }

    private static PointF ComputeWallAvoidance(Car car, Track track)
    {
        var center = car.GetCenter();
        float rad = (car.Angle - 90f) * MathF.PI / 180f;

        float steerX = 0, steerY = 0;
        float urgency = 0;

        for (int i = 0; i < RayCount; i++)
        {
            float angleOffset = (i / (RayCount - 1) - 0.5f) * RaySpread;
            float rayRad = rad + angleOffset * MathF.PI / 180f;

            bool isForward = MathF.Abs(angleOffset) < 35f;
            float rayLen = isForward ? LongRayLength : ShortRayLength;

            float clearance = RaycastClearance(car, angleOffset, track, rayLen);

            float dangerZone = rayLen * 0.55f;
            if (clearance < dangerZone)
            {
                float danger = 1f - (clearance / dangerZone);
                danger = danger * danger * danger;

                PointF rayDir = new PointF(MathF.Cos(rayRad), MathF.Sin(rayRad));
                float avoidStrength = (dangerZone - clearance) * (isForward ? 2.2f : 1f);

                steerX -= rayDir.X * avoidStrength * danger;
                steerY -= rayDir.Y * avoidStrength * danger;
                urgency += danger * (isForward ? 2f : 1f);
            }
        }

        return new PointF(steerX, steerY);
    }

    private static PointF ComputeCarAvoidance(Car car, Track track)
    {
        var cars = GameStateInstance.Cars;
        var center = car.GetCenter();
        float rad = (car.Angle - 90f) * MathF.PI / 180f;
        PointF forward = new PointF(MathF.Cos(rad), MathF.Sin(rad));

        float steerX = 0, steerY = 0;

        foreach (var other in cars)
        {
            if (other == car || other.IsDead) continue;

            var otherPos = other.GetCenter();
            float dx = otherPos.X - center.X;
            float dy = otherPos.Y - center.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist > 80f) continue;
            if (dist < 1f) dist = 1f;

            PointF toOther = new PointF(dx / dist, dy / dist);
            float dot = forward.X * toOther.X + forward.Y * toOther.Y;

            bool isTarget = other == car.Target;
            float range = isTarget ? 80f : 55f;
            if (dist > range) continue;

            float behindPenalty = dot < -0.15f ? 0.3f : 1f;

            if (car.AIState == AIState.Raging && other == car.RageTarget)
            {
                behindPenalty = 1f;
            }
            else if (!isTarget)
            {
                behindPenalty *= 0.5f;
            }

            float danger = ((range - dist) / range) * behindPenalty;
            danger = danger * danger;

            steerX -= toOther.X * danger * 10f;
            steerY -= toOther.Y * danger * 10f;
        }

        return new PointF(steerX, steerY);
    }

    private static Car? SelectTacticalTarget(Car car, List<Car> allCars, Track track)
    {
        Car? bestTarget = null;
        float bestScore = float.MinValue;
        var aiPos = car.GetCenter();
        var aiVelDir = new PointF(car.VelocityX, car.VelocityY);
        float aiSpeed = MathF.Sqrt(aiVelDir.X * aiVelDir.X + aiVelDir.Y * aiVelDir.Y);

        foreach (var other in allCars)
        {
            if (other == car || other.IsDead) continue;

            var otherPos = other.GetCenter();
            float dist = Distance(aiPos, otherPos);
            if (dist > car.AIChaseRadius) continue;

            float score = 0;

            float myVelTowards = 0;
            if (aiSpeed > 0.5f)
            {
                myVelTowards = (aiVelDir.X / aiSpeed * (otherPos.X - aiPos.X) + aiVelDir.Y / aiSpeed * (otherPos.Y - aiPos.Y)) / dist;
            }

            if (myVelTowards > 0.2f)
                score += 60f;

            score -= dist * 0.15f;

            float healthBonus = (100f - other.Health) * 1.2f;
            if (other.Health < 35)
                healthBonus *= 2.5f;
            if (other.Health < 20)
                healthBonus *= 2f;
            score += healthBonus;

            if (IsNearBarrier(other, track))
                score += 150f;

            if (other.HasShield)
                score -= 60f;

            float speedAdvantage = car.Speed - other.Speed;
            score += speedAdvantage * 15f;

            float targetVelMag = other.Speed;
            if (targetVelMag < 3f)
                score += 60f;

            if (other.IsPlayer)
                score += 80f;

            if (other.RageTimer > 0)
                score -= 50f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = other;
            }
        }

        return bestTarget;
    }

    private static PowerUp? FindBestPowerUp(Car car, List<PowerUp> powerUps, PowerUpType preferredType)
    {
        PowerUp? best = null;
        float bestScore = float.MinValue;

        foreach (var pu in powerUps)
        {
            if (pu.Type != preferredType) continue;

            float dist = Distance(car.GetCenter(), pu.Position);
            if (dist > 300f) continue;

            float score = (300f - dist);
            if (pu.Type == PowerUpType.Shield)
                score *= 2f;

            if (score > bestScore)
            {
                bestScore = score;
                best = pu;
            }
        }

        return best;
    }

    private static float RaycastClearance(Car car, float angleOffset, Track track, float maxDist)
    {
        var center = car.GetCenter();
        float baseAngle = (car.Angle - 90f) * MathF.PI / 180f;
        float rayAngle = baseAngle + angleOffset * MathF.PI / 180f;

        float clearDist = 0;
        float step = 6f;

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

    private static bool IsNearBarrier(Car car, Track track)
    {
        var center = car.GetCenter();
        int checkRadius = 20;
        for (int dx = -checkRadius; dx <= checkRadius; dx += 8)
        {
            for (int dy = -checkRadius; dy <= checkRadius; dy += 8)
            {
                if (track.IsBarrierAt(new PointF(center.X + dx, center.Y + dy)))
                    return true;
            }
        }
        return false;
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static class GameStateInstance
    {
        public static List<Car> Cars { get; set; } = new();
    }

    public static void SetGlobalCarList(List<Car> cars)
    {
        GameStateInstance.Cars = cars;
    }
}

public enum AIState
{
    Chasing,
    Raging,
    Repulsing,
    SeekingPowerUp,
    Wandering
}
