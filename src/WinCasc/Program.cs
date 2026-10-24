using System.Diagnostics;

namespace WinCasc;

internal static class Program
{
    public static int Main(string[] args)
    {
        switch (args.FirstOrDefault())
        {
            case "mount" when args.Length >= 2:
                return Mount(ParseMountOptions(args));
            case "unmount" when args.Length == 2 && args[1] == "--all":
                return UnmountAll();
            case "unmount" when args.Length == 2:
                return Unmount(args[1]);
            case "register":
                ShellRegistration.Register();
                Console.WriteLine("Explorer context menu entries registered.");
                return 0;
            case "unregister":
                ShellRegistration.Unregister();
                Console.WriteLine("Explorer context menu entries removed.");
                return 0;
            default:
                PrintUsage();
                return 1;
        }
    }

    private static int Mount(MountOptions options)
    {
        if (options.Detach)
        {
            StartBackgroundMount(options);
            return 0;
        }

        try
        {
            MountSession.Run(options);
            return 0;
        }
        catch (Exception ex) when (options.Background)
        {
            User32.MessageBox(IntPtr.Zero, ex.Message, "WinCasc", User32.MB_ICONERROR);
            return 1;
        }
    }

    private static int Unmount(string mountPoint)
    {
        if (!MountSession.Unmount(mountPoint))
        {
            Console.Error.WriteLine($"'{mountPoint}' is not mounted by WinCasc.");
            return 1;
        }

        Console.WriteLine($"Unmounted {mountPoint}.");
        return 0;
    }

    private static int UnmountAll()
    {
        MountSession.UnmountAll();
        Console.WriteLine("Unmounted all WinCasc mounts.");
        return 0;
    }

    private static void StartBackgroundMount(MountOptions options)
    {
        var startInfo = new ProcessStartInfo(Environment.ProcessPath!) { CreateNoWindow = true, UseShellExecute = false };
        startInfo.ArgumentList.Add("mount");
        startInfo.ArgumentList.Add(options.StoragePath);

        if (options.MountPoint != null)
        {
            startInfo.ArgumentList.Add(options.MountPoint);
        }

        if (options.ListFilePath != null)
        {
            startInfo.ArgumentList.Add("--listfile");
            startInfo.ArgumentList.Add(options.ListFilePath);
        }

        startInfo.ArgumentList.Add("--background");
        Process.Start(startInfo);
    }

    private static MountOptions ParseMountOptions(string[] args)
    {
        string? mountPoint = null;
        string? listFilePath = null;
        var detach = false;
        var background = false;

        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--listfile" when i + 1 < args.Length:
                    listFilePath = Path.GetFullPath(args[++i]);
                    break;
                case "--detach":
                    detach = true;
                    break;
                case "--background":
                    background = true;
                    break;
                default:
                    mountPoint = args[i];
                    break;
            }
        }

        return new MountOptions(args[1], mountPoint, listFilePath, detach, background);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  WinCasc mount <storage path> [drive letter or empty folder] [--listfile <file>] [--detach]");
        Console.WriteLine("  WinCasc unmount <mount point | --all>");
        Console.WriteLine("  WinCasc register");
        Console.WriteLine("  WinCasc unregister");
    }
}
