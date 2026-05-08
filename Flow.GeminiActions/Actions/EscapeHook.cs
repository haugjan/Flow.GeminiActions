using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Flow.GeminiActions.Actions;

/// <summary>
/// Listens for the ESC key system-wide via a low-level keyboard hook.
/// Used to cancel an in-flight Gemini request without stealing focus from
/// whatever app the user is in.
/// </summary>
/// <remarks>
/// WH_KEYBOARD_LL hook callbacks are dispatched on the thread that called
/// SetWindowsHookEx, and that thread must have a message pump. We install
/// (and unhook) on the WPF dispatcher thread so the callback rides the WPF
/// message loop. Installing on a ThreadPool thread leaves the hook silently
/// inert because there is no pump to dispatch the callback.
/// </remarks>
internal sealed class EscapeHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_ESCAPE = 0x1B;

    private readonly LowLevelKeyboardProc _proc;
    private readonly Action _onEscape;
    private readonly Dispatcher _dispatcher;
    private IntPtr _hookId;

    public EscapeHook(Action onEscape)
    {
        _onEscape = onEscape;
        // Hold a strong reference; otherwise the GC will collect the
        // delegate while Windows still owns the function pointer.
        _proc = HookCallback;
        _dispatcher =
            Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("EscapeHook requires a WPF dispatcher.");
        _dispatcher.Invoke(() => _hookId = SetHook(_proc));
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_ESCAPE)
                {
                    try
                    {
                        _onEscape();
                    }
                    catch
                    {
                        // never let an exception escape into the OS hook chain
                    }
                }
            }
        }
        // Always pass to the next hook so ESC keeps working in other apps.
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId == IntPtr.Zero)
            return;

        var hook = _hookId;
        _hookId = IntPtr.Zero;
        if (_dispatcher.CheckAccess())
            UnhookWindowsHookEx(hook);
        else
            _dispatcher.Invoke(() => UnhookWindowsHookEx(hook));
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
