using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IRestoreJobService
{
    /// <summary>
    /// Inicia a restauração em background. Retorna imediatamente com o job criado.
    /// </summary>
    RestoreJobEntry Iniciar(string arquivoBackup, string destinoEco, string apelido);

    /// <summary>
    /// Retorna o job associado ao caminho destino, ou null se não houver.
    /// </summary>
    RestoreJobEntry? ObterPorDestino(string destinoEco);

    /// <summary>
    /// Retorna true se há um job com Status == Restaurando para o caminho destino.
    /// </summary>
    bool EstaRestaurando(string destinoEco);

    /// <summary>
    /// Cancela o job associado ao destino, matando o processo gbak e limpando o .eco parcial.
    /// </summary>
    void Cancelar(string destinoEco);

    /// <summary>
    /// Cancela o job e aguarda sua finalização completa (limpeza de arquivo incluída).
    /// </summary>
    Task CancelarAsync(string destinoEco);

    /// <summary>
    /// Retorna true se há pelo menos um job com status Restaurando.
    /// </summary>
    bool HaJobsAtivos();

    /// <summary>
    /// Cancela imediatamente todos os jobs com status Restaurando (mata os processos gbak).
    /// Não aguarda a limpeza dos arquivos parciais.
    /// </summary>
    void CancelarTodosAtivos();

    /// <summary>
    /// Disparado no thread da UI quando um job muda para Concluido ou Falhou.
    /// </summary>
    event EventHandler<RestoreJobEntry> JobFinalizado;
}
