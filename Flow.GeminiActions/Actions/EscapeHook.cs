using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Flow.GeminiActions.Actions;

/// <summary>
/// Listens for the ESC key system-wide via a low-level keyboard hook.
/// Used to cancel an in-flight Gemini request without stealing focus from
/// whatever app the user is in.
/// </summary>
internal sealed class EscapeHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_ESCAPE = 0x1B;

    private readonly LowLevelKeyboardProc _proc;
    private readonly Action _onEscape;
    private IntPtr _hookId;

    public EscapeHook(Action onEscape)
    {
        _onEscape = onEscape;
        // Hold a strong reference; otherwise the GC will collect the
        // delegate while Windows still owns the function pointer.
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == VK_ESCAPE)
                _onEscape();
        }
        // Always pass to the next hook so ESC keeps working in other apps.
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
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
