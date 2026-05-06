using System;
using System.Collections.Generic;
using System.Drawing;

namespace bumpercars;

public class GameState
{
    public Track Track { get; private set; }
    public List<Car> Cars { get; private set; } = new();

    public GameState()
    {
        Track = new Track();
        InitialiseCars();
    }

    private void InitialiseCars()
    {
        var playArea = Track.GetPlayArea();
        float startX = playArea.X + playArea.Width / 2 - 20;
        float startY = playArea.Y + playArea.Height / 2 - 10;

        var playerCar = new Car(new PointF(startX, startY), true, Color.FromArgb(204, 0, 0));
        playerCar.Angle = 0f;
        Cars.Add(playerCar);
    }

    public void Update(float dt, bool up, bool down, bool left, bool right, bool drift)
    {
        foreach (var car in Cars)
        {
            bool u = car.IsPlayer && up;
            bool d = car.IsPlayer && down;
            bool l = car.IsPlayer && left;
            bool r = car.IsPlayer && right;
            bool dr = car.IsPlayer && drift;
            car.Update(dt, u, d, l, r, dr);

            HandleCollision(car);
        }
    }

    private void HandleCollision(Car car)
    {
        int w = Track.Size.Width;
        int h = Track.Size.Height;
        int wt = Track.WallThickness;

        float cx = car.Position.X;
        float cy = car.Position.Y;

        bool hit = false;
        float newX = cx;
        float newY = cy;

        if (cx < wt)
        {
            newX = wt + 2;
            hit = true;
        }
        if (cx + car.Width > w - wt)
        {
            newX = w - wt - car.Width - 2;
            hit = true;
        }
        if (cy < wt)
        {
            newY = wt + 2;
            hit = true;
        }
        if (cy + car.Height > h - wt)
        {
            newY = h - wt - car.Height - 2;
            hit = true;
        }

        if (!hit) return;

        car.Position = new PointF(newX, newY);
        car.VelocityX = 0;
        car.VelocityY = 0;
    }
}