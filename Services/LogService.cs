using System.IO;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class LogService : ILogService
{
    private static readonly object _lock = new();

    public void Info(string contexto, string mensagem) =>
        Append("INFO", contexto, mensagem);

    public void Warn(string contexto, string mensagem) =>
        Append("WARN", contexto, mensagem);

    public void Error(string contexto, Exception ex) =>
        Append("ERROR", contexto, ex.ToString());

    private static void Append(string nivel, string contexto, string detalhe)
    {
        try
        {
            Directory.CreateDirectory(EcoPathConstants.LogsDir);
            var linha = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{nivel}] {contexto}: {detalhe}{Environment.NewLine}";
            lock (_lock)
                File.AppendAllText(EcoPathConstants.LogPath, linha);
        }
        catch
        {
            // log falhou — não propagar para não derrubar o app
        }
    }
}
