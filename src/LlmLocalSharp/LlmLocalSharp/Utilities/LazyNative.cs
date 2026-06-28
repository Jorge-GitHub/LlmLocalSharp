using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LlmLocalSharp.Utilities;

/// <summary>
/// Loads native libraries from a specific folder, adding that folder
/// to the DLL search path on Windows.
/// 
/// NOTE: The kernel32 calls make this Windows-only as written.
/// For cross-platform support, guard with RuntimeInformation.IsOSPlatform
/// and use plain NativeLibrary.Load on Linux/macOS.
/// </summary>
internal static class LazyNative
{
    /// <summary>Configures the process default DLL search directories on Windows.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetDefaultDllDirectories(uint directoryFlags);

    /// <summary>Adds a directory to the process DLL search path on Windows.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr AddDllDirectory(
        [MarshalAs(UnmanagedType.LPWStr)] string newDirectory);

    private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
    private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

    private static int _initialized;
    private static readonly ConcurrentDictionary<string, byte> _addedFolders =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Loads a native library from the specified folder.</summary>
    public static IntPtr LoadFromFolder(string folder, string dllName)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new ArgumentException("Folder is required.", nameof(folder));
        }

        if (string.IsNullOrWhiteSpace(dllName))
        {
            throw new ArgumentException("DLL name is required.", nameof(dllName));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            EnsureInitialized();

            if (_addedFolders.TryAdd(folder, 0))
            {
                if (AddDllDirectory(folder) == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        var fullPath = Path.Combine(folder, dllName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Native library not found.", fullPath);
        }

        return NativeLibrary.Load(fullPath);
    }

    /// <summary>Initializes the Windows DLL search path settings once.</summary>
    private static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        if (!SetDefaultDllDirectories(
                LOAD_LIBRARY_SEARCH_DEFAULT_DIRS
                | LOAD_LIBRARY_SEARCH_USER_DIRS))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
