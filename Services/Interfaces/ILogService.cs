namespace EcoUtils.Services.Interfaces;

public interface ILogService
{
    void Info(string contexto, string mensagem);
    void Warn(string contexto, string mensagem);
    void Error(string contexto, Exception ex);
}
