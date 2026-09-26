using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Fndr.Services;
using Fndr.ViewModels;
using Fndr.Views;
using Microsoft.Extensions.DependencyInjection;

var start = args.Length > 0 ? Path.GetFullPath(args[0]) : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

var services = new ServiceCollection()
    .AddSingleton<IFileSystem, FileSystem>()
    .AddTransient(_ => new MainViewModel(_.GetRequiredService<IFileSystem>(), start))
    .BuildServiceProvider();

var life = new ClassicDesktopStyleApplicationLifetime { Args = args, ShutdownMode = ShutdownMode.OnLastWindowClose };
AppBuilder.Configure<Fndr.App>().UsePlatformDetect()
    .SetupWithLifetime(life);
life.MainWindow = new MainWindow(services.GetRequiredService<MainViewModel>());
life.Start(args);
