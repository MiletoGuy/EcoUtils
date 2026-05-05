using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class SqlDetailViewModel : ViewModelBase
{
    private readonly ISqlExecutionService _executionService;
    private readonly IUserSettingsService _settingsService;

    // ── Sub-VM ────────────────────────────────────────────────────────────────

    public SqlResultViewModel ResultadoVM { get; }

    // ── Entrada selecionada ───────────────────────────────────────────────────

    private SqlEntry? _entrada;
    public SqlEntry? Entrada
    {
        get => _entrada;
        set
        {
            if (SetProperty(ref _entrada, value))
            {
                OnPropertyChanged(nameof(EntradaDefinida));
                OnPropertyChanged(nameof(PossuiParametros));
                OnPropertyChanged(nameof(TextoParametros));
                ResultadoVM.Limpar();
                (ExecutarCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool   EntradaDefinida  => _entrada is not null;
    public bool   PossuiParametros => _entrada?.Parametros.Count > 0;

    public string TextoParametros => _entrada?.Parametros.Count switch
    {
        null or 0 => string.Empty,
        1         => "1 parâmetro necessário (disponível em breve)",
        int n     => $"{n} parâmetros necessários (disponíveis em breve)"
    };

    // ── Executando ────────────────────────────────────────────────────────────

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

    public SqlDetailViewModel(
        ISqlExecutionService executionService,
        ISqlExportService    exportService,
        IDialogService       dialogService,
        IUserSettingsService settingsService)
    {
        _executionService = executionService;
        _settingsService  = settingsService;
        ResultadoVM       = new SqlResultViewModel(exportService, dialogService);

        ExecutarCommand = new AsyncRelayCommand(
            async _ =>
            {
                if (_entrada is null) return;
                Executando = true;
                try
                {
                    var limite = _settingsService.Settings.LimiteLinhasConsulta;
                    var result = await _executionService.ExecutarAsync(_entrada.CorpoSql, limite);
                    ResultadoVM.CarregarResultado(result);
                }
                finally { Executando = false; }
            },
            _ => _entrada is not null
              && _entrada.Parametros.Count == 0
              && !_executando);
    }
}
