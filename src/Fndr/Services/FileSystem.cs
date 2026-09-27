using Dunet;

namespace Fndr.Services;

public record Entry(string Name, bool IsDir, long Size, DateTime Modified)
{
    public string Glyph => IsDir ? "📁" : "📄";
    public string SizeText => IsDir ? "" : FormatSize(Size);
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm");

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} Б";
        string[] units = ["КБ", "МБ", "ГБ", "ТБ"];
        double value = bytes;
        var i = -1;
        do { value /= 1024; i++; } while (value >= 1024 && i < units.Length - 1);
        return $"{value:0.#} {units[i]}";
    }
}

[Union]
public partial record FsError
{
    public partial record InvalidPath(string Path);
    public partial record NotFound(string Path);
    public partial record AccessDenied(string Path);
    public partial record Unavailable(string Path, string Message);
}

public interface IFileSystem
{
    Result<string, FsError> ResolveDir(string path, string? relativeTo = null);
    Result<IReadOnlyList<Entry>, FsError> List(string dir, bool hidden = false);
    string? Parent(string dir);
}

public class FileSystem : IFileSystem
{
    private const FileAttributes HiddenMask = FileAttributes.Hidden | FileAttributes.System;

    public Result<string, FsError> ResolveDir(string path, string? relativeTo = null)
    {
        string full;
        try { full = relativeTo is null ? Path.GetFullPath(path) : Path.GetFullPath(path, relativeTo); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new FsError.InvalidPath(path);
        }

        return Directory.Exists(full) ? full : new FsError.NotFound(full);
    }

    public Result<IReadOnlyList<Entry>, FsError> List(string dir, bool hidden = false)
    {
        try
        {
            var entries = new DirectoryInfo(dir).EnumerateFileSystemInfos()
                .Where(e => hidden || (!e.Name.StartsWith('.') && (e.Attributes & HiddenMask) == 0))
                .OrderBy(e => e is FileInfo)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Select(e => new Entry(e.Name, e is DirectoryInfo, (e as FileInfo)?.Length ?? 0, e.LastWriteTime))
                .ToList();
            return new Result<IReadOnlyList<Entry>, FsError>.Ok(entries);
        }
        catch (DirectoryNotFoundException) { return new FsError.NotFound(dir); }
        catch (UnauthorizedAccessException) { return new FsError.AccessDenied(dir); }
        catch (IOException ex) { return new FsError.Unavailable(dir, ex.Message); }
    }

    public string? Parent(string dir) => Directory.GetParent(dir)?.FullName;
}
