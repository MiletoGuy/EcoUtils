using System.IO;
using System.Text.RegularExpressions;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class VersionCatalogService : IVersionCatalogService
{
    private static readonly Regex _exeRegex =
        new(@"^Eco_\d+_\d+\.exe$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<IReadOnlyList<EcoExecutavel>> ListarExecutaveisAsync()
    {
        return await Task.Run<IReadOnlyList<EcoExecutavel>>(() =>
        {
            try
            {
                if (!Directory.Exists(EcoPathConstants.WindowsDir))
                    return Array.Empty<EcoExecutavel>();

                var iniPadraoPresente = File.Exists(EcoPathConstants.EcoIniPadrao);

                var resultado = Directory
                    .EnumerateFiles(EcoPathConstants.WindowsDir, "*.exe")
                    .Where(path => _exeRegex.IsMatch(Path.GetFileName(path)))
                    .Select(path => new EcoExecutavel
                    {
                        NomeCompleto      = Path.GetFileNameWithoutExtension(path),
                        ExePath           = path,
                        IniPadraoPresente = iniPadraoPresente
                    })
                    .ToList();

                return resultado;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Array.Empty<EcoExecutavel>();
            }
        });
    }
}
