namespace EcoUtils.ViewModels;

public class NavItem
{
    public string Rotulo    { get; init; } = string.Empty;
    public string Icone     { get; init; } = string.Empty;
    public ViewModelBase ViewModel { get; init; } = null!;
}
