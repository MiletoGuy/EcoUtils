using System.Collections.Generic;
using System.IO;
using System.Windows;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class RestoreJobService : IRestoreJobService
{
    private readonly IRestoreService _restoreService;

    private readonly Dictionary<string, RestoreJobEntry> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event EventHandler<RestoreJobEntry>? JobFinalizado;

    public RestoreJobService(IRestoreService restoreService)
    {
        _restoreService = restoreService;
    }

    public RestoreJobEntry Iniciar(string arquivoBackup, string destinoEco, string apelido)
    {
        lock (_lock)
        {
            if (_jobs.TryGetValue(destinoEco, out var existente) &&
                existente.Status == RestoreJobStatus.Restaurando)
            {
                // Job ativo para o mesmo destino — retorna o existente sem iniciar novo
                return existente;
            }
        }

        var entry = new RestoreJobEntry
        {
            Apelido       = apelido,
            ArquivoBackup = arquivoBackup,
            DestinoEco    = destinoEco
        };

        lock (_lock)
            _jobs[destinoEco] = entry;

        _ = ExecutarJobAsync(entry);

        return entry;
    }

    public RestoreJobEntry? ObterPorDestino(string destinoEco)
    {
        lock (_lock)
            return _jobs.TryGetValue(destinoEco, out var entry) ? entry : null;
    }

    public bool EstaRestaurando(string destinoEco)
    {
        lock (_lock)
            return _jobs.TryGetValue(destinoEco, out var entry) &&
                   entry.Status == RestoreJobStatus.Restaurando;
    }

    public void Cancelar(string destinoEco)
    {
        lock (_lock)
        {
            if (_jobs.TryGetValue(destinoEco, out var entry))
                entry.Cts.Cancel();
        }
    }

    public bool HaJobsAtivos()
    {
        lock (_lock)
            return _jobs.Values.Any(e => e.Status == RestoreJobStatus.Restaurando);
    }

    public void CancelarTodosAtivos()
    {
        List<RestoreJobEntry> ativos;
        lock (_lock)
            ativos = _jobs.Values
                .Where(e => e.Status == RestoreJobStatus.Restaurando)
                .ToList();

        foreach (var entry in ativos)
        {
            try { entry.Cts.Cancel(); } catch { }
        }
    }

    public Task CancelarAsync(string destinoEco)
    {
        RestoreJobEntry? entry;
        lock (_lock)
            _jobs.TryGetValue(destinoEco, out entry);

        if (entry is null || entry.Status != RestoreJobStatus.Restaurando)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnJobFinalizado(object? s, RestoreJobEntry e)
        {
            if (!e.DestinoEco.Equals(destinoEco, StringComparison.OrdinalIgnoreCase)) return;
            JobFinalizado -= OnJobFinalizado;
            tcs.TrySetResult();
        }

        JobFinalizado += OnJobFinalizado;

        // Cancela após registrar o handler para não perder o evento em raças
        entry.Cts.Cancel();

        return tcs.Task;
    }

    private async Task ExecutarJobAsync(RestoreJobEntry entry)
    {
        // Progress<T> captura o SynchronizationContext do thread atual (UI),
        // então o callback é sempre invocado no thread da UI.
        var progress = new Progress<DatabaseImportProgress>(p =>
            entry.UltimaMensagem = p.Mensagem);

        try
        {
            await _restoreService.RestaurarAsync(
                entry.ArquivoBackup,
                entry.DestinoEco,
                progress,
                entry.Cts.Token);

            AtualizarEstadoFinal(entry, RestoreJobStatus.Concluido, erro: null);
        }
        catch (OperationCanceledException)
        {
            AtualizarEstadoFinal(entry, RestoreJobStatus.Falhou, "Restauração cancelada pelo usuário.");
        }
        catch (Exception ex)
        {
            AtualizarEstadoFinal(entry, RestoreJobStatus.Falhou, ex.Message);
        }
    }

    private void AtualizarEstadoFinal(RestoreJobEntry entry, RestoreJobStatus status, string? erro)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            // App encerrando — atualiza o estado diretamente sem dispatch (sem UI para notificar)
            entry.Erro   = erro;
            entry.Status = status;
            return;
        }

        dispatcher.Invoke(() =>
        {
            entry.Erro   = erro;
            entry.Status = status;
            JobFinalizado?.Invoke(this, entry);
        });
    }
}
