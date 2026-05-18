using System.Windows;

namespace EcoUtils.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void AtualizarStatus(string mensagem)
    {
        StatusText.Text = mensagem;
    }
}
