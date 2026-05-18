using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EcoUtils.Services.Interfaces;
using EcoUtils.ViewModels;

namespace EcoUtils;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_BORDER_COLOR  = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR    = 36;

    private readonly IRestoreJobService _restoreJobService;

    public MainWindow(MainViewModel vm, IRestoreJobService restoreJobService)
    {
        InitializeComponent();
        DataContext = vm;

        _restoreJobService = restoreJobService;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"EcoUtils v{version?.Major}.{version?.Minor}.{version?.Build}";

        Loaded  += OnLoaded;
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_restoreJobService.HaJobsAtivos()) return;

        var result = MessageBox.Show(
            "Há uma restauração de base em andamento.\n\n" +
            "Fechar o EcoUtils agora irá interromper o processo e o arquivo parcialmente criado será descartado.\n\n" +
            "Deseja fechar mesmo assim?",
            "Restauração em andamento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        // Cancela todos os gbak antes de fechar para não deixar processos órfãos
        _restoreJobService.CancelarTodosAtivos();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        // Cores do tema (COLORREF = 0x00BBGGRR)
        // SidebarBackground #252526 → R=25 G=25 B=26
        int captionColor = 0x00262525;
        // TextSecondary #8a8a8a
        int textColor    = 0x008a8a8a;
        // PanelBorder #3c3c3c
        int borderColor  = 0x003c3c3c;

        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR,    ref textColor,    sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR,  ref borderColor,  sizeof(int));
    }
}