using Avalonia;
using Avalonia.Markup.Xaml;

namespace Fndr;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
