namespace EcoUtils.Services.Interfaces;

public interface IDialogService
{
    bool    Confirmar(string titulo, string mensagem, string botaoConfirmar = "Confirmar");
    void    Notificar(string titulo, string mensagem);
    string? SelecionarArquivo(string titulo, string filtro);
    string? SolicitarTexto(string titulo, string mensagem, string valorInicial = "");
    (string Major, string Versao, string Build)? SolicitarVersaoBuild();
}
