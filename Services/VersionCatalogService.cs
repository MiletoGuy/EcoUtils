using System.IO;
using System.Text.RegularExpressions;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class VersionCatalogService : IVersionCatalogService
{
    private static readonly Regex _exeRegex =
        new(@"^Eco_[^_]+_[^_]+\.exe$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogService _log;

    public VersionCatalogService(ILogService log) => _log = log;

    public async Task<IReadOnlyList<EcoExecutavel>> ListarExecutaveisAsync()
    {
        return await Task.Run<IReadOnlyList<EcoExecutavel>>(() =>
        {
            try
            {
                if (!Directory.Exists(EcoPathConstants.UtilsDir))
                    return Array.Empty<EcoExecutavel>();

                var resultado = Directory
                    .EnumerateFiles(EcoPathConstants.UtilsDir, "*.exe")
                    .Where(path => _exeRegex.IsMatch(Path.GetFileName(path)))
                    .Select(path => new EcoExecutavel
                    {
                        NomeCompleto = Path.GetFileNameWithoutExtension(path),
                        ExePath      = path
                    })
                    .ToList();

                return resultado;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.Error(nameof(VersionCatalogService) + "." + nameof(ListarExecutaveisAsync), ex);
                return Array.Empty<EcoExecutavel>();
            }
        });
    }
}
