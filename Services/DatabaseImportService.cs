using System.IO;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace EcoUtils.Services;

public class DatabaseImportService : IDatabaseImportService
{
    private static readonly HashSet<string> _formatosCompactados =
        new(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" };

    private static readonly HashSet<string> _formatosBanco =
        new(StringComparer.OrdinalIgnoreCase) { ".eco", ".fbk", ".gbk" };

    public async Task<DatabaseImportResult> ProcessarArquivoAsync(
        string caminhoArquivo,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            string ext = Path.GetExtension(caminhoArquivo);

            if (_formatosCompactados.Contains(ext))
                return DescompactarEProcessar(caminhoArquivo, progresso, ct);

            if (_formatosBanco.Contains(ext))
                return ClassificarArquivo(caminhoArquivo);

            return DatabaseImportResult.OfInvalid(
                $"Formato de arquivo não suportado: \"{ext}\". " +
                "São aceitos .eco, .fbk, .gbk, .zip, .rar e .7z.");
        }, ct);
    }

    private static DatabaseImportResult DescompactarEProcessar(
        string caminho,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct)
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(), "EcoUtils_Import_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDir);

            using var archive = ArchiveFactory.Open(caminho);

            var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
            int total = Math.Max(entries.Count, 1);
            int done  = 0;

            // Normaliza o destino uma vez para reutilizar na validação de cada entrada
            string raizNormalizada = Path.GetFullPath(tempDir);
            if (!raizNormalizada.EndsWith(Path.DirectorySeparatorChar))
                raizNormalizada += Path.DirectorySeparatorChar;

            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();

                // Defesa contra path traversal (CVE-2026-44788 / GHSA-6c8g-7p36-r338):
                // rejeita entradas cujo caminho resolvido escaparia do diretório de destino.
                string chave = entry.Key ?? string.Empty;
                string caminhoResolvido = Path.GetFullPath(Path.Combine(raizNormalizada, chave));
                if (!caminhoResolvido.StartsWith(raizNormalizada, StringComparison.OrdinalIgnoreCase))
                    continue;

                string nome = string.IsNullOrEmpty(entry.Key) ? "arquivo" : entry.Key;
                progresso.Report(new DatabaseImportProgress(
                    $"Extraindo {nome}...", done * 100 / total));

                entry.WriteToDirectory(tempDir,
                    new ExtractionOptions { ExtractFullPath = false, Overwrite = true });
                done++;
            }

            progresso.Report(new DatabaseImportProgress("Extração concluída.", 100));

            string? encontrado = Directory
                .EnumerateFiles(tempDir, "*", SearchOption.AllDirectories)
                .FirstOrDefault(f => _formatosBanco.Contains(Path.GetExtension(f)));

            if (encontrado is null)
                return DatabaseImportResult.OfInvalid(
                    "Nenhum arquivo de banco reconhecido (.eco, .fbk, .gbk) " +
                    "encontrado no arquivo compactado.");

            // Move the found file out of the temp dir before cleanup
            string destTemp = Path.Combine(
                Path.GetTempPath(), Path.GetFileName(encontrado));
            File.Copy(encontrado, destTemp, overwrite: true);

            return ClassificarArquivo(destTemp);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return DatabaseImportResult.OfInvalid($"Erro ao descompactar: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static DatabaseImportResult ClassificarArquivo(string caminho)
    {
        string ext = Path.GetExtension(caminho);

        if (ext.Equals(".eco", StringComparison.OrdinalIgnoreCase))
            return DatabaseImportResult.OfEco(caminho);

        if (ext.Equals(".fbk", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".gbk", StringComparison.OrdinalIgnoreCase))
            return DatabaseImportResult.OfBackup(caminho);

        return DatabaseImportResult.OfInvalid(
            $"Formato de arquivo não suportado: \"{ext}\".");
    }

    public async Task<string> MoverEcoParaDadosAsync(string arquivoEco, string apelido)
    {
        return await Task.Run(() =>
        {
            if (!Directory.Exists(EcoPathConstants.DadosDir))
                Directory.CreateDirectory(EcoPathConstants.DadosDir);

            string destino = Path.Combine(EcoPathConstants.DadosDir, apelido + ".eco");

            if (File.Exists(destino))
                throw new InvalidOperationException(
                    $"Já existe um banco com o nome \"{apelido}\" em {EcoPathConstants.DadosDir}.");

            File.Move(arquivoEco, destino, overwrite: false);
            return destino;
        });
    }
}
