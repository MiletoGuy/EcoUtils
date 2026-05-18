using System.IO;
using System.Text.Json;
using System.Windows;
using EcoUtils.Infrastructure;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;
using EcoUtils.ViewModels;
using EcoUtils.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EcoUtils;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            try
            {
                var log = _services?.GetService<ILogService>();
                log?.Error("DispatcherUnhandledException", args.Exception);
            }
            catch { /* log falhou */ }

            MessageBox.Show(
                $"Ocorreu um erro inesperado:\n\n{args.Exception.Message}\n\n" +
                $"Tipo: {args.Exception.GetType().Name}",
                "EcoUtils — Erro inesperado",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        var splash = new SplashWindow();
        splash.Show();

        _ = IniciarAsync(splash);
    }

    private async Task IniciarAsync(SplashWindow splash)
    {
        try
        {
            CarregarConfiguracoes();

            splash.AtualizarStatus("Preparando ferramentas...");

            // Extrai binários Firebird embedados em background, sem bloquear a UI
            await EmbeddedToolsExtractor.EnsureExtractedAsync(
                new Progress<string>(f => splash.AtualizarStatus($"Extraindo {f}...")));

            splash.AtualizarStatus("Iniciando...");

            var sc = new ServiceCollection();

            // Infrastructure
            sc.AddSingleton<ILogService,            LogService>();

            // Data
            sc.AddSingleton<IInstanceRepository,    InstanceRepository>();

            // Domain services
            sc.AddSingleton<IVersionCatalogService,    VersionCatalogService>();
            sc.AddSingleton<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
            sc.AddSingleton<IDatabaseVersionService,   DatabaseVersionService>();
            sc.AddSingleton<IInstanceSetupService,     InstanceSetupService>();
            sc.AddSingleton<IDatabaseImportService,    DatabaseImportService>();
            sc.AddSingleton<IExecutableImportService,  ExecutableImportService>();
            sc.AddSingleton<IFileLockerService,        FileLockerService>();
            sc.AddSingleton<IRestoreService,           RestoreService>();
            sc.AddSingleton<IRestoreJobService,        RestoreJobService>();
            sc.AddSingleton<ILaunchService,            LaunchService>();
            sc.AddSingleton<IDialogService,            DialogService>();
            sc.AddSingleton<IUpdateService,            UpdateService>();
            sc.AddSingleton<IUserSettingsService,      UserSettingsService>();

            // ViewModels
            sc.AddSingleton<ExecutarEcoViewModel>();
            sc.AddSingleton<MainViewModel>();

            // Shell
            sc.AddSingleton<MainWindow>();

            _services = sc.BuildServiceProvider();

            var window = _services.GetRequiredService<MainWindow>();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Falha ao inicializar o EcoUtils:\n\n{ex.Message}",
                "EcoUtils — Erro na inicialização",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
        finally
        {
            splash.Close();
        }
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
            if (!string.IsNullOrWhiteSpace(settings.FirebirdUser))
                EcoPathConstants.FirebirdUser = settings.FirebirdUser;
            if (!string.IsNullOrWhiteSpace(settings.FirebirdPassword))
                EcoPathConstants.FirebirdPassword = settings.FirebirdPassword;
        }
        catch { /* Configuração ausente ou inválida — usa defaults hardcoded */ }
    }
}

