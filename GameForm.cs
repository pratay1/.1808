using System;
using System.Drawing;
using System.Windows.Forms;

namespace TopDownRacing;

public class GameForm : Form
{
    private const int CarWidth = 40;
    private const int CarHeight = 20;
    private const float Acceleration = 0.2f;
    private const float MaxSpeed = 8f;
    private const float TurnRate = 4f; // degrees per frame

    private Timer _timer;
    private Bitmap _trackBitmap;
    private Bitmap _carBitmap;

    private PointF _carPos = new(200, 200);
    private float _carAngle; // degrees
    private float _speed;

    public GameForm()
    {
        Text = "Top‑Down Racing Prototype";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;

        // simple track (green field) placeholder
        _trackBitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
        using (var g = Graphics.FromImage(_trackBitmap))
        {
            g.Clear(Color.DarkGreen);
        }

        // simple car rectangle sprite
        _carBitmap = new Bitmap(CarWidth, CarHeight);
        using (var g = Graphics.FromImage(_carBitmap))
        {
            g.Clear(Color.Transparent);
            g.FillRectangle(Brushes.Red, 0, 0, CarWidth, CarHeight);
        }

        _timer = new Timer { Interval = 16 }; // ~60 FPS
        _timer.Tick += OnTick;
        _timer.Start();

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
    }

    private bool _up, _down, _left, _right;

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.W:
            case Keys.Up:
                _up = true; break;
            case Keys.S:
            case Keys.Down:
                _down = true; break;
            case Keys.A:
            case Keys.Left:
                _left = true; break;
            case Keys.D:
            case Keys.Right:
                _right = true; break;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.W:
            case Keys.Up:
                _up = false; break;
            case Keys.S:
            case Keys.Down:
                _down = false; break;
            case Keys.A:
            case Keys.Left:
                _left = false; break;
            case Keys.D:
            case Keys.Right:
                _right = false; break;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // acceleration / braking
        if (_up) _speed = MathF.Min(_speed + Acceleration, MaxSpeed);
        else if (_down) _speed = MathF.Max(_speed - Acceleration, -MaxSpeed / 2);
        else _speed = _speed * 0.95f;

        // steering only when moving
        if (_speed != 0)
        {
            if (_left) _carAngle -= TurnRate * (_speed / MaxSpeed);
            if (_right) _carAngle += TurnRate * (_speed / MaxSpeed);
        }

        // move forward
        float rad = _carAngle * MathF.PI / 180f;
        _carPos.X += MathF.Cos(rad) * _speed;
        _carPos.Y += MathF.Sin(rad) * _speed;

        // keep inside bounds
        _carPos.X = Math.Max(0, Math.Min(_carPos.X, ClientSize.Width - CarWidth));
        _carPos.Y = Math.Max(0, Math.Min(_carPos.Y, ClientSize.Height - CarHeight));

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.DrawImage(_trackBitmap, 0, 0);

        // draw rotated car
        g.TranslateTransform(_carPos.X + CarWidth / 2, _carPos.Y + CarHeight / 2);
        g.RotateTransform(_carAngle);
        g.TranslateTransform(-CarWidth / 2, -CarHeight / 2);
        g.DrawImage(_carBitmap, 0, 0);
        g.ResetTransform();
    }
}
