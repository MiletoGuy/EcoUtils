using System.Windows;

namespace EcoUtils.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string titulo, string mensagem, string textoBotao, bool mostrarCancelar, bool botaoDanger)
    {
        InitializeComponent();
        TitleBlock.Text       = titulo;
        MessageBlock.Text     = mensagem;
        ConfirmButton.Content = textoBotao;

        CancelButton.Visibility = mostrarCancelar ? Visibility.Visible : Visibility.Collapsed;

        if (!botaoDanger)
            ConfirmButton.Style = (Style)FindResource("ButtonPrimary");
    }

    private void OnConfirmar(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancelar(object sender, RoutedEventArgs e)  => DialogResult = false;
}
