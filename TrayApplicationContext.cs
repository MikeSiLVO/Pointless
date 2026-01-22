namespace Pointless;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly Settings _settings;
    private readonly MouseHider _mouseHider;
    private readonly HotkeyManager _hotkeyManager;
    private readonly ToolStripMenuItem _enabledMenuItem;
    private readonly ToolStripMenuItem _statusMenuItem;

    public TrayApplicationContext()
    {
        _settings = Settings.Load();
        _mouseHider = new MouseHider(_settings);
        _hotkeyManager = new HotkeyManager();

        // Create context menu
        _statusMenuItem = new ToolStripMenuItem("Cursor: Visible")
        {
            Enabled = false
        };

        _enabledMenuItem = new ToolStripMenuItem("Enabled", null, OnEnabledClick)
        {
            Checked = _settings.Enabled
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_statusMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_enabledMenuItem);
        contextMenu.Items.Add(new ToolStripMenuItem("Settings...", null, OnSettingsClick));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, OnExitClick));

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "Pointless - Cursor auto-hide",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += (s, e) => OnSettingsClick(s, e);

        // Subscribe to cursor visibility changes
        _mouseHider.CursorVisibilityChanged += OnCursorVisibilityChanged;

        // Register hotkey
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        RegisterHotkey();

        // Start monitoring
        _mouseHider.Start();

        UpdateStatusDisplay();
    }

    private void OnCursorVisibilityChanged(object? sender, bool visible)
    {
        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        var status = _mouseHider.IsCursorHidden ? "Hidden" : "Visible";
        var enabled = _settings.Enabled ? "" : " (Disabled)";
        _statusMenuItem.Text = $"Cursor: {status}{enabled}";
        _trayIcon.Text = $"Pointless - Cursor: {status}{enabled}";
    }

    private void OnEnabledClick(object? sender, EventArgs e)
    {
        _settings.Enabled = !_settings.Enabled;
        _enabledMenuItem.Checked = _settings.Enabled;
        _settings.Save();
        UpdateStatusDisplay();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings, OnSettingsChanged);
        form.ShowDialog();
    }

    private void OnSettingsChanged()
    {
        RegisterHotkey();
        UpdateStatusDisplay();
    }

    private void RegisterHotkey()
    {
        if (!_hotkeyManager.Register(_settings) && _settings.HotkeyEnabled)
        {
            MessageBox.Show(
                $"Failed to register hotkey {_settings.GetHotkeyDisplayString()}. It may be in use by another application.",
                "Hotkey Registration Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        _settings.Enabled = !_settings.Enabled;
        _enabledMenuItem.Checked = _settings.Enabled;

        if (!_settings.Enabled)
        {
            // Force show cursor when disabling
            _mouseHider.Toggle();
            if (_mouseHider.IsCursorHidden)
            {
                _mouseHider.Toggle();
            }
        }

        UpdateStatusDisplay();

        // Show balloon tip to indicate state change
        _trayIcon.ShowBalloonTip(
            1000,
            "Pointless",
            _settings.Enabled ? "Cursor auto-hide enabled" : "Cursor auto-hide disabled",
            ToolTipIcon.Info
        );
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _mouseHider.Stop();
        _hotkeyManager.Dispose();
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private static Icon CreateDefaultIcon()
    {
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw a simple cursor-like shape
            using var pen = new Pen(Color.White, 1.5f);
            using var brush = new SolidBrush(Color.FromArgb(64, 64, 64));

            // Arrow shape
            var points = new Point[]
            {
                new(2, 2),
                new(2, 12),
                new(5, 9),
                new(8, 14),
                new(10, 13),
                new(7, 8),
                new(11, 8),
                new(2, 2)
            };

            g.FillPolygon(brush, points);
            g.DrawPolygon(pen, points);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mouseHider.Dispose();
            _hotkeyManager.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
