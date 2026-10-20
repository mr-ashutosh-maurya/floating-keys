using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FloatingKeys;

public sealed class AppSettings
{
    public double Left { get; set; } = double.NaN;
    public double Top { get; set; } = double.NaN;
    public double Width { get; set; } = 360;
    public double Opacity { get; set; } = 0.95;
    public bool TwoRows { get; set; }
    public string Theme { get; set; } = "System";   // System | Light | Dark

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FloatingKeys", "settings.json");

    public static AppSettings Load()
    {
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-fatal */ }
    }
}

internal static class Startup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool IsEnabled()
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey);
        return k?.GetValue("FloatingKeys") is string;
    }
    public static void Set(bool on)
    {
        using var k = Registry.CurrentUser.CreateSubKey(RunKey);
        if (on) k.SetValue("FloatingKeys", $"\"{Environment.ProcessPath}\"");
        else k.DeleteValue("FloatingKeys", false);
    }
}

internal static class Theme
{
    public static void Apply(string mode)
    {
        bool dark = mode == "Dark" || (mode == "System" && SystemIsDark());
        void Set(string key, string argb)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(argb));
            b.Freeze();
            Application.Current.Resources[key] = b;
        }
        if (dark)
        {
            Set("Surface", "#F2202024"); Set("Stroke", "#33FFFFFF"); Set("KeyBg", "#FF32323A");
            Set("KeyHover", "#FF3E3E48"); Set("KeyPress", "#FF2A2A31"); Set("Fg", "#FFFFFFFF");
            Set("FgMuted", "#FFA0A0A8"); Set("Accent", "#FF4CC2FF"); Set("AccentFg", "#FF000000");
        }
        else
        {
            Set("Surface", "#F2F3F3F6"); Set("Stroke", "#1F000000"); Set("KeyBg", "#FFFFFFFF");
            Set("KeyHover", "#FFF0F0F5"); Set("KeyPress", "#FFE2E2EA"); Set("Fg", "#FF1B1B1F");
            Set("FgMuted", "#FF5F5F68"); Set("Accent", "#FF0067C0"); Set("AccentFg", "#FFFFFFFF");
        }
    }

    private static bool SystemIsDark()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }
}
