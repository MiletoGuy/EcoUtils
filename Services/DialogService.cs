using System.Windows;
using System.Linq;
using EcoUtils.Services.Interfaces;
using EcoUtils.Views;
using Microsoft.Win32;

namespace EcoUtils.Services;

public class DialogService : IDialogService
{
    public bool Confirmar(string titulo, string mensagem, string botaoConfirmar = "Confirmar")
    {
        var dlg = new ConfirmDialog(titulo, mensagem, botaoConfirmar, mostrarCancelar: true, botaoDanger: true);
        TentarDefinirOwner(dlg);
        return dlg.ShowDialog() == true;
    }

    public void Notificar(string titulo, string mensagem)
    {
        var dlg = new ConfirmDialog(titulo, mensagem, "OK", mostrarCancelar: false, botaoDanger: false);
        TentarDefinirOwner(dlg);
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
        var dlg = new TextInputDialog(titulo, mensagem, valorInicial);
        TentarDefinirOwner(dlg);
        return dlg.ShowDialog() == true ? dlg.Resultado : null;
    }

    public (string Major, string Versao, string Build)? SolicitarVersaoBuild()
    {
        var dlg = new VersionBuildDialog();
        TentarDefinirOwner(dlg);
        if (dlg.ShowDialog() != true) return null;
        return (dlg.Major!, dlg.Versao!, dlg.Build!);
    }

    private static void TentarDefinirOwner(Window dialog)
    {
        var app = Application.Current;
        if (app is null) return;

        var owner = app.Windows
            .OfType<Window>()
            .Where(w => !ReferenceEquals(w, dialog))
            .FirstOrDefault(w => w.IsActive)
            ?? app.MainWindow;

        if (owner is null || ReferenceEquals(owner, dialog) || !owner.IsLoaded)
            return;

        try
        {
            dialog.Owner = owner;
        }
        catch (InvalidOperationException)
        {
            // Janela ainda não apta para owner; segue sem owner para não bloquear o fluxo.
        }
        catch (ArgumentException)
        {
            // Owner inválido (ex.: mesma janela); segue sem owner.
        }
    }
}
