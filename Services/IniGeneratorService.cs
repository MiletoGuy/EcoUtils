using System.IO;
using System.Text.RegularExpressions;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class IniGeneratorService : IIniGeneratorService
{
    private static readonly Regex _iniSafeRegex =
        new(@"^Eco_.+\.ini$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<string> GerarIniAsync(string exeNome, string basePath)
    {
        var linhas = await File.ReadAllLinesAsync(EcoPathConstants.EcoIniPadrao);

        bool dentroWindowsSection = false;
        bool substituido = false;

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

        var iniDestino = Path.Combine(EcoPathConstants.WindowsDir, $"{exeNome}.ini");
        await File.WriteAllLinesAsync(iniDestino, linhas);

        return iniDestino;
    }

    public void RemoverIni(string iniPath)
    {
        if (string.IsNullOrWhiteSpace(iniPath))
            return;

        if (!File.Exists(iniPath))
            return;

        if (!_iniSafeRegex.IsMatch(Path.GetFileName(iniPath)))
            return;

        File.Delete(iniPath);
    }
}
