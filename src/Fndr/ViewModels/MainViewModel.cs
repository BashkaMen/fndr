using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Fndr.Services;

namespace Fndr.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFileSystem _fs;
    private readonly Stack<string> _back = new();
    private readonly Stack<string> _forward = new();
    private string _path = "";
    private string _status = "";

    public MainViewModel(IFileSystem fs, string start) { _fs = fs; Open(start); }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path { get => _path; private set => Set(ref _path, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool ShowHidden { get; set; }
    public ObservableCollection<Entry> Entries { get; } = [];

    public void Open(string dir, bool? hidden = null)
    {
        if (hidden is bool h) ShowHidden = h;
        if (!System.IO.Path.IsPathRooted(dir) && Path.Length > 0) dir = System.IO.Path.Combine(Path, dir);
        dir = System.IO.Path.GetFullPath(dir);
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
        if (e.IsDir) { Open(System.IO.Path.Combine(Path, e.Name)); return; }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(System.IO.Path.Combine(Path, e.Name)) { UseShellExecute = true });
        }
        catch (Exception ex) { Status = $"не открылось: {ex.Message}"; }
    }

    public void Back()
    {
        while (_back.TryPop(out var prev) && Directory.Exists(prev))
        {
            if (TryList(prev, out var items))
            {
                _forward.Push(Path);
                Apply(prev, items);
                return;
            }
        }
    }

    public void Forward()
    {
        while (_forward.TryPop(out var next) && Directory.Exists(next))
        {
            if (TryList(next, out var items))
            {
                _back.Push(Path);
                Apply(next, items);
                return;
            }
        }
    }

    public void Up()
    {
        var p = _fs.Parent(Path);
        if (p is not null) Open(p);
    }

    bool TryList(string dir, out IReadOnlyList<Entry> items)
    {
        try
        {
            items = _fs.List(dir, ShowHidden);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            items = Array.Empty<Entry>();
            Status = "нет доступа";
            return false;
        }
        catch (IOException)
        {
            items = Array.Empty<Entry>();
            Status = "папка недоступна";
            return false;
        }
    }

    void Apply(string dir, IReadOnlyList<Entry> items)
    {
        Path = dir;
        Entries.Clear();

        int files = 0;
        foreach (var e in items)
        {
            Entries.Add(e);
            if (!e.IsDir) files++;
        }

        Status = $"Итого файлов: {files}";
    }

    void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
