using Microsoft.Win32;

namespace WinCasc;

internal static class ShellRegistration
{
    private const string MountVerbName = "WinCasc.Mount";
    private const string UnmountVerbName = "WinCasc.Unmount";
    private const string UnmountVerbParent = @"Software\Classes\Drive\shell";

    private static readonly string[] MountVerbParents = [@"Software\Classes\Directory\shell", @"Software\Classes\Directory\Background\shell"];

    public static void Register()
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, "WinCascShell.exe");

        foreach (var parent in MountVerbParents)
        {
            RegisterVerb($@"{parent}\{MountVerbName}", "Mount CASC storage", exePath, $"\"{exePath}\" mount \"%V\" --background");
        }

        RegisterVerb($@"{UnmountVerbParent}\{UnmountVerbName}", "Unmount CASC storage", exePath, $"\"{exePath}\" unmount %V");
    }

    public static void Unregister()
    {
        foreach (var parent in MountVerbParents)
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"{parent}\{MountVerbName}", false);
        }

        Registry.CurrentUser.DeleteSubKeyTree($@"{UnmountVerbParent}\{UnmountVerbName}", false);
    }

    private static void RegisterVerb(string keyPath, string text, string iconPath, string command)
    {
        using var verbKey = Registry.CurrentUser.CreateSubKey(keyPath);
        verbKey.SetValue(null, text);
        verbKey.SetValue("Icon", iconPath);

        using var commandKey = verbKey.CreateSubKey("command");
        commandKey.SetValue(null, command);
    }
}
