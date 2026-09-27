namespace Fndr.Services;

public record Entry(string Name, bool IsDir, long Size, DateTime Modified)
{
    public string Glyph => IsDir ? "📁" : "📄";
    public string SizeText => IsDir ? "" : FormatSize(Size);
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm");

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} Б";
        string[] units = ["КБ", "МБ", "ГБ", "ТБ"];
        double value = bytes;
        var i = -1;
        do { value /= 1024; i++; } while (value >= 1024 && i < units.Length - 1);
        return $"{value:0.#} {units[i]}";
    }
}

public interface IFileSystem
{
    IReadOnlyList<Entry> List(string dir, bool hidden = false);
    string? Parent(string dir);
}

public class FileSystem : IFileSystem
{
    private const FileAttributes HiddenMask = FileAttributes.Hidden | FileAttributes.System;

    public IReadOnlyList<Entry> List(string dir, bool hidden = false) =>
        new DirectoryInfo(dir).EnumerateFileSystemInfos()
            .Where(e => hidden || (!e.Name.StartsWith('.') && (e.Attributes & HiddenMask) == 0))
            .OrderBy(e => e is FileInfo)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(e => new Entry(e.Name, e is DirectoryInfo, (e as FileInfo)?.Length ?? 0, e.LastWriteTime))
            .ToList();

    public string? Parent(string dir) => Directory.GetParent(dir)?.FullName;
}
