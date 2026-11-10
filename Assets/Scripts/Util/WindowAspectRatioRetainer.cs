using UnityEngine;

public sealed class WindowAspectRatioRetainer : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private static WindowAspectRatioRetainer instance;
    private System.IntPtr installedWindow;
    private float nextCheck;

    private delegate bool EnumThreadWindowsProc(System.IntPtr window, System.IntPtr parameter);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
            return;

        var retainer = new GameObject(nameof(WindowAspectRatioRetainer));
        DontDestroyOnLoad(retainer);
        instance = retainer.AddComponent<WindowAspectRatioRetainer>();
    }

    private void Update()
    {
        // Re-check periodically: the window can be recreated when the display mode changes.
        if (Time.unscaledTime < nextCheck)
            return;

        nextCheck = Time.unscaledTime + 0.5f;
        var window = FindPlayerWindow();
        if (window == System.IntPtr.Zero || window == installedWindow)
            return;

        if (WindowAspectSubclass.Install(window))
            installedWindow = window;
        else
            Debug.LogError($"Could not install the 16:9 window constraint (error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}).");
    }

    private void OnApplicationQuit()
    {
        if (installedWindow != System.IntPtr.Zero)
            WindowAspectSubclass.Remove(installedWindow);
    }

    private static System.IntPtr FindPlayerWindow()
    {
        var found = System.IntPtr.Zero;
        var name = new System.Text.StringBuilder(64);
        EnumThreadWindowsProc callback = (window, _) =>
        {
            name.Length = 0;
            GetClassName(window, name, name.Capacity);
            if (name.ToString() != "UnityWndClass")
                return true;

            found = window;
            return false;
        };

        EnumThreadWindows(GetCurrentThreadId(), callback, System.IntPtr.Zero);
        System.GC.KeepAlive(callback);
        return found;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint threadId, EnumThreadWindowsProc callback, System.IntPtr parameter);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int GetClassName(System.IntPtr window, System.Text.StringBuilder text, int maxCount);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
#endif
}
