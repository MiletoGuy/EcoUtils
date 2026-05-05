using System.Windows;
using System.Windows.Input;

namespace EcoUtils.Views;

public partial class SqlLivreView
{
    public SqlLivreView()
    {
        InitializeComponent();
    }

    // Ctrl+Enter dispara a execução a partir do editor
    private void OnEditorQueryPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            if (DataContext is ViewModels.SqlLivreViewModel vm)
                vm.ExecutarCommand.Execute(null);
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        EditorQuery.PreviewKeyDown += OnEditorQueryPreviewKeyDown;
    }
}
