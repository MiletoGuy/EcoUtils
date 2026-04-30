using System.Diagnostics;
using System.IO;
using System.Text;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class RestoreService : IRestoreService
{
    public async Task RestaurarAsync(
        string arquivoBackup,
        string destinoEco,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct = default)
    {
        if (!File.Exists(EcoPathConstants.GbakPath))
            throw new FileNotFoundException(
                $"gbak.exe não encontrado em {EcoPathConstants.ToolsDir}. " +
                "Certifique-se de que a instalação do EcoUtils está completa.");

        string? destDir = Path.GetDirectoryName(destinoEco);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        var args = new StringBuilder();
        args.Append("-r -v -SE service_mgr ");
        args.Append($"\"{arquivoBackup}\" ");
        args.Append($"\"{destinoEco}\" ");
        args.Append($"-user {EcoPathConstants.FirebirdUser} ");
        args.Append($"-pass {EcoPathConstants.FirebirdPassword}");

        var psi = new ProcessStartInfo
        {
            FileName               = EcoPathConstants.GbakPath,
            Arguments              = args.ToString(),
            WorkingDirectory       = EcoPathConstants.ToolsDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var process = new Process { StartInfo = psi };

        process.Start();

        // Lê stdout e stderr concorrentemente, repassando cada linha como progresso
        var stdoutTask = LerSaidaAsync(process.StandardOutput, progresso, ct);
        var stderrTask = LerSaidaAsync(process.StandardError,  progresso, ct);

        try
        {
            await process.WaitForExitAsync(ct);
            await Task.WhenAll(stdoutTask, stderrTask);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            try { if (File.Exists(destinoEco)) File.Delete(destinoEco); } catch { }
            throw;
        }

        if (process.ExitCode != 0)
        {
            try { if (File.Exists(destinoEco)) File.Delete(destinoEco); } catch { }

            string? ultimaLinha = stderrTask.IsCompletedSuccessfully ? stderrTask.Result
                                : stdoutTask.IsCompletedSuccessfully ? stdoutTask.Result
                                : null;

            string detalhe = ultimaLinha is not null
                ? $"\n\nÚltima mensagem: {ultimaLinha}"
                : string.Empty;

            bool erroServico = ultimaLinha is not null &&
                (ultimaLinha.Contains("service", StringComparison.OrdinalIgnoreCase) ||
                 ultimaLinha.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
                 ultimaLinha.Contains("cannot attach", StringComparison.OrdinalIgnoreCase));

            if (erroServico)
                throw new InvalidOperationException(
                    "Não foi possível conectar ao serviço Firebird. " +
                    "Verifique se o Firebird está instalado e em execução." + detalhe);

            throw new InvalidOperationException(
                $"gbak encerrou com código {process.ExitCode}. " +
                "Verifique se as credenciais do Firebird estão corretas e se o serviço está ativo." +
                detalhe);
        }
    }

    private static async Task<string?> LerSaidaAsync(
        System.IO.StreamReader reader,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct)
    {
        string? ultimaLinha = null;
        while (!ct.IsCancellationRequested)
        {
            string? linha = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (linha is null) break;
            ultimaLinha = linha;
            progresso.Report(new DatabaseImportProgress(linha, -1));
        }
        return ultimaLinha;
    }
}
