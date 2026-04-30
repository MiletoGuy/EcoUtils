using System.IO;
using System.Text.RegularExpressions;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class InstanceSetupService : IInstanceSetupService
{
    private static readonly Regex _implantadoRegex =
        new(@"^eco_[^_]+_[^_]+_\d+\.(exe|ini)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<(string ExePath, string IniPath)> ImplantarAsync(
        string exeFontePath,
        string basePath,
        IniPreferencias preferencias)
    {
        // 1. Deriva nome base: "Eco_650_10" → "eco_650_10"
        var nomeBase = "eco_" + Path.GetFileNameWithoutExtension(exeFontePath)
            .Substring("Eco_".Length);

        // 2. Determina próximo número sequencial disponível
        var seq = ProximoSequencial(nomeBase);

        var exeDestPath = Path.Combine(EcoPathConstants.WindowsDir, $"{nomeBase}_{seq}.exe");
        var iniDestPath = Path.Combine(EcoPathConstants.WindowsDir, $"{nomeBase}_{seq}.ini");

        // 3. Lê o eco.ini template e valida presença de dados= em [windows]
        var linhas = await File.ReadAllLinesAsync(EcoPathConstants.EcoIniPadrao);

        bool dentroWindowsSection = false;
        bool substituido          = false;

        for (int i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i].Trim();

            if (linha.StartsWith('['))
            {
                dentroWindowsSection = linha.Equals("[windows]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (dentroWindowsSection && linha.StartsWith("dados=", StringComparison.OrdinalIgnoreCase))
            {
                linhas[i] = $"dados={EcoPathConstants.EcoServerHost}:{basePath}";
                substituido = true;
            }
        }

        if (!substituido)
            throw new InvalidOperationException(
                "Chave 'dados=' não encontrada na seção [windows] do eco.ini padrão.");

        // 4. Aplica parâmetros de [PREFERENCIAS]
        linhas = AplicarPreferencias(linhas, preferencias);

        // 5. Copia executável (overwrite: false protege contra condição de corrida)
        File.Copy(exeFontePath, exeDestPath, overwrite: false);

        // 6. Grava .ini
        await File.WriteAllLinesAsync(iniDestPath, linhas);

        return (exeDestPath, iniDestPath);
    }

    public void Remover(string exePath, string iniPath)
    {
        TentarDeletar(exePath);
        TentarDeletar(iniPath);
    }

    public async Task<bool> ValidarEcoIniAsync()
    {
        if (!File.Exists(EcoPathConstants.EcoIniPadrao))
            return false;

        try
        {
            var linhas = await File.ReadAllLinesAsync(EcoPathConstants.EcoIniPadrao);

            bool dentroWindowsSection = false;

            foreach (var linha in linhas)
            {
                var t = linha.Trim();

                if (t.StartsWith('['))
                {
                    dentroWindowsSection = t.Equals("[windows]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (dentroWindowsSection && t.StartsWith("dados=", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { /* inacessível ou corrompido — considerado inválido */ }

        return false;
    }

    public async Task<IniPreferencias> LerPreferenciasAsync(string iniPath)
    {
        var prefs = new IniPreferencias
        {
            Usuario                  = string.Empty,
            PesquisaTotalDosProdutos = false,
            MonitorarTempoSelects    = false,
            SincronizaTabelaPreco    = false,
            MultiplasInstancias      = false,
        };

        if (!File.Exists(iniPath))
            return prefs;

        try
        {
            var linhas = await File.ReadAllLinesAsync(iniPath);
            bool dentroPrefs = false;

            foreach (var linha in linhas)
            {
                var t = linha.Trim();

                if (t.StartsWith('['))
                {
                    dentroPrefs = t.Equals("[preferencias]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!dentroPrefs) continue;

                var eqIdx = t.IndexOf('=');
                if (eqIdx <= 0) continue;

                var key = t.Substring(0, eqIdx);
                var val = t.Substring(eqIdx + 1).Trim();

                if      (key.Equals("usuario",                  StringComparison.OrdinalIgnoreCase))
                    prefs.Usuario = val;
                else if (key.Equals("PesquisaTotalDosProdutos", StringComparison.OrdinalIgnoreCase))
                    prefs.PesquisaTotalDosProdutos = val.Equals("S", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("MonitorarTempoSelects",    StringComparison.OrdinalIgnoreCase))
                    prefs.MonitorarTempoSelects = val.Equals("S", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("SincronizaTabelaPreco",    StringComparison.OrdinalIgnoreCase))
                    prefs.SincronizaTabelaPreco = val.Equals("S", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("MultiplasInstancias",      StringComparison.OrdinalIgnoreCase))
                    prefs.MultiplasInstancias = val.Equals("S", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { /* arquivo inacessível — retorna defaults */ }

        return prefs;
    }

    public async Task AtualizarPreferenciasAsync(string iniPath, IniPreferencias preferencias)
    {
        var linhas    = await File.ReadAllLinesAsync(iniPath);
        var resultado = AplicarPreferencias(linhas, preferencias);
        await File.WriteAllLinesAsync(iniPath, resultado);
    }

    private static string[] AplicarPreferencias(string[] linhas, IniPreferencias prefs)
    {
        var prefValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["usuario"]                  = prefs.Usuario,
            ["PesquisaTotalDosProdutos"] = prefs.PesquisaTotalDosProdutos ? "S" : "N",
            ["MonitorarTempoSelects"]    = prefs.MonitorarTempoSelects    ? "S" : "N",
            ["SincronizaTabelaPreco"]    = prefs.SincronizaTabelaPreco    ? "S" : "N",
            ["MultiplasInstancias"]      = prefs.MultiplasInstancias      ? "S" : "N",
        };

        var encontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultado   = new List<string>();
        bool dentroPrefs = false;

        foreach (var linha in linhas)
        {
            var t = linha.Trim();

            if (t.StartsWith('['))
            {
                if (dentroPrefs)
                {
                    foreach (var kv in prefValues)
                        if (!encontrados.Contains(kv.Key))
                            resultado.Add($"{kv.Key}={kv.Value}");
                }

                dentroPrefs = t.Equals("[preferencias]", StringComparison.OrdinalIgnoreCase);
                resultado.Add(linha);
                continue;
            }

            if (dentroPrefs)
            {
                var eqIdx = t.IndexOf('=');
                if (eqIdx > 0)
                {
                    var key = t.Substring(0, eqIdx);
                    if (prefValues.TryGetValue(key, out var newVal))
                    {
                        encontrados.Add(key);
                        resultado.Add($"{key}={newVal}");
                        continue;
                    }
                }
            }

            resultado.Add(linha);
        }

        // [preferencias] era a última seção
        if (dentroPrefs)
        {
            foreach (var kv in prefValues)
                if (!encontrados.Contains(kv.Key))
                    resultado.Add($"{kv.Key}={kv.Value}");
        }

        return resultado.ToArray();
    }

    private static void TentarDeletar(string path)
    {
        if (string.IsNullOrWhiteSpace(path))       return;
        if (!File.Exists(path))                    return;
        if (!_implantadoRegex.IsMatch(Path.GetFileName(path))) return;

        File.Delete(path);
    }

    private static int ProximoSequencial(string nomeBase)
    {
        if (!Directory.Exists(EcoPathConstants.WindowsDir))
            return 1;

        var prefix = nomeBase + "_";
        var max    = 0;

        foreach (var path in Directory.EnumerateFiles(EcoPathConstants.WindowsDir, $"{nomeBase}_*.exe"))
        {
            var nome = Path.GetFileNameWithoutExtension(path);
            if (!nome.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var sufixo = nome.Substring(prefix.Length);
            if (int.TryParse(sufixo, out var n) && n > max)
                max = n;
        }

        return max + 1;
    }
}
