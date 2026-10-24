using System.Diagnostics;
using Fsp;

namespace WinCasc;

internal static class MountSession
{
    private const string UnmountAllEventName = @"Local\WinCasc.UnmountAll";

    public static void Run(MountOptions options)
    {
        var storagePath = Path.GetFullPath(options.StoragePath);
        var mountPoint = options.MountPoint ?? FindFreeDriveLetter();

        using var unmountEvent = new EventWaitHandle(false, EventResetMode.ManualReset, GetUnmountEventName(mountPoint), out var createdNew);

        if (!createdNew)
        {
            throw new InvalidOperationException($"'{mountPoint}' is already mounted by WinCasc.");
        }

        using var unmountAllEvent = new EventWaitHandle(false, EventResetMode.ManualReset, UnmountAllEventName);

        Console.WriteLine($"Loading '{storagePath}'...");

        using var storage = new CascStorage(storagePath);
        var timestamp = File.GetLastWriteTimeUtc(Path.Combine(storagePath, ".build.info"));
        var fileSystem = new CascFileSystem(storage, options.ListFilePath, Path.GetFileName(storagePath), timestamp);

        using var host = new FileSystemHost(fileSystem);
        var status = host.Mount(mountPoint);

        if (status < 0)
        {
            throw new InvalidOperationException($"Failed to mount at '{mountPoint}' (NTSTATUS 0x{status:X8}).");
        }

        Console.WriteLine($"Mounted at {mountPoint}. Press Ctrl+C or run 'WinCasc unmount {mountPoint}' to unmount.");

        if (options.Background)
        {
            Process.Start("explorer.exe", host.MountPoint());
        }

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            unmountEvent.Set();
        };

        WaitHandle.WaitAny([unmountEvent, unmountAllEvent]);
        host.Unmount();
    }

    public static void UnmountAll()
    {
        using var unmountAllEvent = new EventWaitHandle(false, EventResetMode.ManualReset, UnmountAllEventName);
        unmountAllEvent.Set();

        var mountProcesses = Process.GetProcessesByName("WinCasc").Concat(Process.GetProcessesByName("WinCascShell")).Where(process => process.Id != Environment.ProcessId);

        foreach (var process in mountProcesses)
        {
            process.WaitForExit(TimeSpan.FromSeconds(10));
        }

        unmountAllEvent.Reset();
    }

    public static bool Unmount(string mountPoint)
    {
        if (!EventWaitHandle.TryOpenExisting(GetUnmountEventName(mountPoint), out var unmountEvent))
        {
            return false;
        }

        using (unmountEvent)
        {
            unmountEvent.Set();
        }

        var root = mountPoint.TrimEnd('\\') + '\\';

        for (var attempt = 0; attempt < 100 && Directory.Exists(root); attempt++)
        {
            Thread.Sleep(100);
        }

        return true;
    }

    private static string FindFreeDriveLetter()
    {
        var usedDrives = Environment.GetLogicalDrives();

        for (var letter = 'Z'; letter >= 'D'; letter--)
        {
            var drive = $"{letter}:";

            if (!usedDrives.Contains($@"{drive}\", StringComparer.OrdinalIgnoreCase))
            {
                return drive;
            }
        }

        throw new InvalidOperationException("No free drive letter available.");
    }

    private static string GetUnmountEventName(string mountPoint)
    {
        return @"Local\WinCasc.Unmount." + mountPoint.TrimEnd('\\').ToUpperInvariant().Replace('\\', '/');
    }
}
