using System.IO;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class DatabaseDiscoveryService : IDatabaseDiscoveryService
{
    private readonly ILogService _log;

    public DatabaseDiscoveryService(ILogService log) => _log = log;

    public async Task<IReadOnlyList<EcoDatabase>> ListarBancosAsync()
    {
        return await Task.Run<IReadOnlyList<EcoDatabase>>(() =>
        {
            try
            {
                if (!Directory.Exists(EcoPathConstants.DadosDir))
                    return Array.Empty<EcoDatabase>();

                var resultado = Directory
                    .EnumerateFiles(EcoPathConstants.DadosDir, "*.eco")
                    .Select(path => new EcoDatabase
                    {
                        NomeCompleto = Path.GetFileNameWithoutExtension(path),
                        EcoPath      = path
                    })
                    .ToList();

                return resultado;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.Error(nameof(DatabaseDiscoveryService) + "." + nameof(ListarBancosAsync), ex);
                return Array.Empty<EcoDatabase>();
            }
        });
    }
}
