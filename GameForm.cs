using System;
using System.Drawing;
using System.Windows.Forms;

namespace bumpercars;

public class GameForm : Form
{
    private Timer _timer;
    private GameState _gameState;
    private bool _up, _down, _left, _right, _drift, _useRepulsor;
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
            case Keys.E:
                _useRepulsor = true; break;
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
            case Keys.E:
                _useRepulsor = false; break;
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

        _gameState.Update(dt, _up, _down, _left, _right, _drift, _useRepulsor);

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;

        g.DrawImage(_gameState.Track.Background, 0, 0);

        foreach (var fx in _gameState.RepulsorEffects)
        {
            RenderRepulsorEffect(g, fx);
        }

        foreach (var pu in _gameState.PowerUps)
        {
            pu.Rotation += 2f;
            RenderPowerUp(g, pu);
        }

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

        RenderHUD(g);
        RenderChargeBar(g);

        if (_gameState.IsPaused)
        {
            RenderPauseScreen(g);
        }
        if (_gameState.IsGameOver)
        {
            RenderGameOverScreen(g);
        }
    }

    private void RenderRepulsorEffect(Graphics g, RepulsorEffect fx)
    {
        float progress = 1f - (fx.Life / fx.MaxLife);
        float currentRadius = fx.Radius * progress;

        float pulse = MathF.Sin(DateTime.Now.Ticks / 500000f) * 0.15f + 1f;

        int alpha = (int)(200 * (fx.Life / fx.MaxLife));

        using var brush1 = new SolidBrush(Color.FromArgb(alpha / 2, 0, 150, 255));
        using var brush2 = new SolidBrush(Color.FromArgb(alpha, 100, 200, 255));
        using var pen1 = new Pen(Color.FromArgb(alpha, 150, 200, 255), 4);
        using var pen2 = new Pen(Color.FromArgb(alpha, 200, 230, 255), 2);

        g.FillEllipse(brush1,
            fx.Origin.X - currentRadius * pulse,
            fx.Origin.Y - currentRadius * pulse,
            currentRadius * 2 * pulse,
            currentRadius * 2 * pulse);

        g.DrawEllipse(pen1,
            fx.Origin.X - currentRadius,
            fx.Origin.Y - currentRadius,
            currentRadius * 2,
            currentRadius * 2);

        g.DrawEllipse(pen2,
            fx.Origin.X - currentRadius * 0.7f,
            fx.Origin.Y - currentRadius * 0.7f,
            currentRadius * 1.4f,
            currentRadius * 1.4f);

        int ringCount = 3;
        for (int i = 0; i < ringCount; i++)
        {
            float ringProgress = (progress + i * 0.15f) % 1f;
            float ringRadius = fx.Radius * ringProgress;
            int ringAlpha = (int)(alpha * (1f - ringProgress) * 0.5f);
            using var ringPen = new Pen(Color.FromArgb(ringAlpha, 100, 200, 255), 2);
            g.DrawEllipse(ringPen,
                fx.Origin.X - ringRadius,
                fx.Origin.Y - ringRadius,
                ringRadius * 2,
                ringRadius * 2);
        }
    }

    private void RenderChargeBar(Graphics g)
    {
        var player = _gameState.Cars.Find(c => c.IsPlayer);
        if (player == null || player.IsDead) return;

        float barWidth = 160f;
        float barHeight = 16f;
        float x = (ClientSize.Width - barWidth) / 2;
        float y = ClientSize.Height - 35f;

        var bgBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillRectangle(bgBrush, x - 2, y - 2, barWidth + 4, barHeight + 4);

        float fill = Math.Min(1f, player.RepulsorCharge / GameState.RepulsorChargeTime);
        Color fillColor;
        if (fill >= 1f)
        {
            float pulse = MathF.Sin(DateTime.Now.Ticks / 200000f) * 0.3f + 0.7f;
            fillColor = Color.FromArgb((int)(255 * pulse), 0, 255);
        }
        else
        {
            fillColor = Color.FromArgb(0, 150, 255);
        }

        using var fillBrush = new SolidBrush(fillColor);
        g.FillRectangle(fillBrush, x, y, barWidth * fill, barHeight);

        using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 2);
        g.DrawRectangle(borderPen, x, y, barWidth, barHeight);

        string label = fill >= 1f ? "[E] REPULSOR READY" : $"[E] Charging... {(int)(fill * 100)}%";
        using var labelFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var labelBrush = new SolidBrush(fill >= 1f ? Color.White : Color.FromArgb(180, 180, 180));
        var labelSize = g.MeasureString(label, labelFont);
        float labelX = (ClientSize.Width - labelSize.Width) / 2;
        float labelY = y - labelSize.Height - 3;
        g.DrawString(label, labelFont, labelBrush, labelX, labelY);
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

        var bgBrush = new SolidBrush(Color.FromArgb(50, 0, 0));
        g.FillRectangle(bgBrush, x, y, barWidth, barHeight);

        float healthPercent = car.Health / car.MaxHealth;
        var healthBrush = new SolidBrush(healthPercent > 0.5f ? Color.Lime : healthPercent > 0.25f ? Color.Orange : Color.Red);
        g.FillRectangle(healthBrush, x, y, barWidth * healthPercent, barHeight);

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
            using var hudFont = new Font("Segoe UI", 14, FontStyle.Bold);
            using var hudBrush = new SolidBrush(Color.White);

            g.DrawString($"Speed: {(int)((player.Speed / player.MaxSpeed) * 100)}%", hudFont, hudBrush, new PointF(15, 15));

            using var healthBrush = new SolidBrush(player.Health > 50 ? Color.Lime : player.Health > 25 ? Color.Orange : Color.Red);
            g.DrawString($"Health: {(int)player.Health}%", hudFont, healthBrush, new PointF(15, 35));

            g.DrawString($"Alive: {_gameState.AliveCount}/{_gameState.Cars.Count}", hudFont, hudBrush, new PointF(15, 55));
            g.DrawString($"Time: {_gameState.GameTime:F1}s", hudFont, hudBrush, new PointF(15, 75));
            g.DrawString($"Best: {_gameState.HighScore:F1}s", hudFont, hudBrush, new PointF(15, 95));

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
