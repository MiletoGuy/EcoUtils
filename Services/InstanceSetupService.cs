using System.IO;
using System.Text.RegularExpressions;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class InstanceSetupService : IInstanceSetupService
{
    private static readonly Regex _implantadoRegex =
        new(@"^eco_[^_]+_[^_]+_\d+\.(exe|ini)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<(string ExePath, string IniPath)> ImplantarAsync(
        string exeFontePath,
        string basePath)
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

        // 4. Copia executável (overwrite: false protege contra condição de corrida)
        File.Copy(exeFontePath, exeDestPath, overwrite: false);

        // 5. Grava .ini
        await File.WriteAllLinesAsync(iniDestPath, linhas);

        return (exeDestPath, iniDestPath);
    }

    public void Remover(string exePath, string iniPath)
    {
        TentarDeletar(exePath);
        TentarDeletar(iniPath);
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
