using System.Drawing;
using System.Drawing.Imaging;

namespace TecmoScoreGrabber.Services;

/// <summary>
/// Prepares monitor captures for vision (LLM): full frame, grayscale only (no crop).
/// </summary>
public static class VisionInputNormalizer
{
    /// <summary>Returns a new bitmap (caller must dispose): grayscale clone of the full capture.</summary>
    public static Bitmap NormalizeForLlm(Bitmap screen)
    {
        using var clone = (Bitmap)screen.Clone();
        return ToGrayscale(clone);
    }

    private static Bitmap ToGrayscale(Bitmap src)
    {
        var b = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(b);
        var cm = new ColorMatrix(new[]
        {
            new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
            new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
            new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
            new[] { 0f, 0f, 0f, 1f, 0f },
            new[] { 0f, 0f, 0f, 0f, 1f }
        });
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(cm);
        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
        return b;
    }
}
