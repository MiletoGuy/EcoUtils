namespace EcoUtils.Services.Interfaces;

public enum DatabaseImportFormat { Eco, Backup, Invalid }

public record DatabaseImportProgress(string Mensagem, int Percentual);

public sealed class DatabaseImportResult
{
    public DatabaseImportFormat Format      { get; private init; }
    public string?              ArquivoPath { get; private init; }
    public string?              Erro        { get; private init; }

    public static DatabaseImportResult OfEco(string path) =>
        new() { Format = DatabaseImportFormat.Eco, ArquivoPath = path };

    public static DatabaseImportResult OfBackup(string path) =>
        new() { Format = DatabaseImportFormat.Backup, ArquivoPath = path };

    public static DatabaseImportResult OfInvalid(string? erro = null) =>
        new() { Format = DatabaseImportFormat.Invalid, Erro = erro };
}

public interface IDatabaseImportService
{
    Task<DatabaseImportResult> ProcessarArquivoAsync(
        string caminhoArquivo,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct = default);

    Task<string> MoverEcoParaDadosAsync(string arquivoEco, string apelido);
}
