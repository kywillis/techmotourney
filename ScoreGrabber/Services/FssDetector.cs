using System.Drawing;
using System.Drawing.Imaging;

namespace TecmoScoreGrabber.Services;

/// <summary>Compares a live capture to a reference final-score screen (full-frame similarity).</summary>
public static class FssDetector
{
    public static double ComputeSimilarity(Bitmap reference, Bitmap captured)
    {
        using var refResized = ResizeTo(reference, captured.Width, captured.Height);
        return GrayscaleNormalizedSimilarity(refResized, captured);
    }

    public static Bitmap LoadReference(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("FSS reference image not found: " + path);
        return (Bitmap)Image.FromFile(path);
    }

    private static Bitmap ResizeTo(Bitmap src, int w, int h)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(src, 0, 0, w, h);
        return bmp;
    }

    /// <summary>Returns 0..1 where 1 is identical grayscale.</summary>
    private static double GrayscaleNormalizedSimilarity(Bitmap a, Bitmap b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
            return 0;

        long sum = 0;
        long count = (long)a.Width * a.Height;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                var pa = a.GetPixel(x, y);
                var pb = b.GetPixel(x, y);
                int ga = (pa.R * 30 + pa.G * 59 + pa.B * 11) / 100;
                int gb = (pb.R * 30 + pb.G * 59 + pb.B * 11) / 100;
                int d = Math.Abs(ga - gb);
                sum += 255 - d;
            }
        }

        return count == 0 ? 0 : sum / (double)(count * 255);
    }
}
