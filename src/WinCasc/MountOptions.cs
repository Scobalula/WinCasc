namespace WinCasc;

internal sealed record MountOptions(string StoragePath, string? MountPoint, string? ListFilePath, bool Detach, bool Background);
