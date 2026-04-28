namespace EcoUtils.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _title = "EcoUtils";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
