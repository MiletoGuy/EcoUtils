using FirebirdSql.Data.FirebirdClient;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class DatabaseVersionService : IDatabaseVersionService
{
    private readonly ILogService _log;

    public DatabaseVersionService(ILogService log) => _log = log;

    public async Task<string?> ConsultarVersaoAsync(string ecoBankPath)
    {
        var csb = new FbConnectionStringBuilder
        {
            DataSource        = EcoPathConstants.EcoServerHost,
            Database          = ecoBankPath,
            UserID            = "SYSDBA",
            Password          = "masterkey",
            ConnectionTimeout = 5
        };

        try
        {
            await using var conn = new FbConnection(csb.ToString());
            await conn.OpenAsync();

            await using var cmd = new FbCommand("SELECT FIRST 1 VERSAO FROM TGERLICENCA", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _log.Error(nameof(DatabaseVersionService) + "." + nameof(ConsultarVersaoAsync), ex);
            return null;
        }
    }
}
