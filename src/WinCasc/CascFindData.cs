using System.Runtime.InteropServices;
using System.Text;

namespace WinCasc;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CascFindData
{
    public fixed byte FileName[260];
    public fixed byte CKey[16];
    public fixed byte EKey[16];
    public ulong TagBitMask;
    public ulong FileSize;
    public IntPtr PlainName;
    public uint FileDataId;
    public uint LocaleFlags;
    public uint ContentFlags;
    public uint SpanCount;
    public uint Flags;
    public int NameType;

    public bool IsAvailable => (Flags & 1) != 0;

    public bool IsFullName => NameType == 0;

    public string GetFileName()
    {
        fixed (byte* name = FileName)
        {
            return Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(name));
        }
    }
}
