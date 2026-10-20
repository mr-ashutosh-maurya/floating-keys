using System;
using System.Runtime.InteropServices;

namespace FloatingKeys;

internal static class InputSender
{
    /// <summary>
    /// Sends one key press (down + up) in a single atomic SendInput call.
    /// Modifiers are deliberately NOT injected: the physical Ctrl/Shift/Alt keys are already
    /// down in the system-wide keyboard state, so the target app sees e.g. Ctrl+R.
    /// Because we never inject modifier events, they can never get stuck.
    /// </summary>
    public static bool Tap(ushort vk)
    {
        ushort scan = (ushort)Native.MapVirtualKey(vk, 0 /* MAPVK_VK_TO_VSC */);
        var inputs = new[] { Make(vk, scan, 0), Make(vk, scan, Native.KEYEVENTF_KEYUP) };
        return Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Native.INPUT>()) == inputs.Length;
    }

    private static Native.INPUT Make(ushort vk, ushort scan, uint flags) => new()
    {
        type = Native.INPUT_KEYBOARD,
        u = new Native.INPUTUNION { ki = new Native.KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags } }
    };
}
