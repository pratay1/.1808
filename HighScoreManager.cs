using System;
using System.IO;

namespace bumpercars;

public static class HighScoreManager
{
    private static string SavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "bumpercars",
        "highscores.txt");

    public static float LoadHighScore()
    {
        try
        {
            var dir = Path.GetDirectoryName(SavePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(SavePath))
            {
                var content = File.ReadAllText(SavePath);
                if (float.TryParse(content, out float score))
                {
                    return score;
                }
            }
        }
        catch { }
        return 0f;
    }

    public static void SaveHighScore(float score)
    {
        try
        {
            var dir = Path.GetDirectoryName(SavePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            float current = LoadHighScore();
            if (score > current)
            {
                File.WriteAllText(SavePath, score.ToString());
            }
        }
        catch { }
    }
}