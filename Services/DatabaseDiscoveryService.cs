using System.IO;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class DatabaseDiscoveryService : IDatabaseDiscoveryService
{
    private readonly ILogService _log;

    public DatabaseDiscoveryService(ILogService log) => _log = log;

    public async Task<CollectionLoadResult<EcoDatabase>> ListarBancosAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(EcoPathConstants.DadosDir))
                {
                    return new CollectionLoadResult<EcoDatabase>(
                        Array.Empty<EcoDatabase>(),
                        $"A pasta de bancos não foi encontrada em {EcoPathConstants.DadosDir}. Importe um banco para continuar.");
                }

                var resultado = Directory
                    .EnumerateFiles(EcoPathConstants.DadosDir, "*.eco")
                    .Select(path => new EcoDatabase
                    {
                        NomeCompleto = Path.GetFileNameWithoutExtension(path),
                        EcoPath      = path
                    })
                    .ToList();

                if (resultado.Count == 0)
                {
                    return new CollectionLoadResult<EcoDatabase>(
                        resultado,
                        "Nenhum banco .eco foi encontrado. Use o botão + para importar um banco.");
                }

                return new CollectionLoadResult<EcoDatabase>(resultado);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.Error(nameof(DatabaseDiscoveryService) + "." + nameof(ListarBancosAsync), ex);
                return new CollectionLoadResult<EcoDatabase>(
                    Array.Empty<EcoDatabase>(),
                    "Não foi possível listar os bancos agora. Verifique permissões da pasta de dados e tente novamente.",
                    HasError: true);
            }
        });
    }
}
