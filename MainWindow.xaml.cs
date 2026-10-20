using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloatingKeys;

public partial class MainWindow : Window
{
    private static readonly (char Ch, ushort Vk)[] Keys =
        { ('R', 0x52), ('T', 0x54), ('F', 0x46), ('G', 0x47), ('V', 0x56), ('B', 0x42) };
    private const int HotkeyId = 0xF10A;   // Ctrl+Alt+K toggles show/hide

    private readonly AppSettings _s = AppSettings.Load();
    private readonly List<(Button Btn, char Ch)> _keys = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly System.Windows.Forms.NotifyIcon _tray = new();
    private IntPtr _hwnd;
    private bool _upper, _ready;
    private int _tick;

    public MainWindow()
    {
        Theme.Apply(_s.Theme);
        InitializeComponent();

        Width = Math.Clamp(_s.Width, MinWidth, MaxWidth);
        Opacity = Math.Clamp(_s.Opacity, 0.3, 1);
        OpacitySlider.Value = Opacity;
        TwoRowsCheck.IsChecked = _s.TwoRows;
        StartupCheck.IsChecked = Startup.IsEnabled();
        ThemeBtn.Content = "Theme: " + _s.Theme;
        KeyGrid.Columns = _s.TwoRows ? 3 : 6;

        foreach (var (ch, vk) in Keys)
        {
            var b = new Button { Content = ch.ToString().ToLower(), Style = (Style)FindResource("KeyBtn") };
            char c = ch; ushort v = vk; Button bb = b;
            b.Click += (_, _) => Press(v, bb);
            KeyGrid.Children.Add(b);
            _keys.Add((b, ch));
        }

        // Restore position if it is still on a visible screen; otherwise place bottom-right on Loaded.
        bool onScreen = !double.IsNaN(_s.Left) && !double.IsNaN(_s.Top)
            && _s.Left > SystemParameters.VirtualScreenLeft - 50 && _s.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 50
            && _s.Top > SystemParameters.VirtualScreenTop - 10 && _s.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 30;
        if (onScreen) { Left = _s.Left; Top = _s.Top; }
        Loaded += (_, _) =>
        {
            if (!onScreen)
            {
                var wa = SystemParameters.WorkArea;
                Left = wa.Right - ActualWidth - 20; Top = wa.Bottom - ActualHeight - 20;
            }
            _ready = true;
        };

        _timer.Tick += (_, _) => Poll();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) { _timer.Start(); Poll(); } else _timer.Stop();
            _tray.Text = "Floating Keys \u2014 " + (IsVisible ? "Active" : "Hidden (click to show)");
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Show / Hide", null, (_, _) => ToggleVisible());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        _tray.Icon = System.Drawing.SystemIcons.Application;
        _tray.ContextMenuStrip = menu;
        _tray.Text = "Floating Keys \u2014 Active";
        _tray.MouseClick += (_, e) => { if (e.Button == System.Windows.Forms.MouseButtons.Left) ToggleVisible(); };
        _tray.Visible = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        // NOACTIVATE: the window never becomes the foreground window, so the target keeps focus.
        // TOOLWINDOW: keeps it out of Alt+Tab.
        int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
        Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, ex | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW);
        HwndSource.FromHwnd(_hwnd).AddHook(WndProc);
        Native.RegisterHotKey(_hwnd, HotkeyId, Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, 'K');
    }

    private IntPtr WndProc(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
    {
        if (msg == Native.WM_MOUSEACTIVATE) { handled = true; return (IntPtr)Native.MA_NOACTIVATE; }
        if (msg == Native.WM_HOTKEY && w.ToInt32() == HotkeyId) { ToggleVisible(); handled = true; }
        return IntPtr.Zero;
    }

    private void Press(ushort vk, Button b)
    {
        bool ok = InputSender.Tap(vk);
        if (!ok) FlashDot();
        Flash(b);
    }

    private async void Flash(Button b) { b.Tag = "Flash"; await Task.Delay(120); b.Tag = null; }

    private async void FlashDot()
    {
        StatusDot.Fill = Brushes.IndianRed; await Task.Delay(600);
        StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
    }

    private static bool Down(int vk) => (Native.GetAsyncKeyState(vk) & 0x8000) != 0;

    private void Poll()
    {
        bool shift = Down(0x10), ctrl = Down(0x11), alt = Down(0x12);
        bool caps = (Native.GetKeyState(0x14) & 1) != 0;
        bool upper = shift ^ caps;
        if (upper != _upper || _tick == 0)
        {
            _upper = upper;
            foreach (var (btn, ch) in _keys) btn.Content = upper ? ch.ToString() : ch.ToString().ToLower();
        }
        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl"); if (shift) parts.Add("Shift"); if (alt) parts.Add("Alt");
        ModText.Text = string.Join("+", parts);

        if (++_tick % 20 == 0)   // re-assert topmost every ~2.4 s (other topmost windows can steal it)
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    private void ToggleVisible() { if (IsVisible) { SaveSettings(); Hide(); } else Show(); }

    private void SaveSettings()
    {
        if (!_ready) return;
        _s.Left = Left; _s.Top = Top; _s.Width = Width; _s.Opacity = Opacity; _s.Save();
    }

    private void ExitApp()
    {
        SaveSettings();
        Native.UnregisterHotKey(_hwnd, HotkeyId);
        _tray.Visible = false; _tray.Dispose();
        Application.Current.Shutdown();
    }

    // ---- UI events ----
    private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
    { if (e.ButtonState == MouseButtonState.Pressed) { try { DragMove(); } catch { } SaveSettings(); } }
    private void Settings_Click(object s, RoutedEventArgs e) =>
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    private void Hide_Click(object s, RoutedEventArgs e) => ToggleVisible();
    private void Close_Click(object s, RoutedEventArgs e) => ExitApp();
    private void Opacity_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (IsLoaded) { Opacity = e.NewValue; SaveSettings(); } }
    private void TwoRows_Click(object s, RoutedEventArgs e)
    {
        _s.TwoRows = TwoRowsCheck.IsChecked == true;
        KeyGrid.Columns = _s.TwoRows ? 3 : 6;
        Width = _s.TwoRows ? 220 : 360;
        SaveSettings();
    }
    private void Startup_Click(object s, RoutedEventArgs e) => Startup.Set(StartupCheck.IsChecked == true);
    private void Theme_Click(object s, RoutedEventArgs e)
    {
        _s.Theme = _s.Theme switch { "System" => "Light", "Light" => "Dark", _ => "System" };
        ThemeBtn.Content = "Theme: " + _s.Theme;
        Theme.Apply(_s.Theme); SaveSettings();
    }
}
