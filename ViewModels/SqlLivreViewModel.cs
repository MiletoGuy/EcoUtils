using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class SqlLivreViewModel : ViewModelBase
{
    private readonly ISqlExecutionService _executionService;
    private readonly IUserSettingsService _userSettingsService;

    // Resultado integrado
    public SqlResultViewModel ResultadoVM { get; }

    // ── Query ─────────────────────────────────────────────────────────────────

    private string _textoQuery = string.Empty;
    public string TextoQuery
    {
        get => _textoQuery;
        set => SetProperty(ref _textoQuery, value);
    }

    // ── Estado ────────────────────────────────────────────────────────────────

    private bool _executando;
    public bool Executando
    {
        get => _executando;
        private set
        {
            if (SetProperty(ref _executando, value))
                (ExecutarCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand ExecutarCommand { get; }

    // ── Ctor ──────────────────────────────────────────────────────────────────

    public SqlLivreViewModel(
        ISqlExecutionService executionService,
        ISqlExportService    exportService,
        IDialogService       dialogService,
        IUserSettingsService userSettingsService)
    {
        _executionService    = executionService;
        _userSettingsService = userSettingsService;

        ResultadoVM = new SqlResultViewModel(exportService, dialogService);

        ExecutarCommand = new AsyncRelayCommand(
            async _ => await ExecutarAsync(),
            _ => !_executando && !string.IsNullOrWhiteSpace(_textoQuery));
    }

    // ── Execução ──────────────────────────────────────────────────────────────

    public async Task ExecutarAsync()
    {
        if (string.IsNullOrWhiteSpace(_textoQuery)) return;

        Executando = true;
        try
        {
            int? limite = _userSettingsService.Settings.LimiteLinhasConsulta > 0
                ? _userSettingsService.Settings.LimiteLinhasConsulta
                : null;

            var result = await _executionService.ExecutarAsync(_textoQuery.Trim(), limite);
            ResultadoVM.CarregarResultado(result);
        }
        finally
        {
            Executando = false;
        }
    }
}
