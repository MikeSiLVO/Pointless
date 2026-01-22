namespace Pointless;

public class MouseHider : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Settings _settings;
    private NativeMethods.POINT _lastMousePosition;
    private bool _isCursorHidden;
    private IntPtr _blankCursor;
    private DateTime _lastMouseMoveTime;
    private uint _lastInputTime;

    private static readonly uint[] CursorIds =
    [
        NativeMethods.OCR_NORMAL,
        NativeMethods.OCR_IBEAM,
        NativeMethods.OCR_WAIT,
        NativeMethods.OCR_CROSS,
        NativeMethods.OCR_UP,
        NativeMethods.OCR_SIZENWSE,
        NativeMethods.OCR_SIZENESW,
        NativeMethods.OCR_SIZEWE,
        NativeMethods.OCR_SIZENS,
        NativeMethods.OCR_SIZEALL,
        NativeMethods.OCR_NO,
        NativeMethods.OCR_HAND,
        NativeMethods.OCR_APPSTARTING
    ];

    public event EventHandler<bool>? CursorVisibilityChanged;

    public bool IsCursorHidden => _isCursorHidden;

    public MouseHider(Settings settings)
    {
        _settings = settings;
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 100
        };
        _timer.Tick += Timer_Tick;

        CreateBlankCursor();
        NativeMethods.GetCursorPos(out _lastMousePosition);
        _lastMouseMoveTime = DateTime.UtcNow;
        _lastInputTime = GetSystemInputTime();
    }

    private void CreateBlankCursor()
    {
        // Create a 32x32 blank cursor
        // AND mask: all 1s = transparent background
        // XOR mask: all 0s = no visible pixels
        var andMask = new byte[128];
        var xorMask = new byte[128];

        for (int i = 0; i < 128; i++)
        {
            andMask[i] = 0xFF;
            xorMask[i] = 0x00;
        }

        _blankCursor = NativeMethods.CreateCursor(IntPtr.Zero, 0, 0, 32, 32, andMask, xorMask);
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        ShowCursor();
    }

    public void Toggle()
    {
        if (_isCursorHidden)
        {
            ShowCursor();
        }
        else
        {
            HideCursor();
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.Enabled)
        {
            if (_isCursorHidden)
            {
                ShowCursor();
            }
            return;
        }

        NativeMethods.GetCursorPos(out var currentPos);
        bool mouseMoved = currentPos.X != _lastMousePosition.X ||
                         currentPos.Y != _lastMousePosition.Y;

        var currentInputTime = GetSystemInputTime();
        bool keyboardActivity = currentInputTime != _lastInputTime && !mouseMoved;

        if (mouseMoved)
        {
            _lastMousePosition = currentPos;
            _lastMouseMoveTime = DateTime.UtcNow;
            _lastInputTime = currentInputTime;
            if (_isCursorHidden)
            {
                ShowCursor();
            }
            return;
        }

        if (keyboardActivity)
        {
            _lastInputTime = currentInputTime;
            if (_isCursorHidden && !_settings.IgnoreKeyboardActivity)
            {
                ShowCursor();
            }
            return;
        }

        var idleTime = GetIdleTime();
        var threshold = TimeSpan.FromSeconds(_settings.IdleTimeoutSeconds);

        if (idleTime >= threshold && !_isCursorHidden)
        {
            HideCursor();
        }
    }

    private uint GetSystemInputTime()
    {
        var lastInput = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };
        NativeMethods.GetLastInputInfo(ref lastInput);
        return lastInput.dwTime;
    }

    private TimeSpan GetIdleTime()
    {
        if (_settings.IgnoreKeyboardActivity)
        {
            // Only track mouse movement
            return DateTime.UtcNow - _lastMouseMoveTime;
        }

        // Track all input (mouse + keyboard)
        var lastInput = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };

        if (NativeMethods.GetLastInputInfo(ref lastInput))
        {
            var idleTicks = (uint)Environment.TickCount - lastInput.dwTime;
            return TimeSpan.FromMilliseconds(idleTicks);
        }

        return TimeSpan.Zero;
    }

    private void HideCursor()
    {
        if (_isCursorHidden || _blankCursor == IntPtr.Zero) return;

        foreach (var cursorId in CursorIds)
        {
            // CopyIcon creates a copy that SetSystemCursor will take ownership of
            var cursorCopy = NativeMethods.CopyIcon(_blankCursor);
            if (cursorCopy != IntPtr.Zero)
            {
                NativeMethods.SetSystemCursor(cursorCopy, cursorId);
            }
        }

        _isCursorHidden = true;
        CursorVisibilityChanged?.Invoke(this, false);
    }

    private void ShowCursor()
    {
        if (!_isCursorHidden) return;

        // Restore default system cursors
        NativeMethods.SystemParametersInfoW(NativeMethods.SPI_SETCURSORS, 0, IntPtr.Zero, 0);

        _isCursorHidden = false;
        CursorVisibilityChanged?.Invoke(this, true);
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();

        if (_blankCursor != IntPtr.Zero)
        {
            NativeMethods.DestroyCursor(_blankCursor);
            _blankCursor = IntPtr.Zero;
        }
    }
}
