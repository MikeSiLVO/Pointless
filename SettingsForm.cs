using Microsoft.Win32;

namespace Pointless;

public class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly Action _onSettingsChanged;

    private NumericUpDown _timeoutNumeric = null!;
    private CheckBox _ignoreKeyboardCheckbox = null!;
    private CheckBox _startWithWindowsCheckbox = null!;
    private CheckBox _hotkeyEnabledCheckbox = null!;
    private CheckBox _hotkeyCtrlCheckbox = null!;
    private CheckBox _hotkeyAltCheckbox = null!;
    private CheckBox _hotkeyShiftCheckbox = null!;
    private CheckBox _hotkeyWinCheckbox = null!;
    private ComboBox _hotkeyKeyCombo = null!;

    public SettingsForm(Settings settings, Action onSettingsChanged)
    {
        _settings = settings;
        _onSettingsChanged = onSettingsChanged;
        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        Text = "Pointless Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9f);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 6,
            ColumnCount = 1,
            AutoSize = true
        };

        // Timeout setting
        var timeoutPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        timeoutPanel.Controls.Add(new Label
        {
            Text = "Hide cursor after",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 0)
        });
        _timeoutNumeric = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 60,
            Margin = new Padding(5, 3, 5, 0)
        };
        timeoutPanel.Controls.Add(_timeoutNumeric);
        timeoutPanel.Controls.Add(new Label
        {
            Text = "seconds of inactivity",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 0)
        });
        mainPanel.Controls.Add(timeoutPanel);

        // Ignore keyboard checkbox
        _ignoreKeyboardCheckbox = new CheckBox
        {
            Text = "Ignore keyboard activity (only track mouse movement)",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        mainPanel.Controls.Add(_ignoreKeyboardCheckbox);

        // Hotkey group
        var hotkeyGroup = new GroupBox
        {
            Text = "Global Hotkey",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0)
        };

        var hotkeyPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 2,
            ColumnCount = 1,
            AutoSize = true
        };

        _hotkeyEnabledCheckbox = new CheckBox
        {
            Text = "Enable hotkey to toggle cursor hiding",
            AutoSize = true
        };
        _hotkeyEnabledCheckbox.CheckedChanged += (s, e) => UpdateHotkeyControlsEnabled();
        hotkeyPanel.Controls.Add(_hotkeyEnabledCheckbox);

        var modifiersPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        _hotkeyCtrlCheckbox = new CheckBox { Text = "Ctrl", AutoSize = true };
        _hotkeyAltCheckbox = new CheckBox { Text = "Alt", AutoSize = true };
        _hotkeyShiftCheckbox = new CheckBox { Text = "Shift", AutoSize = true };
        _hotkeyWinCheckbox = new CheckBox { Text = "Win", AutoSize = true };
        modifiersPanel.Controls.Add(_hotkeyCtrlCheckbox);
        modifiersPanel.Controls.Add(_hotkeyAltCheckbox);
        modifiersPanel.Controls.Add(_hotkeyShiftCheckbox);
        modifiersPanel.Controls.Add(_hotkeyWinCheckbox);
        modifiersPanel.Controls.Add(new Label
        {
            Text = "+",
            AutoSize = true,
            Margin = new Padding(5, 6, 5, 0)
        });

        _hotkeyKeyCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        // Add letter keys
        for (char c = 'A'; c <= 'Z'; c++)
        {
            _hotkeyKeyCombo.Items.Add(c.ToString());
        }
        // Add function keys
        for (int i = 1; i <= 12; i++)
        {
            _hotkeyKeyCombo.Items.Add($"F{i}");
        }
        modifiersPanel.Controls.Add(_hotkeyKeyCombo);

        hotkeyPanel.Controls.Add(modifiersPanel);
        hotkeyGroup.Controls.Add(hotkeyPanel);
        mainPanel.Controls.Add(hotkeyGroup);

        // Start with Windows checkbox
        _startWithWindowsCheckbox = new CheckBox
        {
            Text = "Start with Windows",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        mainPanel.Controls.Add(_startWithWindowsCheckbox);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 15, 0, 0)
        };

        var cancelButton = new Button { Text = "Cancel", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
        cancelButton.Click += (s, e) => Close();

        var saveButton = new Button { Text = "Save", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
        saveButton.Click += SaveButton_Click;

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);
        mainPanel.Controls.Add(buttonPanel);

        Controls.Add(mainPanel);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void LoadSettings()
    {
        _timeoutNumeric.Value = _settings.IdleTimeoutSeconds;
        _ignoreKeyboardCheckbox.Checked = _settings.IgnoreKeyboardActivity;
        _startWithWindowsCheckbox.Checked = _settings.StartWithWindows;
        _hotkeyEnabledCheckbox.Checked = _settings.HotkeyEnabled;
        _hotkeyCtrlCheckbox.Checked = _settings.HotkeyCtrl;
        _hotkeyAltCheckbox.Checked = _settings.HotkeyAlt;
        _hotkeyShiftCheckbox.Checked = _settings.HotkeyShift;
        _hotkeyWinCheckbox.Checked = _settings.HotkeyWin;

        // Set the key in combo box
        var keyString = _settings.HotkeyKey.ToString();
        if (keyString.Length == 1)
        {
            _hotkeyKeyCombo.SelectedItem = keyString;
        }
        else if (keyString.StartsWith("F") && keyString.Length <= 3)
        {
            _hotkeyKeyCombo.SelectedItem = keyString;
        }
        else
        {
            _hotkeyKeyCombo.SelectedIndex = 0;
        }

        UpdateHotkeyControlsEnabled();
    }

    private void UpdateHotkeyControlsEnabled()
    {
        var enabled = _hotkeyEnabledCheckbox.Checked;
        _hotkeyCtrlCheckbox.Enabled = enabled;
        _hotkeyAltCheckbox.Enabled = enabled;
        _hotkeyShiftCheckbox.Enabled = enabled;
        _hotkeyWinCheckbox.Enabled = enabled;
        _hotkeyKeyCombo.Enabled = enabled;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // Validate hotkey - at least one modifier must be selected if hotkey is enabled
        if (_hotkeyEnabledCheckbox.Checked &&
            !_hotkeyCtrlCheckbox.Checked &&
            !_hotkeyAltCheckbox.Checked &&
            !_hotkeyShiftCheckbox.Checked &&
            !_hotkeyWinCheckbox.Checked)
        {
            MessageBox.Show(
                "Please select at least one modifier key (Ctrl, Alt, Shift, or Win) for the hotkey.",
                "Invalid Hotkey",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        _settings.IdleTimeoutSeconds = (int)_timeoutNumeric.Value;
        _settings.IgnoreKeyboardActivity = _ignoreKeyboardCheckbox.Checked;
        _settings.StartWithWindows = _startWithWindowsCheckbox.Checked;
        _settings.HotkeyEnabled = _hotkeyEnabledCheckbox.Checked;
        _settings.HotkeyCtrl = _hotkeyCtrlCheckbox.Checked;
        _settings.HotkeyAlt = _hotkeyAltCheckbox.Checked;
        _settings.HotkeyShift = _hotkeyShiftCheckbox.Checked;
        _settings.HotkeyWin = _hotkeyWinCheckbox.Checked;

        // Parse the selected key
        var selectedKey = _hotkeyKeyCombo.SelectedItem?.ToString() ?? "P";
        if (selectedKey.Length == 1)
        {
            _settings.HotkeyKey = (Keys)Enum.Parse(typeof(Keys), selectedKey);
        }
        else if (selectedKey.StartsWith("F"))
        {
            _settings.HotkeyKey = (Keys)Enum.Parse(typeof(Keys), selectedKey);
        }

        UpdateStartWithWindows(_settings.StartWithWindows);
        _settings.Save();
        _onSettingsChanged();
        Close();
    }

    private static void UpdateStartWithWindows(bool enable)
    {
        const string appName = "Pointless";
        using var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            writable: true
        );

        if (key == null) return;

        if (enable)
        {
            var exePath = Application.ExecutablePath;
            key.SetValue(appName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(appName, throwOnMissingValue: false);
        }
    }
}
