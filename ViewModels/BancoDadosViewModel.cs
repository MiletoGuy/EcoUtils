using System.Collections.ObjectModel;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public enum EstadoConexao { Nenhum, Conectando, Conectado, Erro }

public class BancoDadosViewModel : ViewModelBase
{
    private readonly ISqlExecutionService    _executionService;
    private readonly IDatabaseDiscoveryService _discoveryService;
    private readonly IDialogService          _dialogService;

    // ── Sub-VMs ───────────────────────────────────────────────────────────────

    public SqlLivreViewModel SqlLivreVM { get; }

    // ── Bancos disponíveis ────────────────────────────────────────────────────

    public ObservableCollection<EcoDatabase> BancosDisponiveis { get; } = [];

    private EcoDatabase? _bancoSelecionado;
    public EcoDatabase? BancoSelecionado
    {
        get => _bancoSelecionado;
        set
        {
            if (_bancoSelecionado == value) return;

            // Trocar banco com transação aberta exige confirmação
            if (_executionService.TransacaoPendente)
            {
                bool confirmar = _dialogService.Confirmar(
                    "Transação pendente",
                    "Há uma transação aberta. Trocar de banco fará rollback automático. Deseja continuar?",
                    "Trocar banco (rollback)");
                if (!confirmar) return;

                _ = RollbackSilenciosoAsync();
            }

            SetProperty(ref _bancoSelecionado, value);
            _executionService.DefinirBancoAtivo(value?.EcoPath);
            _ = TestarConexaoAsync();
        }
    }

    // ── Status de conexão ─────────────────────────────────────────────────────

    private EstadoConexao _estadoConexao = EstadoConexao.Nenhum;
    public EstadoConexao EstadoConexao
    {
        get => _estadoConexao;
        private set
        {
            if (SetProperty(ref _estadoConexao, value))
            {
                OnPropertyChanged(nameof(ConexaoOk));
                OnPropertyChanged(nameof(ConexaoErro));
                OnPropertyChanged(nameof(ConexaoConectando));
                OnPropertyChanged(nameof(TextoStatusConexao));
            }
        }
    }

    public bool ConexaoOk         => _estadoConexao == EstadoConexao.Conectado;
    public bool ConexaoErro       => _estadoConexao == EstadoConexao.Erro;
    public bool ConexaoConectando => _estadoConexao == EstadoConexao.Conectando;

    public string TextoStatusConexao => _estadoConexao switch
    {
        EstadoConexao.Conectado  => "conectado",
        EstadoConexao.Erro       => "erro",
        EstadoConexao.Conectando => "conectando...",
        _                        => string.Empty
    };

    // ── Transação ─────────────────────────────────────────────────────────────

    public bool TransacaoPendente => _executionService.TransacaoPendente;

    // ── SQL Livre flyout ──────────────────────────────────────────────────────

    private bool _sqlLivreAberto;
    public bool SqlLivreAberto
    {
        get => _sqlLivreAberto;
        set => SetProperty(ref _sqlLivreAberto, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand CommitCommand          { get; }
    public ICommand RollbackCommand        { get; }
    public ICommand AbrirSqlLivreCommand   { get; }
    public ICommand CarregarBancosCommand  { get; }

    // ── Ctor ──────────────────────────────────────────────────────────────────

    public BancoDadosViewModel(
        ISqlExecutionService     executionService,
        IDatabaseDiscoveryService discoveryService,
        ISqlExportService        exportService,
        IDialogService           dialogService,
        IUserSettingsService     userSettingsService)
    {
        _executionService  = executionService;
        _discoveryService  = discoveryService;
        _dialogService     = dialogService;

        SqlLivreVM = new SqlLivreViewModel(executionService, exportService, dialogService, userSettingsService);

        _executionService.TransacaoPendenteChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TransacaoPendente));
            (CommitCommand  as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (RollbackCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        };

        CommitCommand = new AsyncRelayCommand(
            async _ =>
            {
                await _executionService.CommitAsync();
            },
            _ => _executionService.TransacaoPendente);

        RollbackCommand = new AsyncRelayCommand(
            async _ =>
            {
                bool confirmar = _dialogService.Confirmar(
                    "Confirmar Rollback",
                    "Desfazer todas as alterações da transação atual?",
                    "Rollback");
                if (!confirmar) return;
                await _executionService.RollbackAsync();
            },
            _ => _executionService.TransacaoPendente);

        AbrirSqlLivreCommand = new RelayCommand(_ => SqlLivreAberto = !SqlLivreAberto);

        CarregarBancosCommand = new AsyncRelayCommand(async _ => await CarregarBancosAsync());

        _ = CarregarBancosAsync();
    }

    // ── Carregamento de bancos ────────────────────────────────────────────────

    private async Task CarregarBancosAsync()
    {
        var bancos = await _discoveryService.ListarBancosAsync();
        BancosDisponiveis.Clear();
        foreach (var b in bancos)
            BancosDisponiveis.Add(b);
    }

    // ── Teste de conexão ──────────────────────────────────────────────────────

    private async Task TestarConexaoAsync()
    {
        if (_bancoSelecionado is null)
        {
            EstadoConexao = EstadoConexao.Nenhum;
            return;
        }

        EstadoConexao = EstadoConexao.Conectando;
        bool ok = await _executionService.TestarConexaoAsync(_bancoSelecionado.EcoPath);
        EstadoConexao = ok ? EstadoConexao.Conectado : EstadoConexao.Erro;
    }

    private async Task RollbackSilenciosoAsync()
    {
        try { await _executionService.RollbackAsync(); }
        catch { /* silencioso — contexto de troca de banco */ }
    }
}
