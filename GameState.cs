using System;
using System.Collections.Generic;
using System.Drawing;

namespace bumpercars;

public class GameState
{
    public Track Track { get; private set; }
    public List<Car> Cars { get; private set; } = new();
    public List<PowerUp> PowerUps { get; private set; } = new();
    public List<RepulsorEffect> RepulsorEffects { get; private set; } = new();
    public bool IsGameOver;
    public bool IsPaused;
    public int AliveCount;
    public float GameTime;
    public Car? Winner;
    public float HighScore { get; private set; }

    public const float RepulsorChargeTime = 5f;
    public const float RepulsorRadius = 120f;
    public const float RepulsorForce = 25f;
    public const float RepulsorStunDuration = 1.5f;

    private readonly Random _rng = new();

    public GameState(TrackLayout layout = TrackLayout.Arena)
    {
        Track = new Track(800, 600, layout);
        HighScore = HighScoreManager.LoadHighScore();
        InitialiseCars();
    }

    private void InitialiseCars()
    {
        var playArea = Track.GetPlayArea();

        var playerCar = new Car(new PointF(playArea.X + playArea.Width * 0.3f, playArea.Y + playArea.Height / 2), true, Color.FromArgb(204, 0, 0));
        playerCar.Angle = 0f;
        Cars.Add(playerCar);

        var bot1 = new Car(new PointF(playArea.X + playArea.Width * 0.7f, playArea.Y + playArea.Height * 0.3f), false, Color.FromArgb(0, 100, 255));
        bot1.IsAI = true;
        bot1.Angle = 180f;
        Cars.Add(bot1);

        var bot2 = new Car(new PointF(playArea.X + playArea.Width * 0.7f, playArea.Y + playArea.Height * 0.7f), false, Color.FromArgb(0, 200, 100));
        bot2.IsAI = true;
        bot2.Angle = 180f;
        Cars.Add(bot2);

        var bot3 = new Car(new PointF(playArea.X + playArea.Width * 0.5f, playArea.Y + playArea.Height * 0.2f), false, Color.FromArgb(255, 200, 0));
        bot3.IsAI = true;
        bot3.Angle = 90f;
        Cars.Add(bot3);

        var bot4 = new Car(new PointF(playArea.X + playArea.Width * 0.5f, playArea.Y + playArea.Height * 0.8f), false, Color.FromArgb(200, 0, 200));
        bot4.IsAI = true;
        bot4.Angle = 270f;
        Cars.Add(bot4);

        AliveCount = Cars.Count;
    }

    public void Update(float dt, bool up, bool down, bool left, bool right, bool drift, bool useRepulsor)
    {
        if (IsPaused || IsGameOver) return;

        GameTime += dt;

        foreach (var car in Cars)
        {
            if (car.IsDead) continue;

            if (car.RepulsorCharge < RepulsorChargeTime)
                car.RepulsorCharge = Math.Min(RepulsorChargeTime, car.RepulsorCharge + dt);

            if (car.IsStunned) car.StunTimer -= dt;

            if (car.RepulsorCooldownTimer > 0)
                car.RepulsorCooldownTimer -= dt;
        }

        var player = Cars.Find(c => c.IsPlayer);
        if (player != null && !player.IsDead && !player.IsStunned)
        {
            if (useRepulsor && player.RepulsorCharge >= RepulsorChargeTime)
            {
                TriggerRepulsor(player);
            }
        }

        foreach (var car in Cars)
        {
            if (car.IsAI && !car.IsDead)
            {
                if (car.RageTimer > 0) car.RageTimer -= dt;
                AIController.SetGlobalCarList(Cars);
                AIController.UpdateAI(car, Cars, Track, dt, PowerUps, this);
            }
        }

        foreach (var car in Cars)
        {
            if (car.LastHitByCarTime >= 0)
                car.LastHitByCarTime += dt;
        }

        foreach (var car in Cars)
        {
            if (car.IsPlayer && !car.IsDead)
            {
                car.Update(dt, up, down, left, right, drift);
            }
            else if (car.IsDead)
            {
                car.Update(dt, false, false, false, false, false);
            }
        }

        HandleCarCollisions();
        HandleBarrierCollisions();
        UpdatePowerUps(dt);

        for (int i = RepulsorEffects.Count - 1; i >= 0; i--)
        {
            RepulsorEffects[i].Life -= dt;
            if (RepulsorEffects[i].Life <= 0)
                RepulsorEffects.RemoveAt(i);
        }

        CheckGameOver();
    }

    public void TriggerRepulsor(Car source)
    {
        source.RepulsorCharge = 0f;
        source.RepulsorCooldownTimer = 0.5f;

        RepulsorEffects.Add(new RepulsorEffect(source.GetCenter(), RepulsorRadius));

        foreach (var car in Cars)
        {
            if (car == source || car.IsDead) continue;

            float dx = car.GetCenter().X - source.GetCenter().X;
            float dy = car.GetCenter().Y - source.GetCenter().Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < RepulsorRadius)
            {
                float force = RepulsorForce * (1f - dist / RepulsorRadius);
                float nx = dx / (dist + 0.1f);
                float ny = dy / (dist + 0.1f);

                car.VelocityX += nx * force;
                car.VelocityY += ny * force;

                if (!car.HasShield)
                {
                    car.IsStunned = true;
                    car.StunTimer = RepulsorStunDuration;
                }
            }
        }
    }

    private void HandleCarCollisions()
    {
        for (int i = 0; i < Cars.Count; i++)
        {
            for (int j = i + 1; j < Cars.Count; j++)
            {
                var a = Cars[i];
                var b = Cars[j];

                if (a.IsDead || b.IsDead) continue;

                if (CheckCarCollision(a, b))
                {
                    ResolveCarCollision(a, b);
                }
            }
        }
    }

    private bool CheckCarCollision(Car a, Car b)
    {
        var aCenter = a.GetCenter();
        var bCenter = b.GetCenter();
        float dx = bCenter.X - aCenter.X;
        float dy = bCenter.Y - aCenter.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float minDist = (a.Width + b.Width) / 2.5f;
        return dist < minDist;
    }

    private void ResolveCarCollision(Car a, Car b)
    {
        var aCenter = a.GetCenter();
        var bCenter = b.GetCenter();

        float dx = bCenter.X - aCenter.X;
        float dy = bCenter.Y - aCenter.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.1f) dist = 0.1f;

        float nx = dx / dist;
        float ny = dy / dist;

        float overlap = (a.Width + b.Width) / 2.5f - dist;
        if (overlap > 0)
        {
            a.Position.X -= nx * overlap * 0.5f;
            a.Position.Y -= ny * overlap * 0.5f;
            b.Position.X += nx * overlap * 0.5f;
            b.Position.Y += ny * overlap * 0.5f;
        }

        float relVelX = a.VelocityX - b.VelocityX;
        float relVelY = a.VelocityY - b.VelocityY;
        float relVelDot = relVelX * nx + relVelY * ny;

        if (relVelDot < 0)
        {
            float aSpeedPercent = (a.Speed / a.MaxSpeed) * 100f;
            float bSpeedPercent = (b.Speed / b.MaxSpeed) * 100f;

            bool aHitsB = relVelDot < 0;

            if (aHitsB)
            {
                float damage = 1f * (aSpeedPercent / 10f);
                b.TakeDamage(damage, a);
            }
            else
            {
                float damage = 1f * (bSpeedPercent / 10f);
                a.TakeDamage(damage, b);
            }

            float pushStrengthA = bSpeedPercent / 100f * 15f;
            float pushStrengthB = aSpeedPercent / 100f * 15f;

            a.VelocityX -= nx * pushStrengthB;
            a.VelocityY -= ny * pushStrengthB;
            b.VelocityX += nx * pushStrengthA;
            b.VelocityY += ny * pushStrengthA;

            float bounce = 0.8f;
            float tempVx = a.VelocityX;
            float tempVy = a.VelocityY;
            a.VelocityX = b.VelocityX * bounce;
            a.VelocityY = b.VelocityY * bounce;
            b.VelocityX = tempVx * bounce;
            b.VelocityY = tempVy * bounce;
        }
    }

    private void HandleBarrierCollisions()
    {
        foreach (var car in Cars)
        {
            if (car.IsDead) continue;

            bool touchingBarrier = IsTouchingBarrier(car);

            if (touchingBarrier && !car.IsTouchingBarrier)
            {
                car.IsTouchingBarrier = true;
                car.BarrierTouchTime = 0;
            }
            else if (!touchingBarrier)
            {
                car.IsTouchingBarrier = false;
                car.BarrierTouchTime = 0;
            }

            if (car.IsTouchingBarrier)
            {
                car.BarrierTouchTime += 1f / 60f;
                float damagePerSecond = 10f;
                float damage = damagePerSecond * (1f / 60f);
                car.TakeDamage(damage, null);
            }

            KeepCarInBounds(car);
        }
    }

    private bool IsTouchingBarrier(Car car)
    {
        var center = car.GetCenter();
        float rad = (car.Angle - 90f) * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);
        float hw = car.Width / 2f + 4f;
        float hh = car.Height / 2f + 2f;

        var checkPoints = new PointF[]
        {
            new PointF(center.X + cos * hw, center.Y + sin * hw),
            new PointF(center.X - cos * hw, center.Y - sin * hw),
            new PointF(center.X + sin * hh, center.Y - cos * hh),
            new PointF(center.X - sin * hh, center.Y + cos * hh),
            new PointF(center.X + cos * hw * 0.7f + sin * hh * 0.7f, center.Y + sin * hw * 0.7f - cos * hh * 0.7f),
            new PointF(center.X + cos * hw * 0.7f - sin * hh * 0.7f, center.Y + sin * hw * 0.7f + cos * hh * 0.7f),
            new PointF(center.X - cos * hw * 0.7f + sin * hh * 0.7f, center.Y - sin * hw * 0.7f - cos * hh * 0.7f),
            new PointF(center.X - cos * hw * 0.7f - sin * hh * 0.7f, center.Y - sin * hw * 0.7f + cos * hh * 0.7f),
            new PointF(center.X + cos * hw * 0.5f, center.Y + sin * hw * 0.5f),
            new PointF(center.X - cos * hw * 0.5f, center.Y - sin * hw * 0.5f),
            new PointF(center.X + sin * hh * 0.5f, center.Y - cos * hh * 0.5f),
            new PointF(center.X - sin * hh * 0.5f, center.Y + cos * hh * 0.5f),
        };

        foreach (var pt in checkPoints)
        {
            if (Track.IsBarrierAt(pt)) return true;
        }
        return false;
    }

    private PointF[] GetCarCorners(Car car)
    {
        var center = car.GetCenter();
        float rad = (car.Angle - 90f) * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);
        float hw = car.Width / 2f;
        float hh = car.Height / 2f;

        return new PointF[]
        {
            new PointF(center.X + cos * hw - sin * hh, center.Y + sin * hw + cos * hh),
            new PointF(center.X + cos * hw + sin * hh, center.Y + sin * hw - cos * hh),
            new PointF(center.X - cos * hw - sin * hh, center.Y - sin * hw + cos * hh),
            new PointF(center.X - cos * hw + sin * hh, center.Y - sin * hw - cos * hh),
        };
    }

    private void KeepCarInBounds(Car car)
    {
        int w = Track.Size.Width;
        int h = Track.Size.Height;
        int margin = Track.BoundsClampMargin;

        float cx = car.Position.X;
        float cy = car.Position.Y;

        bool hit = false;
        float newX = cx;
        float newY = cy;

        if (cx < margin)
        {
            newX = margin + 2;
            hit = true;
        }
        if (cx + car.Width > w - margin)
        {
            newX = w - margin - car.Width - 2;
            hit = true;
        }
        if (cy < margin)
        {
            newY = margin + 2;
            hit = true;
        }
        if (cy + car.Height > h - margin)
        {
            newY = h - margin - car.Height - 2;
            hit = true;
        }

        if (hit)
        {
            car.Position = new PointF(newX, newY);
            car.VelocityX *= 0.5f;
            car.VelocityY *= 0.5f;
        }
    }

    private void UpdatePowerUps(float dt)
    {
        if (_rng.NextDouble() < 0.001 && PowerUps.Count < 3)
        {
            SpawnPowerUp();
        }

        foreach (var car in Cars)
        {
            if (car.IsDead) continue;

            for (int i = PowerUps.Count - 1; i >= 0; i--)
            {
                var pu = PowerUps[i];
                var carCenter = car.GetCenter();
                float dx = carCenter.X - pu.Position.X;
                float dy = carCenter.Y - pu.Position.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < 30)
                {
                    ApplyPowerUp(car, pu);
                    PowerUps.RemoveAt(i);
                }
            }
        }
    }

    private void SpawnPowerUp()
    {
        var playArea = Track.GetPlayArea();
        for (int attempt = 0; attempt < 28; attempt++)
        {
            float x = playArea.X + (float)_rng.NextDouble() * playArea.Width;
            float y = playArea.Y + (float)_rng.NextDouble() * playArea.Height;
            var pt = new PointF(x, y);
            if (Track.IsBarrierAt(pt))
            {
                continue;
            }

            var type = (PowerUpType)(_rng.Next(2));
            PowerUps.Add(new PowerUp(pt, type));
            break;
        }
    }

    private void ApplyPowerUp(Car car, PowerUp pu)
    {
        switch (pu.Type)
        {
            case PowerUpType.SpeedBoost:
                car.MaxSpeed *= 1.5f;
                car.Acceleration *= 1.5f;
                break;
            case PowerUpType.Shield:
                car.HasShield = true;
                break;
        }
    }

    private void CheckGameOver()
    {
        int alive = 0;
        Car? lastAlive = null;

        foreach (var car in Cars)
        {
            if (!car.IsDead)
            {
                alive++;
                lastAlive = car;
            }
        }

        AliveCount = alive;

        if (alive <= 1 && Cars.Count > 1)
        {
            IsGameOver = true;
            Winner = lastAlive;

            if (GameTime > HighScore)
            {
                HighScore = GameTime;
                HighScoreManager.SaveHighScore(GameTime);
            }
        }

        var player = Cars.Find(c => c.IsPlayer);
        if (player != null && player.IsDead)
        {
            if (alive <= 1)
            {
                IsGameOver = true;
                if (GameTime > HighScore)
                {
                    HighScore = GameTime;
                    HighScoreManager.SaveHighScore(GameTime);
                }
            }
        }
    }

    public void Reset()
    {
        Cars.Clear();
        PowerUps.Clear();
        RepulsorEffects.Clear();
        IsGameOver = false;
        IsPaused = false;
        GameTime = 0;
        Winner = null;
        InitialiseCars();
    }
}

public enum PowerUpType
{
    SpeedBoost,
    Shield
}

public class PowerUp
{
    public PointF Position;
    public PowerUpType Type;
    public float Rotation;

    public PowerUp(PointF pos, PowerUpType type)
    {
        Position = pos;
        Type = type;
    }
}

public class RepulsorEffect
{
    public PointF Origin;
    public float Radius;
    public float Life;
    public float MaxLife = 0.5f;

    public RepulsorEffect(PointF origin, float radius)
    {
        Origin = origin;
        Radius = radius;
        Life = MaxLife;
    }
}
