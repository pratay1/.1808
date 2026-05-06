using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TopDownRacing;

public class GameState
{
    public Track Track { get; private set; }
    public List<Car> Cars { get; private set; } = new();
    public int LapsToWin { get; private set; } = 3;

    // Simple checkpoint system – a list of rectangles that define a lap loop.
    private readonly List<RectangleF> _checkpoints = new();
    private readonly Dictionary<Car, int> _carCheckpointIndex = new();
    private readonly Dictionary<Car, int> _carLapCount = new();

    public GameState()
    {
        // Initialise track (same size as form later)
        Track = new Track();
        InitialiseCars();
        InitialiseCheckpoints();
    }

    private void InitialiseCars()
    {
        // Player car – start near bottom‑left of the road
        var playerCar = new Car(new PointF(250, 300), true, Color.Red);
        Cars.Add(playerCar);
        _carCheckpointIndex[playerCar] = 0;
        _carLapCount[playerCar] = 0;


    }

    private void InitialiseCheckpoints()
    {
        // We'll place four checkpoints along the outer barrier, forming a rectangle.
        // They are thin rectangles that the car must cross in order.
        int w = Track.Size.Width;
        int h = Track.Size.Height;
        int margin = 110; // matches the road rectangle margin used in Track constructor
        int thickness = 6;
        // top checkpoint (horizontal)
        _checkpoints.Add(new RectangleF(margin, margin - thickness / 2, w - 2 * margin, thickness));
        // right checkpoint (vertical)
        _checkpoints.Add(new RectangleF(w - margin - thickness / 2, margin, thickness, h - 2 * margin));
        // bottom checkpoint (horizontal)
        _checkpoints.Add(new RectangleF(margin, h - margin - thickness / 2, w - 2 * margin, thickness));
        // left checkpoint (vertical)
        _checkpoints.Add(new RectangleF(margin - thickness / 2, margin, thickness, h - 2 * margin));
    }

    public void Update(float dt, bool up, bool down, bool left, bool right)
    {
        foreach (var car in Cars)
        {
            // Ensure dictionaries contain an entry for this car (prevents KeyNotFoundException)
            if (!_carCheckpointIndex.ContainsKey(car))
            {
                _carCheckpointIndex[car] = 0;
                _carLapCount[car] = 0;
            }
            // Save previous position for potential revert on collision
            var previousPos = car.Position;

            // Player inputs only affect the player car
            bool u = car.IsPlayer && up;
            bool d = car.IsPlayer && down;
            bool l = car.IsPlayer && left;
            bool r = car.IsPlayer && right;
            car.Update(dt, u, d, l, r);

            // Barrier collision – use precise corner‑based detection and sliding response
            if (Track.CollidesWithBarrier(car.GetCorners()))
            {
                // Compute current velocity vector
                float rad = car.Angle * MathF.PI / 180f;
                float vx = MathF.Cos(rad) * car.Speed;
                float vy = MathF.Sin(rad) * car.Speed;

                // Approximate surface normal as direction from track centre to car centre
                var centre = Track.GetCenter();
                float nx = car.Position.X + car.Size.Width / 2f - centre.X;
                float ny = car.Position.Y + car.Size.Height / 2f - centre.Y;
                float len = MathF.Sqrt(nx * nx + ny * ny);
                if (len == 0) len = 1; // avoid divide‑by‑zero
                nx /= len; ny /= len;

                // Reflect velocity: v' = v - 2 (v·n) n
                float dot = vx * nx + vy * ny;
                float rvx = vx - 2 * dot * nx;
                float rvy = vy - 2 * dot * ny;

                // Update speed and angle based on reflected vector
                car.Speed = MathF.Sqrt(rvx * rvx + rvy * rvy);
                car.Angle = MathF.Atan2(rvy, rvx) * 180f / MathF.PI;

                // Move the car slightly away from the barrier to avoid immediate re‑collision
                car.Position = new PointF(
                    car.Position.X + rvx * 0.1f,
                    car.Position.Y + rvy * 0.1f);
            }


            // Keep car inside the window – same logic as before but after barrier handling
            car.Position = new PointF(
                Math.Max(0, Math.Min(car.Position.X, Track.Size.Width - car.Size.Width)),
                Math.Max(0, Math.Min(car.Position.Y, Track.Size.Height - car.Size.Height)));

            UpdateLapProgress(car);
        }
    }

    private void UpdateLapProgress(Car car)
    {
        int idx = _carCheckpointIndex[car];
        var checkpoint = _checkpoints[idx];
        if (car.Bounds.IntersectsWith(checkpoint))
        {
            // Advance to next checkpoint
            idx = (idx + 1) % _checkpoints.Count;
            _carCheckpointIndex[car] = idx;
            // If we just passed the final checkpoint, a lap is completed
            if (idx == 0)
            {
                _carLapCount[car]++;
            }
        }
    }

    public int GetLapCount(Car car) => _carLapCount.TryGetValue(car, out var lap) ? lap : 0;
    public bool IsRaceFinished(out Car? winner)
    {
        foreach (var kvp in _carLapCount)
        {
            if (kvp.Value >= LapsToWin)
            {
                winner = kvp.Key;
                return true;
            }
        }
        winner = null;
        return false;
    }
}
