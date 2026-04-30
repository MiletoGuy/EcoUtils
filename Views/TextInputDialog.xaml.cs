using System.IO;
using System.Windows;
using System.Windows.Input;

namespace EcoUtils.Views;

public partial class TextInputDialog : Window
{
    public string? Resultado { get; private set; }

    public TextInputDialog(string titulo, string mensagem, string valorInicial = "")
    {
        InitializeComponent();
        TitleBlock.Text   = titulo;
        MessageBlock.Text = mensagem;
        InputBox.Text     = valorInicial;
        InputBox.SelectAll();
        Loaded += (_, _) => InputBox.Focus();
    }

    private void OnConfirmar(object sender, RoutedEventArgs e)
    {
        var valor = InputBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(valor))
        {
            MostrarErro("O nome não pode estar em branco.");
            return;
        }

        var invalidos = Path.GetInvalidFileNameChars();
        if (valor.Any(c => invalidos.Contains(c)))
        {
            MostrarErro("O nome contém caracteres inválidos para um nome de arquivo.");
            return;
        }

        Resultado    = valor;
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
