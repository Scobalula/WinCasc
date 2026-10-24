namespace WinCasc;

internal sealed class CascNode
{
    public string Name { get; }

    public string? CascName { get; }

    public ulong Index { get; }

    public ulong Size { get; set; }

    public uint SpanCount { get; }

    public Dictionary<string, CascNode>? Children { get; }

    public CascNode[] SortedChildren { get; set; } = [];

    public bool IsDirectory => Children != null;

    public CascNode(string name, ulong index)
    {
        Name = name;
        Index = index;
        Children = new Dictionary<string, CascNode>(StringComparer.OrdinalIgnoreCase);
    }

    public CascNode(string name, ulong index, string cascName, ulong size, uint spanCount)
    {
        Name = name;
        Index = index;
        CascName = cascName;
        Size = size;
        SpanCount = spanCount;
    }
}
