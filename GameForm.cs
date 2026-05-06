using System;
using System.Drawing;
using System.Windows.Forms;

namespace bumpercars;

public class GameForm : Form
{
    private Timer _timer;
    private GameState _gameState;
    private bool _up, _down, _left, _right, _drift;
    private int _lastTick;

    public GameForm()
    {
        Text = "bumpercars";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;

        _gameState = new GameState();

        _timer = new Timer { Interval = 8 };
        _timer.Tick += OnTick;
        _timer.Start();
        _lastTick = Environment.TickCount;

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
    }

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
            case Keys.Space:
                _drift = true; break;
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
            case Keys.Space:
                _drift = false; break;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        int now = Environment.TickCount;
        int delta = now - _lastTick;
        if (delta < 1) delta = 1;
        if (delta > 33) delta = 33;
        _lastTick = now;
        float dt = delta / 1000f;

        _gameState.Update(dt, _up, _down, _left, _right, _drift);

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;

        g.DrawImage(_gameState.Track.Background, 0, 0);

        foreach (var car in _gameState.Cars)
        {
            car.Render(g);
        }

        if (_gameState.Cars.Find(c => c.IsPlayer) is Car player)
        {
            int speedPercent = (int)((player.Speed / player.MaxSpeed) * 100);
            string hud = $"Speed: {speedPercent}%";
            using var hudFont = new Font("Segoe UI", 14, FontStyle.Bold);
            using var hudBrush = new SolidBrush(Color.White);
            g.DrawString(hud, hudFont, hudBrush, new PointF(15, 15));
        }
    }
}