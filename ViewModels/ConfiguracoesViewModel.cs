using System.IO;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Services.Interfaces;
using Microsoft.Win32;

namespace EcoUtils.ViewModels;

public class ConfiguracoesViewModel : ViewModelBase
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly Action               _fechar;

    private string _ibExpertPath;
    public string IbExpertPath
    {
        get => _ibExpertPath;
        set => SetProperty(ref _ibExpertPath, value);
    }

    public ICommand SalvarCommand         { get; }
    public ICommand CancelarCommand       { get; }
    public ICommand BrowseIbExpertCommand { get; }

    public ConfiguracoesViewModel(IUserSettingsService userSettingsService, Action fechar)
    {
        _userSettingsService = userSettingsService;
        _fechar              = fechar;
        _ibExpertPath        = userSettingsService.Settings.IbExpertPath;

        SalvarCommand = new AsyncRelayCommand(async _ =>
        {
            _userSettingsService.Settings.IbExpertPath = IbExpertPath.Trim();
            await _userSettingsService.SalvarAsync();
            _fechar();
        });

        CancelarCommand = new RelayCommand(_ => _fechar());

        BrowseIbExpertCommand = new RelayCommand(_ =>
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Localizar IBExpert.exe",
                Filter = "IBExpert|IBExpert.exe|Executáveis (*.exe)|*.exe",
            };

            if (File.Exists(IbExpertPath))
                dlg.InitialDirectory = Path.GetDirectoryName(IbExpertPath);
            else if (Directory.Exists(Path.GetDirectoryName(IbExpertPath)))
                dlg.InitialDirectory = Path.GetDirectoryName(IbExpertPath);

            if (dlg.ShowDialog() == true)
                IbExpertPath = dlg.FileName;
        });
    }

    /// <summary>Sincroniza o campo com o valor atual salvo (ao abrir o painel).</summary>
    public void Resetar()
        => IbExpertPath = _userSettingsService.Settings.IbExpertPath;
}
