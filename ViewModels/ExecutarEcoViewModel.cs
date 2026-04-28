using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
    private readonly IIniGeneratorService      _iniGeneratorService;
    private readonly ILaunchService            _launchService;

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
        IIniGeneratorService iniGeneratorService,
        ILaunchService launchService)
    {
        _instanceRepository       = instanceRepository;
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _iniGeneratorService      = iniGeneratorService;
        _launchService            = launchService;

        Instancias = new ObservableCollection<EcoInstance>();
        Instancias.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ListaVazia));

        AdicionarCommand = new RelayCommand(_ => AbrirFlyoutNovo());
        EditarCommand    = new RelayCommand(inst => AbrirFlyoutEditar((EcoInstance)inst!));
        ExcluirCommand   = new RelayCommand(inst => ExcluirInstancia((EcoInstance)inst!));
        ExecutarCommand  = new RelayCommand(inst => IniciarEco((EcoInstance)inst!));

        _ = CarregarInstanciasAsync();
    }

    private async System.Threading.Tasks.Task CarregarInstanciasAsync()
    {
        var lista = await _instanceRepository.CarregarAsync();
        foreach (var inst in lista)
            Instancias.Add(inst);
    }

    private void AbrirFlyoutNovo()
    {
        FlyoutVM = new InstanceFlyoutViewModel(
            _versionCatalogService,
            _databaseDiscoveryService,
            _iniGeneratorService,
            instancia =>
            {
                Instancias.Add(instancia);
                _ = _instanceRepository.SalvarAsync(new System.Collections.Generic.List<EcoInstance>(Instancias));
            },
            () => FlyoutAberto = false);
        FlyoutAberto = true;
    }

    private void AbrirFlyoutEditar(EcoInstance instancia)
    {
        // Implementado no Commit 11
    }

    private void ExcluirInstancia(EcoInstance instancia)
    {
        // Implementado no Commit 11
    }

    private void IniciarEco(EcoInstance instancia)
    {
        // Implementado no Commit 11
    }
}
