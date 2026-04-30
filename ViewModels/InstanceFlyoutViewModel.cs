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
    private readonly IRestoreService              _restoreService;
    private readonly IDialogService               _dialogService;
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

    public ObservableCollection<EcoExecutavel> Executaveis { get; } = new();

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
            ConfirmarCommand.RaiseCanExecuteChanged();
        }
    }

    public bool NaoImportandoExe => !_isImportandoExe;

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

    public bool PodeConfirmar =>
        !string.IsNullOrWhiteSpace(Apelido) &&
        !ApelidoDuplicado                   &&
        ExecutavelSelecionado is not null    &&
        BancoSelecionado is not null         &&
        EcoIniValido                        &&
        !IsImportandoBanco &&
        !IsImportandoExe;

    public ICommand CancelarCommand  { get; }
    public AsyncRelayCommand ConfirmarCommand    { get; }
    public AsyncRelayCommand AdicionarBancoCommand { get; }
    public AsyncRelayCommand AdicionarExeCommand   { get; }

    public InstanceFlyoutViewModel(
        IVersionCatalogService versionCatalogService,
        IDatabaseDiscoveryService databaseDiscoveryService,
        IDatabaseVersionService databaseVersionService,
        IInstanceSetupService instanceSetupService,
        IDatabaseImportService databaseImportService,
        IExecutableImportService executableImportService,
        IRestoreService restoreService,
        IDialogService dialogService,
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
        _restoreService           = restoreService;
        _dialogService            = dialogService;
        _onConfirmado             = onConfirmado;
        _fecharFlyout             = fecharFlyout;
        _apelidosExistentes       = apelidosExistentes;
        _instanciaExistente       = instanciaExistente;

        if (instanciaExistente is not null)
            _apelido = instanciaExistente.Apelido;

        ConfirmarCommand      = new AsyncRelayCommand(async _ => await ConfirmarAsync(), _ => PodeConfirmar);
        CancelarCommand       = new RelayCommand(_ => _fecharFlyout());
        AdicionarBancoCommand = new AsyncRelayCommand(async _ => await AdicionarBancoAsync(), _ => !IsImportandoBanco && !IsImportandoExe);
        AdicionarExeCommand   = new AsyncRelayCommand(async _ => await AdicionarExeAsync(),   _ => !IsImportandoBanco && !IsImportandoExe);

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

    private async System.Threading.Tasks.Task AtualizarStatusBancoAsync()
    {
        if (BancoSelecionado is null)
        {
            _versaoBancoRaw   = null;
            StatusBancoVersao = string.Empty;
            AtualizarCompatibilidade();
            return;
        }

        StatusBancoVersao = "Consultando versão...";
        _versaoBancoRaw   = null;

        var versaoBancoRaw = await _databaseVersionService.ConsultarVersaoAsync(BancoSelecionado.EcoPath);
        _versaoBancoRaw = versaoBancoRaw;

        if (versaoBancoRaw is null)
        {
            StatusBancoVersao = "Não foi possível consultar a versão do banco.";
        }
        else
        {
            var versao = ExtrairVersaoBanco(versaoBancoRaw);
            StatusBancoVersao = versao is not null
                ? $"Versão {versao}"
                : $"Versão: {versaoBancoRaw}";
        }

        AtualizarCompatibilidade();
    }

    private void AtualizarCompatibilidade()
    {
        if (BancoSelecionado is null || ExecutavelSelecionado is null || _versaoBancoRaw is null)
        {
            StatusVersao     = string.Empty;
            VersaoCompativel = null;
            return;
        }

        var versaoBanco = ExtrairVersaoBanco(_versaoBancoRaw);
        var versaoExe   = ExtrairVersaoExe(ExecutavelSelecionado.NomeCompleto);

        if (versaoBanco is null || versaoExe is null)
        {
            StatusVersao     = $"Formato de versão não reconhecido para comparação.";
            VersaoCompativel = null;
            return;
        }

        VersaoCompativel = string.Equals(versaoBanco, versaoExe, StringComparison.Ordinal);
        StatusVersao = VersaoCompativel == true
            ? $"Versão {versaoExe} — banco e executável compatíveis."
            : $"Incompatibilidade: banco v{versaoBanco} × executável v{versaoExe}.";
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
                    ErroImportacao = $"Já existe um banco com o nome \"{apelidoBkp}\" em {EcoPathConstants.DadosDir}.";
                    return;
                }

                IsIndeterminadaImportacao = true;
                MensagemProgresso         = "Restaurando base de dados...";

                var progressoRestore = new Progress<DatabaseImportProgress>(p =>
                    MensagemProgresso = p.Mensagem);

                await _restoreService.RestaurarAsync(
                    resultado.ArquivoPath!, destinoRestore, progressoRestore);

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

            string novoCaminho = await _databaseImportService.MoverEcoParaDadosAsync(
                resultado.ArquivoPath!, apelido);

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

            var nomeExe    = $"Eco_{versaoBuild.Value.Versao}_{versaoBuild.Value.Build}.exe";
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
                versaoBuild.Value.Versao,
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
                IniPath             = iniPath,
                VersaoBanco         = _versaoBancoRaw ?? string.Empty
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
