namespace Pointless;

public class HotkeyManager : IDisposable
{
    private const int HOTKEY_ID = 1;
    private readonly HotkeyWindow _window;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;

    public HotkeyManager()
    {
        _window = new HotkeyWindow();
        _window.HotkeyPressed += (s, e) => HotkeyPressed?.Invoke(this, EventArgs.Empty);
    }

    public bool Register(Settings settings)
    {
        Unregister();

        if (!settings.HotkeyEnabled)
            return true;

        uint modifiers = NativeMethods.MOD_NOREPEAT;
        if (settings.HotkeyCtrl) modifiers |= NativeMethods.MOD_CONTROL;
        if (settings.HotkeyAlt) modifiers |= NativeMethods.MOD_ALT;
        if (settings.HotkeyShift) modifiers |= NativeMethods.MOD_SHIFT;
        if (settings.HotkeyWin) modifiers |= NativeMethods.MOD_WIN;

        _isRegistered = NativeMethods.RegisterHotKey(
            _window.Handle,
            HOTKEY_ID,
            modifiers,
            (uint)settings.HotkeyKey
        );

        return _isRegistered;
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            NativeMethods.UnregisterHotKey(_window.Handle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    public void Dispose()
    {
        Unregister();
        _window.Dispose();
    }

    // Hidden window to receive hotkey messages
    private class HotkeyWindow : NativeWindow, IDisposable
    {
        public event EventHandler? HotkeyPressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
