using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Infrastructure;
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
    private readonly IDatabaseImportService     _databaseImportService;
    private readonly IExecutableImportService   _executableImportService;
    private readonly IRestoreJobService        _restoreJobService;
    private readonly IFileLockerService        _fileLockerService;
    private readonly ILaunchService            _launchService;
    private readonly IDialogService            _dialogService;
    private readonly ILogService               _log;
    private readonly IUserSettingsService      _userSettingsService;

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

    private bool _mostrarVersaoOriginal = false;
    public bool MostrarVersaoOriginal
    {
        get => _mostrarVersaoOriginal;
        set => SetProperty(ref _mostrarVersaoOriginal, value);
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
    public ICommand CopiarCaminhoBaseCommand  { get; }
    public ICommand OrdenarCommand              { get; }
    public ICommand ToggleConfigColunasCommand  { get; }
    public ICommand CancelarRestauracaoCommand  { get; }

    public ExecutarEcoViewModel(
        IInstanceRepository instanceRepository,
        IVersionCatalogService versionCatalogService,
        IDatabaseDiscoveryService databaseDiscoveryService,
        IDatabaseVersionService databaseVersionService,
        IInstanceSetupService instanceSetupService,
        IDatabaseImportService databaseImportService,
        IExecutableImportService executableImportService,
        IRestoreJobService restoreJobService,
        IFileLockerService fileLockerService,
        ILaunchService launchService,
        IDialogService dialogService,
        ILogService log,
        IUserSettingsService userSettingsService)
    {
        _instanceRepository       = instanceRepository;
        _versionCatalogService    = versionCatalogService;
        _databaseDiscoveryService = databaseDiscoveryService;
        _databaseVersionService   = databaseVersionService;
        _instanceSetupService     = instanceSetupService;
        _databaseImportService    = databaseImportService;
        _executableImportService  = executableImportService;
        _restoreJobService        = restoreJobService;
        _fileLockerService        = fileLockerService;
        _launchService            = launchService;
        _dialogService            = dialogService;
        _log                      = log;
        _userSettingsService      = userSettingsService;

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
        CopiarCaminhoBaseCommand  = new RelayCommand(inst => System.Windows.Clipboard.SetText(((EcoInstance)inst!).BasePath));
        OrdenarCommand             = new RelayCommand(col => AplicarOrdenacaoPorColuna((string)col!));
        ToggleConfigColunasCommand    = new RelayCommand(_ => ConfigColunasAberto = !ConfigColunasAberto);
        CancelarRestauracaoCommand     = new RelayCommand(inst => _restoreJobService.Cancelar(((EcoInstance)inst!).BasePath));

        _restoreJobService.JobFinalizado += OnJobFinalizado;

        _ = CarregarInstanciasAsync();
    }

    private async Task CarregarInstanciasAsync()
    {
        try
        {
            var lista = await _instanceRepository.CarregarAsync();
            foreach (var inst in lista)
            {
                Instancias.Add(inst);

                var job = _restoreJobService.ObterPorDestino(inst.BasePath);
                if (job != null)
                {
                    VincularJobAInstancia(inst, job);
                }
                else if (!string.IsNullOrEmpty(inst.BasePath) && !File.Exists(inst.BasePath))
                {
                    inst.StatusRestauracao = RestoreJobStatus.Falhou;
                    inst.ErroRestauracao   = "Arquivo de base não encontrado. A restauração pode ter sido interrompida.";
                }
                else if (!string.IsNullOrEmpty(inst.BasePath) && File.Exists(inst.BasePath)
                         && !string.IsNullOrEmpty(inst.ExecutavelNome))
                {
                    var versaoBanco = ExtrairVersaoBancoRaw(inst.VersaoBanco);
                    var majorBanco  = EcoVersionHelper.ExtrairMajor(inst.VersaoBanco);
                    var versaoExe   = ExtrairVersaoExeNome(inst.ExecutavelNome);
                    var majorExe    = EcoVersionHelper.ExtrairMajorExe(inst.ExecutavelNome);
                    inst.VersaoIncompativel = versaoBanco is not null
                        && majorBanco is not null
                        && versaoExe is not null
                        && majorExe is not null
                        && (!string.Equals(versaoBanco, versaoExe, StringComparison.OrdinalIgnoreCase)
                            || !string.Equals(majorBanco, majorExe, StringComparison.OrdinalIgnoreCase));
                }
            }
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
        try
        {
            var apelidosExistentes = Instancias.Select(i => i.Apelido).ToList();
            FlyoutVM = new InstanceFlyoutViewModel(
                _versionCatalogService,
                _databaseDiscoveryService,
                _databaseVersionService,
                _instanceSetupService,
                _databaseImportService,
                _executableImportService,
                _restoreJobService,
                _fileLockerService,
                _dialogService,
                _log,
                _userSettingsService,
                async instancia =>
                {
                    Instancias.Add(instancia);
                    var job = _restoreJobService.ObterPorDestino(instancia.BasePath);
                    if (job != null) VincularJobAInstancia(instancia, job);

                    var vBanco = ExtrairVersaoBancoRaw(instancia.VersaoBanco);
                    var majorB = EcoVersionHelper.ExtrairMajor(instancia.VersaoBanco);
                    var vExe   = ExtrairVersaoExeNome(instancia.ExecutavelNome);
                    var majorE = EcoVersionHelper.ExtrairMajorExe(instancia.ExecutavelNome);
                    instancia.VersaoIncompativel = vBanco is not null
                        && majorB is not null
                        && vExe is not null
                        && majorE is not null
                        && (!string.Equals(vBanco, vExe, StringComparison.OrdinalIgnoreCase)
                            || !string.Equals(majorB, majorE, StringComparison.OrdinalIgnoreCase));

                    await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
                },
                () => FlyoutAberto = false,
                apelidosExistentes);
            FlyoutAberto = true;
        }
        catch (Exception ex)
        {
            _log.Error(nameof(AbrirFlyoutNovo), ex);
            _dialogService.Notificar("Erro ao abrir formulário",
                $"Não foi possível abrir o formulário de nova instância.\n\n{ex.Message}");
        }
    }

    private void AbrirFlyoutEditar(EcoInstance instancia)
    {
        try
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
                _databaseImportService,
                _executableImportService,
                _restoreJobService,
                _fileLockerService,
                _dialogService,
                _log,
                _userSettingsService,
                async instanciaEditada =>
                {
                    // Busca por Id para tolerar substituições intermediárias feitas
                    // durante a sessão de edição (ex.: restauração de versão original).
                    var idx = -1;
                    for (int i = 0; i < Instancias.Count; i++)
                        if (Instancias[i].Id == instanciaEditada.Id) { idx = i; break; }

                    if (idx >= 0) Instancias[idx] = instanciaEditada;
                    var job = _restoreJobService.ObterPorDestino(instanciaEditada.BasePath);
                    if (job != null) VincularJobAInstancia(instanciaEditada, job);

                    // Recalcula incompatibilidade com base na versão já armazenada
                    var vBanco = ExtrairVersaoBancoRaw(instanciaEditada.VersaoBanco);
                    var majorB = EcoVersionHelper.ExtrairMajor(instanciaEditada.VersaoBanco);
                    var vExe   = ExtrairVersaoExeNome(instanciaEditada.ExecutavelNome);
                    var majorE = EcoVersionHelper.ExtrairMajorExe(instanciaEditada.ExecutavelNome);
                    instanciaEditada.VersaoIncompativel = vBanco is not null
                        && majorB is not null
                        && vExe is not null
                        && majorE is not null
                        && (!string.Equals(vBanco, vExe, StringComparison.OrdinalIgnoreCase)
                            || !string.Equals(majorB, majorE, StringComparison.OrdinalIgnoreCase));

                    await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
                },
                () => FlyoutAberto = false,
                apelidosExistentes,
                instancia);
            FlyoutAberto = true;
        }
        catch (Exception ex)
        {
            _log.Error(nameof(AbrirFlyoutEditar), ex);
            _dialogService.Notificar("Erro ao abrir formulário",
                $"Não foi possível abrir o formulário de edição.\n\n{ex.Message}");
        }
    }

    private async Task ExcluirInstanciaAsync(EcoInstance instancia)
    {
        bool confirmar;
        try
        {
            confirmar = _dialogService.Confirmar("Excluir instância", $"Excluir \"{instancia.Apelido}\"?", "Excluir");
        }
        catch (Exception ex)
        {
            _log.Error(nameof(ExcluirInstanciaAsync), ex);
            _dialogService.Notificar("Não foi possível excluir",
                $"Falha ao abrir a confirmação de exclusão.\n\n{ex.Message}");
            return;
        }

        if (!confirmar) return;

        _log.Info(nameof(ExcluirInstanciaAsync), $"Excluindo instância \"{instancia.Apelido}\" ({instancia.Id})");

        EncerrarProcessosDoExe(instancia.ExecutavelPath);
        EncerrarProcessosTravandoArquivo(instancia.ExecutavelPath);
        EncerrarProcessosTravandoArquivo(instancia.IniPath);

        string? avisoArquivos = null;

        try
        {
            _instanceSetupService.Remover(instancia.ExecutavelPath, instancia.IniPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error(nameof(ExcluirInstanciaAsync), ex);
            avisoArquivos = "A instância será removida da lista, mas alguns arquivos não puderam ser excluídos (acesso negado).";
        }
        catch (Exception ex)
        {
            _log.Error(nameof(ExcluirInstanciaAsync), ex);
            avisoArquivos = $"A instância será removida da lista, mas houve erro ao excluir arquivos:\n\n{ex.Message}";
        }

        var idxOriginal = Instancias.IndexOf(instancia);
        if (idxOriginal < 0)
            idxOriginal = Instancias.Count;

        Instancias.Remove(instancia);

        try
        {
            await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
        }
        catch (Exception ex)
        {
            _log.Error(nameof(ExcluirInstanciaAsync), ex);
            Instancias.Insert(Math.Min(idxOriginal, Instancias.Count), instancia);
            _dialogService.Notificar("Não foi possível excluir",
                $"Falha ao salvar a remoção da instância.\n\n{ex.Message}");
            return;
        }

        _log.Info(nameof(ExcluirInstanciaAsync), $"Instância \"{instancia.Apelido}\" removida com sucesso.");

        if (avisoArquivos is not null)
            _dialogService.Notificar("Instância removida com aviso", avisoArquivos);
    }

    private void EncerrarProcessosDoExe(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath)) return;

        try
        {
            foreach (var processo in Process.GetProcesses())
            {
                try
                {
                    if (string.Equals(processo.MainModule?.FileName, exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        _log.Info(nameof(EncerrarProcessosDoExe),
                            $"Encerrando processo PID={processo.Id} ({processo.ProcessName}) que usa \"{exePath}\"");
                        processo.Kill(entireProcessTree: true);
                        processo.WaitForExit(3000);
                    }
                }
                catch { /* processo já encerrou ou sem acesso ao módulo — ignorar */ }
                finally { processo.Dispose(); }
            }
        }
        catch (Exception ex)
        {
            _log.Warn(nameof(EncerrarProcessosDoExe), $"Falha ao enumerar processos: {ex.Message}");
        }
    }

    private void EncerrarProcessosTravandoArquivo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        try
        {
            var travadores = _fileLockerService.ObterProcessosTravando(path);
            foreach (var (processId, processName) in travadores)
            {
                _log.Info(nameof(EncerrarProcessosTravandoArquivo),
                    $"Encerrando processo travando arquivo (PID={processId}, Nome={processName}, Arquivo={path})");
                _fileLockerService.EncerrarProcesso(processId);
            }
        }
        catch (Exception ex)
        {
            _log.Warn(nameof(EncerrarProcessosTravandoArquivo),
                $"Falha ao encerrar processos travando '{path}': {ex.Message}");
        }
    }

    private async Task IniciarEcoAsync(EcoInstance instancia)
    {
        if (instancia.StatusRestauracao == RestoreJobStatus.Restaurando)
        {
            _log.Warn(nameof(IniciarEcoAsync), $"Tentativa de iniciar instância em restauração: {instancia.Apelido}");
            return;
        }

        _log.Info(nameof(IniciarEcoAsync), $"Iniciando instância \"{instancia.Apelido}\" com exe \"{instancia.ExecutavelNome}\"");
        var (sucesso, erro) = await _launchService.ExecutarAsync(instancia);
        if (!sucesso)
        {
            _log.Warn(nameof(IniciarEcoAsync), $"Falha ao iniciar \"{instancia.Apelido}\": {erro}");
            _dialogService.Notificar("Erro ao executar", erro ?? "Erro desconhecido.");
        }
        else
        {
            _log.Info(nameof(IniciarEcoAsync), $"Instância \"{instancia.Apelido}\" iniciada com sucesso.");
        }
    }

    private void VincularJobAInstancia(EcoInstance inst, RestoreJobEntry job)
    {
        inst.StatusRestauracao         = job.Status;
        inst.UltimaMensagemRestauracao = job.UltimaMensagem;
        inst.ErroRestauracao           = job.Erro;

        job.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RestoreJobEntry.UltimaMensagem))
                inst.UltimaMensagemRestauracao = job.UltimaMensagem;
            if (e.PropertyName == nameof(RestoreJobEntry.Status))
                inst.StatusRestauracao = job.Status;
            if (e.PropertyName == nameof(RestoreJobEntry.Erro))
                inst.ErroRestauracao = job.Erro;
        };
    }

    private async void OnJobFinalizado(object? sender, RestoreJobEntry job)
    {
        try
        {
            var inst = Instancias.FirstOrDefault(i => i.BasePath == job.DestinoEco);
            if (inst == null)
            {
                _log.Warn(nameof(OnJobFinalizado), $"Job finalizado para destino sem instância vinculada: {job.DestinoEco}");
                return;
            }

            inst.StatusRestauracao = job.Status;
            inst.ErroRestauracao   = job.Erro;

            if (job.Status == RestoreJobStatus.Concluido)
            {
                _log.Info(nameof(OnJobFinalizado), $"Restauração concluída para \"{inst.Apelido}\". Consultando versão do banco...");
                await AtualizarVersaoBancoAsync(inst);

                if (string.IsNullOrEmpty(inst.ExecutavelFontePath))
                    await TentarAutoSelecionarExeAsync(inst);

                await Task.Delay(TimeSpan.FromSeconds(30));
                if (inst.StatusRestauracao != RestoreJobStatus.Falhou)
                {
                    inst.StatusRestauracao         = null;
                    inst.UltimaMensagemRestauracao = null;
                }
            }
            else if (job.Status == RestoreJobStatus.Falhou)
            {
                _log.Warn(nameof(OnJobFinalizado), $"Restauração falhou para \"{inst.Apelido}\": {job.Erro}");
            }
        }
        catch (Exception ex)
        {
            _log.Error(nameof(OnJobFinalizado), ex);
        }
    }

    private async Task TentarAutoSelecionarExeAsync(EcoInstance inst)
    {
        _log.Info(nameof(TentarAutoSelecionarExeAsync), $"Tentando auto-selecionar executável para \"{inst.Apelido}\"...");

        var executaveis  = await _versionCatalogService.ListarExecutaveisAsync();
        var versaoBanco  = ExtrairVersaoBancoRaw(inst.VersaoBanco);
        var major        = EcoVersionHelper.ExtrairMajor(inst.VersaoBanco);

        EcoExecutavel? melhorExe = null;
        if (versaoBanco is not null)
        {
            melhorExe = executaveis
                .Where(e => string.Equals(EcoVersionHelper.ExtrairMajorExe(e.NomeCompleto), major, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(ExtrairVersaoExeNome(e.NomeCompleto), versaoBanco, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.NumeroBuild, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        if (melhorExe is null)
        {
            var detalhe = versaoBanco is not null
                ? $"nenhum executável compatível com a versão {versaoBanco} foi encontrado"
                : "não foi possível determinar a versão do banco";
            _log.Warn(nameof(TentarAutoSelecionarExeAsync),
                $"Auto-seleção falhou para \"{inst.Apelido}\": {detalhe}.");
            inst.StatusRestauracao = RestoreJobStatus.Falhou;
            inst.ErroRestauracao   = $"Restauração concluída, mas {detalhe}. Edite a instância para configurar o executável.";
            await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
            return;
        }

        try
        {
            var prefs  = inst.PreferenciasIniPendente ?? new IniPreferencias();
            bool eFb25 = string.Equals(inst.VersaoFirebird, "2.5", StringComparison.Ordinal);
            var s      = _userSettingsService.Settings;
            var opcoes = new ImplantarOpcoes(
                prefs,
                inst.VersaoFirebird,
                eFb25 ? s.PortaFirebird25   : s.PortaFirebird50,
                eFb25 ? s.DllFirebird25Path : s.DllFirebird50Path);
            var (exePath, iniPath) = await _instanceSetupService.ImplantarAsync(melhorExe.ExePath, inst.BasePath, opcoes);

            inst.ExecutavelPath          = exePath;
            inst.ExecutavelFontePath     = melhorExe.ExePath;
            inst.ExecutavelNome          = melhorExe.NomeCompleto;
            inst.IniPath                 = iniPath;
            inst.VersaoIncompativel      = false;
            inst.PreferenciasIniPendente = null;

            _log.Info(nameof(TentarAutoSelecionarExeAsync),
                $"Executável \"{melhorExe.NomeCompleto}\" implantado automaticamente para \"{inst.Apelido}\".");

            await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
        }
        catch (Exception ex)
        {
            _log.Error(nameof(TentarAutoSelecionarExeAsync), ex);
            inst.StatusRestauracao = RestoreJobStatus.Falhou;
            inst.ErroRestauracao   = $"Restauração concluída, mas falha ao implantar o executável automaticamente: {ex.Message}";
        }
    }

    // "14650000" → "650"  (mesmo algoritmo do flyout)
    private static string? ExtrairVersaoBancoRaw(string raw)
        => EcoVersionHelper.ExtrairVersao(raw);

    // "Eco_650_10" → "650"
    // "Eco_15001_10" → "001"
    private static string? ExtrairVersaoExeNome(string nomeCompleto)
        => EcoVersionHelper.ExtrairVersaoExeSemMajor(nomeCompleto);

    private async Task AtualizarVersaoBancoAsync(EcoInstance inst)
    {
        var raw = await _databaseVersionService.ConsultarVersaoAsync(
            inst.BasePath,
            ObterPortaFirebird(inst.VersaoFirebird));
        if (raw is null)
        {
            _log.Warn(nameof(AtualizarVersaoBancoAsync), $"Não foi possível obter versão do banco para \"{inst.Apelido}\" ({inst.BasePath})");
            return;
        }

        inst.VersaoBanco = raw;

        var versaoBanco = ExtrairVersaoBancoRaw(raw);
        var majorBanco  = EcoVersionHelper.ExtrairMajor(raw);
        var versaoExe   = ExtrairVersaoExeNome(inst.ExecutavelNome);
        var majorExe    = EcoVersionHelper.ExtrairMajorExe(inst.ExecutavelNome);
        inst.VersaoIncompativel = versaoBanco is not null
            && majorBanco is not null
            && versaoExe is not null
            && majorExe is not null
            && (!string.Equals(versaoBanco, versaoExe, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(majorBanco, majorExe, StringComparison.OrdinalIgnoreCase));

        _log.Info(nameof(AtualizarVersaoBancoAsync),
            $"Versão do banco \"{inst.Apelido}\": raw={raw}, major={majorBanco ?? "n/a"}, versão={versaoBanco ?? "n/a"}, exeMajor={majorExe ?? "n/a"}, exeVersao={versaoExe ?? "n/a"}, incompatível={inst.VersaoIncompativel}");

        await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
    }

    private string ObterPortaFirebird(string versaoFirebird)
    {
        var settings = _userSettingsService.Settings;
        return string.Equals(versaoFirebird, "2.5", StringComparison.Ordinal)
            ? settings.PortaFirebird25
            : settings.PortaFirebird50;
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

