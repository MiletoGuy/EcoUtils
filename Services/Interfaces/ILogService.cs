namespace EcoUtils.Services.Interfaces;

public interface ILogService
{
    void Error(string contexto, Exception ex);
    void Warn(string contexto, string mensagem);
}
