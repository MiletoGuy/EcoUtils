using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services.Interfaces;

public interface IExecutableImportService
{
    /// <summary>
    /// Recebe o arquivo selecionado pelo usuário (qualquer .exe, .zip, .rar ou .7z),
    /// descompacta se necessário, localiza o primeiro .exe encontrado e retorna seu caminho.
    /// Retorna null no ArquivoPath em caso de erro — use o campo Erro para detalhes.
    /// </summary>
    Task<ExecutableImportResult> ProcessarArquivoAsync(
        string caminhoArquivo,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct = default);

    /// <summary>
    /// Copia o eco.exe para UtilsDir com o nome "Eco_{versao}_{build}.exe".
    /// Retorna o EcoExecutavel resultante.
    /// </summary>
    Task<EcoUtils.Models.EcoExecutavel> InstalarExecutavelAsync(
        string arquivoExe,
        string versao,
        string build,
        bool substituir = false);
}

public sealed class ExecutableImportResult
{
    public string? ArquivoPath { get; private init; }
    public string? Erro        { get; private init; }
    public bool    Sucesso     => ArquivoPath is not null;

    public static ExecutableImportResult Ok(string path)    => new() { ArquivoPath = path };
    public static ExecutableImportResult Falha(string erro) => new() { Erro = erro };
}
