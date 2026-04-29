using System.Windows;
using EcoUtils.Services.Interfaces;
using EcoUtils.Views;

namespace EcoUtils.Services;

public class DialogService : IDialogService
{
    public bool Confirmar(string titulo, string mensagem, string botaoConfirmar = "Confirmar")
    {
        var dlg = new ConfirmDialog(titulo, mensagem, botaoConfirmar, mostrarCancelar: true, botaoDanger: true)
        {
            Owner = Application.Current.MainWindow
        };
        return dlg.ShowDialog() == true;
    }

    public void Notificar(string titulo, string mensagem)
    {
        var dlg = new ConfirmDialog(titulo, mensagem, "OK", mostrarCancelar: false, botaoDanger: false)
        {
            Owner = Application.Current.MainWindow
        };
        dlg.ShowDialog();
    }
}
