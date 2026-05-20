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

    private static string MontarMensagemInline(string resumo, string proximoPasso, string? detalhe = null)
    {
        if (string.IsNullOrWhiteSpace(detalhe))
            return $"{resumo} {proximoPasso}";

        return $"{resumo} {proximoPasso} Detalhe: {detalhe}";
    }

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
            OnPropertyChanged(nameof(TemAtualizacao));
            OnPropertyChanged(nameof(EstaAtualizando));
            (AtualizarCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (AbrirFlyoutUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool EstaVerificando  => _estado == EstadoUpdate.Verificando;
    public bool TemAtualizacao   => _estado == EstadoUpdate.AtualizacaoDisponivel;
    public bool EstaAtualizando  => _estado == EstadoUpdate.Atualizando;

    // ── Flyout de atualização disponível ────────────────────────
    private bool _flyoutUpdateAberto;
    public bool FlyoutUpdateAberto
    {
        get => _flyoutUpdateAberto;
        set => SetProperty(ref _flyoutUpdateAberto, value);
    }

    private string _versaoNova = string.Empty;
    public string VersaoNova
    {
        get => _versaoNova;
        private set => SetProperty(ref _versaoNova, value);
    }

    public ICommand AtualizarCommand       { get; }
    public ICommand DepoisCommand          { get; }
    public ICommand AbrirFlyoutUpdateCommand { get; }

    private string? _mensagemUpdateFluxo;
    public string? MensagemUpdateFluxo
    {
        get => _mensagemUpdateFluxo;
        private set
        {
            SetProperty(ref _mensagemUpdateFluxo, value);
            OnPropertyChanged(nameof(TemMensagemUpdateFluxo));
        }
    }

    public bool TemMensagemUpdateFluxo => !string.IsNullOrWhiteSpace(MensagemUpdateFluxo);

    private bool _mensagemUpdateErro;
    public bool MensagemUpdateErro
    {
        get => _mensagemUpdateErro;
        private set => SetProperty(ref _mensagemUpdateErro, value);
    }

    public MainViewModel(ExecutarEcoViewModel executarEcoVm, IUserSettingsService userSettingsService, IUpdateService updateService, IDialogService dialogService, IInstanceRepository instanceRepository, IInstanceSetupService instanceSetupService, ILogService log)
    {
        _updateService = updateService;

        ConfiguracoesVM  = new ConfiguracoesViewModel(userSettingsService, updateService, dialogService, instanceRepository, instanceSetupService, log, () => ConfigAberto = false);
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

        DepoisCommand            = new RelayCommand(_ => FlyoutUpdateAberto = false);
        AbrirFlyoutUpdateCommand = new RelayCommand(_ => FlyoutUpdateAberto = true, _ => TemAtualizacao);

        _ = VerificarAtualizacaoAsync();
    }

    private async Task VerificarAtualizacaoAsync()
    {
        Estado = EstadoUpdate.Verificando;
        MensagemUpdateFluxo = null;
        MensagemUpdateErro = false;

        try
        {
            _updateDisponivel = await _updateService.VerificarAtualizacaoAsync();
            Estado = _updateDisponivel is not null
                ? EstadoUpdate.AtualizacaoDisponivel
                : EstadoUpdate.SemAtualizacao;

            VersaoNova = _updateDisponivel?.Versao ?? string.Empty;

            if (_updateDisponivel is not null)
                FlyoutUpdateAberto = true;
        }
        catch (Exception ex)
        {
            _updateDisponivel = null;
            Estado = EstadoUpdate.SemAtualizacao;
            MensagemUpdateErro = false;
            MensagemUpdateFluxo = MontarMensagemInline(
                "Não foi possível verificar atualizações agora.",
                "Tente novamente mais tarde ou abra Configurações > Geral.",
                ex.Message);
        }
    }

    private async Task ExecutarAtualizacaoAsync()
    {
        if (_updateDisponivel is null) return;
        FlyoutUpdateAberto = false;
        Estado = EstadoUpdate.Atualizando;
        MensagemUpdateFluxo = null;
        MensagemUpdateErro = false;

        try
        {
            await _updateService.AtualizarAsync(_updateDisponivel);
        }
        catch (Exception ex)
        {
            Estado = EstadoUpdate.AtualizacaoDisponivel;
            MensagemUpdateErro = true;
            MensagemUpdateFluxo = MontarMensagemInline(
                "Não foi possível concluir a atualização.",
                "Verifique sua conexão e tente novamente.",
                ex.Message);
            FlyoutUpdateAberto = true;
        }
    }
}
