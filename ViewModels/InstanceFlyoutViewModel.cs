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
    private readonly IDatabaseVersionService      _databaseVersionService;
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
            _ = AtualizarStatusIniAsync();
            _ = AtualizarStatusVersaoAsync();
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<EcoDatabase> Bancos { get; } = new();

    private EcoDatabase? _bancoSelecionado;
    public EcoDatabase? BancoSelecionado
    {
        get => _bancoSelecionado;
        set
        {
            SetProperty(ref _bancoSelecionado, value);
            _ = AtualizarStatusVersaoAsync();
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
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

    private string _statusVersao = string.Empty;
    public string StatusVersao
    {
        get => _statusVersao;
        private set => SetProperty(ref _statusVersao, value);
    }

    // null = indeterminado (ainda consultando ou campos vazios); true = compatível; false = incompatível
    private bool? _versaoCompativel;
    public bool? VersaoCompativel
    {
        get => _versaoCompativel;
        private set => SetProperty(ref _versaoCompativel, value);
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
        IDatabaseVersionService databaseVersionService,
        IInstanceSetupService instanceSetupService,
        Func<EcoInstance, Task> onConfirmado,
        Action fecharFlyout,
        IReadOnlyCollection<string> apelidosExistentes,
        EcoInstance? instanciaExistente = null)
    {
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _databaseVersionService   = databaseVersionService;
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
        try
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
        catch (Exception ex)
        {
            ErroConfirmacao = $"Erro ao carregar dados do formulário: {ex.Message}";
        }
    }

private async System.Threading.Tasks.Task AtualizarStatusVersaoAsync()
    {
        if (BancoSelecionado is null || ExecutavelSelecionado is null)
        {
            StatusVersao     = string.Empty;
            VersaoCompativel = null;
            return;
        }

        StatusVersao     = "Consultando versão do banco...";
        VersaoCompativel = null;

        var versaoBancoRaw = await _databaseVersionService.ConsultarVersaoAsync(BancoSelecionado.EcoPath);
        if (versaoBancoRaw is null)
        {
            StatusVersao     = "Não foi possível consultar a versão do banco.";
            VersaoCompativel = null;
            return;
        }

        var versaoBanco = ExtrairVersaoBanco(versaoBancoRaw);
        var versaoExe   = ExtrairVersaoExe(ExecutavelSelecionado.NomeCompleto);

        if (versaoBanco is null || versaoExe is null)
        {
            StatusVersao     = $"Versão do banco: {versaoBancoRaw} (formato não reconhecido para comparação).";
            VersaoCompativel = null;
            return;
        }

        VersaoCompativel = string.Equals(versaoBanco, versaoExe, StringComparison.Ordinal);
        StatusVersao = VersaoCompativel == true
            ? $"Versão {versaoExe} — banco e executável compatíveis."
            : $"Incompatibilidade de versão: banco v{versaoBanco} × executável v{versaoExe}.";
    }

    // "14650000" → "650"  (ignora os 2 primeiros dígitos do major e os 3 últimos do patch)
    private static string? ExtrairVersaoBanco(string versaoBancoRaw)
    {
        if (versaoBancoRaw.Length > 5 && versaoBancoRaw.All(char.IsDigit))
            return versaoBancoRaw.Substring(2, versaoBancoRaw.Length - 5);
        return null;
    }

    // "Eco_650_10" → "650"  (primeiro segmento numérico após "Eco_")
    private static string? ExtrairVersaoExe(string nomeCompleto)
    {
        var partes = nomeCompleto.Split('_');
        return partes.Length >= 2 ? partes[1] : null;
    }

    private async System.Threading.Tasks.Task AtualizarStatusIniAsync()
    {
        if (ExecutavelSelecionado is null)
        {
            StatusIni    = string.Empty;
            EcoIniValido = false;
            return;
        }

        if (!File.Exists(EcoPathConstants.EcoIniPadrao))
        {
            StatusIni    = $"eco.ini padrão não encontrado em {EcoPathConstants.WindowsDir}.";
            EcoIniValido = false;
            return;
        }

        bool valido  = await _instanceSetupService.ValidarEcoIniAsync();
        StatusIni    = valido
            ? "eco.ini padrão encontrado e válido."
            : "eco.ini padrão inválido: chave 'dados=' não encontrada na seção [windows].";
        EcoIniValido = valido;
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
