using System.Collections.ObjectModel;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<NavItem> Abas { get; }

    private NavItem? _abaAtiva;
    public NavItem? AbaAtiva
    {
        get => _abaAtiva;
        set => SetProperty(ref _abaAtiva, value);
    }

    public MainViewModel()
    {
        IInstanceRepository       instanceRepo       = new InstanceRepository();
        IVersionCatalogService    versionService     = new VersionCatalogService();
        IDatabaseDiscoveryService databaseService    = new DatabaseDiscoveryService();
        IIniGeneratorService      iniService         = new IniGeneratorService();
        ILaunchService            launchService      = new LaunchService();

        Abas = new ObservableCollection<NavItem>
        {
            new NavItem
            {
                Rotulo    = "Executar ECO",
                Icone     = "\u25B6",
                ViewModel = new ExecutarEcoViewModel(
                    instanceRepo,
                    versionService,
                    databaseService,
                    iniService,
                    launchService)
            }
        };

        AbaAtiva = Abas[0];
    }
}
