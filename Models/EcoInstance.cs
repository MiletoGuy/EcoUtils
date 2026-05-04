using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EcoUtils.Models;

public class EcoInstance : INotifyPropertyChanged
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

    // ── Dados persistidos (serializam para instancias.json) ──────────
    public Guid   Id                  { get; set; }
    public string Apelido             { get; set; } = string.Empty;
    public string ExecutavelPath      { get; set; } = string.Empty;
    public string ExecutavelFontePath { get; set; } = string.Empty;
    public string ExecutavelNome      { get; set; } = string.Empty;
    public string BasePath            { get; set; } = string.Empty;
    public string BaseNome            { get; set; } = string.Empty;
    public string IniPath             { get; set; } = string.Empty;

    private string _versaoBanco = string.Empty;
    public string VersaoBanco
    {
        get => _versaoBanco;
        set => SetProperty(ref _versaoBanco, value);
    }

    // ── Estado de restauração (somente em memória, nunca serializado) ─
    private RestoreJobStatus? _statusRestauracao;
    [JsonIgnore]
    public RestoreJobStatus? StatusRestauracao
    {
        get => _statusRestauracao;
        set
        {
            if (!SetProperty(ref _statusRestauracao, value)) return;
            OnPropertyChanged(nameof(EstaRestaurando));
            OnPropertyChanged(nameof(RestauracaoConcluida));
            OnPropertyChanged(nameof(RestauracaoFalhou));
        }
    }

    [JsonIgnore] public bool EstaRestaurando      => _statusRestauracao == RestoreJobStatus.Restaurando;
    [JsonIgnore] public bool RestauracaoConcluida  => _statusRestauracao == RestoreJobStatus.Concluido;
    [JsonIgnore] public bool RestauracaoFalhou     => _statusRestauracao == RestoreJobStatus.Falhou;

    private bool _versaoIncompativel;
    [JsonIgnore]
    public bool VersaoIncompativel
    {
        get => _versaoIncompativel;
        set => SetProperty(ref _versaoIncompativel, value);
    }

    private string? _ultimaMensagemRestauracao;
    [JsonIgnore]
    public string? UltimaMensagemRestauracao
    {
        get => _ultimaMensagemRestauracao;
        set => SetProperty(ref _ultimaMensagemRestauracao, value);
    }

    private string? _erroRestauracao;
    [JsonIgnore]
    public string? ErroRestauracao
    {
        get => _erroRestauracao;
        set => SetProperty(ref _erroRestauracao, value);
    }
}
