using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Fndr.Services;
using Fndr.ViewModels;

namespace Fndr.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        DataContext = _vm = vm;
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // ponytail: в Avalonia 12 у PointerPressedEventArgs нет InitialPressMouseButton, берём из Properties
        if (e.Properties.IsXButton1Pressed) { _vm.Back(); e.Handled = true; }
        else if (e.Properties.IsXButton2Pressed) { _vm.Forward(); e.Handled = true; }
    }

    private void OnPlace(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Place place }) _vm.Open(place.Path);
    }

    private void OnBack(object? sender, RoutedEventArgs e) => _vm.Back();
    private void OnUp(object? sender, RoutedEventArgs e) => _vm.Up();

    private void OnPathKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox box) _vm.Open(box.Text ?? "");
    }

    private void OnOpen(object? sender, TappedEventArgs e) => _vm.Enter((sender as ListBox)?.SelectedItem as Entry);

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        _vm.Select((sender as ListBox)?.SelectedItems?.OfType<Entry>().ToList() ?? []);

    private void OnListKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back) _vm.Up();
    }
}
