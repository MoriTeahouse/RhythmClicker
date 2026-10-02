// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
namespace ClickerGame;
public sealed class TextRenderer : IDisposable
{
    private sealed class Entry { public required Texture2D Texture; public long Frame; }
    private readonly GraphicsDevice device;
    private readonly Dictionary<string, Entry> cache = new();
    private long frame;
    public int TextureCount => cache.Count;
    public long CacheBytes { get; private set; }
    public TextRenderer(GraphicsDevice graphicsDevice) => device = graphicsDevice;
    public void BeginFrame() => frame++;
    // Called after SpriteBatch.End; textures still referenced by the current batch are never disposed mid-draw.
    public void EndFrame()
    {
        if (cache.Count <= 384 && CacheBytes <= 32 * 1024 * 1024) return;
        foreach (var pair in cache.OrderBy(p => p.Value.Frame).ToArray())
        {
            if (cache.Count <= 384 && CacheBytes <= 32 * 1024 * 1024) break;
            CacheBytes -= pair.Value.Texture.Width * (long)pair.Value.Texture.Height * 4;
            pair.Value.Texture.Dispose(); cache.Remove(pair.Key);
        }
    }
    public Texture2D GetTexture(string text, string fontName, int size, Microsoft.Xna.Framework.Color color)
    {
        string key = $"{fontName}|{size}|{color.PackedValue}|{text}";
        if (cache.TryGetValue(key, out var entry)) { entry.Frame = frame; return entry.Texture; }
        using var font = new Font(fontName, size, FontStyle.Bold, GraphicsUnit.Pixel);
        using var measureBitmap = new Bitmap(1, 1); using var measure = Graphics.FromImage(measureBitmap);
        var measured = measure.MeasureString(text, font);
        using var bitmap = new Bitmap(Math.Max(1, (int)Math.Ceiling(measured.Width) + 3), Math.Max(1, (int)Math.Ceiling(measured.Height) + 3));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent); graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var brush = new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        graphics.DrawString(text, font, brush, 0, 0);
        using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); stream.Position = 0;
        var texture = Texture2D.FromStream(device, stream); cache.Add(key, new() { Texture = texture, Frame = frame }); CacheBytes += texture.Width * (long)texture.Height * 4; return texture;
    }
    public void Precache(string text, string fontName, int size, Microsoft.Xna.Framework.Color color) => GetTexture(text, fontName, size, color);
    public void Dispose() { foreach (var entry in cache.Values) entry.Texture.Dispose(); cache.Clear(); CacheBytes = 0; }
}
