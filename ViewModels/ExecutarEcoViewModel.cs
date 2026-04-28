using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class ExecutarEcoViewModel : ViewModelBase
{
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
    }

    private readonly IInstanceRepository       _instanceRepository;
    private readonly IVersionCatalogService    _versionCatalogService;
    private readonly IDatabaseDiscoveryService _databaseDiscoveryService;
    private readonly IIniGeneratorService      _iniGeneratorService;
    private readonly ILaunchService            _launchService;
}
