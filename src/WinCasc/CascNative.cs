using System.Runtime.InteropServices;

namespace WinCasc;

internal static partial class CascNative
{
    public const uint CASC_LOCALE_ALL = 0xFFFFFFFF;
    public const uint CASC_OPEN_BY_NAME = 0;
    public const uint FILE_BEGIN = 0;
    public const ulong CASC_INVALID_SIZE64 = 0xFFFFFFFFFFFFFFFF;
    public static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    [LibraryImport("CascLib", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascOpenStorage(string szParams, uint dwLocaleMask, out IntPtr phStorage);

    [LibraryImport("CascLib")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascCloseStorage(IntPtr hStorage);

    [LibraryImport("CascLib", SetLastError = true)]
    public static partial IntPtr CascFindFirstFile(IntPtr hStorage, [MarshalAs(UnmanagedType.LPUTF8Str)] string szMask, ref CascFindData pFindData, [MarshalAs(UnmanagedType.LPWStr)] string? szListFile);

    [LibraryImport("CascLib")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascFindNextFile(IntPtr hFind, ref CascFindData pFindData);

    [LibraryImport("CascLib")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascFindClose(IntPtr hFind);

    [LibraryImport("CascLib", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascOpenFile(IntPtr hStorage, [MarshalAs(UnmanagedType.LPUTF8Str)] string pvFileName, uint dwLocaleFlags, uint dwOpenFlags, out IntPtr phFile);

    [LibraryImport("CascLib", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascGetFileSize64(IntPtr hFile, out ulong pFileSize);

    [LibraryImport("CascLib", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascSetFilePointer64(IntPtr hFile, long distanceToMove, out ulong pNewPos, uint dwMoveMethod);

    [LibraryImport("CascLib", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascReadFile(IntPtr hFile, IntPtr lpBuffer, uint dwToRead, out uint pdwRead);

    [LibraryImport("CascLib")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CascCloseFile(IntPtr hFile);
}
