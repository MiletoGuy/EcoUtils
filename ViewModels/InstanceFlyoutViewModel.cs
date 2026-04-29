using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class InstanceFlyoutViewModel : ViewModelBase
{
    private readonly IVersionCatalogService       _versionCatalogService;
    private readonly IDatabaseDiscoveryService    _databaseDiscoveryService;
    private readonly IInstanceSetupService        _instanceSetupService;
    private readonly Func<EcoInstance, Task>      _onConfirmado;
    private readonly Action                       _fecharFlyout;
    private readonly EcoInstance?                 _instanciaExistente;
    private readonly IReadOnlyCollection<string>  _apelidosExistentes;

    public string Titulo              => _instanciaExistente is null ? "Nova Instância ECO" : "Editar Instância";
    public string TextoBotaoConfirmar => _instanciaExistente is null ? "Confirmar"          : "Salvar";

    private string _apelido = string.Empty;
    public string Apelido
    {
        get => _apelido;
        set
        {
            SetProperty(ref _apelido, value);
            OnPropertyChanged(nameof(ApelidoDuplicado));
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public bool ApelidoDuplicado =>
        !string.IsNullOrWhiteSpace(Apelido) &&
        _apelidosExistentes.Contains(Apelido.Trim(), StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<EcoExecutavel> Executaveis { get; } = new();

    private EcoExecutavel? _executavelSelecionado;
    public EcoExecutavel? ExecutavelSelecionado
    {
        get => _executavelSelecionado;
        set
        {
            SetProperty(ref _executavelSelecionado, value);
            AtualizarStatusIni();
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<EcoDatabase> Bancos { get; } = new();

    private EcoDatabase? _bancoSelecionado;
    public EcoDatabase? BancoSelecionado
    {
        get => _bancoSelecionado;
        set { SetProperty(ref _bancoSelecionado, value); ConfirmarCommand.RaiseCanExecuteChanged(); }
    }

    private string _statusIni = string.Empty;
    public string StatusIni
    {
        get => _statusIni;
        private set => SetProperty(ref _statusIni, value);
    }

    private bool _ecoIniValido;
    public bool EcoIniValido
    {
        get => _ecoIniValido;
        private set { SetProperty(ref _ecoIniValido, value); ConfirmarCommand.RaiseCanExecuteChanged(); }
    }

    private string? _erroConfirmacao;
    public string? ErroConfirmacao
    {
        get => _erroConfirmacao;
        private set
        {
            SetProperty(ref _erroConfirmacao, value);
            OnPropertyChanged(nameof(TemErro));
        }
    }

    public bool TemErro => ErroConfirmacao is not null;

    public bool PodeConfirmar =>
        !string.IsNullOrWhiteSpace(Apelido) &&
        !ApelidoDuplicado                   &&
        ExecutavelSelecionado is not null    &&
        BancoSelecionado is not null         &&
        EcoIniValido;

    public ICommand CancelarCommand  { get; }
    public AsyncRelayCommand ConfirmarCommand { get; }

    public InstanceFlyoutViewModel(
        IVersionCatalogService versionCatalogService,
        IDatabaseDiscoveryService databaseDiscoveryService,
        IInstanceSetupService instanceSetupService,
        Func<EcoInstance, Task> onConfirmado,
        Action fecharFlyout,
        IReadOnlyCollection<string> apelidosExistentes,
        EcoInstance? instanciaExistente = null)
    {
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _instanceSetupService     = instanceSetupService;
        _onConfirmado             = onConfirmado;
        _fecharFlyout             = fecharFlyout;
        _apelidosExistentes       = apelidosExistentes;
        _instanciaExistente       = instanciaExistente;

        if (instanciaExistente is not null)
            _apelido = instanciaExistente.Apelido;

        ConfirmarCommand = new AsyncRelayCommand(async _ => await ConfirmarAsync(), _ => PodeConfirmar);
        CancelarCommand  = new RelayCommand(_ => _fecharFlyout());

        _ = CarregarDadosAsync();
    }

    private async System.Threading.Tasks.Task CarregarDadosAsync()
    {
        var exes   = await _versionCatalogService.ListarExecutaveisAsync();
        var bancos = await _databaseDiscoveryService.ListarBancosAsync();

        foreach (var exe   in exes)   Executaveis.Add(exe);
        foreach (var banco in bancos) Bancos.Add(banco);

        if (_instanciaExistente is not null)
        {
            ExecutavelSelecionado = Executaveis.FirstOrDefault(e => e.ExePath == _instanciaExistente.ExecutavelFontePath);
            BancoSelecionado      = Bancos.FirstOrDefault(b => b.EcoPath == _instanciaExistente.BasePath);
        }
    }

    private void AtualizarStatusIni()
    {
        if (ExecutavelSelecionado is null)
        {
            StatusIni    = string.Empty;
            EcoIniValido = false;
            return;
        }

        bool existe  = File.Exists(EcoPathConstants.EcoIniPadrao);
        EcoIniValido = existe;
        StatusIni    = existe
            ? "eco.ini padrão encontrado."
            : $"eco.ini padrão não encontrado em {EcoPathConstants.WindowsDir}.";
    }

    private async System.Threading.Tasks.Task ConfirmarAsync()
    {
        if (!PodeConfirmar) return;

        ErroConfirmacao = null;

        try
        {
            string exePath;
            string iniPath;

            bool fonteAlterada = _instanciaExistente is null
                || _instanciaExistente.ExecutavelFontePath != ExecutavelSelecionado!.ExePath
                || _instanciaExistente.BasePath            != BancoSelecionado!.EcoPath;

            if (fonteAlterada)
            {
                // Remove arquivos implantados anteriores (no-op em criação nova)
                if (_instanciaExistente is not null)
                    _instanceSetupService.Remover(
                        _instanciaExistente.ExecutavelPath,
                        _instanciaExistente.IniPath);

                (exePath, iniPath) = await _instanceSetupService.ImplantarAsync(
                    ExecutavelSelecionado!.ExePath,
                    BancoSelecionado!.EcoPath);
            }
            else
            {
                // Executável e base não mudaram — reutiliza os arquivos já implantados
                exePath = _instanciaExistente!.ExecutavelPath;
                iniPath = _instanciaExistente.IniPath;
            }

            var instancia = new EcoInstance
            {
                Id                  = _instanciaExistente?.Id ?? Guid.NewGuid(),
                Apelido             = Apelido.Trim(),
                ExecutavelPath      = exePath,
                ExecutavelFontePath = ExecutavelSelecionado!.ExePath,
                ExecutavelNome      = ExecutavelSelecionado.NomeCompleto,
                BasePath            = BancoSelecionado!.EcoPath,
                BaseNome            = BancoSelecionado.NomeCompleto,
                IniPath             = iniPath
            };

            await _onConfirmado(instancia);
            _fecharFlyout();
        }
        catch (Exception ex)
        {
            ErroConfirmacao = ex.Message;
        }
    }
}
