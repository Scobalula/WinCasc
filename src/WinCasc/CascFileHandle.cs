namespace WinCasc;

internal sealed class CascFileHandle : IDisposable
{
    private readonly IntPtr _handle;
    private readonly Lock _lock = new();

    public CascFileHandle(IntPtr handle)
    {
        _handle = handle;
    }

    public ulong GetSize()
    {
        CascNative.CascGetFileSize64(_handle, out var size);
        return size;
    }

    public bool Read(ulong offset, IntPtr buffer, uint length, out uint bytesRead)
    {
        lock (_lock)
        {
            bytesRead = 0;
            return CascNative.CascSetFilePointer64(_handle, (long)offset, out _, CascNative.FILE_BEGIN) && CascNative.CascReadFile(_handle, buffer, length, out bytesRead);
        }
    }

    public void Dispose()
    {
        CascNative.CascCloseFile(_handle);
    }
}
