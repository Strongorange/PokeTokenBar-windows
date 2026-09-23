namespace PokeTokenBar.Platform.Windows;

public interface IWslDistroSource
{
    IReadOnlyList<string> DistributionNames();
}

public sealed class RegistryWslDistroSource : IWslDistroSource
{
    public const string LxssRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    public IReadOnlyList<string> DistributionNames()
    {
        if (!OperatingSystem.IsWindows()) return [];
        try
        {
            using var lxss = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(LxssRegistryPath);
            if (lxss is null) return [];
            var names = new List<string>();
            foreach (var subKeyName in lxss.GetSubKeyNames())
            {
                using var distro = lxss.OpenSubKey(subKeyName);
                if (distro?.GetValue("DistributionName") is string name && !string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
            return names;
        }
        catch
        {
            return [];
        }
    }
}
