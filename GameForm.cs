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

    public GameForm(TrackLayout layout = TrackLayout.Arena)
    {
        Text = "Bumper Cars";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;

        _gameState = new GameState(layout);

        _timer = new Timer { Interval = 8 };
        _timer.Tick += OnTick;
        _timer.Start();
        _lastTick = Environment.TickCount;

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            _gameState.IsPaused = !_gameState.IsPaused;
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
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
            case Keys.R:
                if (_gameState.IsGameOver)
                {
                    _gameState.Reset();
                }
                break;
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

        // Render power-ups
        foreach (var pu in _gameState.PowerUps)
        {
            pu.Rotation += 2f;
            RenderPowerUp(g, pu);
        }

        // Render cars
        foreach (var car in _gameState.Cars)
        {
            car.Render(g);
            if (!car.IsDead)
            {
                RenderHealthBar(g, car);
                if (car.HasShield)
                {
                    RenderShield(g, car);
                }
            }
        }

        // Render HUD
        RenderHUD(g);

        // Render pause/game over
        if (_gameState.IsPaused)
        {
            RenderPauseScreen(g);
        }
        if (_gameState.IsGameOver)
        {
            RenderGameOverScreen(g);
        }
    }

    private void RenderPowerUp(Graphics g, PowerUp pu)
    {
        var center = pu.Position;
        var saved = g.Transform;
        g.TranslateTransform(center.X, center.Y);
        g.RotateTransform(pu.Rotation);

        if (pu.Type == PowerUpType.SpeedBoost)
        {
            var brush = new SolidBrush(Color.FromArgb(255, 255, 100));
            g.FillEllipse(brush, -12, -12, 24, 24);
            var pen = new Pen(Color.FromArgb(255, 200, 0), 2);
            g.DrawEllipse(pen, -12, -12, 24, 24);
        }
        else
        {
            var brush = new SolidBrush(Color.FromArgb(100, 200, 255));
            g.FillEllipse(brush, -12, -12, 24, 24);
            var pen = new Pen(Color.FromArgb(50, 150, 255), 2);
            g.DrawEllipse(pen, -12, -12, 24, 24);
        }

        g.Transform = saved;
    }

    private void RenderHealthBar(Graphics g, Car car)
    {
        var center = car.GetCenter();
        float barWidth = 40f;
        float barHeight = 6f;
        float x = center.X - barWidth / 2;
        float y = center.Y - car.Height / 2 - 12;

        // Background
        var bgBrush = new SolidBrush(Color.FromArgb(50, 0, 0));
        g.FillRectangle(bgBrush, x, y, barWidth, barHeight);

        // Health
        float healthPercent = car.Health / car.MaxHealth;
        var healthBrush = new SolidBrush(healthPercent > 0.5f ? Color.Lime : healthPercent > 0.25f ? Color.Orange : Color.Red);
        g.FillRectangle(healthBrush, x, y, barWidth * healthPercent, barHeight);

        // Border
        var pen = new Pen(Color.White, 1);
        g.DrawRectangle(pen, x, y, barWidth, barHeight);
    }

    private void RenderShield(Graphics g, Car car)
    {
        var center = car.GetCenter();
        var pen = new Pen(Color.FromArgb(150, 100, 255), 3);
        g.DrawEllipse(pen, center.X - car.Width / 2 - 5, center.Y - car.Height / 2 - 5, car.Width + 10, car.Height + 10);
    }

    private void RenderHUD(Graphics g)
    {
        if (_gameState.Cars.Find(c => c.IsPlayer) is Car player)
        {
            // Speed
            int speedPercent = (int)((player.Speed / player.MaxSpeed) * 100);
            string hud = $"Speed: {speedPercent}%";
            using var hudFont = new Font("Segoe UI", 14, FontStyle.Bold);
            using var hudBrush = new SolidBrush(Color.White);
            g.DrawString(hud, hudFont, hudBrush, new PointF(15, 15));

            // Health
            string healthHud = $"Health: {(int)player.Health}%";
            using var healthBrush = new SolidBrush(player.Health > 50 ? Color.Lime : player.Health > 25 ? Color.Orange : Color.Red);
            g.DrawString(healthHud, hudFont, healthBrush, new PointF(15, 35));

            // Alive count
            string aliveHud = $"Alive: {_gameState.AliveCount}/{_gameState.Cars.Count}";
            g.DrawString(aliveHud, hudFont, hudBrush, new PointF(15, 55));

            // Timer
            string timeHud = $"Time: {_gameState.GameTime:F1}s";
            g.DrawString(timeHud, hudFont, hudBrush, new PointF(15, 75));

            // High score
            string hsHud = $"Best: {_gameState.HighScore:F1}s";
            g.DrawString(hsHud, hudFont, hudBrush, new PointF(15, 95));

            // Controls hint
            using var hintFont = new Font("Segoe UI", 10);
            using var hintBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
            g.DrawString("ESC: Pause", hintFont, hintBrush, new PointF(15, ClientSize.Height - 28));
        }
    }

    private void RenderPauseScreen(Graphics g)
    {
        var overlay = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        g.FillRectangle(overlay, 0, 0, ClientSize.Width, ClientSize.Height);

        using var titleFont = new Font("Segoe UI", 36, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Color.White);
        var titleSize = g.MeasureString("PAUSED", titleFont);
        g.DrawString("PAUSED", titleFont, titleBrush, (ClientSize.Width - titleSize.Width) / 2, ClientSize.Height / 2 - 30);

        using var subFont = new Font("Segoe UI", 16);
        using var subBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
        var subSize = g.MeasureString("Press ESC to continue", subFont);
        g.DrawString("Press ESC to continue", subFont, subBrush, (ClientSize.Width - subSize.Width) / 2, ClientSize.Height / 2 + 20);
    }

    private void RenderGameOverScreen(Graphics g)
    {
        var overlay = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        g.FillRectangle(overlay, 0, 0, ClientSize.Width, ClientSize.Height);

        using var titleFont = new Font("Segoe UI", 36, FontStyle.Bold);
        var winner = _gameState.Winner;
        string resultText;
        Color resultColor;

        if (winner != null)
        {
            if (winner.IsPlayer)
            {
                resultText = "YOU WIN!";
                resultColor = Color.Lime;
            }
            else
            {
                resultText = "GAME OVER";
                resultColor = Color.Red;
            }
        }
        else
        {
            resultText = "GAME OVER";
            resultColor = Color.Red;
        }

        using var titleBrush = new SolidBrush(resultColor);
        var titleSize = g.MeasureString(resultText, titleFont);
        g.DrawString(resultText, titleFont, titleBrush, (ClientSize.Width - titleSize.Width) / 2, ClientSize.Height / 2 - 50);

        using var subFont = new Font("Segoe UI", 16);
        using var subBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
        string timeText = $"Survived: {_gameState.GameTime:F1}s";
        var timeSize = g.MeasureString(timeText, subFont);
        g.DrawString(timeText, subFont, subBrush, (ClientSize.Width - timeSize.Width) / 2, ClientSize.Height / 2 + 10);

        string restartText = "Press R to restart";
        var restartSize = g.MeasureString(restartText, subFont);
        g.DrawString(restartText, subFont, subBrush, (ClientSize.Width - restartSize.Width) / 2, ClientSize.Height / 2 + 50);
    }
}