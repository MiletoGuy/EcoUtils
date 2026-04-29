using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class ExecutarEcoViewModel : ViewModelBase
{
    private readonly IInstanceRepository        _instanceRepository;
    private readonly IVersionCatalogService     _versionCatalogService;
    private readonly IDatabaseDiscoveryService  _databaseDiscoveryService;
    private readonly IDatabaseVersionService    _databaseVersionService;
    private readonly IInstanceSetupService      _instanceSetupService;
    private readonly ILaunchService            _launchService;
    private readonly IDialogService            _dialogService;
    private readonly ILogService               _log;

    public ObservableCollection<EcoInstance> Instancias { get; }

    private readonly ICollectionView _instanciasView;
    public ICollectionView InstanciasView => _instanciasView;

    public bool ListaVazia => !Instancias.Any();

    // ── Pesquisa ────────────────────────────────────────────────
    private string _filtroTexto = string.Empty;
    public string FiltroTexto
    {
        get => _filtroTexto;
        set { SetProperty(ref _filtroTexto, value); _instanciasView.Refresh(); }
    }

    // ── Ordenação ───────────────────────────────────────────────
    private string _ordenacaoColuna = nameof(EcoInstance.Apelido);
    public string OrdenacaoColuna
    {
        get => _ordenacaoColuna;
        private set => SetProperty(ref _ordenacaoColuna, value);
    }

    private bool _ordenacaoAscendente = true;
    public bool OrdenacaoAscendente
    {
        get => _ordenacaoAscendente;
        private set => SetProperty(ref _ordenacaoAscendente, value);
    }

    // ── Visibilidade de colunas ──────────────────────────────────
    private bool _mostrarExecutavel = false;
    public bool MostrarExecutavel
    {
        get => _mostrarExecutavel;
        set
        {
            if (!SetProperty(ref _mostrarExecutavel, value)) return;
            if (!value)
            {
                if (_colWidthExecutavel.Value > 0)
                    _storedWidthExecutavel = _colWidthExecutavel;
                ColWidthExecutavel = new GridLength(0);
            }
            else
                ColWidthExecutavel = _storedWidthExecutavel;
            OnPropertyChanged(nameof(MostrarSplitterExecutavelBanco));
        }
    }

    private bool _mostrarBanco = false;
    public bool MostrarBanco
    {
        get => _mostrarBanco;
        set
        {
            if (!SetProperty(ref _mostrarBanco, value)) return;
            if (!value)
            {
                if (_colWidthBanco.Value > 0)
                    _storedWidthBanco = _colWidthBanco;
                ColWidthBanco = new GridLength(0);
            }
            else
                ColWidthBanco = _storedWidthBanco;
            OnPropertyChanged(nameof(MostrarSplitterExecutavelBanco));
        }
    }

    private bool _mostrarVersao = true;
    public bool MostrarVersao
    {
        get => _mostrarVersao;
        set => SetProperty(ref _mostrarVersao, value);
    }

    // ── Larguras de colunas (resizáveis) ─────────────────────
    private GridLength _colWidthApelido = new GridLength(2, GridUnitType.Star);
    public  GridLength  ColWidthApelido
    {
        get => _colWidthApelido;
        set => SetProperty(ref _colWidthApelido, value);
    }

    private GridLength _storedWidthExecutavel = new GridLength(2, GridUnitType.Star);
    private GridLength _colWidthExecutavel    = new GridLength(0);
    public  GridLength  ColWidthExecutavel
    {
        get => _colWidthExecutavel;
        set => SetProperty(ref _colWidthExecutavel, value);
    }

    private GridLength _storedWidthBanco = new GridLength(2, GridUnitType.Star);
    private GridLength _colWidthBanco    = new GridLength(0);
    public  GridLength  ColWidthBanco
    {
        get => _colWidthBanco;
        set => SetProperty(ref _colWidthBanco, value);
    }

    public bool MostrarSplitterExecutavelBanco => _mostrarExecutavel && _mostrarBanco;

    // ── Config de colunas popup ──────────────────────────────────
    private bool _configColunasAberto;
    public bool ConfigColunasAberto
    {
        get => _configColunasAberto;
        set => SetProperty(ref _configColunasAberto, value);
    }

    private bool _flyoutAberto;
    public bool FlyoutAberto
    {
        get => _flyoutAberto;
        set => SetProperty(ref _flyoutAberto, value);
    }

    private InstanceFlyoutViewModel? _flyoutVm;
    public InstanceFlyoutViewModel? FlyoutVM
    {
        get => _flyoutVm;
        set => SetProperty(ref _flyoutVm, value);
    }

    public ICommand AdicionarCommand          { get; }
    public ICommand EditarCommand             { get; }
    public ICommand ExcluirCommand            { get; }
    public ICommand ExecutarCommand           { get; }
    public ICommand OrdenarCommand            { get; }
    public ICommand ToggleConfigColunasCommand { get; }

    public ExecutarEcoViewModel(
        IInstanceRepository instanceRepository,
        IVersionCatalogService versionCatalogService,
        IDatabaseDiscoveryService databaseDiscoveryService,
        IDatabaseVersionService databaseVersionService,
        IInstanceSetupService instanceSetupService,
        ILaunchService launchService,
        IDialogService dialogService,
        ILogService log)
    {
        _instanceRepository       = instanceRepository;
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _databaseVersionService   = databaseVersionService;
        _instanceSetupService     = instanceSetupService;
        _launchService            = launchService;
        _dialogService            = dialogService;
        _log                      = log;

        Instancias = new ObservableCollection<EcoInstance>();
        Instancias.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ListaVazia));

        _instanciasView = CollectionViewSource.GetDefaultView(Instancias);
        _instanciasView.Filter = o => FiltrarInstancia((EcoInstance)o);
        AplicarOrdenacao();

        AdicionarCommand           = new RelayCommand(_ => AbrirFlyoutNovo());
        EditarCommand              = new RelayCommand(inst => AbrirFlyoutEditar((EcoInstance)inst!));
        ExcluirCommand             = new AsyncRelayCommand(
            async inst => await ExcluirInstanciaAsync((EcoInstance)inst!),
            onError: ex => _log.Error(nameof(ExcluirInstanciaAsync), ex));
        ExecutarCommand            = new AsyncRelayCommand(
            async inst => await IniciarEcoAsync((EcoInstance)inst!),
            onError: ex => _log.Error(nameof(IniciarEcoAsync), ex));
        OrdenarCommand             = new RelayCommand(col => AplicarOrdenacaoPorColuna((string)col!));
        ToggleConfigColunasCommand = new RelayCommand(_ => ConfigColunasAberto = !ConfigColunasAberto);

        _ = CarregarInstanciasAsync();
    }

    private async Task CarregarInstanciasAsync()
    {
        try
        {
            var lista = await _instanceRepository.CarregarAsync();
            foreach (var inst in lista)
                Instancias.Add(inst);
        }
        catch (Exception ex)
        {
            _log.Error(nameof(CarregarInstanciasAsync), ex);
            _dialogService.Notificar("Erro ao carregar instâncias",
                "Não foi possível carregar a lista de instâncias. Verifique os logs.");
        }
    }

    private void AbrirFlyoutNovo()
    {
        var apelidosExistentes = Instancias.Select(i => i.Apelido).ToList();
        FlyoutVM = new InstanceFlyoutViewModel(
            _versionCatalogService,
            _databaseDiscoveryService,
            _databaseVersionService,
            _instanceSetupService,
            async instancia =>
            {
                Instancias.Add(instancia);
                await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
            },
            () => FlyoutAberto = false,
            apelidosExistentes);
        FlyoutAberto = true;
    }

    private void AbrirFlyoutEditar(EcoInstance instancia)
    {
        var apelidosExistentes = Instancias
            .Where(i => i.Id != instancia.Id)
            .Select(i => i.Apelido)
            .ToList();
        FlyoutVM = new InstanceFlyoutViewModel(
            _versionCatalogService,
            _databaseDiscoveryService,
            _databaseVersionService,
            _instanceSetupService,
            async instanciaEditada =>
            {
                var idx = Instancias.IndexOf(instancia);
                if (idx >= 0) Instancias[idx] = instanciaEditada;
                await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
            },
            () => FlyoutAberto = false,
            apelidosExistentes,
            instancia);
        FlyoutAberto = true;
    }

    private async Task ExcluirInstanciaAsync(EcoInstance instancia)
    {
        if (!_dialogService.Confirmar("Excluir instância", $"Excluir \"{instancia.Apelido}\"?", "Excluir"))
            return;

        _instanceSetupService.Remover(instancia.ExecutavelPath, instancia.IniPath);
        Instancias.Remove(instancia);
        await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
    }

    private async Task IniciarEcoAsync(EcoInstance instancia)
    {
        var (sucesso, erro) = await _launchService.ExecutarAsync(instancia);
        if (!sucesso)
            _dialogService.Notificar("Erro ao executar", erro ?? "Erro desconhecido.");
    }

    private bool FiltrarInstancia(EcoInstance inst)
    {
        if (string.IsNullOrWhiteSpace(_filtroTexto)) return true;
        var texto = _filtroTexto.Trim();
        return inst.Apelido.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || inst.ExecutavelNome.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || inst.BaseNome.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || inst.VersaoBanco.Contains(texto, StringComparison.OrdinalIgnoreCase);
    }

    private void AplicarOrdenacaoPorColuna(string coluna)
    {
        if (OrdenacaoColuna == coluna)
            OrdenacaoAscendente = !OrdenacaoAscendente;
        else
        {
            OrdenacaoColuna     = coluna;
            OrdenacaoAscendente = true;
        }
        AplicarOrdenacao();
    }

    private void AplicarOrdenacao()
    {
        _instanciasView.SortDescriptions.Clear();
        _instanciasView.SortDescriptions.Add(new SortDescription(
            OrdenacaoColuna,
            OrdenacaoAscendente ? ListSortDirection.Ascending : ListSortDirection.Descending));
    }
}

