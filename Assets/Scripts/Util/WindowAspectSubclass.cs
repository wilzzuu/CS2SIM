#if UNITY_STANDALONE_WIN || WINDOW_ASPECT_TESTS
using System;
using System.Runtime.InteropServices;

// Keeps the client area of a resizable Win32 window at 16:9. Must be used from the thread that owns the window.
internal static class WindowAspectSubclass
{
    private const double AspectRatio = 16.0 / 9.0;
    private static readonly UIntPtr SubclassId = new UIntPtr(0x43533253u);

    private const uint WM_GETMINMAXINFO = 0x0024;
    private const uint WM_NCDESTROY = 0x0082;
    private const uint WM_WINDOWPOSCHANGING = 0x0046;
    private const uint WM_WINDOWPOSCHANGED = 0x0047;
    private const uint WM_SIZING = 0x0214;
    private const int WMSZ_LEFT = 1, WMSZ_RIGHT = 2, WMSZ_TOP = 3, WMSZ_TOPLEFT = 4,
                      WMSZ_TOPRIGHT = 5, WMSZ_BOTTOM = 6, WMSZ_BOTTOMLEFT = 7, WMSZ_BOTTOMRIGHT = 8;
    private const int GWL_STYLE = -16;
    private const long WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000;
    private const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private delegate IntPtr SubclassProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data);

    // Held in a static field so the native side never calls a collected delegate.
    private static readonly SubclassProc Callback = WindowProcedure;
    private static bool correcting;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO { public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize; }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS { public IntPtr hwnd, hwndInsertAfter; public int x, y, cx, cy; public uint flags; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

    [DllImport("comctl32.dll")] private static extern bool SetWindowSubclass(IntPtr window, SubclassProc proc, UIntPtr id, UIntPtr data);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(IntPtr window, SubclassProc proc, UIntPtr id);
    [DllImport("comctl32.dll")] private static extern IntPtr DefSubclassProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out RECT rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out RECT rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    public static bool Install(IntPtr window)
    {
        if (window == IntPtr.Zero || !IsWindow(window) || GetWindowThreadProcessId(window, out _) != GetCurrentThreadId())
            return false;
        if (!SetWindowSubclass(window, Callback, SubclassId, UIntPtr.Zero))
            return false;

        Correct(window);
        return true;
    }

    public static bool Remove(IntPtr window)
    {
        return window != IntPtr.Zero && IsWindow(window) && RemoveWindowSubclass(window, Callback, SubclassId);
    }

    private static bool IsResizable(IntPtr window)
    {
        long style = GetWindowLongPtr64(window, GWL_STYLE).ToInt64();
        return (style & (WS_CAPTION | WS_THICKFRAME)) == (WS_CAPTION | WS_THICKFRAME);
    }

    private static bool GetFrame(IntPtr window, out int frameWidth, out int frameHeight)
    {
        frameWidth = frameHeight = 0;
        if (!GetWindowRect(window, out RECT outer) || !GetClientRect(window, out RECT client))
            return false;

        frameWidth = (outer.right - outer.left) - (client.right - client.left);
        frameHeight = (outer.bottom - outer.top) - (client.bottom - client.top);
        return true;
    }

    private static int HeightForWidth(int width) => (int)Math.Round(width / AspectRatio);
    private static int WidthForHeight(int height) => (int)Math.Round(height * AspectRatio);

    // Fixes a window that is currently not 16:9 (for example one created before the hook was installed).
    private static void Correct(IntPtr window)
    {
        if (!IsResizable(window) || IsIconic(window) || !GetFrame(window, out int frameWidth, out int frameHeight))
            return;

        if (IsZoomed(window))
        {
            if (GetMaximizedBounds(window, frameWidth, frameHeight, out RECT bounds))
                SetWindowPos(window, IntPtr.Zero, bounds.left, bounds.top, bounds.right - bounds.left,
                    bounds.bottom - bounds.top, SWP_NOZORDER | SWP_NOACTIVATE);
        }
        else if (GetWindowRect(window, out RECT rect))
        {
            int clientWidth = Math.Max(1, rect.right - rect.left - frameWidth);
            SetWindowPos(window, IntPtr.Zero, 0, 0, rect.right - rect.left, HeightForWidth(clientWidth) + frameHeight,
                SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    private static bool GetMaximizedBounds(IntPtr window, int frameWidth, int frameHeight, out RECT bounds)
    {
        bounds = default;
        var monitor = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (!GetMonitorInfo(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), ref monitor))
            return false;

        int availableWidth = monitor.rcWork.right - monitor.rcWork.left;
        int availableHeight = monitor.rcWork.bottom - monitor.rcWork.top;
        int clientWidth = Math.Max(1, Math.Min(availableWidth - frameWidth, WidthForHeight(availableHeight - frameHeight)));
        int width = clientWidth + frameWidth;
        int height = HeightForWidth(clientWidth) + frameHeight;
        bounds.left = monitor.rcWork.left + (availableWidth - width) / 2;
        bounds.top = monitor.rcWork.top + (availableHeight - height) / 2;
        bounds.right = bounds.left + width;
        bounds.bottom = bounds.top + height;
        return true;
    }

    internal static void ConstrainSizingRect(ref RECT rect, RECT current, int edge, int frameWidth, int frameHeight)
    {
        bool left = edge == WMSZ_LEFT || edge == WMSZ_TOPLEFT || edge == WMSZ_BOTTOMLEFT;
        bool right = edge == WMSZ_RIGHT || edge == WMSZ_TOPRIGHT || edge == WMSZ_BOTTOMRIGHT;
        bool top = edge == WMSZ_TOP || edge == WMSZ_TOPLEFT || edge == WMSZ_TOPRIGHT;
        bool bottom = edge == WMSZ_BOTTOM || edge == WMSZ_BOTTOMLEFT || edge == WMSZ_BOTTOMRIGHT;
        if (!left && !right && !top && !bottom)
            return;

        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;
        bool widthDrives = left || right;
        if ((left || right) && (top || bottom))
        {
            int widthChange = Math.Abs(width - (current.right - current.left));
            int heightChange = Math.Abs(height - (current.bottom - current.top));
            widthDrives = widthChange >= WidthForHeight(heightChange);
        }

        if (widthDrives)
        {
            int newHeight = HeightForWidth(Math.Max(1, width - frameWidth)) + frameHeight;
            if (top) rect.top = rect.bottom - newHeight;
            else if (bottom) rect.bottom = rect.top + newHeight;
            else { rect.top += (height - newHeight) / 2; rect.bottom = rect.top + newHeight; }
        }
        else
        {
            int newWidth = WidthForHeight(Math.Max(1, height - frameHeight)) + frameWidth;
            if (left) rect.left = rect.right - newWidth;
            else if (right) rect.right = rect.left + newWidth;
            else { rect.left += (width - newWidth) / 2; rect.right = rect.left + newWidth; }
        }
    }

    private static IntPtr WindowProcedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
    {
        if (message == WM_NCDESTROY)
        {
            RemoveWindowSubclass(window, Callback, id);
            return DefSubclassProc(window, message, wParam, lParam);
        }

        if (!IsResizable(window) || !GetFrame(window, out int frameWidth, out int frameHeight))
            return DefSubclassProc(window, message, wParam, lParam);

        switch (message)
        {
            case WM_SIZING:
            {
                var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                if (GetWindowRect(window, out RECT current))
                {
                    ConstrainSizingRect(ref rect, current, wParam.ToInt32(), frameWidth, frameHeight);
                    Marshal.StructureToPtr(rect, lParam, false);
                }
                return new IntPtr(1);
            }
            case WM_GETMINMAXINFO:
            {
                IntPtr result = DefSubclassProc(window, message, wParam, lParam);
                var limits = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                int minimumWidth = Math.Max(limits.ptMinTrackSize.x - frameWidth,
                    WidthForHeight(limits.ptMinTrackSize.y - frameHeight));
                limits.ptMinTrackSize.x = minimumWidth + frameWidth;
                limits.ptMinTrackSize.y = HeightForWidth(minimumWidth) + frameHeight;

                var monitor = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMaximizedBounds(window, frameWidth, frameHeight, out RECT bounds) &&
                    GetMonitorInfo(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), ref monitor))
                {
                    limits.ptMaxSize = new POINT { x = bounds.right - bounds.left, y = bounds.bottom - bounds.top };
                    limits.ptMaxTrackSize = limits.ptMaxSize;
                    limits.ptMaxPosition = new POINT { x = bounds.left - monitor.rcMonitor.left, y = bounds.top - monitor.rcMonitor.top };
                }

                Marshal.StructureToPtr(limits, lParam, false);
                return result;
            }
            case WM_WINDOWPOSCHANGING:
            {
                if (IsIconic(window) || IsZoomed(window))
                    break;

                var position = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                if ((position.flags & SWP_NOSIZE) == 0 && position.cx > frameWidth && position.cy > frameHeight &&
                    GetWindowRect(window, out RECT current))
                {
                    int clientWidth = position.cx - frameWidth;
                    int clientHeight = position.cy - frameHeight;
                    if (Math.Abs(clientHeight - HeightForWidth(clientWidth)) > 1)
                    {
                        if (position.cx == current.right - current.left && position.cy != current.bottom - current.top)
                            position.cx = WidthForHeight(clientHeight) + frameWidth;
                        else
                            position.cy = HeightForWidth(clientWidth) + frameHeight;

                        Marshal.StructureToPtr(position, lParam, false);
                    }
                }
                break;
            }
            case WM_WINDOWPOSCHANGED:
            {
                IntPtr result = DefSubclassProc(window, message, wParam, lParam);
                if (!correcting && !IsIconic(window) && !IsZoomed(window) && GetClientRect(window, out RECT client) &&
                    Math.Abs((client.bottom - client.top) - HeightForWidth(client.right - client.left)) > 1)
                {
                    correcting = true;
                    try { Correct(window); }
                    finally { correcting = false; }
                }

                return result;
            }
        }

        return DefSubclassProc(window, message, wParam, lParam);
    }
}
#endif
