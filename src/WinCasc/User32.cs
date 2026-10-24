using System.Runtime.InteropServices;

namespace WinCasc;

internal static partial class User32
{
    public const uint MB_ICONERROR = 0x10;

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
