using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Services;
using EcoUtils.Services.Interfaces;
using Microsoft.Win32;

namespace EcoUtils.ViewModels;

public class ConfiguracoesViewModel : ViewModelBase
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly IUpdateService       _updateService;
    private readonly IDialogService       _dialogService;
    private readonly Action               _fechar;

    // ── IBExpert ────────────────────────────────────────────────
    private string _ibExpertPath;
    public string IbExpertPath
    {
        get => _ibExpertPath;
        set => SetProperty(ref _ibExpertPath, value);
    }

    // ── Versão do Utils ─────────────────────────────────────────
    public string VersaoAtual => _updateService.VersaoAtual;

    public ObservableCollection<UpdateInfo> VersoesDisponiveis { get; } = new();

    private UpdateInfo? _versaoSelecionada;
    public UpdateInfo? VersaoSelecionada
    {
        get => _versaoSelecionada;
        set
        {
            SetProperty(ref _versaoSelecionada, value);
            (TrocarVersaoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool _isCarregandoVersoes;
    public bool IsCarregandoVersoes
    {
        get => _isCarregandoVersoes;
        private set
        {
            SetProperty(ref _isCarregandoVersoes, value);
            OnPropertyChanged(nameof(PodeInteragirComVersoes));
            (TrocarVersaoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool _isTrocandoVersao;
    public bool IsTrocandoVersao
    {
        get => _isTrocandoVersao;
        private set
        {
            SetProperty(ref _isTrocandoVersao, value);
            OnPropertyChanged(nameof(PodeInteragirComVersoes));
            (TrocarVersaoCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool PodeInteragirComVersoes => !IsCarregandoVersoes && !IsTrocandoVersao;

    private string? _erroVersoes;
    public string? ErroVersoes
    {
        get => _erroVersoes;
        private set
        {
            SetProperty(ref _erroVersoes, value);
            OnPropertyChanged(nameof(TemErroVersoes));
        }
    }

    public bool TemErroVersoes => !string.IsNullOrEmpty(ErroVersoes);

    public ICommand SalvarCommand         { get; }
    public ICommand CancelarCommand       { get; }
    public ICommand BrowseIbExpertCommand { get; }
    public AsyncRelayCommand TrocarVersaoCommand { get; }

    public ConfiguracoesViewModel(
        IUserSettingsService userSettingsService,
        IUpdateService updateService,
        IDialogService dialogService,
        Action fechar)
    {
        _userSettingsService = userSettingsService;
        _updateService       = updateService;
        _dialogService       = dialogService;
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

        TrocarVersaoCommand = new AsyncRelayCommand(
            async _ => await TrocarVersaoAsync(),
            _ => VersaoSelecionada is not null
                 && !string.Equals(VersaoSelecionada.Versao, VersaoAtual, StringComparison.OrdinalIgnoreCase)
                 && !IsTrocandoVersao
                 && !IsCarregandoVersoes);
    }

    /// <summary>Sincroniza os campos com os valores atuais salvos (ao abrir o painel).</summary>
    public void Resetar()
    {
        IbExpertPath = _userSettingsService.Settings.IbExpertPath;
        _ = CarregarVersoesAsync();
    }

    private async Task CarregarVersoesAsync()
    {
        IsCarregandoVersoes = true;
        ErroVersoes         = null;
        try
        {
            var versoes = await _updateService.ListarVersoesAsync();
            VersoesDisponiveis.Clear();
            foreach (var v in versoes)
                VersoesDisponiveis.Add(v);

            VersaoSelecionada = VersoesDisponiveis
                .FirstOrDefault(v => string.Equals(v.Versao, VersaoAtual, StringComparison.OrdinalIgnoreCase));

            if (VersoesDisponiveis.Count == 0)
                ErroVersoes = "Nenhuma versão encontrada. Verifique sua conexão com a internet.";
        }
        catch (Exception ex)
        {
            ErroVersoes = $"Não foi possível carregar as versões: {ex.Message}";
        }
        finally
        {
            IsCarregandoVersoes = false;
        }
    }

    private async Task TrocarVersaoAsync()
    {
        if (VersaoSelecionada is null) return;

        bool confirmar = _dialogService.Confirmar(
            "Trocar versão",
            $"O EcoUtils será reiniciado para instalar a versão {VersaoSelecionada.Versao}.\n\nDeseja continuar?",
            "Trocar e Reiniciar");

        if (!confirmar) return;

        IsTrocandoVersao = true;
        try
        {
            await _updateService.AtualizarAsync(VersaoSelecionada);
        }
        catch (Exception ex)
        {
            ErroVersoes      = $"Erro ao trocar versão: {ex.Message}";
            IsTrocandoVersao = false;
        }
    }
}

