using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinCasc;

internal sealed class CascStorage : IDisposable
{
    private readonly IntPtr _handle;

    public CascStorage(string path)
    {
        if (!CascNative.CascOpenStorage(path, CascNative.CASC_LOCALE_ALL, out _handle))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Failed to open CASC storage '{path}'");
        }
    }

    public IEnumerable<CascEntry> EnumerateLocalFiles(string? listFilePath)
    {
        var data = new CascFindData();
        var find = CascNative.CascFindFirstFile(_handle, "*", ref data, listFilePath);

        if (find == CascNative.INVALID_HANDLE_VALUE)
        {
            yield break;
        }

        try
        {
            do
            {
                if (data.IsAvailable)
                {
                    yield return new CascEntry(data.GetFileName(), data.FileSize, data.SpanCount, data.IsFullName);
                }
            }
            while (CascNative.CascFindNextFile(find, ref data));
        }
        finally
        {
            CascNative.CascFindClose(find);
        }
    }

    public CascFileHandle? OpenFile(string name)
    {
        if (!CascNative.CascOpenFile(_handle, name, CascNative.CASC_LOCALE_ALL, CascNative.CASC_OPEN_BY_NAME, out var file))
        {
            return null;
        }

        return new CascFileHandle(file);
    }

    public void Dispose()
    {
        CascNative.CascCloseStorage(_handle);
    }
}
