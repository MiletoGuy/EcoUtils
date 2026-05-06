using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EcoUtils.Commands;
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
    private readonly IUserSettingsService _userSettingsService;
    private readonly IUpdateService       _updateService;
    private readonly IDialogService       _dialogService;
    private readonly Action               _fechar;

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

    // ── Geral ────────────────────────────────────────────────────
    private string _ibExpertPath;
    public string IbExpertPath
    {
        get => _ibExpertPath;
        set => SetProperty(ref _ibExpertPath, value);
    }

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

    public ICommand SalvarCommand         { get; }
    public ICommand CancelarCommand       { get; }
    public ICommand BrowseIbExpertCommand { get; }
    public AsyncRelayCommand TrocarVersaoCommand { get; }

    // ── Patchnotes ───────────────────────────────────────────────
    public IReadOnlyList<PatchnotePatch> Patches { get; }

    public ConfiguracoesViewModel(
        IUserSettingsService userSettingsService,
        IUpdateService updateService,
        IDialogService dialogService,
        Action fechar)
    {
        _userSettingsService = userSettingsService;
        _updateService       = updateService;
        _dialogService       = dialogService;
        _fechar              = fechar;
        _ibExpertPath        = userSettingsService.Settings.IbExpertPath;

        NavGeralCommand      = new RelayCommand(_ => AbaAtiva = AbaConfiguracoes.Geral);
        NavPatchnotesCommand = new RelayCommand(_ => AbaAtiva = AbaConfiguracoes.Patchnotes);

        SalvarCommand = new AsyncRelayCommand(async _ =>
        {
            _userSettingsService.Settings.IbExpertPath = IbExpertPath.Trim();
            await _userSettingsService.SalvarAsync();
            _fechar();
        });

        CancelarCommand = new RelayCommand(_ => _fechar());

        BrowseIbExpertCommand = new RelayCommand(_ =>
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Localizar IBExpert.exe",
                Filter = "IBExpert|IBExpert.exe|Executáveis (*.exe)|*.exe",
            };

            if (File.Exists(IbExpertPath))
                dlg.InitialDirectory = Path.GetDirectoryName(IbExpertPath);
            else if (Directory.Exists(Path.GetDirectoryName(IbExpertPath)))
                dlg.InitialDirectory = Path.GetDirectoryName(IbExpertPath);

            if (dlg.ShowDialog() == true)
                IbExpertPath = dlg.FileName;
        });

        TrocarVersaoCommand = new AsyncRelayCommand(
            async _ => await TrocarVersaoAsync(),
            _ => VersaoSelecionada is not null
                 && !string.Equals(VersaoSelecionada.Versao, VersaoAtual, StringComparison.OrdinalIgnoreCase)
                 && !IsTrocandoVersao
                 && !IsCarregandoVersoes);

        Patches = CarregarPatches();
    }

    /// <summary>Sincroniza os campos com os valores atuais salvos (ao abrir o painel).</summary>
    public void Resetar()
    {
        IbExpertPath = _userSettingsService.Settings.IbExpertPath;
        AbaAtiva     = AbaConfiguracoes.Geral;
        _ = CarregarVersoesAsync();
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
                ErroVersoes = "Nenhuma versão encontrada. Verifique sua conexão com a internet.";
        }
        catch (Exception ex)
        {
            ErroVersoes = $"Não foi possível carregar as versões: {ex.Message}";
        }
        finally
        {
            IsCarregandoVersoes = false;
        }
    }

    private async Task TrocarVersaoAsync()
    {
        if (VersaoSelecionada is null) return;

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
            ErroVersoes      = $"Erro ao trocar versão: {ex.Message}";
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

