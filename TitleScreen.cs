using System;
using System.Drawing;
using System.Windows.Forms;

namespace bumpercars;

public class TitleScreen : Form
{
    public TitleScreen()
    {
        Text = "bumpercars";
        ClientSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(25, 25, 25);
        DoubleBuffered = true;

        // Title - centered
        var title = new Label
        {
            Text = "BUMPERCARS",
            Font = new Font("Arial", 42, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(800 / 2 - 130, 200)
        };
        Controls.Add(title);

        // Tagline - centered
        var tagline = new Label
        {
            Text = "Smash into your friends!",
            Font = new Font("Arial", 14),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(800 / 2 - 85, 260)
        };
        Controls.Add(tagline);

        // Play button - centered
        var playBtn = new Button
        {
            Text = "PLAY",
            Font = new Font("Arial", 16, FontStyle.Bold),
            Size = new Size(160, 48),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Location = new Point(800 / 2 - 80, 340)
        };
        playBtn.FlatAppearance.BorderSize = 0;
        playBtn.MouseEnter += (s, e) => playBtn.BackColor = Color.FromArgb(80, 80, 80);
        playBtn.MouseLeave += (s, e) => playBtn.BackColor = Color.FromArgb(50, 50, 50);
        playBtn.Click += (s, e) =>
        {
            var game = new GameForm();
            game.FormClosed += (s2, e2) => Application.Exit();
            game.Show();
            Hide();
        };
        Controls.Add(playBtn);

        // Instructions - centered
        var instr = new Label
        {
            Text = "WASD or Arrow Keys to drive",
            Font = new Font("Arial", 11),
            ForeColor = Color.FromArgb(70, 70, 70),
            AutoSize = true,
            Location = new Point(800 / 2 - 85, 420)
        };
        Controls.Add(instr);
    }
}