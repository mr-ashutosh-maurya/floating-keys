# Floating Keys — on-screen R T F G V B for Windows 11

C# / WPF (.NET 8) desktop utility. No internet, no telemetry, no background services.

## How it works
- **Focus:** the window has the `WS_EX_NOACTIVATE` style and answers `WM_MOUSEACTIVATE` with `MA_NOACTIVATE`, so clicking it never makes it the foreground window. Your text field keeps focus.
- **Typing:** each click sends one key-down + key-up pair in a single `SendInput` call (virtual-key + scan code). Real key events, so shortcuts work.
- **Ctrl/Shift/Alt:** the app does not inject modifiers. Your physical modifier keys are already down in Windows' global keyboard state, so the injected `R` arrives as `Ctrl+R`, `Shift+R`, `Ctrl+Shift+R`, etc. Left/right variants work, and since the app never presses a modifier, none can get stuck. Caps Lock works the same way (system state); the key labels follow Shift XOR Caps Lock.
- **Always on top:** `Topmost` plus a re-assert every ~2.4 s. Hidden from Alt+Tab and the taskbar; lives in the tray.
- Runs as a normal user (`asInvoker`), PerMonitorV2 DPI aware.

## Build & run
Requires Windows 11 + [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
```
cd FloatingKeys
dotnet run -c Release            # or double-click run.bat
```
Standalone exe (no .NET needed on the target machine):
```
powershell -ExecutionPolicy Bypass -File publish.ps1
# -> .\publish\FloatingKeys.exe
```
Visual Studio 2022: open `FloatingKeys.csproj`, press F5.

## Using it
- Drag the title strip to move; drag the window edges to resize (keys scale).
- ⚙ Settings: opacity, two-row layout, theme (System/Light/Dark), start with Windows.
- – hides to the tray. Restore via tray icon click or **Ctrl+Alt+K**. ✕ exits.
- Tray tooltip shows Active / Hidden. The title dot is green; it flashes red if Windows reports an injection failure.
- Settings (position, size, opacity, theme, layout) are saved in `%AppData%\FloatingKeys\settings.json`.
- **Start with Windows** writes `HKCU\...\Run\FloatingKeys` (no admin). Untick to remove.

## Testing checklist
Open Notepad, then repeat in a browser, VS Code and a terminal.
- [ ] Click each of r t f g v b → lowercase letter appears; Notepad keeps its caret and focus.
- [ ] Hold physical Shift, click each → R T F G V B.
- [ ] Toggle Caps Lock → labels turn uppercase, typed letters uppercase; Caps+Shift → lowercase.
- [ ] Ctrl+click: R (browser refresh), T (new tab), F (find), G (find next / go to line), V (paste), B (bold / sidebar in VS Code).
- [ ] Ctrl+Shift+click T (reopen closed tab), R (hard refresh), B, F (VS Code search).
- [ ] Release all modifiers, then type normally on the physical keyboard: no stuck Ctrl/Shift.
- [ ] One click = exactly one character.
- [ ] Switch between apps; the keyboard stays on top.
- [ ] Hide (–), restore via tray and via Ctrl+Alt+K. Restart: position/opacity remembered.
- [ ] Opacity slider, two-row layout, light/dark theme.
- [ ] Drag to a monitor with a different scaling factor.

## Limitations
- **Elevated (admin) apps:** Windows UIPI blocks injected input from a non-elevated process. Run Floating Keys as administrator only if you need this (it cannot detect the block itself).
- Secure desktop (UAC prompts, Ctrl+Alt+Del, lock/sign-in screen) is not reachable.
- Games, remote-desktop clients, VMs and anti-cheat software may ignore or filter injected input.
- A single click sends one press; holding the mouse does not auto-repeat.
- Another always-on-top app can briefly cover it until the next re-assert.
- If a physical modifier is held while you click, it applies to that key; that is the intended behavior.

## Troubleshooting
- **Typing goes nowhere / focus moves:** make sure the target field is focused before you click; confirm no other copy is running (check the tray). Run as admin if the target app is elevated.
- **Shortcut does nothing:** the app may not bind that shortcut (e.g. Ctrl+G), or intercepts input at a low level.
- **Keyboard disappeared:** tray icon → Show / Hide, or Ctrl+Alt+K. If the hotkey is taken by another program, use the tray.
- **Window off-screen after a monitor change:** delete `%AppData%\FloatingKeys\settings.json`.
- **Build error about Windows targeting:** build on Windows; WPF does not build elsewhere without extra flags.
