using System.ComponentModel;
using System.Diagnostics;
using Dunet;

namespace Fndr.Services;

[Union]
public partial record LaunchError
{
    public partial record NotFound(string Path);
    public partial record Failed(string Path, string Message);
}

public interface ILauncher
{
    Result<Unit, LaunchError> Open(string path);
}

public class ShellLauncher : ILauncher
{
    public Result<Unit, LaunchError> Open(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return new LaunchError.NotFound(path);

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
            return new Unit();
        }
        catch (Exception ex) when (ex is Win32Exception or PlatformNotSupportedException)
        {
            return new LaunchError.Failed(path, ex.Message);
        }
    }
}
