using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Infrastructure;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;
using Microsoft.Win32;

namespace EcoUtils.ViewModels;

public enum AbaConfiguracoes { Geral, Patchnotes }

public class PatchnotePatch
{
    public string Versao                        { get; init; } = "";
    public string Resumo                        { get; init; } = "";
    public IReadOnlyList<string> Features       { get; init; } = [];
    public IReadOnlyList<string> BugsCorrigidos { get; init; } = [];
    public bool TemResumo  => !string.IsNullOrWhiteSpace(Resumo);
    public bool TemFeatures => Features.Count > 0;
    public bool TemBugs     => BugsCorrigidos.Count > 0;
}

public class ConfiguracoesViewModel : ViewModelBase
{
    private readonly IUserSettingsService  _userSettingsService;
    private readonly IUpdateService        _updateService;
    private readonly IDialogService        _dialogService;
    private readonly IInstanceRepository   _instanceRepository;
    private readonly IInstanceSetupService _instanceSetupService;
    private readonly ILogService           _log;
    private readonly Action                _fechar;

    // ── Navegação interna ────────────────────────────────────────
    private AbaConfiguracoes _abaAtiva = AbaConfiguracoes.Geral;
    public AbaConfiguracoes AbaAtiva
    {
        get => _abaAtiva;
        set
        {
            SetProperty(ref _abaAtiva, value);
            OnPropertyChanged(nameof(AbaGeralAtiva));
            OnPropertyChanged(nameof(AbaPatchnotesAtiva));
        }
    }

    public bool AbaGeralAtiva       => _abaAtiva == AbaConfiguracoes.Geral;
    public bool AbaPatchnotesAtiva  => _abaAtiva == AbaConfiguracoes.Patchnotes;

    public ICommand NavGeralCommand      { get; }
    public ICommand NavPatchnotesCommand { get; }

    // ── Firebird ─────────────────────────────────────────
    private string _portaFirebird25 = string.Empty;
    public string PortaFirebird25
    {
        get => _portaFirebird25;
        set => SetProperty(ref _portaFirebird25, value);
    }

    private string _portaFirebird50 = string.Empty;
    public string PortaFirebird50
    {
        get => _portaFirebird50;
        set => SetProperty(ref _portaFirebird50, value);
    }

    private string _dllFirebird25Path = string.Empty;
    public string DllFirebird25Path
    {
        get => _dllFirebird25Path;
        set => SetProperty(ref _dllFirebird25Path, value);
    }

    private string _dllFirebird50Path = string.Empty;
    public string DllFirebird50Path
    {
        get => _dllFirebird50Path;
        set => SetProperty(ref _dllFirebird50Path, value);
    }

    // ── Postgres (TGERCONFIGURACAO.CONFIGURACAO) ─────────────────────────
    private bool _sobrescreverConfiguracaoPostgres;
    public bool SobrescreverConfiguracaoPostgres
    {
        get => _sobrescreverConfiguracaoPostgres;
        set
        {
            if (!SetProperty(ref _sobrescreverConfiguracaoPostgres, value)) return;
            if (value) AplicarDefaultsPostgresSeVazio();
        }
    }

    private string _postgresIpServidor = string.Empty;
    public string PostgresIpServidor
    {
        get => _postgresIpServidor;
        set => SetProperty(ref _postgresIpServidor, value);
    }

    private string _postgresPortaServidor = string.Empty;
    public string PostgresPortaServidor
    {
        get => _postgresPortaServidor;
        set => SetProperty(ref _postgresPortaServidor, value);
    }

    private string _postgresUsuarioServidor = string.Empty;
    public string PostgresUsuarioServidor
    {
        get => _postgresUsuarioServidor;
        set => SetProperty(ref _postgresUsuarioServidor, value);
    }

    private string _postgresSenhaServidor = string.Empty;
    public string PostgresSenhaServidor
    {
        get => _postgresSenhaServidor;
        set => SetProperty(ref _postgresSenhaServidor, value);
    }

    private string _postgresNomeBanco = string.Empty;
    public string PostgresNomeBanco
    {
        get => _postgresNomeBanco;
        set => SetProperty(ref _postgresNomeBanco, value);
    }

    private static string DefaultPostgresIp => "LOCALHOST";
    private static string DefaultPostgresPorta => "5432";
    private static string DefaultPostgresUsuario => "postgres";
    private static string DefaultPostgresSenha => "postgres";

    // ── Versão do Utils ─────────────────────────────────────────
    public string VersaoAtual => _updateService.VersaoAtual;

    public ObservableCollection<UpdateInfo> VersoesDisponiveis { get; } = new();

    private UpdateInfo? _versaoSelecionada;
    public UpdateInfo? VersaoSelecionada
    {
        get => _versaoSelecionada;
        set
        {
            SetProperty(ref _versaoSelecionada, value);
            (TrocarVersaoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool _isCarregandoVersoes;
    public bool IsCarregandoVersoes
    {
        get => _isCarregandoVersoes;
        private set
        {
            SetProperty(ref _isCarregandoVersoes, value);
            OnPropertyChanged(nameof(PodeInteragirComVersoes));
            (TrocarVersaoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool _isTrocandoVersao;
    public bool IsTrocandoVersao
    {
        get => _isTrocandoVersao;
        private set
        {
            SetProperty(ref _isTrocandoVersao, value);
            OnPropertyChanged(nameof(PodeInteragirComVersoes));
            (TrocarVersaoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool PodeInteragirComVersoes => !IsCarregandoVersoes && !IsTrocandoVersao;

    private string? _erroVersoes;
    public string? ErroVersoes
    {
        get => _erroVersoes;
        private set
        {
            SetProperty(ref _erroVersoes, value);
            OnPropertyChanged(nameof(TemErroVersoes));
        }
    }

    public bool TemErroVersoes => !string.IsNullOrEmpty(ErroVersoes);

    private string? _mensagemInlineGeral;
    public string? MensagemInlineGeral
    {
        get => _mensagemInlineGeral;
        private set
        {
            SetProperty(ref _mensagemInlineGeral, value);
            OnPropertyChanged(nameof(TemMensagemInlineGeral));
        }
    }

    public bool TemMensagemInlineGeral => !string.IsNullOrWhiteSpace(MensagemInlineGeral);

    private bool _mensagemInlineGeralErro;
    public bool MensagemInlineGeralErro
    {
        get => _mensagemInlineGeralErro;
        private set => SetProperty(ref _mensagemInlineGeralErro, value);
    }

    public ICommand SalvarCommand               { get; }
    public ICommand CancelarCommand             { get; }

    public ICommand BrowseFirebird25DllCommand  { get; }
    public ICommand BrowseFirebird50DllCommand  { get; }
    public AsyncRelayCommand TrocarVersaoCommand { get; }

    // ── Patchnotes ───────────────────────────────────────────────
    public IReadOnlyList<PatchnotePatch> Patches { get; }

    public ConfiguracoesViewModel(
        IUserSettingsService  userSettingsService,
        IUpdateService        updateService,
        IDialogService        dialogService,
        IInstanceRepository   instanceRepository,
        IInstanceSetupService instanceSetupService,
        ILogService           log,
        Action                fechar)
    {
        _userSettingsService  = userSettingsService;
        _updateService        = updateService;
        _dialogService        = dialogService;
        _instanceRepository   = instanceRepository;
        _instanceSetupService = instanceSetupService;
        _log                  = log;
        _fechar               = fechar;
        _portaFirebird25      = userSettingsService.Settings.PortaFirebird25;
        _portaFirebird50      = userSettingsService.Settings.PortaFirebird50;
        _dllFirebird25Path    = userSettingsService.Settings.DllFirebird25Path;
        _dllFirebird50Path    = userSettingsService.Settings.DllFirebird50Path;
        _sobrescreverConfiguracaoPostgres = userSettingsService.Settings.SobrescreverConfiguracaoPostgres;
        _postgresIpServidor               = userSettingsService.Settings.PostgresIpServidor;
        _postgresPortaServidor            = userSettingsService.Settings.PostgresPortaServidor;
        _postgresUsuarioServidor          = userSettingsService.Settings.PostgresUsuarioServidor;
        _postgresSenhaServidor            = userSettingsService.Settings.PostgresSenhaServidor;
        _postgresNomeBanco                = userSettingsService.Settings.PostgresNomeBanco;

        if (_sobrescreverConfiguracaoPostgres)
            AplicarDefaultsPostgresSeVazio();

        NavGeralCommand      = new RelayCommand(_ => AbaAtiva = AbaConfiguracoes.Geral);
        NavPatchnotesCommand = new RelayCommand(_ => AbaAtiva = AbaConfiguracoes.Patchnotes);

        SalvarCommand = new AsyncRelayCommand(async _ => await SalvarAsync());

        CancelarCommand = new RelayCommand(_ => _fechar());

        BrowseFirebird25DllCommand = new RelayCommand(_ =>
        {
            var caminho = BrowseDll(DllFirebird25Path);
            if (caminho is not null) DllFirebird25Path = caminho;
        });

        BrowseFirebird50DllCommand = new RelayCommand(_ =>
        {
            var caminho = BrowseDll(DllFirebird50Path);
            if (caminho is not null) DllFirebird50Path = caminho;
        });

        TrocarVersaoCommand = new AsyncRelayCommand(
            async _ => await TrocarVersaoAsync(),
            _ => VersaoSelecionada is not null
                 && !string.Equals(VersaoSelecionada.Versao, VersaoAtual, StringComparison.OrdinalIgnoreCase)
                 && !IsTrocandoVersao
                 && !IsCarregandoVersoes);

        Patches = CarregarPatches();
    }

    private static string MontarMensagemInline(string resumo, string proximoPasso, string? detalhe = null)
    {
        if (string.IsNullOrWhiteSpace(detalhe))
            return $"{resumo} {proximoPasso}";

        return $"{resumo} {proximoPasso} Detalhe: {detalhe}";
    }

    /// <summary>Sincroniza os campos com os valores atuais salvos (ao abrir o painel).</summary>
    public void Resetar()
    {
        var s = _userSettingsService.Settings;
        PortaFirebird25   = s.PortaFirebird25;
        PortaFirebird50   = s.PortaFirebird50;
        DllFirebird25Path = string.IsNullOrEmpty(s.DllFirebird25Path) && File.Exists(EcoPathConstants.Firebird25DllPadrao)
            ? EcoPathConstants.Firebird25DllPadrao
            : s.DllFirebird25Path;
        DllFirebird50Path = string.IsNullOrEmpty(s.DllFirebird50Path) && File.Exists(EcoPathConstants.Firebird50DllPadrao)
            ? EcoPathConstants.Firebird50DllPadrao
            : s.DllFirebird50Path;
        SobrescreverConfiguracaoPostgres = s.SobrescreverConfiguracaoPostgres;
        PostgresIpServidor = s.PostgresIpServidor;
        PostgresPortaServidor = s.PostgresPortaServidor;
        PostgresUsuarioServidor = s.PostgresUsuarioServidor;
        PostgresSenhaServidor = s.PostgresSenhaServidor;
        PostgresNomeBanco = s.PostgresNomeBanco;

        if (SobrescreverConfiguracaoPostgres)
            AplicarDefaultsPostgresSeVazio();

        MensagemInlineGeral     = null;
        MensagemInlineGeralErro = false;
        AbaAtiva = AbaConfiguracoes.Geral;
        _ = CarregarVersoesAsync();
    }

    private async Task SalvarAsync()
    {
        MensagemInlineGeral = null;
        MensagemInlineGeralErro = false;

        var porta25 = PortaFirebird25.Trim();
        var porta50 = PortaFirebird50.Trim();
        if (!int.TryParse(porta25, out var p25) || p25 <= 0 || p25 > 65535
            || !int.TryParse(porta50, out var p50) || p50 <= 0 || p50 > 65535)
        {
            MensagemInlineGeralErro = true;
            MensagemInlineGeral = "Porta inválida. Use valores entre 1 e 65535 para Firebird 2.5 e 5.0.";
            return;
        }

        var sobrescreverPostgres = SobrescreverConfiguracaoPostgres;
        var postgresIp = PostgresIpServidor.Trim();
        var postgresPorta = PostgresPortaServidor.Trim();
        var postgresUsuario = PostgresUsuarioServidor.Trim();
        var postgresSenha = PostgresSenhaServidor.Trim();
        var postgresNomeBanco = PostgresNomeBanco.Trim();

        if (sobrescreverPostgres)
        {
            if (string.IsNullOrWhiteSpace(postgresIp)
                || string.IsNullOrWhiteSpace(postgresUsuario)
                || string.IsNullOrWhiteSpace(postgresSenha))
            {
                MensagemInlineGeralErro = true;
                MensagemInlineGeral = "Preencha IP, usuário e senha do Postgres para sobrescrever a configuração.";
                return;
            }

            if (!int.TryParse(postgresPorta, out var portaPg) || portaPg <= 0 || portaPg > 65535)
            {
                MensagemInlineGeralErro = true;
                MensagemInlineGeral = "Porta do Postgres inválida. Use valores entre 1 e 65535.";
                return;
            }
        }

        try
        {
            var s = _userSettingsService.Settings;
            var portaAntiga25 = s.PortaFirebird25;
            var portaAntiga50 = s.PortaFirebird50;

            s.PortaFirebird25   = porta25;
            s.PortaFirebird50   = porta50;
            s.DllFirebird25Path = DllFirebird25Path.Trim();
            s.DllFirebird50Path = DllFirebird50Path.Trim();
            s.SobrescreverConfiguracaoPostgres = sobrescreverPostgres;
            s.PostgresIpServidor = postgresIp;
            s.PostgresPortaServidor = postgresPorta;
            s.PostgresUsuarioServidor = postgresUsuario;
            s.PostgresSenhaServidor = postgresSenha;
            s.PostgresNomeBanco = postgresNomeBanco;
            await _userSettingsService.SalvarAsync();

            // Propaga mudança de porta para todos os .ini existentes da versão afetada
            bool portaMudou25 = !string.Equals(portaAntiga25, s.PortaFirebird25, StringComparison.Ordinal);
            bool portaMudou50 = !string.Equals(portaAntiga50, s.PortaFirebird50, StringComparison.Ordinal);

            if (portaMudou25 || portaMudou50)
            {
                var (_, falhas) = await PropagarPortasAsync(portaMudou25, portaMudou50, s);
                if (falhas > 0)
                {
                    MensagemInlineGeralErro = false;
                    MensagemInlineGeral = $"Configurações salvas, mas {falhas} instância(s) não tiveram o .ini atualizado automaticamente. Revise os logs e tente novamente.";
                    return;
                }
            }

            _fechar();
        }
        catch (Exception ex)
        {
            _log.Error(nameof(SalvarAsync), ex);
            MensagemInlineGeralErro = true;
            MensagemInlineGeral = MontarMensagemInline(
                "Não foi possível salvar as configurações.",
                "Revise os campos e tente novamente.",
                ex.Message);
        }
    }

    private async Task<(int Atualizados, int Falhas)> PropagarPortasAsync(bool fb25, bool fb50, Models.UserSettings s)
    {
        IReadOnlyList<Models.EcoInstance> instancias;
        try
        {
            instancias = await _instanceRepository.CarregarAsync();
        }
        catch (Exception ex)
        {
            _log.Error(nameof(PropagarPortasAsync), ex);
            return (0, 1);
        }

        var atualizados = 0;
        var falhas = 0;

        foreach (var inst in instancias)
        {
            if (string.IsNullOrEmpty(inst.IniPath) || !File.Exists(inst.IniPath))
                continue;

            bool ehFb25 = string.Equals(inst.VersaoFirebird, "2.5", StringComparison.Ordinal);
            bool ehFb50 = string.Equals(inst.VersaoFirebird, "5.0", StringComparison.Ordinal);

            if ((fb25 && ehFb25) || (fb50 && ehFb50))
            {
                var porta = ehFb25 ? s.PortaFirebird25 : s.PortaFirebird50;
                var dll   = ehFb25 ? s.DllFirebird25Path : s.DllFirebird50Path;
                var opcoes = new ImplantarOpcoes(null, inst.VersaoFirebird, porta, dll);
                try
                {
                    await _instanceSetupService.AtualizarSecoesFbAsync(inst.IniPath, opcoes);
                    atualizados++;
                }
                catch (Exception ex)
                {
                    falhas++;
                    _log.Error(nameof(PropagarPortasAsync),
                        new Exception($"Falha ao atualizar .ini de '{inst.Apelido}': {ex.Message}", ex));
                }
            }
        }

        return (atualizados, falhas);
    }

    private static string? BrowseDll(string caminhoAtual)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Localizar fbclient.dll",
            Filter = "fbclient.dll|fbclient.dll|Bibliotecas (*.dll)|*.dll",
        };

        if (File.Exists(caminhoAtual))
            dlg.InitialDirectory = Path.GetDirectoryName(caminhoAtual);
        else if (!string.IsNullOrEmpty(caminhoAtual) && Directory.Exists(Path.GetDirectoryName(caminhoAtual)))
            dlg.InitialDirectory = Path.GetDirectoryName(caminhoAtual);

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private void AplicarDefaultsPostgresSeVazio()
    {
        if (string.IsNullOrWhiteSpace(PostgresIpServidor))
            PostgresIpServidor = DefaultPostgresIp;
        if (string.IsNullOrWhiteSpace(PostgresPortaServidor))
            PostgresPortaServidor = DefaultPostgresPorta;
        if (string.IsNullOrWhiteSpace(PostgresUsuarioServidor))
            PostgresUsuarioServidor = DefaultPostgresUsuario;
        if (string.IsNullOrWhiteSpace(PostgresSenhaServidor))
            PostgresSenhaServidor = DefaultPostgresSenha;
    }

    private async Task CarregarVersoesAsync()
    {
        IsCarregandoVersoes = true;
        ErroVersoes         = null;
        try
        {
            var versoes = await _updateService.ListarVersoesAsync();
            VersoesDisponiveis.Clear();
            foreach (var v in versoes)
                VersoesDisponiveis.Add(v);

            VersaoSelecionada = VersoesDisponiveis
                .FirstOrDefault(v => string.Equals(v.Versao, VersaoAtual, StringComparison.OrdinalIgnoreCase));

            if (VersoesDisponiveis.Count == 0)
                ErroVersoes = "Não encontramos versões disponíveis agora. Verifique sua conexão e tente novamente.";
        }
        catch (Exception ex)
        {
            ErroVersoes = MontarMensagemInline(
                "Não foi possível carregar as versões agora.",
                "Verifique sua conexão e tente novamente.",
                ex.Message);
        }
        finally
        {
            IsCarregandoVersoes = false;
        }
    }

    private async Task TrocarVersaoAsync()
    {
        if (VersaoSelecionada is null) return;

        // Regra UX: confirmação com reinício é obrigatória e permanece modal.
        bool confirmar = _dialogService.Confirmar(
            "Trocar versão",
            $"O EcoUtils será reiniciado para instalar a versão {VersaoSelecionada.Versao}.\n\nDeseja continuar?",
            "Trocar e Reiniciar");

        if (!confirmar) return;

        IsTrocandoVersao = true;
        try
        {
            await _updateService.AtualizarAsync(VersaoSelecionada);
        }
        catch (Exception ex)
        {
            ErroVersoes = MontarMensagemInline(
                "Não foi possível trocar a versão agora.",
                "Tente novamente em alguns instantes.",
                ex.Message);
            IsTrocandoVersao = false;
        }
    }

    private static IReadOnlyList<PatchnotePatch> CarregarPatches()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string prefix = "PatchNotes.";

            return assembly.GetManifestResourceNames()
                .Where(n => n.Contains(prefix, StringComparison.OrdinalIgnoreCase)
                         && n.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Select(name =>
                {
                    // nome do recurso: EcoUtils.Documentos.PatchNotes.v0.2.6.md
                    // extrai o label (ex: "v0.2.6") removendo prefixo e ".md"
                    var idx   = name.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) + prefix.Length;
                    var label = name.Substring(idx, name.Length - idx - ".md".Length);

                    var partes = label.TrimStart('v').Split('.');
                    int major    = partes.Length > 0 && int.TryParse(partes[0], out var maj) ? maj : 0;
                    int minor    = partes.Length > 1 && int.TryParse(partes[1], out var min) ? min : 0;
                    int patchNum = partes.Length > 2 && int.TryParse(partes[2], out var pat) ? pat : 0;

                    using var stream  = assembly.GetManifestResourceStream(name)!;
                    using var reader  = new StreamReader(stream);
                    var conteudo = reader.ReadToEnd();

                    return new { Major = major, Minor = minor, PatchNum = patchNum, Label = label, Conteudo = conteudo };
                })
                .OrderByDescending(p => p.Major)
                .ThenByDescending(p => p.Minor)
                .ThenByDescending(p => p.PatchNum)
                .Select(p => ParsePatch(p.Label, p.Conteudo))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static PatchnotePatch ParsePatch(string versao, string markdown)
    {
        var resumo          = new System.Text.StringBuilder();
        var features        = new List<string>();
        var bugsCorrigidos  = new List<string>();

        // Seção atual: null=preâmbulo/resumo, "FEATURES", "BUGS"
        string? secao = null;

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd();

            // Pular linha de título da versão (# vX.Y.Z)
            if (line.StartsWith("# ")) continue;

            // Separadores horizontais
            if (line.StartsWith("---")) continue;

            // Detecta cabeçalhos de seção
            var upper = line.TrimStart('#', ' ').ToUpperInvariant();
            if (line.StartsWith("#"))
            {
                if (upper.Contains("FEATURE"))      secao = "FEATURES";
                else if (upper.Contains("BUG"))      secao = "BUGS";
                else                                 secao = null;
                continue;
            }

            // Bullet points
            if (line.TrimStart().StartsWith("- "))
            {
                var item = line.TrimStart().Substring(2).Trim();
                if (secao == "FEATURES")     features.Add(item);
                else if (secao == "BUGS")    bugsCorrigidos.Add(item);
                continue;
            }

            // Texto de resumo (linhas fora de seção específica)
            if (secao == null && !string.IsNullOrWhiteSpace(line))
            {
                if (resumo.Length > 0) resumo.Append(' ');
                resumo.Append(line.Trim());
            }
        }

        return new PatchnotePatch
        {
            Versao          = versao,
            Resumo          = resumo.ToString(),
            Features        = features,
            BugsCorrigidos  = bugsCorrigidos,
        };
    }
}

