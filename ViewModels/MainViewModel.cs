using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;

    public ObservableCollection<NavItem> Abas { get; }

    private NavItem? _abaAtiva;
    public NavItem? AbaAtiva
    {
        get => _abaAtiva;
        set => SetProperty(ref _abaAtiva, value);
    }

    private UpdateInfo? _updateDisponivel;
    public UpdateInfo? UpdateDisponivel
    {
        get => _updateDisponivel;
        private set
        {
            SetProperty(ref _updateDisponivel, value);
            OnPropertyChanged(nameof(TemUpdate));
        }
    }

    public bool TemUpdate => _updateDisponivel is not null;

    private bool _atualizando;
    public bool Atualizando
    {
        get => _atualizando;
        private set => SetProperty(ref _atualizando, value);
    }

    private double _progressoUpdate;
    public double ProgressoUpdate
    {
        get => _progressoUpdate;
        private set => SetProperty(ref _progressoUpdate, value);
    }

    public ICommand AtualizarCommand { get; }

    public MainViewModel(ExecutarEcoViewModel executarEcoVm, IUpdateService updateService)
    {
        _updateService = updateService;

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

        AtualizarCommand = new AsyncRelayCommand(_ => ExecutarAtualizacaoAsync());

        _ = VerificarAtualizacaoAsync();
    }

    private async Task VerificarAtualizacaoAsync()
    {
        UpdateDisponivel = await _updateService.VerificarAtualizacaoAsync();
    }

    private async Task ExecutarAtualizacaoAsync()
    {
        if (_updateDisponivel is null) return;

        Atualizando   = true;
        ProgressoUpdate = 0;

        var progresso = new Progress<double>(p => ProgressoUpdate = p * 100);
        await _updateService.AtualizarAsync(_updateDisponivel, progresso);
    }
}
