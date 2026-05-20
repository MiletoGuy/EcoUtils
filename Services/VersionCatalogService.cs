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

    public async Task<CollectionLoadResult<EcoExecutavel>> ListarExecutaveisAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(EcoPathConstants.UtilsDir))
                {
                    return new CollectionLoadResult<EcoExecutavel>(
                        Array.Empty<EcoExecutavel>(),
                        $"A pasta Utils não foi encontrada em {EcoPathConstants.UtilsDir}. Importe um executável para continuar.");
                }

                var resultado = Directory
                    .EnumerateFiles(EcoPathConstants.UtilsDir, "*.exe")
                    .Where(path => _exeRegex.IsMatch(Path.GetFileName(path)))
                    .Select(path => new EcoExecutavel
                    {
                        NomeCompleto = Path.GetFileNameWithoutExtension(path),
                        ExePath      = path
                    })
                    .ToList();

                if (resultado.Count == 0)
                {
                    return new CollectionLoadResult<EcoExecutavel>(
                        resultado,
                        "Nenhum executável ECO foi encontrado. Use o botão + para importar um executável.");
                }

                return new CollectionLoadResult<EcoExecutavel>(resultado);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.Error(nameof(VersionCatalogService) + "." + nameof(ListarExecutaveisAsync), ex);
                return new CollectionLoadResult<EcoExecutavel>(
                    Array.Empty<EcoExecutavel>(),
                    "Não foi possível listar os executáveis agora. Verifique permissões da pasta Utils e tente novamente.",
                    HasError: true);
            }
        });
    }
}
