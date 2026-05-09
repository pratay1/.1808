using System;
using System.Drawing;
using System.Windows.Forms;

namespace bumpercars;

public class TitleScreen : Form
{
    public TitleScreen()
    {
        Text = "Bumper Cars";
        ClientSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(25, 25, 25);
        DoubleBuffered = true;

        int cx = ClientSize.Width / 2;
        const TextFormatFlags measureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

        static Size Measure(Font font, string text) =>
            TextRenderer.MeasureText(text, font, Size.Empty, measureFlags);

        int y = 48;

        var titleFont = new Font("Segoe UI", 44f, FontStyle.Bold);
        var titleText = "Bumper Cars";
        var title = new Label
        {
            Text = titleText,
            Font = titleFont,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(cx - Measure(titleFont, titleText).Width / 2, y)
        };
        Controls.Add(title);
        y += Measure(titleFont, titleText).Height + 14;

        var tagFont = new Font("Segoe UI", 15f, FontStyle.Regular);
        var tagText = "Smash into your friends!";
        var tagline = new Label
        {
            Text = tagText,
            Font = tagFont,
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(cx - Measure(tagFont, tagText).Width / 2, y)
        };
        Controls.Add(tagline);
        y += Measure(tagFont, tagText).Height + 36;

        var trackHdrFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        var trackHdrText = "SELECT TRACK";
        var trackLabel = new Label
        {
            Text = trackHdrText,
            Font = trackHdrFont,
            ForeColor = Color.FromArgb(150, 150, 150),
            AutoSize = true,
            Location = new Point(cx - Measure(trackHdrFont, trackHdrText).Width / 2, y)
        };
        Controls.Add(trackLabel);
        y += Measure(trackHdrFont, trackHdrText).Height + 18;

        string[] tracks = { "ARENA", "OVAL", "FIGURE-8", "TRIANGLE" };
        TrackLayout[] layouts = { TrackLayout.Arena, TrackLayout.Oval, TrackLayout.Figure8, TrackLayout.Triangle };
        const int btnWidth = 160;
        const int btnHeight = 46;
        var btnFont = new Font("Segoe UI", 13f, FontStyle.Bold);

        for (int i = 0; i < tracks.Length; i++)
        {
            var trackBtn = new Button
            {
                Text = tracks[i],
                Font = btnFont,
                Size = new Size(btnWidth, btnHeight),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(cx - btnWidth / 2, y + i * (btnHeight + 10))
            };
            trackBtn.FlatAppearance.BorderSize = 0;
            trackBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            int layoutIndex = i;
            trackBtn.Click += (s, e) =>
            {
                var game = new GameForm(layouts[layoutIndex]);
                game.FormClosed += (s2, e2) => Application.Exit();
                game.Show();
                Hide();
            };
            Controls.Add(trackBtn);
        }

        var instrFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        var instrText = "WASD or Arrow Keys to drive  |  SPACE to drift  |  ESC to pause";
        var instr = new Label
        {
            Text = instrText,
            Font = instrFont,
            ForeColor = Color.FromArgb(90, 90, 90),
            AutoSize = true,
            Location = new Point(cx - Measure(instrFont, instrText).Width / 2, ClientSize.Height - 92)
        };
        Controls.Add(instr);

        float hs = HighScoreManager.LoadHighScore();
        if (hs > 0)
        {
            var hsFont = new Font("Segoe UI", 13f, FontStyle.Regular);
            var hsText = $"Best Time: {hs:F1}s";
            var hsLabel = new Label
            {
                Text = hsText,
                Font = hsFont,
                ForeColor = Color.FromArgb(190, 190, 110),
                AutoSize = true,
                Location = new Point(cx - Measure(hsFont, hsText).Width / 2, ClientSize.Height - 58)
            };
            Controls.Add(hsLabel);
        }
    }
}
