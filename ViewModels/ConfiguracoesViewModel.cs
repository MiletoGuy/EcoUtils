using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using EcoUtils.Commands;
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

    private int _limiteLinhasConsulta;
    public int LimiteLinhasConsulta
    {
        get => _limiteLinhasConsulta;
        set => SetProperty(ref _limiteLinhasConsulta, value);
    }

    public ICommand SalvarCommand         { get; }
    public ICommand CancelarCommand       { get; }
    public ICommand BrowseIbExpertCommand { get; }

    // ── Patchnotes ───────────────────────────────────────────────
    public IReadOnlyList<PatchnotePatch> Patches { get; }

    public ConfiguracoesViewModel(IUserSettingsService userSettingsService, Action fechar)
    {
        _userSettingsService    = userSettingsService;
        _fechar                  = fechar;
        _ibExpertPath            = userSettingsService.Settings.IbExpertPath;
        _limiteLinhasConsulta    = userSettingsService.Settings.LimiteLinhasConsulta;

        NavGeralCommand      = new RelayCommand(_ => AbaAtiva = AbaConfiguracoes.Geral);
        NavPatchnotesCommand = new RelayCommand(_ => AbaAtiva = AbaConfiguracoes.Patchnotes);

        SalvarCommand = new AsyncRelayCommand(async _ =>
        {
            _userSettingsService.Settings.IbExpertPath         = IbExpertPath.Trim();
            _userSettingsService.Settings.LimiteLinhasConsulta  = LimiteLinhasConsulta <= 0 ? 1000 : LimiteLinhasConsulta;
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

        Patches = CarregarPatches();
    }

    /// <summary>Sincroniza campos com o valor salvo atual (ao abrir o painel).</summary>
    public void Resetar()
    {
        IbExpertPath          = _userSettingsService.Settings.IbExpertPath;
        LimiteLinhasConsulta  = _userSettingsService.Settings.LimiteLinhasConsulta;
        AbaAtiva              = AbaConfiguracoes.Geral;
    }

    private static IReadOnlyList<PatchnotePatch> CarregarPatches()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "PatchNotes");
            if (!Directory.Exists(dir)) return [];

            return Directory.GetFiles(dir, "v*.md")
                .Select(f =>
                {
                    var nome   = Path.GetFileNameWithoutExtension(f);
                    var partes = nome.TrimStart('v').Split('.');
                    return new
                    {
                        Major    = partes.Length > 0 ? int.Parse(partes[0]) : 0,
                        Minor    = partes.Length > 1 ? int.Parse(partes[1]) : 0,
                        PatchNum = partes.Length > 2 ? int.Parse(partes[2]) : 0,
                        Label    = nome,
                        Conteudo = File.ReadAllText(f),
                    };
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
