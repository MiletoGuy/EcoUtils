using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EcoUtils.Models;

/// <summary>
/// Representa um job de restauração de backup em andamento ou finalizado.
/// Implementa INotifyPropertyChanged para que o ViewModel possa observar mudanças de estado.
/// </summary>
public class RestoreJobEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    // ── Dados imutáveis do job ────────────────────────────────────────
    public Guid   Id            { get; init; } = Guid.NewGuid();
    public string Apelido       { get; init; } = string.Empty;
    public string ArquivoBackup { get; init; } = string.Empty;
    public string DestinoEco    { get; init; } = string.Empty;

    // ── Estado mutável (notifica observers) ──────────────────────────
    private RestoreJobStatus _status = RestoreJobStatus.Restaurando;
    public RestoreJobStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private string _ultimaMensagem = string.Empty;
    public string UltimaMensagem
    {
        get => _ultimaMensagem;
        set => SetProperty(ref _ultimaMensagem, value);
    }

    private string? _erro;
    public string? Erro
    {
        get => _erro;
        set => SetProperty(ref _erro, value);
    }

    // ── Controle de cancelamento ─────────────────────────────────────
    public CancellationTokenSource Cts { get; } = new();

    internal TaskCompletionSource Finalizacao { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
