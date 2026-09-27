using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Fndr.Services;
using IOPath = System.IO.Path;

namespace Fndr.ViewModels;

public record Place(string Title, string Path);

public record StartDir(string Path);

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFileSystem _fs;
    private readonly Stack<string> _back = new();
    private readonly Stack<string> _forward = new();

    public MainViewModel(IFileSystem fs, StartDir start)
    {
        _fs = fs;
        Open(start.Path);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path { get; private set => Set(ref field, value); } = "";
    public string Status { get; private set => Set(ref field, value); } = "";
    public string Selection { get; private set => Set(ref field, value); } = "";
    public ObservableCollection<Entry> Entries { get; } = [];

    public bool ShowHidden
    {
        get;
        set { if (Set(ref field, value)) Open(Path); }
    }

    public IReadOnlyList<Place> Places { get; } =
    [
        new("⌂  Домой", Folder(Environment.SpecialFolder.UserProfile)),
        new("☰  Документы", Folder(Environment.SpecialFolder.MyDocuments)),
        new("↓  Загрузки", IOPath.Combine(Folder(Environment.SpecialFolder.UserProfile), "Downloads")),
        new("♪  Музыка", Folder(Environment.SpecialFolder.MyMusic)),
        new("▣  Картинки", Folder(Environment.SpecialFolder.MyPictures)),
        new("▶  Видео", Folder(Environment.SpecialFolder.MyVideos)),
    ];

    public void Open(string dir)
    {
        dir = Path.Length > 0 ? IOPath.GetFullPath(dir, Path) : IOPath.GetFullPath(dir);
        if (!Directory.Exists(dir)) { Status = "нет такой папки"; return; }
        if (!TryList(dir, out var items)) return;

        if (Path.Length > 0 && Path != dir)
        {
            _back.Push(Path);
            _forward.Clear();
        }

        Apply(dir, items);
    }

    public void Enter(Entry? e)
    {
        if (e is null) return;

        var full = IOPath.Combine(Path, e.Name);
        if (e.IsDir) { Open(full); return; }

        try { Process.Start(new ProcessStartInfo(full) { UseShellExecute = true }); }
        catch (Exception ex) { Status = $"не открылось: {ex.Message}"; }
    }

    public void Select(IReadOnlyCollection<Entry> selected)
    {
        var files = selected.Where(e => !e.IsDir).ToList();
        Selection = (selected.Count, files.Count) switch
        {
            (0, _) => "",
            (var n, 0) => $"Выделено: {n}",
            var (n, _) => $"Выделено: {n} ({Entry.FormatSize(files.Sum(e => e.Size))})",
        };
    }

    public void Back() => Navigate(_back, _forward);
    public void Forward() => Navigate(_forward, _back);

    public void Up()
    {
        if (_fs.Parent(Path) is { } parent) Open(parent);
    }

    private void Navigate(Stack<string> from, Stack<string> to)
    {
        while (from.TryPop(out var dir) && Directory.Exists(dir))
        {
            if (!TryList(dir, out var items)) continue;
            to.Push(Path);
            Apply(dir, items);
            return;
        }
    }

    private bool TryList(string dir, out IReadOnlyList<Entry> items)
    {
        try
        {
            items = _fs.List(dir, ShowHidden);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            items = [];
            Status = ex is UnauthorizedAccessException ? "нет доступа" : "папка недоступна";
            return false;
        }
    }

    private void Apply(string dir, IReadOnlyList<Entry> items)
    {
        Path = dir;
        Entries.Clear();
        foreach (var e in items) Entries.Add(e);
        var dirs = items.Count(e => e.IsDir);
        var files = items.Count - dirs;
        Status = $"Итого: {Plural(dirs, "папка", "папки", "папок")}, {Plural(files, "файл", "файла", "файлов")}";
    }

    private static string Plural(int n, string one, string few, string many) => (n % 10, n % 100) switch
    {
        (1, not 11) => $"{n} {one}",
        (>= 2 and <= 4, < 12 or > 14) => $"{n} {few}",
        _ => $"{n} {many}",
    };

    private static string Folder(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
