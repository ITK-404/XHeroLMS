using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class IdenticonGenerator
{
    /// <summary>
    /// Generate a GitHub-like identicon (5x5 symmetric grid) from a string.
    /// </summary>
    /// <param name="input">Username/email/any stable string</param>
    /// <param name="size">Output image size in pixels (e.g., 256)</param>
    /// <param name="padding">Padding ratio (0..0.5). 0.08 ~ 8% looks nice.</param>
    /// <param name="background">Background color</param>
    /// <returns>Texture2D RGBA32</returns>
    public static Texture2D Generate(
        string input,
        int size = 256,
        float padding = 0.08f,
        Color? background = null
    )
    {
        if (string.IsNullOrEmpty(input)) input = " ";

        // 1) Hash input (MD5 like classic identicon)
        byte[] hash;
        using (var md5 = MD5.Create())
        {
            hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        // 2) Pick a color from first 3 bytes (you can tweak to match your taste)
        // GitHub's exact mapping isn't required; "hash -> color" is the key idea.
        Color fg = new Color(hash[0] / 255f, hash[1] / 255f, hash[2] / 255f, 1f);

        // Optional: slightly increase saturation/brightness so it looks nicer in UI
        fg = NicerColor(fg);

        Color bg = background ?? new Color(0.95f, 0.95f, 0.95f, 1f);

        // 3) Build a 5x5 boolean grid, symmetric left-right
        // We only decide 3 columns (0,1,2) then mirror to (3,4).
        bool[,] grid = new bool[5, 5];

        // Need 5 rows * 3 cols = 15 bits/bytes. We'll read bytes from hash.
        // Use hash bytes from index 3 onward (since 0..2 used for color).
        int idx = 3;
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                byte b = hash[idx % hash.Length];
                idx++;

                // Use LSB as on/off
                bool on = (b & 1) == 1;

                grid[x, y] = on;
                grid[4 - x, y] = on; // mirror
            }
        }

        // 4) Render to Texture2D
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point; // sharp pixel look

        // Fill background
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

        // Compute layout
        int padPx = Mathf.RoundToInt(size * Mathf.Clamp01(padding));
        int usable = size - padPx * 2;
        int cell = usable / 5;            // integer cell size
        int gridSize = cell * 5;
        int offsetX = (size - gridSize) / 2;
        int offsetY = (size - gridSize) / 2;

        // Paint cells
        for (int gy = 0; gy < 5; gy++)
        {
            for (int gx = 0; gx < 5; gx++)
            {
                if (!grid[gx, gy]) continue;

                int x0 = offsetX + gx * cell;
                int y0 = offsetY + gy * cell;

                FillRect(pixels, size, x0, y0, cell, cell, fg);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    /// <summary>Save Texture2D to PNG bytes.</summary>
    public static byte[] ToPng(Texture2D tex)
    {
        if (tex == null) throw new ArgumentNullException(nameof(tex));
        return tex.EncodeToPNG();
    }

    // ----------------- Helpers -----------------

    private static void FillRect(Color[] pixels, int width, int x, int y, int w, int h, Color c)
    {
        int xMax = Mathf.Min(x + w, width);
        int yMax = Mathf.Min(y + h, width); // width == height here

        for (int yy = y; yy < yMax; yy++)
        {
            int row = yy * width;
            for (int xx = x; xx < xMax; xx++)
            {
                pixels[row + xx] = c;
            }
        }
    }

    // Make the color a bit more "pleasant" for UI (optional).
    private static Color NicerColor(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);

        // clamp into a nicer range
        s = Mathf.Clamp(s, 0.45f, 0.85f);
        v = Mathf.Clamp(v, 0.55f, 0.90f);

        return Color.HSVToRGB(h, s, v);
    }
}
