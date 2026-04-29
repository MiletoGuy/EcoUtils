using System.IO;
using System.Text.Json;
using System.Windows;
using EcoUtils.Infrastructure;
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

        CarregarConfiguracoes();

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

    private static void CarregarConfiguracoes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return;

        try
        {
            var json     = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<EcoSettings>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (settings is null) return;

            if (!string.IsNullOrWhiteSpace(settings.WindowsDir))
                EcoPathConstants.WindowsDir = settings.WindowsDir;
            if (!string.IsNullOrWhiteSpace(settings.DadosDir))
                EcoPathConstants.DadosDir = settings.DadosDir;
            if (!string.IsNullOrWhiteSpace(settings.LogsDir))
                EcoPathConstants.LogsDir = settings.LogsDir;
            if (!string.IsNullOrWhiteSpace(settings.EcoServerHost))
                EcoPathConstants.EcoServerHost = settings.EcoServerHost;
        }
        catch { /* Configuração ausente ou inválida — usa defaults hardcoded */ }
    }
}

