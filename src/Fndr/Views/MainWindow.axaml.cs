using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Fndr.Services;
using Fndr.ViewModels;

namespace Fndr.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        DataContext = vm;
        AvaloniaXamlLoader.Load(this);
    }
    private MainViewModel Vm => (MainViewModel)DataContext!;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // ponytail: в Avalonia 12 у PointerPressedEventArgs нет InitialPressMouseButton, берём из Properties
        if (e.Properties.IsXButton1Pressed) { Vm.Back(); e.Handled = true; }
        else if (e.Properties.IsXButton2Pressed) { Vm.Forward(); e.Handled = true; }
    }

    void Go(Environment.SpecialFolder folder) => Vm.Open(Environment.GetFolderPath(folder));
    void OnHome(object? s, RoutedEventArgs e) => Go(Environment.SpecialFolder.UserProfile);
    void OnDocs(object? s, RoutedEventArgs e) => Go(Environment.SpecialFolder.MyDocuments);
    void OnDownloads(object? s, RoutedEventArgs e) =>
        Vm.Open(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
    void OnMusic(object? s, RoutedEventArgs e) => Go(Environment.SpecialFolder.MyMusic);
    void OnPictures(object? s, RoutedEventArgs e) => Go(Environment.SpecialFolder.MyPictures);
    void OnVideos(object? s, RoutedEventArgs e) => Go(Environment.SpecialFolder.MyVideos);
    void OnBack(object? s, RoutedEventArgs e) => Vm.Back();
    void OnUp(object? s, RoutedEventArgs e) => Vm.Up();

    void OnHidden(object? sender, RoutedEventArgs e) =>
        Vm.Open(Vm.Path, sender is CheckBox c && c.IsChecked == true);

    void OnPathKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox box) return;
        Vm.Open(box.Text ?? "", Vm.ShowHidden);
    }

    void OnOpen(object? sender, TappedEventArgs e) => Vm.Enter(sender is ListBox list ? list.SelectedItem as Entry : null);

    void OnListKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Back) return;
        Vm.Up();
    }
}
