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
    private readonly IDatabaseImportService       _databaseImportService;
    private readonly IExecutableImportService     _executableImportService;
    private readonly IRestoreJobService           _restoreJobService;
    private readonly IFileLockerService           _fileLockerService;
    private readonly IDialogService               _dialogService;
    private readonly ILogService                  _log;
    private readonly IUserSettingsService         _userSettingsService;
    private readonly Func<EcoInstance, Task>      _onConfirmado;
    private readonly Action                       _fecharFlyout;
    private readonly EcoInstance?                 _instanciaExistente;
    private readonly IReadOnlyCollection<string>  _apelidosExistentes;

    public string Titulo              => _instanciaExistente is null ? "Nova Instância ECO" : "Editar Instância";
    public string TextoBotaoConfirmar => _instanciaExistente is null ? "Confirmar"          : "Salvar";

    private string _apelido              = string.Empty;
    private bool   _apelidoAutoPreenchido = false;
    private bool   _autoFillingApelido    = false;

    public string Apelido
    {
        get => _apelido;
        set
        {
            SetProperty(ref _apelido, value);
            if (!_autoFillingApelido) _apelidoAutoPreenchido = false;
            OnPropertyChanged(nameof(ApelidoDuplicado));
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public bool ApelidoDuplicado =>
        !string.IsNullOrWhiteSpace(Apelido) &&
        _apelidosExistentes.Contains(Apelido.Trim(), StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<EcoExecutavel> Executaveis            { get; } = new();
    public ObservableCollection<string>        MajorsSelecionaveis    { get; } = new();
    public ObservableCollection<string>        VersoesSelecionaveis   { get; } = new();
    public ObservableCollection<EcoExecutavel> BuildsSelecionaveis    { get; } = new();

    private string? _majorSelecionado;
    public string? MajorSelecionado
    {
        get => _majorSelecionado;
        set
        {
            if (!SetProperty(ref _majorSelecionado, value)) return;
            VersaoSelecionada = null;
            AtualizarVersoesSelecionaveis();
            OnPropertyChanged(nameof(PodeSelecionarVersao));
            OnPropertyChanged(nameof(PodeSelecionarBuild));
        }
    }

    private string? _versaoSelecionada;
    public string? VersaoSelecionada
    {
        get => _versaoSelecionada;
        set
        {
            if (!SetProperty(ref _versaoSelecionada, value)) return;
            RecalcularBuilds();
            OnPropertyChanged(nameof(PodeSelecionarBuild));
        }
    }

    public bool PodeSelecionarVersao => _majorSelecionado is not null && !_isImportandoExe;

    private void AtualizarMajorsSelecionaveis()
    {
        MajorsSelecionaveis.Clear();
        foreach (var m in Executaveis
            .Select(e => EcoVersionHelper.ExtrairMajorExe(e.NomeCompleto))
            .Where(m => m is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase))
            MajorsSelecionaveis.Add(EcoVersionHelper.FormatarMajor(m!));

        if (_majorSelecionado is null && MajorsSelecionaveis.Count > 0)
            MajorSelecionado = MajorsSelecionaveis[0];
    }

    private void AtualizarVersoesSelecionaveis()
    {
        VersoesSelecionaveis.Clear();
        if (_majorSelecionado is null) return;

        var majorCompacto = EcoVersionHelper.NormalizarMajorInput(_majorSelecionado);
        if (majorCompacto is null) return;

        foreach (var v in Executaveis
            .Where(e => string.Equals(EcoVersionHelper.ExtrairMajorExe(e.NomeCompleto), majorCompacto, StringComparison.OrdinalIgnoreCase))
            .Select(e => EcoVersionHelper.ExtrairVersaoExeSemMajor(e.NomeCompleto))
            .Where(v => v is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
            VersoesSelecionaveis.Add(v!);
        RecalcularBuilds();
    }

    private void RecalcularBuilds()
    {
        BuildsSelecionaveis.Clear();
        if (_versaoSelecionada is null || _majorSelecionado is null) return;

        var majorCompacto = EcoVersionHelper.NormalizarMajorInput(_majorSelecionado);
        if (majorCompacto is null) return;

        foreach (var exe in Executaveis.Where(e =>
            string.Equals(EcoVersionHelper.ExtrairMajorExe(e.NomeCompleto), majorCompacto, StringComparison.OrdinalIgnoreCase)
            && string.Equals(EcoVersionHelper.ExtrairVersaoExeSemMajor(e.NomeCompleto), _versaoSelecionada, StringComparison.OrdinalIgnoreCase)))
            BuildsSelecionaveis.Add(exe);
    }

    private EcoExecutavel? _executavelSelecionado;
    public EcoExecutavel? ExecutavelSelecionado
    {
        get => _executavelSelecionado;
        set
        {
            SetProperty(ref _executavelSelecionado, value);
            _ = AtualizarStatusIniAsync();
            AtualizarCompatibilidade();
            ConfirmarCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(AguardandoExeAutoSelecionado));
            OnPropertyChanged(nameof(PodeUsarVersaoExecutavel));
            OnPropertyChanged(nameof(PodeAlterarUsarVersaoExecutavel));
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
            if (value is not null && (_apelidoAutoPreenchido || string.IsNullOrWhiteSpace(_apelido)))
            {
                _autoFillingApelido    = true;
                Apelido                = value.NomeCompleto;
                _apelidoAutoPreenchido = true;
                _autoFillingApelido    = false;
            }
            BaseEmRestauracao = value is not null && _restoreJobService.EstaRestaurando(value.EcoPath);
            _ = AtualizarStatusBancoAsync();
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

    private string _statusBancoVersao = string.Empty;
    public string StatusBancoVersao
    {
        get => _statusBancoVersao;
        private set => SetProperty(ref _statusBancoVersao, value);
    }

    private string _statusVersao = string.Empty;
    public string StatusVersao
    {
        get => _statusVersao;
        private set => SetProperty(ref _statusVersao, value);
    }

    private string? _versaoBancoRaw;

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

    // ── Importação de banco ──────────────────────────────────────
    private bool _isImportandoBanco;
    public bool IsImportandoBanco
    {
        get => _isImportandoBanco;
        private set
        {
            SetProperty(ref _isImportandoBanco, value);
            OnPropertyChanged(nameof(NaoImportando));
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public bool NaoImportando => !_isImportandoBanco;

    private bool _isIndeterminadaImportacao;
    public bool IsIndeterminadaImportacao
    {
        get => _isIndeterminadaImportacao;
        private set => SetProperty(ref _isIndeterminadaImportacao, value);
    }

    private int _progressoImportacao;
    public int ProgressoImportacao
    {
        get => _progressoImportacao;
        private set => SetProperty(ref _progressoImportacao, value);
    }

    private string _mensagemProgresso = string.Empty;
    public string MensagemProgresso
    {
        get => _mensagemProgresso;
        private set => SetProperty(ref _mensagemProgresso, value);
    }

    private string? _erroImportacao;
    public string? ErroImportacao
    {
        get => _erroImportacao;
        private set
        {
            SetProperty(ref _erroImportacao, value);
            OnPropertyChanged(nameof(TemErroImportacao));
        }
    }

    public bool TemErroImportacao => ErroImportacao is not null;

    // ── Importação de executável ─────────────────────────────────
    private bool _isImportandoExe;
    public bool IsImportandoExe
    {
        get => _isImportandoExe;
        private set
        {
            SetProperty(ref _isImportandoExe, value);
            OnPropertyChanged(nameof(NaoImportandoExe));
            OnPropertyChanged(nameof(PodeSelecionarVersao));
            OnPropertyChanged(nameof(PodeSelecionarBuild));
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public bool NaoImportandoExe    => !_isImportandoExe;
    public bool PodeSelecionarBuild  => _versaoSelecionada is not null && _majorSelecionado is not null && !_isImportandoExe;

    private int _progressoExe;
    public int ProgressoExe
    {
        get => _progressoExe;
        private set => SetProperty(ref _progressoExe, value);
    }

    private string _mensagemProgressoExe = string.Empty;
    public string MensagemProgressoExe
    {
        get => _mensagemProgressoExe;
        private set => SetProperty(ref _mensagemProgressoExe, value);
    }

    private string? _erroImportacaoExe;
    public string? ErroImportacaoExe
    {
        get => _erroImportacaoExe;
        private set
        {
            SetProperty(ref _erroImportacaoExe, value);
            OnPropertyChanged(nameof(TemErroImportacaoExe));
        }
    }

    public bool TemErroImportacaoExe => ErroImportacaoExe is not null;

    // ── Configurações .ini [PREFERENCIAS] ───────────────────────
    private bool _iniExpanded;
    public bool IniExpanded
    {
        get => _iniExpanded;
        private set
        {
            SetProperty(ref _iniExpanded, value);
            OnPropertyChanged(nameof(TextoBotaoIni));
        }
    }

    public string TextoBotaoIni => IniExpanded ? "Configurações do .ini ▴" : "Configurações do .ini ▸";

    private string _usuario = "SUPERVISOR";
    public string Usuario
    {
        get => _usuario;
        set => SetProperty(ref _usuario, value);
    }

    private bool _pesquisaTotalDosProdutos = true;
    public bool PesquisaTotalDosProdutos
    {
        get => _pesquisaTotalDosProdutos;
        set => SetProperty(ref _pesquisaTotalDosProdutos, value);
    }

    private bool _monitorarTempoSelects;
    public bool MonitorarTempoSelects
    {
        get => _monitorarTempoSelects;
        set => SetProperty(ref _monitorarTempoSelects, value);
    }

    private bool _sincronizaTabelaPreco;
    public bool SincronizaTabelaPreco
    {
        get => _sincronizaTabelaPreco;
        set => SetProperty(ref _sincronizaTabelaPreco, value);
    }

    private bool _multiplasInstancias = true;
    public bool MultiplasInstancias
    {
        get => _multiplasInstancias;
        set => SetProperty(ref _multiplasInstancias, value);
    }

    // ── Versão forçada do banco ──────────────────────────────────────
    private bool _usarVersaoExecutavel;
    public bool UsarVersaoExecutavel
    {
        get => _usarVersaoExecutavel;
        set => SetProperty(ref _usarVersaoExecutavel, value);
    }

    public bool PodeUsarVersaoExecutavel =>
        _versaoBancoRaw is not null &&
        ExecutavelSelecionado is not null &&
        !BaseEmRestauracao;

    // Checkbox fica bloqueado enquanto a versão original ainda não foi restaurada
    public bool PodeAlterarUsarVersaoExecutavel =>
        PodeUsarVersaoExecutavel && !PodeRestaurarVersaoOriginal;

    public bool PodeRestaurarVersaoOriginal =>
        _instanciaExistente is not null &&
        _instanciaExistente.UsarVersaoExecutavel &&
        !string.IsNullOrEmpty(_instanciaExistente.VersaoBancoOriginal);

    // ── Versão do Firebird ─────────────────────────────────
    private string _versaoFirebird = "2.5";
    public string VersaoFirebird
    {
        get => _versaoFirebird;
        set
        {
            if (!SetProperty(ref _versaoFirebird, value)) return;
            OnPropertyChanged(nameof(VersaoFirebird25));
            OnPropertyChanged(nameof(VersaoFirebird50));
        }
    }

    public bool VersaoFirebird25
    {
        get => _versaoFirebird == "2.5";
        set { if (value) VersaoFirebird = "2.5"; }
    }

    public bool VersaoFirebird50
    {
        get => _versaoFirebird == "5.0";
        set { if (value) VersaoFirebird = "5.0"; }
    }

    private IniPreferencias BuildPreferencias() => new()
    {
        Usuario                  = Usuario.Trim(),
        PesquisaTotalDosProdutos = PesquisaTotalDosProdutos,
        MonitorarTempoSelects    = MonitorarTempoSelects,
        SincronizaTabelaPreco    = SincronizaTabelaPreco,
        MultiplasInstancias      = MultiplasInstancias,
    };

    // ── Aviso de restauração ativa ────────────────────────────────────
    private bool _baseEmRestauracao;
    public bool BaseEmRestauracao
    {
        get => _baseEmRestauracao;
        private set
        {
            if (!SetProperty(ref _baseEmRestauracao, value)) return;
            ConfirmarCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(AguardandoExeAutoSelecionado));

            if (value && BancoSelecionado is not null)
            {
                var job = _restoreJobService.ObterPorDestino(BancoSelecionado.EcoPath);
                if (job is not null)
                {
                    _jobObservado       = job;
                    MensagemRestauracao = job.UltimaMensagem;
                    job.PropertyChanged += OnJobPropertyChanged;
                }
            }
            else
            {
                MensagemRestauracao = string.Empty;
                UnsubscribeJob();
            }
        }
    }

    private RestoreJobEntry? _jobObservado;

    private void OnJobPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is RestoreJobEntry job && e.PropertyName == nameof(RestoreJobEntry.UltimaMensagem))
            MensagemRestauracao = job.UltimaMensagem;
    }

    private void UnsubscribeJob()
    {
        if (_jobObservado is null) return;
        _jobObservado.PropertyChanged -= OnJobPropertyChanged;
        _jobObservado = null;
    }

    private string _mensagemRestauracao = string.Empty;
    public string MensagemRestauracao
    {
        get => _mensagemRestauracao;
        private set => SetProperty(ref _mensagemRestauracao, value);
    }

    /// <summary>True quando o banco está em restauração e o usuário ainda não selecionou executável — o exe será auto-selecionado após a restauração.</summary>
    public bool AguardandoExeAutoSelecionado => BaseEmRestauracao && ExecutavelSelecionado is null;

    public bool PodeConfirmar =>
        !string.IsNullOrWhiteSpace(Apelido) &&
        !ApelidoDuplicado                   &&
        BancoSelecionado is not null         &&
        !IsImportandoBanco                  &&
        !IsImportandoExe                    &&
        (AguardandoExeAutoSelecionado || (ExecutavelSelecionado is not null && EcoIniValido));

    public ICommand CancelarCommand                            { get; }
    public AsyncRelayCommand ConfirmarCommand                  { get; }
    public AsyncRelayCommand AdicionarBancoCommand             { get; }
    public AsyncRelayCommand AdicionarExeCommand               { get; }
    public ICommand ToggleIniExpandedCommand                   { get; }
    public AsyncRelayCommand CancelarRestauracaoFlyoutCommand   { get; }
    public AsyncRelayCommand RestaurarVersaoOriginalCommand     { get; }

    private bool _isCancellingRestauracao;
    public bool IsCancellingRestauracao
    {
        get => _isCancellingRestauracao;
        private set => SetProperty(ref _isCancellingRestauracao, value);
    }

    public InstanceFlyoutViewModel(
        IVersionCatalogService versionCatalogService,
        IDatabaseDiscoveryService databaseDiscoveryService,
        IDatabaseVersionService databaseVersionService,
        IInstanceSetupService instanceSetupService,
        IDatabaseImportService databaseImportService,
        IExecutableImportService executableImportService,
        IRestoreJobService restoreJobService,
        IFileLockerService fileLockerService,
        IDialogService dialogService,
        ILogService log,
        IUserSettingsService userSettingsService,
        Func<EcoInstance, Task> onConfirmado,
        Action fecharFlyout,
        IReadOnlyCollection<string> apelidosExistentes,
        EcoInstance? instanciaExistente = null)
    {
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _databaseVersionService   = databaseVersionService;
        _instanceSetupService     = instanceSetupService;
        _databaseImportService    = databaseImportService;
        _executableImportService  = executableImportService;
        _restoreJobService        = restoreJobService;
        _fileLockerService        = fileLockerService;
        _dialogService            = dialogService;
        _log                      = log;
        _userSettingsService      = userSettingsService;
        _onConfirmado             = onConfirmado;
        _fecharFlyout             = fecharFlyout;
        _apelidosExistentes       = apelidosExistentes;
        _instanciaExistente       = instanciaExistente;

        if (instanciaExistente is not null)
            _apelido = instanciaExistente.Apelido;

        ConfirmarCommand                  = new AsyncRelayCommand(async _ => await ConfirmarAsync(), _ => PodeConfirmar);
        CancelarCommand                   = new RelayCommand(_ => _fecharFlyout());
        AdicionarBancoCommand             = new AsyncRelayCommand(async _ => await AdicionarBancoAsync(), _ => !IsImportandoBanco && !IsImportandoExe);
        AdicionarExeCommand               = new AsyncRelayCommand(async _ => await AdicionarExeAsync(),   _ => !IsImportandoBanco && !IsImportandoExe);
        ToggleIniExpandedCommand = new RelayCommand(_ => IniExpanded = !IniExpanded);
        CancelarRestauracaoFlyoutCommand = new AsyncRelayCommand(async _ =>
        {
            if (BancoSelecionado is null) return;
            bool confirmar = _dialogService.Confirmar(
                "Cancelar restauração",
                "Cancelar a restauração irá interromper o processo e excluir o arquivo parcialmente criado. Deseja continuar?",
                "Cancelar restauração");
            if (!confirmar) return;
            IsCancellingRestauracao = true;
            var bancoParaCancelar = BancoSelecionado;
            try
            {
                await _restoreJobService.CancelarAsync(bancoParaCancelar.EcoPath);
                BaseEmRestauracao = false;
                if (!File.Exists(bancoParaCancelar.EcoPath))
                {
                    Bancos.Remove(bancoParaCancelar);
                    BancoSelecionado = null;
                }
            }
            finally { IsCancellingRestauracao = false; }
        });

        Executaveis.CollectionChanged += (_, _) => AtualizarVersoesSelecionaveis();
        Executaveis.CollectionChanged += (_, _) => AtualizarMajorsSelecionaveis();

        RestaurarVersaoOriginalCommand = new AsyncRelayCommand(
            async _ => await RestaurarVersaoOriginalAsync(),
            _          => PodeRestaurarVersaoOriginal);

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

            AtualizarMajorsSelecionaveis();

            if (_instanciaExistente is not null)
            {
                var exeParaSelecionar = Executaveis.FirstOrDefault(e => e.ExePath == _instanciaExistente.ExecutavelFontePath);
                if (exeParaSelecionar is not null)
                {
                    var major = EcoVersionHelper.ExtrairMajorExe(exeParaSelecionar.NomeCompleto);
                    MajorSelecionado = major is not null ? EcoVersionHelper.FormatarMajor(major) : null;
                    VersaoSelecionada = EcoVersionHelper.ExtrairVersaoExeSemMajor(exeParaSelecionar.NomeCompleto);
                }
                ExecutavelSelecionado = exeParaSelecionar;
                BancoSelecionado      = Bancos.FirstOrDefault(b => string.Equals(b.EcoPath, _instanciaExistente.BasePath, StringComparison.OrdinalIgnoreCase));

                // Se o banco não foi encontrado no disco, pode estar sendo restaurado (arquivo ainda não existe)
                if (BancoSelecionado is null && !string.IsNullOrEmpty(_instanciaExistente.BasePath))
                {
                    var job = _restoreJobService.ObterPorDestino(_instanciaExistente.BasePath);
                    if (job is not null && job.Status == RestoreJobStatus.Restaurando)
                    {
                        var bancoRestaurando = new EcoDatabase
                        {
                            NomeCompleto = job.Apelido,
                            EcoPath      = job.DestinoEco
                        };
                        Bancos.Add(bancoRestaurando);
                        // Preservar o apelido salvo na instância, não sobrescrever com o nome do banco
                        _apelidoAutoPreenchido = false;
                        BancoSelecionado       = bancoRestaurando;
                    }
                }

                if (File.Exists(_instanciaExistente.IniPath))
                {
                    var prefs = await _instanceSetupService.LerPreferenciasAsync(_instanciaExistente.IniPath);
                    Usuario                  = prefs.Usuario;
                    PesquisaTotalDosProdutos = prefs.PesquisaTotalDosProdutos;
                    MonitorarTempoSelects    = prefs.MonitorarTempoSelects;
                    SincronizaTabelaPreco    = prefs.SincronizaTabelaPreco;
                    MultiplasInstancias      = prefs.MultiplasInstancias;
                    VersaoFirebird           = prefs.VersaoFirebird;
                }
                else
                {
                    VersaoFirebird = _instanciaExistente.VersaoFirebird;
                }

                UsarVersaoExecutavel = _instanciaExistente.UsarVersaoExecutavel;
            }
        }
        catch (Exception ex)
        {
            ErroConfirmacao = $"Erro ao carregar dados do formulário: {ex.Message}";
        }
    }

    private async System.Threading.Tasks.Task AtualizarStatusBancoAsync()
    {
        if (BancoSelecionado is null)
        {
            _versaoBancoRaw   = null;
            StatusBancoVersao = string.Empty;
            AtualizarCompatibilidade();
            OnPropertyChanged(nameof(PodeUsarVersaoExecutavel));
            OnPropertyChanged(nameof(PodeAlterarUsarVersaoExecutavel));
            return;
        }

        StatusBancoVersao = "Consultando versão...";
        _versaoBancoRaw   = null;

        if (BaseEmRestauracao)
        {
            StatusBancoVersao = "Aguardando conclusão da restauração...";
            _versaoBancoRaw   = null;
            AtualizarCompatibilidade();
            OnPropertyChanged(nameof(PodeUsarVersaoExecutavel));
            OnPropertyChanged(nameof(PodeAlterarUsarVersaoExecutavel));
            return;
        }

        var versaoBancoRaw = await _databaseVersionService.ConsultarVersaoAsync(
            BancoSelecionado.EcoPath,
            ObterPortaFirebirdAtual());
        _versaoBancoRaw = versaoBancoRaw;

        if (versaoBancoRaw is null)
        {
            StatusBancoVersao = "Não foi possível consultar a versão do banco.";
        }
        else
        {
            var versaoFormatada = EcoVersionHelper.FormatarVersao(versaoBancoRaw);
            StatusBancoVersao = versaoFormatada is not null
                ? $"Versão {versaoFormatada}"
                : $"Versão: {versaoBancoRaw}";
        }

        AtualizarCompatibilidade();
        OnPropertyChanged(nameof(PodeUsarVersaoExecutavel));
        OnPropertyChanged(nameof(PodeAlterarUsarVersaoExecutavel));
    }

    private string ObterPortaFirebirdAtual()
        => ObterPortaFirebird(VersaoFirebird);

    private string ObterPortaFirebird(string versaoFirebird)
    {
        var settings = _userSettingsService.Settings;
        return string.Equals(versaoFirebird, "2.5", StringComparison.Ordinal)
            ? settings.PortaFirebird25
            : settings.PortaFirebird50;
    }

    private void AtualizarCompatibilidade()
    {
        if (BancoSelecionado is null || ExecutavelSelecionado is null || _versaoBancoRaw is null)
        {
            StatusVersao     = string.Empty;
            VersaoCompativel = null;
            return;
        }

        var versaoBanco = EcoVersionHelper.ExtrairVersao(_versaoBancoRaw);
        var majorBanco  = EcoVersionHelper.ExtrairMajor(_versaoBancoRaw);
        var majorExe    = EcoVersionHelper.ExtrairMajorExe(ExecutavelSelecionado.NomeCompleto);
        var versaoExe   = EcoVersionHelper.ExtrairVersaoExeSemMajor(ExecutavelSelecionado.NomeCompleto);

        if (versaoBanco is null || majorBanco is null || majorExe is null || string.IsNullOrEmpty(versaoExe))
        {
            StatusVersao     = $"Formato de versão não reconhecido para comparação.";
            VersaoCompativel = null;
            return;
        }

        var majorIgual  = string.Equals(majorBanco, majorExe, StringComparison.OrdinalIgnoreCase);
        var versaoIgual = string.Equals(versaoBanco, versaoExe, StringComparison.OrdinalIgnoreCase);
        VersaoCompativel = majorIgual && versaoIgual;
        var versaoDisplay = EcoVersionHelper.FormatarVersao(_versaoBancoRaw) ?? versaoBanco;
        var exeDisplay = $"{EcoVersionHelper.FormatarMajor(majorExe)}.{versaoExe}";
        StatusVersao = VersaoCompativel == true
            ? $"Versão {versaoDisplay} — banco e executável compatíveis."
            : $"Incompatibilidade: banco v{versaoDisplay} × executável v{exeDisplay}.";
    }

    // "14650000" → "650"  (ignora os 2 primeiros dígitos do major e os 3 últimos do patch)
    private static string? ExtrairVersaoBanco(string versaoBancoRaw)
        => EcoVersionHelper.ExtrairVersao(versaoBancoRaw);

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

    private async System.Threading.Tasks.Task AdicionarBancoAsync()
    {
        ErroImportacao = null;

        string? arquivo = _dialogService.SelecionarArquivo(
            "Selecionar banco de dados",
            "Arquivos suportados|*.eco;*.fbk;*.gbk;*.zip;*.rar;*.7z|Todos os arquivos|*.*");

        if (arquivo is null) return;

        IsImportandoBanco   = true;
        ProgressoImportacao = 0;
        MensagemProgresso   = "Analisando arquivo...";

        try
        {
            var progresso = new Progress<DatabaseImportProgress>(p =>
            {
                MensagemProgresso   = p.Mensagem;
                ProgressoImportacao = p.Percentual;
            });

            var resultado = await _databaseImportService.ProcessarArquivoAsync(arquivo, progresso);

            if (resultado.Format == DatabaseImportFormat.Invalid)
            {
                ErroImportacao = resultado.Erro ?? "Formato do arquivo inválido.";
                return;
            }

            if (resultado.Format == DatabaseImportFormat.Backup)
            {
                bool confirmar = _dialogService.Confirmar(
                    "Restauração necessária",
                    "Será preciso restaurar a base. Deseja restaurar agora?",
                    "Restaurar");

                if (!confirmar) return;

                string nomeBackup = Path.GetFileNameWithoutExtension(resultado.ArquivoPath!);
                string? apelidoBkp = _dialogService.SolicitarTexto(
                    "Nomear banco de dados",
                    "Escolha um apelido para o banco restaurado (nome do arquivo .eco de destino):",
                    nomeBackup);

                if (apelidoBkp is null) return;

                string destinoRestore = System.IO.Path.Combine(
                    EcoPathConstants.DadosDir, apelidoBkp + ".eco");

                if (System.IO.File.Exists(destinoRestore))
                {
                    bool substituir = _dialogService.Confirmar(
                        "Banco já existe",
                        $"Já existe um banco com o nome \"{apelidoBkp}\" na pasta de dados. Deseja substituí-lo?",
                        "Substituir");

                    if (!substituir) return;

                    try
                    {
                        System.IO.File.Delete(destinoRestore);
                    }
                    catch (Exception ex)
                    {
                        ErroImportacao = $"Não foi possível remover o banco existente: {ex.Message}";
                        return;
                    }
                }

                _restoreJobService.Iniciar(resultado.ArquivoPath!, destinoRestore, apelidoBkp);

                MensagemProgresso = $"Restauração de \"{apelidoBkp}\" iniciada em segundo plano.";

                var novoBancoRestore = new EcoDatabase
                {
                    NomeCompleto = apelidoBkp,
                    EcoPath      = destinoRestore
                };

                Bancos.Add(novoBancoRestore);
                BancoSelecionado = novoBancoRestore;
                return;
            }

            // .eco — solicita apelido e move para dados
            string nomeAtual = Path.GetFileNameWithoutExtension(resultado.ArquivoPath!);
            string? apelido  = _dialogService.SolicitarTexto(
                "Nomear banco de dados",
                "Escolha um apelido para o banco (será usado como nome do arquivo .eco):",
                nomeAtual);

            if (apelido is null) return;

            MensagemProgresso   = "Movendo banco para a pasta de dados...";
            ProgressoImportacao = 0;

            string? novoCaminho = null;
            try
            {
                novoCaminho = await _databaseImportService.MoverEcoParaDadosAsync(
                    resultado.ArquivoPath!, apelido);
            }
            catch (IOException)
            {
                novoCaminho = await TentarLiberarEMoverAsync(resultado.ArquivoPath!, apelido);
            }

            if (novoCaminho is null) return;

            var novoBanco = new EcoDatabase
            {
                NomeCompleto = apelido,
                EcoPath      = novoCaminho
            };

            Bancos.Add(novoBanco);
            BancoSelecionado = novoBanco;
        }
        catch (Exception ex)
        {
            ErroImportacao = ex.Message;
        }
        finally
        {
            IsImportandoBanco         = false;
            IsIndeterminadaImportacao = false;
            MensagemProgresso         = string.Empty;
            ProgressoImportacao       = 0;
        }
    }

    private async System.Threading.Tasks.Task<string?> TentarLiberarEMoverAsync(
        string arquivoEco, string apelido)
    {
        var travadores = _fileLockerService.ObterProcessosTravando(arquivoEco);

        if (travadores.Count == 0)
            throw new IOException("O arquivo está em uso por outro processo, " +
                "mas não foi possível identificar qual. Feche os programas que possam estar usando o arquivo e tente novamente.");

        string nomes = string.Join(", ",
            travadores.Select(p => p.ProcessName).Distinct(StringComparer.OrdinalIgnoreCase));

        string msg = travadores.Count == 1
            ? $"O arquivo está sendo usado pelo processo:\n\n• {nomes}\n\nDeseja encerrá-lo para prosseguir com a importação?"
            : $"O arquivo está sendo usado pelos processos:\n\n{string.Join("\n", travadores.Select(p => p.ProcessName).Distinct().Select(n => $"• {n}"))}\n\nDeseja encerrá-los para prosseguir com a importação?";

        bool confirmar = _dialogService.Confirmar(
            "Arquivo em uso",
            msg,
            "Encerrar e Importar");

        if (!confirmar) return null;

        MensagemProgresso = "Encerrando processos...";
        foreach (var (id, _) in travadores)
            _fileLockerService.EncerrarProcesso(id);

        await System.Threading.Tasks.Task.Delay(800);

        MensagemProgresso = "Movendo banco para a pasta de dados...";
        return await _databaseImportService.MoverEcoParaDadosAsync(arquivoEco, apelido);
    }

    private async System.Threading.Tasks.Task AdicionarExeAsync()
    {
        ErroImportacaoExe = null;

        string? arquivo = _dialogService.SelecionarArquivo(
            "Selecionar executável ECO",
            "Todos os arquivos|*.*|Pacotes compactados|*.zip;*.rar;*.7z|Executável ECO|eco.exe;*.exe");

        if (arquivo is null) return;

        IsImportandoExe   = true;
        ProgressoExe      = 0;
        MensagemProgressoExe = "Analisando arquivo...";

        try
        {
            var progresso = new Progress<DatabaseImportProgress>(p =>
            {
                MensagemProgressoExe = p.Mensagem;
                if (p.Percentual >= 0) ProgressoExe = p.Percentual;
            });

            var resultado = await _executableImportService.ProcessarArquivoAsync(arquivo, progresso);

            if (!resultado.Sucesso)
            {
                ErroImportacaoExe = resultado.Erro ?? "Erro ao processar o arquivo.";
                return;
            }

            var versaoBuild = _dialogService.SolicitarVersaoBuild();
            if (versaoBuild is null) return;

            var majorCompacto = EcoVersionHelper.NormalizarMajorInput(versaoBuild.Value.Major);
            if (majorCompacto is null)
            {
                ErroImportacaoExe = "Major inválida. Use formato 1.4, 1.5, 14 ou 15.";
                return;
            }

            var segmentoVersao = EcoVersionHelper.ConstruirSegmentoVersaoExe(majorCompacto, versaoBuild.Value.Versao);
            if (segmentoVersao is null)
            {
                ErroImportacaoExe = "Não foi possível montar a versão do executável com a major informada.";
                return;
            }

            var nomeExe    = $"Eco_{segmentoVersao}_{versaoBuild.Value.Build}.exe";
            var destinoExe = System.IO.Path.Combine(EcoPathConstants.UtilsDir, nomeExe);

            bool substituir = false;
            if (System.IO.File.Exists(destinoExe))
            {
                bool confirmar = _dialogService.Confirmar(
                    "Executável já existe",
                    $"Já existe um executável \"{nomeExe}\" na pasta Utils. Deseja substituí-lo?",
                    "Substituir");

                if (!confirmar) return;
                substituir = true;
            }

            MensagemProgressoExe = "Instalando executável...";
            ProgressoExe         = 0;

            var novoExe = await _executableImportService.InstalarExecutavelAsync(
                resultado.ArquivoPath!,
                segmentoVersao,
                versaoBuild.Value.Build,
                substituir);

            if (substituir)
            {
                var idx = -1;
                for (int i = 0; i < Executaveis.Count; i++)
                {
                    if (string.Equals(Executaveis[i].ExePath, destinoExe, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0)
                    Executaveis[idx] = novoExe;
                else
                    Executaveis.Add(novoExe);
            }
            else
            {
                Executaveis.Add(novoExe);
            }

            MajorSelecionado      = EcoVersionHelper.FormatarMajor(majorCompacto);
            VersaoSelecionada     = EcoVersionHelper.ExtrairVersaoExeSemMajor(novoExe.NomeCompleto);
            ExecutavelSelecionado = novoExe;
        }
        catch (Exception ex)
        {
            ErroImportacaoExe = ex.Message;
        }
        finally
        {
            IsImportandoExe      = false;
            MensagemProgressoExe = string.Empty;
            ProgressoExe         = 0;
        }
    }

    private async System.Threading.Tasks.Task ConfirmarAsync()
    {
        if (!PodeConfirmar) return;

        ErroConfirmacao = null;

        var modoDescricao = _instanciaExistente is null ? "criação" : "edição";
        _log.Info(nameof(ConfirmarAsync), $"Confirmando {modoDescricao} de instância: \"{Apelido.Trim()}\"");

        EcoInstance? instancia = null;
        bool instanciaPublicada = false;

        try
        {
            string exePath;
            string iniPath;

            var prefs = BuildPreferencias();

            bool emCriacao = _instanciaExistente is null;

            if (emCriacao)
            {
                instancia = new EcoInstance
                {
                    Id                      = Guid.NewGuid(),
                    Apelido                 = Apelido.Trim(),
                    ExecutavelPath          = string.Empty,
                    ExecutavelFontePath     = ExecutavelSelecionado?.ExePath ?? string.Empty,
                    ExecutavelNome          = ExecutavelSelecionado?.NomeCompleto ?? string.Empty,
                    BasePath                = BancoSelecionado!.EcoPath,
                    BaseNome                = BancoSelecionado.NomeCompleto,
                    IniPath                 = string.Empty,
                    VersaoBanco             = _versaoBancoRaw ?? string.Empty,
                    UsarVersaoExecutavel    = UsarVersaoExecutavel,
                    VersaoBancoOriginal     = _instanciaExistente?.VersaoBancoOriginal ?? string.Empty,
                    VersaoFirebird          = VersaoFirebird,
                    PreferenciasIniPendente = ExecutavelSelecionado is null ? prefs : null,
                    CriacaoEmAndamento      = true,
                    UltimaMensagemRestauracao = "Preparando executável e eco.ini..."
                };

                await _onConfirmado(instancia);
                instanciaPublicada = true;
                _fecharFlyout();
            }

            // ── Resolver porta e DLL conforme versão do Firebird ───────────
            var s = _userSettingsService.Settings;
            bool eFb25 = string.Equals(VersaoFirebird, "2.5", StringComparison.Ordinal);
            string porta   = eFb25 ? s.PortaFirebird25   : s.PortaFirebird50;
            string dllPath = eFb25 ? s.DllFirebird25Path : s.DllFirebird50Path;

            if (string.IsNullOrEmpty(dllPath))
            {
                if (eFb25 && File.Exists(EcoPathConstants.FirebirdLegacyDll))
                {
                    // Migra fbclient.dll legada para a pasta estruturada
                    Directory.CreateDirectory(EcoPathConstants.Firebird25Dir);
                    File.Copy(EcoPathConstants.FirebirdLegacyDll, EcoPathConstants.Firebird25DllPadrao, overwrite: true);
                    dllPath = EcoPathConstants.Firebird25DllPadrao;
                    s.DllFirebird25Path = dllPath;
                    await _userSettingsService.SalvarAsync();
                    _log.Info(nameof(ConfirmarAsync), $"fbclient.dll legada migrada para '{dllPath}'.");
                }
                else if (!eFb25 && File.Exists(EcoPathConstants.Firebird50DllPadrao))
                {
                    dllPath = EcoPathConstants.Firebird50DllPadrao;
                }
            }

            bool avisoDll = !File.Exists(dllPath);
            if (avisoDll)
                _log.Info(nameof(ConfirmarAsync), $"fbclient.dll não encontrada para FB {VersaoFirebird}: '{dllPath}'.");

            var opcoes = new ImplantarOpcoes(prefs, VersaoFirebird, porta, dllPath);

            if (ExecutavelSelecionado is not null)
            {
                bool fonteAlterada = _instanciaExistente is null
                    || _instanciaExistente.ExecutavelFontePath != ExecutavelSelecionado.ExePath
                    || _instanciaExistente.BasePath            != BancoSelecionado!.EcoPath;

                if (fonteAlterada)
                {
                    // Remove arquivos implantados anteriores (no-op em criação nova)
                    if (_instanciaExistente is not null)
                        _instanceSetupService.Remover(
                            _instanciaExistente.ExecutavelPath,
                            _instanciaExistente.IniPath);

                    (exePath, iniPath) = await _instanceSetupService.ImplantarAsync(
                        ExecutavelSelecionado.ExePath,
                        BancoSelecionado!.EcoPath,
                        opcoes);
                }
                else
                {
                    // Executável e base não mudaram — reutiliza os arquivos já implantados
                    exePath = _instanciaExistente!.ExecutavelPath;
                    iniPath = _instanciaExistente.IniPath;
                    await _instanceSetupService.AtualizarPreferenciasAsync(iniPath, opcoes);
                }
            }
            else
            {
                // Banco em restauração — executável será vinculado automaticamente ao final da restauração
                exePath = string.Empty;
                iniPath  = string.Empty;
            }

            // ── Versão forçada do banco ──────────────────────────────────────────────
            string versaoBancoFinal    = _versaoBancoRaw ?? string.Empty;
            string versaoBancoOriginal = string.Empty;

            if (UsarVersaoExecutavel && ExecutavelSelecionado is not null && !BaseEmRestauracao && _versaoBancoRaw is not null)
            {
                var versaoExe = ExtrairVersaoExe(ExecutavelSelecionado.NomeCompleto);
                if (versaoExe is null)
                {
                    ErroConfirmacao = "Não foi possível extrair a versão do executável para alterar o banco.";
                    return;
                }

                var novaVersaoDB = ConstruirVersaoDBComExe(_versaoBancoRaw, versaoExe);
                if (novaVersaoDB is null)
                {
                    ErroConfirmacao = "Formato da versão do banco não suportado para alteração.";
                    return;
                }

                // Preserva o original somente na primeira vez que o override é ativado
                versaoBancoOriginal = (!string.IsNullOrEmpty(_instanciaExistente?.VersaoBancoOriginal) && _instanciaExistente.UsarVersaoExecutavel)
                    ? _instanciaExistente.VersaoBancoOriginal
                    : _versaoBancoRaw;

                await _databaseVersionService.AlterarVersaoAsync(
                    BancoSelecionado!.EcoPath,
                    novaVersaoDB,
                    porta);
                versaoBancoFinal = novaVersaoDB;
                _log.Info(nameof(ConfirmarAsync), $"Versão do banco forçada para '{novaVersaoDB}' (original: '{versaoBancoOriginal}') em '{BancoSelecionado.EcoPath}'.");
            }
            else if (!UsarVersaoExecutavel && _instanciaExistente is not null)
            {
                // Mantém o original registrado para que o botão de restauração ainda funcione
                versaoBancoOriginal = _instanciaExistente.VersaoBancoOriginal;
            }

            instancia ??= new EcoInstance
            {
                Id = _instanciaExistente?.Id ?? Guid.NewGuid()
            };

            instancia.Apelido                 = Apelido.Trim();
            instancia.ExecutavelPath          = exePath;
            instancia.ExecutavelFontePath     = ExecutavelSelecionado?.ExePath ?? string.Empty;
            instancia.ExecutavelNome          = ExecutavelSelecionado?.NomeCompleto ?? string.Empty;
            instancia.BasePath                = BancoSelecionado!.EcoPath;
            instancia.BaseNome                = BancoSelecionado.NomeCompleto;
            instancia.IniPath                 = iniPath;
            instancia.VersaoBanco             = versaoBancoFinal;
            instancia.UsarVersaoExecutavel    = UsarVersaoExecutavel;
            instancia.VersaoBancoOriginal     = versaoBancoOriginal;
            instancia.VersaoFirebird          = VersaoFirebird;
            instancia.AvisoDllFirebird        = avisoDll;
            instancia.PreferenciasIniPendente = ExecutavelSelecionado is null ? prefs : null;
            instancia.CriacaoEmAndamento      = false;
            instancia.UltimaMensagemRestauracao = null;
            instancia.ErroRestauracao         = null;

            await _onConfirmado(instancia);

            if (_restoreJobService.EstaRestaurando(instancia.BasePath))
                instancia.StatusRestauracao = RestoreJobStatus.Restaurando;

            _log.Info(nameof(ConfirmarAsync), $"Instância \"{instancia.Apelido}\" salva com sucesso. BasePath={instancia.BasePath}");
            if (!instanciaPublicada)
                _fecharFlyout();
        }
        catch (Exception ex)
        {
            if (instanciaPublicada && instancia is not null)
            {
                instancia.CriacaoEmAndamento = false;
                instancia.StatusRestauracao = RestoreJobStatus.Falhou;
                instancia.ErroRestauracao = $"Falha ao concluir criação: {ex.Message}";
                await _onConfirmado(instancia);

                _dialogService.Notificar(
                    "Falha ao concluir criação",
                    $"A instância foi adicionada à lista, mas a implantação não foi concluída.\n\n{ex.Message}");
            }
            else
            {
                ErroConfirmacao = ex.Message;
            }
        }
    }

    private async System.Threading.Tasks.Task RestaurarVersaoOriginalAsync()
    {
        if (_instanciaExistente is null || string.IsNullOrEmpty(_instanciaExistente.VersaoBancoOriginal)) return;

        bool confirmar = _dialogService.Confirmar(
            "Restaurar versão original",
            $"Isso irá reverter a versão do banco para \"{_instanciaExistente.VersaoBancoOriginal}\". Deseja continuar?",
            "Restaurar");

        if (!confirmar) return;

        try
        {
            await _databaseVersionService.AlterarVersaoAsync(
                _instanciaExistente.BasePath,
                _instanciaExistente.VersaoBancoOriginal,
                ObterPortaFirebird(_instanciaExistente.VersaoFirebird));

            _log.Info(nameof(RestaurarVersaoOriginalAsync),
                $"Versão original '{_instanciaExistente.VersaoBancoOriginal}' restaurada em '{_instanciaExistente.BasePath}'.");

            var instanciaRestaurada = new EcoInstance
            {
                Id                   = _instanciaExistente.Id,
                Apelido              = _instanciaExistente.Apelido,
                ExecutavelPath       = _instanciaExistente.ExecutavelPath,
                ExecutavelFontePath  = _instanciaExistente.ExecutavelFontePath,
                ExecutavelNome       = _instanciaExistente.ExecutavelNome,
                BasePath             = _instanciaExistente.BasePath,
                BaseNome             = _instanciaExistente.BaseNome,
                IniPath              = _instanciaExistente.IniPath,
                VersaoBanco          = _instanciaExistente.VersaoBancoOriginal,
                UsarVersaoExecutavel = false,
                VersaoBancoOriginal  = string.Empty,
                VersaoFirebird       = _instanciaExistente.VersaoFirebird,
                AvisoDllFirebird     = _instanciaExistente.AvisoDllFirebird,
            };

            await _onConfirmado(instanciaRestaurada);

            // Atualiza o estado da instância existente para refletir a restauração
            // sem fechar o flyout, permitindo que o usuário continue editando.
            var versaoRestaurada = _instanciaExistente.VersaoBancoOriginal;
            _instanciaExistente.UsarVersaoExecutavel = false;
            _instanciaExistente.VersaoBancoOriginal  = string.Empty;

            _versaoBancoRaw          = versaoRestaurada;
            UsarVersaoExecutavel     = false;
            var v = EcoVersionHelper.FormatarVersao(versaoRestaurada);
            StatusBancoVersao        = v is not null ? $"Versão {v}" : $"Versão: {versaoRestaurada}";

            AtualizarCompatibilidade();
            OnPropertyChanged(nameof(PodeRestaurarVersaoOriginal));
            OnPropertyChanged(nameof(PodeUsarVersaoExecutavel));
            OnPropertyChanged(nameof(PodeAlterarUsarVersaoExecutavel));
        }
        catch (Exception ex)
        {
            ErroConfirmacao = $"Erro ao restaurar versão original: {ex.Message}";
        }
    }

    // "14650000" + "651" → "14651000"  (substitui os dígitos do meio)
    // "15001000" + "15002" → "15002000"  (strip do prefixo major embutido no nome do exe)
    private static string? ConstruirVersaoDBComExe(string versaoBancoRaw, string versaoExe)
        => EcoVersionHelper.ConstruirVersaoDBComExe(versaoBancoRaw, versaoExe);

    private static string? ExtrairVersaoExe(string nomeCompleto)
        => EcoVersionHelper.ExtrairSegmentoVersaoExe(nomeCompleto);
}
