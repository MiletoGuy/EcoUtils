using System.Collections.ObjectModel;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class SqlDetailViewModel : ViewModelBase
{
    private readonly ISqlExecutionService _executionService;
    private readonly IUserSettingsService _settingsService;
    private readonly IDialogService       _dialogService;

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
                ReconstruirParametros();
                ResultadoVM.Limpar();
                (ExecutarCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (EditarCommand   as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool EntradaDefinida  => _entrada is not null;
    public bool PossuiParametros => Parametros.Count > 0;

    // ── Parâmetros ────────────────────────────────────────────────────────────

    public ObservableCollection<SqlParameterInstance> Parametros { get; } = [];

    private void ReconstruirParametros()
    {
        Parametros.Clear();
        if (_entrada is null) return;
        foreach (var p in _entrada.Parametros)
            Parametros.Add(new SqlParameterInstance(p));
    }

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
    public ICommand EditarCommand   { get; }

    // ── Ctor ──────────────────────────────────────────────────────────────────

    public SqlDetailViewModel(
        ISqlExecutionService executionService,
        ISqlExportService    exportService,
        IDialogService       dialogService,
        IUserSettingsService settingsService,
        Action<SqlEntry>     onEditar)
    {
        _executionService = executionService;
        _settingsService  = settingsService;
        _dialogService    = dialogService;
        ResultadoVM       = new SqlResultViewModel(exportService, dialogService);

        EditarCommand = new RelayCommand(
            _ => onEditar(_entrada!),
            _ => _entrada is not null);

        ExecutarCommand = new AsyncRelayCommand(
            async _ =>
            {
                if (_entrada is null) return;
                Executando = true;
                try
                {
                    var limite = _settingsService.Settings.LimiteLinhasConsulta;

                    SqlExecutionResult result;
                    if (Parametros.Count == 0)
                    {
                        result = await _executionService.ExecutarAsync(_entrada.CorpoSql, limite);
                    }
                    else
                    {
                        // Valida e converte todos os parâmetros antes de executar
                        var valores = new List<(string nome, object? valor)>(Parametros.Count);
                        foreach (var inst in Parametros)
                        {
                            var (ok, erro, valor) = inst.TentarConverter();
                            if (!ok)
                            {
                                _dialogService.Notificar("Parâmetro inválido", erro ?? "Valor inválido.");
                                return;
                            }
                            valores.Add((inst.Definicao.Nome, valor));
                        }
                        result = await _executionService.ExecutarAsync(_entrada.CorpoSql, valores, limite);
                    }

                    ResultadoVM.CarregarResultado(result);
                }
                finally { Executando = false; }
            },
            _ => _entrada is not null && !_executando);
    }
}
