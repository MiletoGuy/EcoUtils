using System.IO;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace EcoUtils.Services;

public class ExecutableImportService : IExecutableImportService
{
    private static readonly HashSet<string> _formatosCompactados =
        new(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" };

    public async Task<ExecutableImportResult> ProcessarArquivoAsync(
        string caminhoArquivo,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            string ext = Path.GetExtension(caminhoArquivo);

            if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                string nome = Path.GetFileName(caminhoArquivo);
                if (!nome.Equals("eco.exe", StringComparison.OrdinalIgnoreCase))
                    return ExecutableImportResult.Falha(
                        $"O arquivo selecionado é \"{nome}\". " +
                        "Selecione o arquivo \"eco.exe\" ou um pacote compactado (.zip, .rar, .7z).");

                return ExecutableImportResult.Ok(caminhoArquivo);
            }

            if (_formatosCompactados.Contains(ext))
                return DescompactarELocalizar(caminhoArquivo, progresso, ct);

            return ExecutableImportResult.Falha(
                $"Formato de arquivo não suportado: \"{ext}\". " +
                "São aceitos eco.exe, .zip, .rar e .7z.");
        }, ct);
    }

    private static ExecutableImportResult DescompactarELocalizar(
        string caminho,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct)
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(), "EcoUtils_ExeImport_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDir);

            using var archive = ArchiveFactory.Open(caminho);

            var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
            int total   = Math.Max(entries.Count, 1);
            int done    = 0;

            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();

                string nome = string.IsNullOrEmpty(entry.Key) ? "arquivo" : entry.Key;
                progresso.Report(new DatabaseImportProgress(
                    $"Extraindo {nome}...", done * 100 / total));

                entry.WriteToDirectory(tempDir,
                    new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                done++;
            }

            progresso.Report(new DatabaseImportProgress("Extração concluída. Localizando eco.exe...", 100));

            string? encontrado = Directory
                .EnumerateFiles(tempDir, "eco.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (encontrado is null)
                return ExecutableImportResult.Falha(
                    "eco.exe não encontrado no arquivo compactado.");

            // Copia para fora do tempDir antes da limpeza
            string destTemp = Path.Combine(
                Path.GetTempPath(), "eco_" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(encontrado, destTemp, overwrite: true);

            return ExecutableImportResult.Ok(destTemp);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ExecutableImportResult.Falha($"Erro ao descompactar: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    public async Task<EcoExecutavel> InstalarExecutavelAsync(
        string arquivoExe,
        string versao,
        string build,
        bool substituir = false)
    {
        return await Task.Run(() =>
        {
            versao = versao.Trim();
            build  = build.Trim();

            if (string.IsNullOrWhiteSpace(versao) || string.IsNullOrWhiteSpace(build))
                throw new ArgumentException("Versão e build não podem estar em branco.");

            if (!Directory.Exists(EcoPathConstants.UtilsDir))
                Directory.CreateDirectory(EcoPathConstants.UtilsDir);

            string nome    = $"Eco_{versao}_{build}";
            string destino = Path.Combine(EcoPathConstants.UtilsDir, nome + ".exe");

            File.Copy(arquivoExe, destino, overwrite: substituir);

            // Tenta limpar o temp intermediário silenciosamente
            try
            {
                string dir = Path.GetDirectoryName(arquivoExe) ?? string.Empty;
                if (dir.Equals(Path.GetTempPath().TrimEnd('\\', '/'),
                        StringComparison.OrdinalIgnoreCase))
                    File.Delete(arquivoExe);
            }
            catch { /* best-effort */ }

            return new EcoExecutavel
            {
                NomeCompleto = nome,
                ExePath      = destino
            };
        });
    }
}
