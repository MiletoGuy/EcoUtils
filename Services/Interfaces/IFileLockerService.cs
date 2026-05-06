namespace EcoUtils.Services.Interfaces;

public interface IFileLockerService
{
    /// <summary>
    /// Retorna a lista de processos que estão com o arquivo bloqueado (handle aberto).
    /// </summary>
    IReadOnlyList<(int ProcessId, string ProcessName)> ObterProcessosTravando(string caminhoArquivo);

    /// <summary>
    /// Força o encerramento do processo com o ID indicado.
    /// </summary>
    void EncerrarProcesso(int processId);
}
