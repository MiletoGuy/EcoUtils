namespace EcoUtils.Services.Interfaces;

public interface IDialogService
{
    /// <summary>
    /// Regra de UX do app: usar diálogo modal apenas para confirmação destrutiva,
    /// confirmação obrigatória ou erro realmente bloqueante/global.
    /// </summary>
    bool    Confirmar(string titulo, string mensagem, string botaoConfirmar = "Confirmar");

    /// <summary>
    /// Reservado para notificações bloqueantes/globais.
    /// Para erros recuperáveis de fluxo, preferir feedback inline na tela/flyout.
    /// </summary>
    void    Notificar(string titulo, string mensagem);

    /// <summary>
    /// Entrada obrigatória de arquivo escolhida pelo usuário.
    /// </summary>
    string? SelecionarArquivo(string titulo, string filtro);

    /// <summary>
    /// Entrada obrigatória de texto escolhida pelo usuário.
    /// </summary>
    string? SolicitarTexto(string titulo, string mensagem, string valorInicial = "");

    /// <summary>
    /// Entrada obrigatória de versão/build para importação de executável.
    /// </summary>
    (string Major, string Versao, string Build)? SolicitarVersaoBuild();
}
