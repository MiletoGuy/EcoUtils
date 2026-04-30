using System.Windows;
using EcoUtils.Services.Interfaces;
using EcoUtils.Views;
using Microsoft.Win32;

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

    public string? SelecionarArquivo(string titulo, string filtro)
    {
        var dlg = new OpenFileDialog
        {
            Title  = titulo,
            Filter = filtro
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SolicitarTexto(string titulo, string mensagem, string valorInicial = "")
    {
        var dlg = new TextInputDialog(titulo, mensagem, valorInicial)
        {
            Owner = Application.Current.MainWindow
        };
        return dlg.ShowDialog() == true ? dlg.Resultado : null;
    }

    public (string Versao, string Build)? SolicitarVersaoBuild()
    {
        var dlg = new VersionBuildDialog
        {
            Owner = Application.Current.MainWindow
        };
        if (dlg.ShowDialog() != true) return null;
        return (dlg.Versao!, dlg.Build!);
    }
}
