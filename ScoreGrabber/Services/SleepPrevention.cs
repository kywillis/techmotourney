using System.Runtime.InteropServices;

namespace TecmoScoreGrabber.Services;

/// <summary>Prevents system sleep while capture is running.</summary>
public sealed class SleepPrevention : IDisposable
{
    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private bool _active;

    public void Start()
    {
        if (_active)
            return;
        SetThreadExecutionState(EsContinuous | EsSystemRequired);
        _active = true;
    }

    public void Stop()
    {
        if (!_active)
            return;
        SetThreadExecutionState(EsContinuous);
        _active = false;
    }

    public void Dispose() => Stop();
}
