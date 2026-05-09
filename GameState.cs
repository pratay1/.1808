using System;
using System.Collections.Generic;
using System.Drawing;

namespace bumpercars;

public class GameState
{
    public Track Track { get; private set; }
    public List<Car> Cars { get; private set; } = new();
    public List<PowerUp> PowerUps { get; private set; } = new();
    public bool IsGameOver;
    public bool IsPaused;
    public int AliveCount;
    public float GameTime;
    public Car? Winner;
    public float HighScore { get; private set; }

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

        // Player car - red
        var playerCar = new Car(new PointF(playArea.X + playArea.Width * 0.3f, playArea.Y + playArea.Height / 2), true, Color.FromArgb(204, 0, 0));
        playerCar.Angle = 0f;
        Cars.Add(playerCar);

        // AI Bot 1 - blue
        var bot1 = new Car(new PointF(playArea.X + playArea.Width * 0.7f, playArea.Y + playArea.Height * 0.3f), false, Color.FromArgb(0, 100, 255));
        bot1.IsAI = true;
        bot1.Angle = 180f;
        Cars.Add(bot1);

        // AI Bot 2 - green
        var bot2 = new Car(new PointF(playArea.X + playArea.Width * 0.7f, playArea.Y + playArea.Height * 0.7f), false, Color.FromArgb(0, 200, 100));
        bot2.IsAI = true;
        bot2.Angle = 180f;
        Cars.Add(bot2);

        // AI Bot 3 - yellow
        var bot3 = new Car(new PointF(playArea.X + playArea.Width * 0.5f, playArea.Y + playArea.Height * 0.2f), false, Color.FromArgb(255, 200, 0));
        bot3.IsAI = true;
        bot3.Angle = 90f;
        Cars.Add(bot3);

        // AI Bot 4 - magenta
        var bot4 = new Car(new PointF(playArea.X + playArea.Width * 0.5f, playArea.Y + playArea.Height * 0.8f), false, Color.FromArgb(200, 0, 200));
        bot4.IsAI = true;
        bot4.Angle = 270f;
        Cars.Add(bot4);

        AliveCount = Cars.Count;
    }

    public void Update(float dt, bool up, bool down, bool left, bool right, bool drift)
    {
        if (IsPaused || IsGameOver) return;

        GameTime += dt;

        // Update AI
        foreach (var car in Cars)
        {
            if (car.IsAI && !car.IsDead)
            {
                AIController.UpdateAI(car, Cars, Track, dt);
            }
        }

        // Update player input
        foreach (var car in Cars)
        {
            if (car.IsPlayer && !car.IsDead)
            {
                car.Update(dt, up, down, left, right, drift);
            }
            else if (car.IsDead)
            {
                // Dead cars still get updated for physics but no input
                car.Update(dt, false, false, false, false, false);
            }
        }

        // Handle collisions
        HandleCarCollisions();
        HandleBarrierCollisions();

        // Update power-ups
        UpdatePowerUps(dt);

        // Check win condition
        CheckGameOver();
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

        // Normalize direction
        float nx = dx / dist;
        float ny = dy / dist;

        // Separate cars
        float overlap = (a.Width + b.Width) / 2.5f - dist;
        if (overlap > 0)
        {
            a.Position.X -= nx * overlap * 0.5f;
            a.Position.Y -= ny * overlap * 0.5f;
            b.Position.X += nx * overlap * 0.5f;
            b.Position.Y += ny * overlap * 0.5f;
        }

        // Calculate relative velocity
        float relVelX = a.VelocityX - b.VelocityX;
        float relVelY = a.VelocityY - b.VelocityY;
        float relVelDot = relVelX * nx + relVelY * ny;

        // Only process if cars are moving toward each other
        if (relVelDot < 0)
        {
            float aSpeedPercent = (a.Speed / a.MaxSpeed) * 100f;
            float bSpeedPercent = (b.Speed / b.MaxSpeed) * 100f;

            // Determine who hits whom based on relative velocity direction
            // If a is moving faster toward b, a hits b
            bool aHitsB = relVelDot < 0;

            if (aHitsB)
            {
                // B takes damage: 1 * (a.speed% / 10)
                float damage = 1f * (aSpeedPercent / 10f);
                b.TakeDamage(damage, a);
            }
            else
            {
                // A takes damage: 1 * (b.speed% / 10)
                float damage = 1f * (bSpeedPercent / 10f);
                a.TakeDamage(damage, b);
            }

            // Calculate pushback based on speed percentage
            float pushStrengthA = bSpeedPercent / 100f * 15f;
            float pushStrengthB = aSpeedPercent / 100f * 15f;

            // Apply pushback impulse
            a.VelocityX -= nx * pushStrengthB;
            a.VelocityY -= ny * pushStrengthB;
            b.VelocityX += nx * pushStrengthA;
            b.VelocityY += ny * pushStrengthA;

            // Bounce effect - swap some velocity
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

            // Barrier damage: 10 damage per second while touching
            if (car.IsTouchingBarrier)
            {
                car.BarrierTouchTime += 1f / 60f; // Approximate
                float damagePerSecond = 10f;
                float damage = damagePerSecond * (1f / 60f);
                car.TakeDamage(damage, null);
            }

            // Keep car in bounds
            KeepCarInBounds(car);
        }
    }

    private bool IsTouchingBarrier(Car car)
    {
        var corners = GetCarCorners(car);
        foreach (var pt in corners)
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
        // Spawn power-ups occasionally
        if (_rng.NextDouble() < 0.001 && PowerUps.Count < 3)
        {
            SpawnPowerUp();
        }

        // Check collision with power-ups
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

            // Save high score
            if (GameTime > HighScore)
            {
                HighScore = GameTime;
                HighScoreManager.SaveHighScore(GameTime);
            }
        }

        // Also check if player is dead
        var player = Cars.Find(c => c.IsPlayer);
        if (player != null && player.IsDead)
        {
            // Give a moment before game over
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