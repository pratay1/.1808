using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TopDownRacing;

public class GameForm : Form
{
    private Timer _timer;
    private GameState _gameState;
    private bool _up, _down, _left, _right;

    public GameForm()
    {
        Text = "Top‑Down Racing Prototype";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;

        // Initialise game state (track, cars, checkpoints)
        _gameState = new GameState();

        // Timer drives the game loop (~60 FPS)
        _timer = new Timer { Interval = 16 };
        _timer.Tick += OnTick;
        _timer.Start();

        // Hook keyboard events for player input
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
        // Update all cars – provide the current input flags only for the player car
        _gameState.Update(0.016f, _up, _down, _left, _right);

        // If someone has completed the required laps, stop the timer and show a message box
        if (_gameState.IsRaceFinished(out var winner))
        {
            _timer.Stop();
            string msg = winner?.IsPlayer == true ? "You win!" : "AI wins!";
            MessageBox.Show(msg, "Race Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        Invalidate(); // request repaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        // Draw track background
        g.DrawImage(_gameState.Track.Background, 0, 0);

        // Render each car (player and AI)
        foreach (var car in _gameState.Cars)
        {
            car.Render(g);
        }

        // Simple HUD – show player lap count and speed
        if (_gameState.Cars.FirstOrDefault(c => c.IsPlayer) is Car player)
        {
            int lap = _gameState.GetLapCount(player) + 1; // laps are 1‑based for display
            string hud = $"Lap: {lap}/{_gameState.LapsToWin}   Speed: {player.Speed:0.0}";
            using var hudFont = new Font("Segoe UI", 12);
            using var hudBrush = new SolidBrush(Color.White);
            g.DrawString(hud, hudFont, hudBrush, new PointF(10, 10));
        }
    }
}
