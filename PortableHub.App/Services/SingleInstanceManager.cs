using System.Runtime.InteropServices;

namespace PortableHub.App.Services;

public class SingleInstanceManager : IDisposable
{
    private const string MutexName = "PortableHub_SingleInstance_Mutex";
    public const int WM_SHOWME = 0x8001;

    private Mutex? _mutex;
    private bool _hasHandle;

    public bool IsFirstInstance()
    {
        _mutex = new Mutex(true, MutexName, out _hasHandle);
        return _hasHandle;
    }

    public static void NotifyExistingInstance()
    {
        PostMessage((IntPtr)HWND_BROADCAST, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
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

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
