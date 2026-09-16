using System.Runtime.InteropServices;

namespace PortableHub.App.Services;

public class SingleInstanceManager : IDisposable
{
    private const string MutexName = "PortableHub_SingleInstance_Mutex";
    public static readonly int WM_SHOWME = RegisterWindowMessage("WM_PORTABLEHUB_SHOWME");

    private Mutex? _mutex;
    private bool _hasHandle;

    public bool IsFirstInstance()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out _hasHandle);
            return _hasHandle;
        }
        catch (AbandonedMutexException)
        {
            _hasHandle = true;
            return true;
        }
        catch
        {
            return true;
        }
    }

    public static void NotifyExistingInstance()
    {
        PostMessage((IntPtr)HWND_BROADCAST, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
    }

    private const uint MSGFLT_ADD = 1;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilter(int message, uint dwFlag);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, int msg, uint action, IntPtr pChangeFilterStruct);

    static SingleInstanceManager()
    {
        AllowUipiMessage();
    }

    public static void AllowUipiMessage(IntPtr? hwnd = null)
    {
        try
        {
            ChangeWindowMessageFilter(WM_SHOWME, MSGFLT_ADD);
            if (hwnd.HasValue && hwnd.Value != IntPtr.Zero)
            {
                ChangeWindowMessageFilterEx(hwnd.Value, WM_SHOWME, MSGFLT_ADD, IntPtr.Zero);
            }
        }
        catch
        {
            // Defensive: ignore on systems where filtering API is restricted
        }
    }

    public void Dispose()
    {
        if (_hasHandle && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // Ignored
            }
        }
        _mutex?.Dispose();
        GC.SuppressFinalize(this);
    }

    private const int HWND_BROADCAST = 0xffff;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
