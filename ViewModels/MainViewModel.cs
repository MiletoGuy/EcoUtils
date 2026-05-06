using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public enum EstadoUpdate { Verificando, SemAtualizacao, AtualizacaoDisponivel, Atualizando }

public class MainViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;
    private UpdateInfo? _updateDisponivel;

    public ObservableCollection<NavItem> Abas { get; }

    private NavItem? _abaAtiva;
    public NavItem? AbaAtiva
    {
        get => _abaAtiva;
        set => SetProperty(ref _abaAtiva, value);
    }

    // ── Configurações overlay ────────────────────────────────────
    public ConfiguracoesViewModel ConfiguracoesVM { get; }

    private bool _configAberto;
    public bool ConfigAberto
    {
        get => _configAberto;
        set => SetProperty(ref _configAberto, value);
    }

    public ICommand AbrirConfigCommand { get; }

    private EstadoUpdate _estado = EstadoUpdate.Verificando;
    private EstadoUpdate Estado
    {
        get => _estado;
        set
        {
            _estado = value;
            OnPropertyChanged(nameof(EstaVerificando));
            OnPropertyChanged(nameof(SemAtualizacao));
            OnPropertyChanged(nameof(TemAtualizacao));
            OnPropertyChanged(nameof(EstaAtualizando));
            (AtualizarCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool EstaVerificando  => _estado == EstadoUpdate.Verificando;
    public bool SemAtualizacao   => _estado == EstadoUpdate.SemAtualizacao;
    public bool TemAtualizacao   => _estado == EstadoUpdate.AtualizacaoDisponivel;
    public bool EstaAtualizando  => _estado == EstadoUpdate.Atualizando;

    public ICommand AtualizarCommand { get; }

    public MainViewModel(ExecutarEcoViewModel executarEcoVm, IUserSettingsService userSettingsService, IUpdateService updateService, IDialogService dialogService)
    {
        _updateService = updateService;

        ConfiguracoesVM  = new ConfiguracoesViewModel(userSettingsService, updateService, dialogService, () => ConfigAberto = false);
        AbrirConfigCommand = new RelayCommand(_ =>
        {
            ConfiguracoesVM.Resetar();
            ConfigAberto = true;
        });

        Abas = new ObservableCollection<NavItem>
        {
            new NavItem
            {
                Rotulo    = "Executar ECO",
                Icone     = "\u25B6",
                ViewModel = executarEcoVm
            }
        };

        AbaAtiva = Abas[0];

        AtualizarCommand = new AsyncRelayCommand(
            _ => ExecutarAtualizacaoAsync(),
            _ => TemAtualizacao);

        _ = VerificarAtualizacaoAsync();
    }

    private async Task VerificarAtualizacaoAsync()
    {
        Estado = EstadoUpdate.Verificando;
        _updateDisponivel = await _updateService.VerificarAtualizacaoAsync();
        Estado = _updateDisponivel is not null
            ? EstadoUpdate.AtualizacaoDisponivel
            : EstadoUpdate.SemAtualizacao;
    }

    private async Task ExecutarAtualizacaoAsync()
    {
        if (_updateDisponivel is null) return;
        Estado = EstadoUpdate.Atualizando;
        await _updateService.AtualizarAsync(_updateDisponivel);
    }
}
