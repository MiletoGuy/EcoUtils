using System.Windows;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;
using EcoUtils.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EcoUtils;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var sc = new ServiceCollection();

        // Infrastructure
        sc.AddSingleton<ILogService,            LogService>();

        // Data
        sc.AddSingleton<IInstanceRepository,    InstanceRepository>();

        // Domain services
        sc.AddSingleton<IVersionCatalogService,    VersionCatalogService>();
        sc.AddSingleton<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
        sc.AddSingleton<IInstanceSetupService,     InstanceSetupService>();
        sc.AddSingleton<ILaunchService,            LaunchService>();
        sc.AddSingleton<IDialogService,            DialogService>();

        // ViewModels
        sc.AddSingleton<ExecutarEcoViewModel>();
        sc.AddSingleton<MainViewModel>();

        // Shell
        sc.AddSingleton<MainWindow>();

        _services = sc.BuildServiceProvider();

        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}

