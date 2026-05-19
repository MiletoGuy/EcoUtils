using System.Windows;
using System.Windows.Input;
using EcoUtils.Infrastructure;

namespace EcoUtils.Views;

public partial class VersionBuildDialog : Window
{
    public string? Major  { get; private set; }
    public string? Versao { get; private set; }
    public string? Build  { get; private set; }

    public VersionBuildDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => MajorBox.Focus();
    }

    private void OnConfirmar(object sender, RoutedEventArgs e)
    {
        var major  = MajorBox.Text.Trim();
        var versao = VersaoBox.Text.Trim();
        var build  = BuildBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(major))
        {
            MostrarErro("Informe a major (ex.: 1.4 ou 1.5).");
            MajorBox.Focus();
            return;
        }

        if (EcoVersionHelper.NormalizarMajorInput(major) is null)
        {
            MostrarErro("Major inválida. Use 1.4, 1.5, 14, 15 etc.");
            MajorBox.Focus();
            return;
        }

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

        Major        = major;
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
