using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class ExecutarEcoViewModel : ViewModelBase
{
    private readonly IInstanceRepository       _instanceRepository;
    private readonly IVersionCatalogService    _versionCatalogService;
    private readonly IDatabaseDiscoveryService _databaseDiscoveryService;
    private readonly IInstanceSetupService     _instanceSetupService;
    private readonly ILaunchService            _launchService;
    private readonly IDialogService            _dialogService;
    private readonly ILogService               _log;

    public ObservableCollection<EcoInstance> Instancias { get; }

    public bool ListaVazia => !Instancias.Any();

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

    public ICommand AdicionarCommand { get; }
    public ICommand EditarCommand    { get; }
    public ICommand ExcluirCommand   { get; }
    public ICommand ExecutarCommand  { get; }

    public ExecutarEcoViewModel(
        IInstanceRepository instanceRepository,
        IVersionCatalogService versionCatalogService,
        IDatabaseDiscoveryService databaseDiscoveryService,
        IInstanceSetupService instanceSetupService,
        ILaunchService launchService,
        IDialogService dialogService,
        ILogService log)
    {
        _instanceRepository       = instanceRepository;
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _instanceSetupService     = instanceSetupService;
        _launchService            = launchService;
        _dialogService            = dialogService;
        _log                      = log;

        Instancias = new ObservableCollection<EcoInstance>();
        Instancias.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ListaVazia));

        AdicionarCommand = new RelayCommand(_ => AbrirFlyoutNovo());
        EditarCommand    = new RelayCommand(inst => AbrirFlyoutEditar((EcoInstance)inst!));
        ExcluirCommand   = new AsyncRelayCommand(
            async inst => await ExcluirInstanciaAsync((EcoInstance)inst!),
            onError: ex => _log.Error(nameof(ExcluirInstanciaAsync), ex));
        ExecutarCommand  = new AsyncRelayCommand(
            async inst => await IniciarEcoAsync((EcoInstance)inst!),
            onError: ex => _log.Error(nameof(IniciarEcoAsync), ex));

        _ = CarregarInstanciasAsync();
    }

    private async Task CarregarInstanciasAsync()
    {
        var lista = await _instanceRepository.CarregarAsync();
        foreach (var inst in lista)
            Instancias.Add(inst);
    }

    private void AbrirFlyoutNovo()
    {
        var apelidosExistentes = Instancias.Select(i => i.Apelido).ToList();
        FlyoutVM = new InstanceFlyoutViewModel(
            _versionCatalogService,
            _databaseDiscoveryService,
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
}

