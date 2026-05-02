using System;
using System.Drawing;
using System.Windows.Forms;

namespace TopDownRacing;

public class TitleScreen : Form
{
    private Button _playButton;
    private Label _titleLabel;

    public TitleScreen()
    {
        Text = "Top‑Down Racing";
        ClientSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        DoubleBuffered = true;

        // Title label – docked at top, centered horizontally
        _titleLabel = new Label
        {
            Text = "Top‑Down Racing",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 32, FontStyle.Bold),
            AutoSize = false,
            Height = 80,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        Controls.Add(_titleLabel);

        // Play button – centered in the remaining client area
        _playButton = new Button
        {
            Text = "Play",
            Font = new Font("Segoe UI", 18),
            AutoSize = true,
            BackColor = Color.DarkGray,
            ForeColor = Color.White,
        };
        _playButton.Click += PlayClicked;
        Controls.Add(_playButton);

        // Position button after adding it (so its size is known)
        // Center horizontally and place a bit below the title
        this.Load += (s, e) =>
        {
            var btnSize = _playButton.PreferredSize;
            _playButton.Location = new Point((ClientSize.Width - btnSize.Width) / 2, (ClientSize.Height / 2) - btnSize.Height / 2);
        };
    }

    private void PlayClicked(object? sender, EventArgs e)
    {
        var game = new GameForm();
        game.FormClosed += (s, ev) => Application.Exit();
        game.Show();
        this.Hide();
    }
}
