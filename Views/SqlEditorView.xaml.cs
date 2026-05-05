using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using EcoUtils.ViewModels;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace EcoUtils.Views;

public partial class SqlEditorView : UserControl
{
    private static readonly IHighlightingDefinition SqlDarkHighlighting = CarregarHighlighting();

    private SqlEditorViewModel? _vm;

    public SqlEditorView()
    {
        InitializeComponent();

        EditorSql.SyntaxHighlighting = SqlDarkHighlighting;

        // Pusha edições do editor → ViewModel
        EditorSql.TextChanged += (_, _) =>
        {
            if (_vm is not null && EditorSql.Text != _vm.CorpoSql)
                _vm.CorpoSql = EditorSql.Text;
        };

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as SqlEditorViewModel;

        if (_vm is not null)
        {
            EditorSql.Text = _vm.CorpoSql;
            _vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    // Pusha alterações feitas programaticamente no VM → editor (ex: Carregar())
    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SqlEditorViewModel.CorpoSql) && _vm is not null)
        {
            if (EditorSql.Text != _vm.CorpoSql)
                EditorSql.Text = _vm.CorpoSql;
        }
    }

    private static IHighlightingDefinition CarregarHighlighting()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("EcoUtils.Resources.sql-dark.xshd");

        if (stream is null)
            return HighlightingManager.Instance.GetDefinition("SQL")
                ?? HighlightingManager.Instance.GetDefinition("Default")!;

        using var reader = new System.Xml.XmlTextReader(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
