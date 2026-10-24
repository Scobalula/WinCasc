using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Fsp;

namespace WinCasc;

internal sealed class CascFileSystem : FileSystemBase
{
    private const ulong AllocationUnit = 4096;

    private static readonly char[] PathSeparators = ['\\', '/', ':'];

    private readonly CascStorage _storage;
    private readonly CascNode _root;
    private readonly string _volumeLabel;
    private readonly ulong _fileTime;
    private readonly byte[] _securityDescriptor;
    private ulong _nextIndex;
    private ulong _totalSize;

    public CascFileSystem(CascStorage storage, string? listFilePath, string volumeLabel, DateTime timestamp)
    {
        _storage = storage;
        _volumeLabel = volumeLabel;
        _fileTime = (ulong)timestamp.ToFileTimeUtc();
        _root = new CascNode(string.Empty, _nextIndex++);

        var descriptor = new RawSecurityDescriptor("O:BAG:BAD:P(A;;FRFX;;;WD)");
        _securityDescriptor = new byte[descriptor.BinaryLength];
        descriptor.GetBinaryForm(_securityDescriptor, 0);

        foreach (var entry in storage.EnumerateLocalFiles(listFilePath))
        {
            AddFile(entry);
        }

        FinalizeDirectory(_root);
    }

    public override int Init(object host)
    {
        var fileSystemHost = (FileSystemHost)host;
        fileSystemHost.SectorSize = (ushort)AllocationUnit;
        fileSystemHost.SectorsPerAllocationUnit = 1;
        fileSystemHost.MaxComponentLength = 255;
        fileSystemHost.FileInfoTimeout = uint.MaxValue;
        fileSystemHost.CaseSensitiveSearch = false;
        fileSystemHost.CasePreservedNames = true;
        fileSystemHost.UnicodeOnDisk = true;
        fileSystemHost.PersistentAcls = true;
        fileSystemHost.PostCleanupWhenModifiedOnly = true;
        fileSystemHost.VolumeCreationTime = _fileTime;
        fileSystemHost.FileSystemName = "CASC";
        return STATUS_SUCCESS;
    }

    public override int GetVolumeInfo(out Fsp.Interop.VolumeInfo volumeInfo)
    {
        volumeInfo = default;
        volumeInfo.TotalSize = _totalSize;
        volumeInfo.FreeSize = 0;
        volumeInfo.SetVolumeLabel(_volumeLabel);
        return STATUS_SUCCESS;
    }

    public override int GetSecurityByName(string fileName, out uint fileAttributes, ref byte[] securityDescriptor)
    {
        fileAttributes = 0;

        var node = FindNode(fileName);

        if (node == null)
        {
            return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        fileAttributes = GetAttributes(node);

        if (securityDescriptor != null)
        {
            securityDescriptor = _securityDescriptor;
        }

        return STATUS_SUCCESS;
    }

    public override int GetSecurity(object fileNode, object fileDesc, ref byte[] securityDescriptor)
    {
        securityDescriptor = _securityDescriptor;
        return STATUS_SUCCESS;
    }

    public override int Create(string fileName, uint createOptions, uint grantedAccess, uint fileAttributes, byte[] securityDescriptor, ulong allocationSize, out object fileNode, out object fileDesc, out Fsp.Interop.FileInfo fileInfo, out string normalizedName)
    {
        fileNode = null!;
        fileDesc = null!;
        fileInfo = default;
        normalizedName = null!;
        return STATUS_MEDIA_WRITE_PROTECTED;
    }

    public override int Open(string fileName, uint createOptions, uint grantedAccess, out object fileNode, out object fileDesc, out Fsp.Interop.FileInfo fileInfo, out string normalizedName)
    {
        fileNode = null!;
        fileDesc = null!;
        fileInfo = default;
        normalizedName = null!;

        var node = FindNode(fileName);

        if (node == null)
        {
            return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        if (node.CascName != null)
        {
            var handle = _storage.OpenFile(node.CascName);

            if (handle == null)
            {
                return NtStatusFromWin32((uint)Marshal.GetLastPInvokeError());
            }

            if (node.Size == CascNative.CASC_INVALID_SIZE64)
            {
                node.Size = handle.GetSize();
            }

            fileDesc = handle;
        }

        fileNode = node;
        fileInfo = GetFileInfo(node);
        return STATUS_SUCCESS;
    }

    public override void Close(object fileNode, object fileDesc)
    {
        (fileDesc as CascFileHandle)?.Dispose();
    }

    public override int Read(object fileNode, object fileDesc, IntPtr buffer, ulong offset, uint length, out uint bytesTransferred)
    {
        bytesTransferred = 0;

        var node = (CascNode)fileNode;

        if (offset >= node.Size)
        {
            return STATUS_END_OF_FILE;
        }

        if (!((CascFileHandle)fileDesc).Read(offset, buffer, length, out bytesTransferred))
        {
            return NtStatusFromWin32((uint)Marshal.GetLastPInvokeError());
        }

        return STATUS_SUCCESS;
    }

    public override int GetFileInfo(object fileNode, object fileDesc, out Fsp.Interop.FileInfo fileInfo)
    {
        fileInfo = GetFileInfo((CascNode)fileNode);
        return STATUS_SUCCESS;
    }

    public override bool ReadDirectoryEntry(object fileNode, object fileDesc, string pattern, string marker, ref object context, out string fileName, out Fsp.Interop.FileInfo fileInfo)
    {
        var node = (CascNode)fileNode;
        var children = node.SortedChildren;
        var position = context as int? ?? GetStartPosition(node, marker);

        context = position + 1;

        if (position < 0)
        {
            fileName = position == -2 ? "." : "..";
            fileInfo = GetFileInfo(node);
            return true;
        }

        if (position >= children.Length)
        {
            fileName = null!;
            fileInfo = default;
            return false;
        }

        fileName = children[position].Name;
        fileInfo = GetFileInfo(children[position]);
        return true;
    }

    private void AddFile(CascEntry entry)
    {
        var parts = entry.Name.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return;
        }

        var directory = entry.IsFullName ? _root : GetOrAddDirectory(_root, "_Unnamed");

        for (var i = 0; i < parts.Length - 1; i++)
        {
            directory = GetOrAddDirectory(directory, SanitizeName(parts[i]));
        }

        var fileName = SanitizeName(parts[^1]);

        if (directory.Children!.ContainsKey(fileName))
        {
            return;
        }

        directory.Children.Add(fileName, new CascNode(fileName, _nextIndex++, entry.Name, entry.Size, entry.SpanCount));

        if (entry.Size != CascNative.CASC_INVALID_SIZE64)
        {
            _totalSize += entry.Size;
        }
    }

    private CascNode GetOrAddDirectory(CascNode parent, string name)
    {
        if (parent.Children!.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var directory = new CascNode(name, _nextIndex++);
        parent.Children.Add(name, directory);
        return directory;
    }

    private CascNode? FindNode(string fileName)
    {
        var node = _root;

        foreach (var part in fileName.Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            if (node.Children == null || !node.Children.TryGetValue(part, out node))
            {
                return null;
            }
        }

        return node;
    }

    private int GetStartPosition(CascNode node, string marker)
    {
        if (marker == null)
        {
            return node == _root ? 0 : -2;
        }

        if (marker == ".")
        {
            return -1;
        }

        if (marker == "..")
        {
            return 0;
        }

        var low = 0;
        var high = node.SortedChildren.Length;

        while (low < high)
        {
            var middle = (low + high) / 2;

            if (string.Compare(node.SortedChildren[middle].Name, marker, StringComparison.OrdinalIgnoreCase) <= 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private Fsp.Interop.FileInfo GetFileInfo(CascNode node)
    {
        var size = node.IsDirectory ? 0 : node.Size;

        return new Fsp.Interop.FileInfo
        {
            FileAttributes = GetAttributes(node),
            FileSize = size,
            AllocationSize = (size + AllocationUnit - 1) / AllocationUnit * AllocationUnit,
            CreationTime = _fileTime,
            LastAccessTime = _fileTime,
            LastWriteTime = _fileTime,
            ChangeTime = _fileTime,
            IndexNumber = node.Index,
        };
    }

    private static uint GetAttributes(CascNode node)
    {
        return node.IsDirectory ? (uint)FileAttributes.Directory : (uint)FileAttributes.ReadOnly;
    }

    private static string SanitizeName(string name)
    {
        return string.Create(name.Length, name, (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = source[i] < ' ' || "*?\"<>|".Contains(source[i]) ? '_' : source[i];
            }
        });
    }

    private void FinalizeDirectory(CascNode node)
    {
        foreach (var file in node.Children!.Values.Where(child => child.SpanCount > 1).ToArray())
        {
            for (var part = 1; part <= file.SpanCount; part++)
            {
                if (node.Children.Remove($"{file.Name}_{part}", out var spanPart))
                {
                    _totalSize -= spanPart.Size;
                }
            }
        }

        node.SortedChildren = node.Children.Values.OrderBy(child => child.Name, StringComparer.OrdinalIgnoreCase).ToArray();

        foreach (var child in node.SortedChildren)
        {
            if (child.IsDirectory)
            {
                FinalizeDirectory(child);
            }
        }
    }
}
