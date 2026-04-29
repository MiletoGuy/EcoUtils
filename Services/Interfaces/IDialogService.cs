namespace EcoUtils.Services.Interfaces;

public interface IDialogService
{
    bool Confirmar(string titulo, string mensagem, string botaoConfirmar = "Confirmar");
    void Notificar(string titulo, string mensagem);
}
