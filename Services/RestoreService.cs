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
        args.Append("-r -v ");
        args.Append($"\"{arquivoBackup}\" ");
        // Prefixo host:caminho — sem -SE service_mgr o gbak usa conexão direta ao fbserver.
        // Ao matar o gbak a conexão TCP cai e o fbserver aborta imediatamente,
        // liberando os handles dos arquivos quase que instantaneamente.
        args.Append($"\"{EcoPathConstants.EcoServerHost}:{destinoEco}\" ");
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

        // Registra o kill direto no token — mais confiável do que capturar a exceção
        // do WaitForExitAsync, que pode não propagar em alguns cenários.
        // Com -SE service_mgr o fbserver.exe é quem segura os arquivos; matar o gbak
        // derruba a conexão e o serviço libera os handles após detectar o RST.
        using var killRegistration = ct.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        // Lê stdout e stderr concorrentemente — sem ct, pois o processo será morto via
        // killRegistration e as streams fecharão naturalmente.
        var stdoutTask = LerSaidaAsync(process.StandardOutput, progresso, CancellationToken.None);
        var stderrTask = LerSaidaAsync(process.StandardError,  progresso, CancellationToken.None);

        await process.WaitForExitAsync(CancellationToken.None);
        await Task.WhenAll(stdoutTask, stderrTask);

        if (ct.IsCancellationRequested)
        {
            // Aguarda o serviço Firebird detectar a desconexão e liberar os arquivos,
            // tentando deletar o .eco parcial com retentativas espaçadas.
            await DeletarComRetentativaAsync(destinoEco);
            ct.ThrowIfCancellationRequested();
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

    private static async Task DeletarComRetentativaAsync(string path, int tentativas = 8, int delayMs = 1000)
    {
        for (int i = 0; i < tentativas; i++)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return; // sucesso
            }
            catch (IOException)
            {
                // Arquivo ainda bloqueado pelo serviço Firebird — aguarda e tenta de novo
                if (i < tentativas - 1)
                    await Task.Delay(delayMs);
            }
        }
        // Esgotou tentativas — deixa o arquivo; será detectado no próximo carregamento
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
