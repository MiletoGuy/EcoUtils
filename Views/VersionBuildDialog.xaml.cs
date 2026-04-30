using System.Windows;
using System.Windows.Input;

namespace EcoUtils.Views;

public partial class VersionBuildDialog : Window
{
    public string? Versao { get; private set; }
    public string? Build  { get; private set; }

    public VersionBuildDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => VersaoBox.Focus();
    }

    private void OnConfirmar(object sender, RoutedEventArgs e)
    {
        var versao = VersaoBox.Text.Trim();
        var build  = BuildBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(versao))
        {
            MostrarErro("Informe a versão.");
            VersaoBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(build))
        {
            MostrarErro("Informe o build.");
            BuildBox.Focus();
            return;
        }

        Versao       = versao;
        Build        = build;
        DialogResult = true;
    }

    private void OnCancelar(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  OnConfirmar(sender, e);
        if (e.Key == Key.Escape) OnCancelar(sender, e);
    }

    private void MostrarErro(string mensagem)
    {
        ErrorBlock.Text       = mensagem;
        ErrorBlock.Visibility = Visibility.Visible;
    }
}
