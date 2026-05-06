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
        ILogService log)
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
                    var versaoExe   = ExtrairVersaoExeNome(inst.ExecutavelNome);
                    inst.VersaoIncompativel = versaoBanco is not null && versaoExe is not null
                        && !string.Equals(versaoBanco, versaoExe, StringComparison.OrdinalIgnoreCase);
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
                async instancia =>
                {
                    Instancias.Add(instancia);
                    var job = _restoreJobService.ObterPorDestino(instancia.BasePath);
                    if (job != null) VincularJobAInstancia(instancia, job);

                    var vBanco = ExtrairVersaoBancoRaw(instancia.VersaoBanco);
                    var vExe   = ExtrairVersaoExeNome(instancia.ExecutavelNome);
                    instancia.VersaoIncompativel = vBanco is not null && vExe is not null
                        && !string.Equals(vBanco, vExe, StringComparison.OrdinalIgnoreCase);

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
                async instanciaEditada =>
                {
                    var idx = Instancias.IndexOf(instancia);
                    if (idx >= 0) Instancias[idx] = instanciaEditada;
                    var job = _restoreJobService.ObterPorDestino(instanciaEditada.BasePath);
                    if (job != null) VincularJobAInstancia(instanciaEditada, job);

                    // Recalcula incompatibilidade com base na versão já armazenada
                    var vBanco = ExtrairVersaoBancoRaw(instanciaEditada.VersaoBanco);
                    var vExe   = ExtrairVersaoExeNome(instanciaEditada.ExecutavelNome);
                    instanciaEditada.VersaoIncompativel = vBanco is not null && vExe is not null
                        && !string.Equals(vBanco, vExe, StringComparison.OrdinalIgnoreCase);

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
        if (!_dialogService.Confirmar("Excluir instância", $"Excluir \"{instancia.Apelido}\"?", "Excluir"))
            return;

        _log.Info(nameof(ExcluirInstanciaAsync), $"Excluindo instância \"{instancia.Apelido}\" ({instancia.Id})");

        EncerrarProcessosDoExe(instancia.ExecutavelPath);

        try
        {
            _instanceSetupService.Remover(instancia.ExecutavelPath, instancia.IniPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error(nameof(ExcluirInstanciaAsync), ex);
            _dialogService.Notificar("Não foi possível excluir",
                "Acesso negado ao tentar excluir os arquivos da instância mesmo após encerrar os processos associados.\n\nVerifique manualmente se algum processo ainda está usando o arquivo.");
            return;
        }
        catch (Exception ex)
        {
            _log.Error(nameof(ExcluirInstanciaAsync), ex);
            _dialogService.Notificar("Não foi possível excluir",
                $"Erro ao excluir os arquivos da instância:\n\n{ex.Message}");
            return;
        }

        Instancias.Remove(instancia);
        await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
        _log.Info(nameof(ExcluirInstanciaAsync), $"Instância \"{instancia.Apelido}\" removida com sucesso.");
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

            await Task.Delay(TimeSpan.FromSeconds(30));
            inst.StatusRestauracao         = null;
            inst.UltimaMensagemRestauracao = null;
        }
        else if (job.Status == RestoreJobStatus.Falhou)
        {
            _log.Warn(nameof(OnJobFinalizado), $"Restauração falhou para \"{inst.Apelido}\": {job.Erro}");
        }
    }

    // "14650000" → "650"  (mesmo algoritmo do flyout)
    private static string? ExtrairVersaoBancoRaw(string raw)
    {
        if (raw.Length > 5 && raw.All(char.IsDigit))
            return raw.Substring(2, raw.Length - 5);
        return null;
    }

    // "Eco_650_10" → "650"
    private static string? ExtrairVersaoExeNome(string nomeCompleto)
    {
        var partes = nomeCompleto.Split('_');
        return partes.Length >= 2 ? partes[1] : null;
    }

    private async Task AtualizarVersaoBancoAsync(EcoInstance inst)
    {
        var raw = await _databaseVersionService.ConsultarVersaoAsync(inst.BasePath);
        if (raw is null)
        {
            _log.Warn(nameof(AtualizarVersaoBancoAsync), $"Não foi possível obter versão do banco para \"{inst.Apelido}\" ({inst.BasePath})");
            return;
        }

        inst.VersaoBanco = raw;

        var versaoBanco = ExtrairVersaoBancoRaw(raw);
        var versaoExe   = ExtrairVersaoExeNome(inst.ExecutavelNome);
        inst.VersaoIncompativel = versaoBanco is not null && versaoExe is not null
            && !string.Equals(versaoBanco, versaoExe, StringComparison.OrdinalIgnoreCase);

        _log.Info(nameof(AtualizarVersaoBancoAsync),
            $"Versão do banco \"{inst.Apelido}\": raw={raw}, extraída={versaoBanco ?? "n/a"}, exe={versaoExe ?? "n/a"}, incompatível={inst.VersaoIncompativel}");

        await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
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

