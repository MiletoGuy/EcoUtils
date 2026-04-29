using System.Collections.ObjectModel;

namespace EcoUtils.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<NavItem> Abas { get; }

    private NavItem? _abaAtiva;
    public NavItem? AbaAtiva
    {
        get => _abaAtiva;
        set => SetProperty(ref _abaAtiva, value);
    }

    public MainViewModel(ExecutarEcoViewModel executarEcoVm)
    {
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
    }
}
