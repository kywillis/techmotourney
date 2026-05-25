using System.Text;

namespace TecmoScoreGrabber.Services;

/// <summary>Append-only file log capped at approximately maxBytes.</summary>
public sealed class RollingFileLogger
{
    private readonly string _path;
    private readonly int _maxBytes;
    private readonly object _lock = new();

    public RollingFileLogger(string logDirectory, int maxBytes)
    {
        _maxBytes = Math.Max(64 * 1024, maxBytes);
        Directory.CreateDirectory(logDirectory);
        _path = Path.Combine(logDirectory, "score-grabber.log");
    }

    public void AppendLine(string line)
    {
        var text = $"{DateTime.Now:O} {line}{Environment.NewLine}";
        var bytes = Encoding.UTF8.GetBytes(text);
        lock (_lock)
        {
            File.AppendAllText(_path, text, Encoding.UTF8);
            TrimIfNeeded();
        }
    }

    private void TrimIfNeeded()
    {
        try
        {
            var fi = new FileInfo(_path);
            if (!fi.Exists || fi.Length <= _maxBytes)
                return;

            using var fs = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite);
            var len = fs.Length;
            var over = len - _maxBytes + _maxBytes / 10;
            if (over <= 0)
                return;
            fs.Seek(over, SeekOrigin.Begin);
            var remainder = new MemoryStream();
            fs.CopyTo(remainder);
            fs.SetLength(0);
            fs.Seek(0, SeekOrigin.Begin);
            remainder.Seek(0, SeekOrigin.Begin);
            remainder.CopyTo(fs);
        }
        catch
        {
            // best effort
        }
    }
}
