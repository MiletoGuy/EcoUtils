using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class SqlLibraryViewModel : ViewModelBase
{
    private readonly ISqlLibraryService _libraryService;

    // ── Sub-VM ────────────────────────────────────────────────────────────────

    public SqlDetailViewModel DetalheVM { get; }

    // ── Dados ─────────────────────────────────────────────────────────────────

    private readonly ObservableCollection<SqlEntry> _todasEntradas = [];
    public ICollectionView EntradasView { get; }

    public ObservableCollection<string> Categorias { get; } = ["Todas"];

    // ── Filtros ───────────────────────────────────────────────────────────────

    private string _textoBusca = string.Empty;
    public string TextoBusca
    {
        get => _textoBusca;
        set { SetProperty(ref _textoBusca, value); EntradasView.Refresh(); }
    }

    private string _categoriaFiltro = "Todas";
    public string CategoriaFiltro
    {
        get => _categoriaFiltro;
        set { SetProperty(ref _categoriaFiltro, value); EntradasView.Refresh(); }
    }

    // ── Seleção ───────────────────────────────────────────────────────────────

    private SqlEntry? _entradaSelecionada;
    public SqlEntry? EntradaSelecionada
    {
        get => _entradaSelecionada;
        set { SetProperty(ref _entradaSelecionada, value); DetalheVM.Entrada = value; }
    }

    // ── Ctor ──────────────────────────────────────────────────────────────────

    public SqlLibraryViewModel(
        ISqlLibraryService   libraryService,
        ISqlExecutionService executionService,
        ISqlExportService    exportService,
        IDialogService       dialogService,
        IUserSettingsService settingsService)
    {
        _libraryService = libraryService;
        DetalheVM       = new SqlDetailViewModel(executionService, exportService, dialogService, settingsService);

        EntradasView = CollectionViewSource.GetDefaultView(_todasEntradas);
        EntradasView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SqlEntry.Categoria)));
        EntradasView.SortDescriptions.Add(new SortDescription(nameof(SqlEntry.Categoria), ListSortDirection.Ascending));
        EntradasView.SortDescriptions.Add(new SortDescription(nameof(SqlEntry.Nome),      ListSortDirection.Ascending));
        EntradasView.Filter = FiltrarEntrada;

        CarregarEntradas();
    }

    // ── Carregamento ──────────────────────────────────────────────────────────

    private void CarregarEntradas()
    {
        var todas = _libraryService.ObterTodas();

        _todasEntradas.Clear();
        Categorias.Clear();
        Categorias.Add("Todas");

        var categoriasVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in todas)
        {
            _todasEntradas.Add(e);
            if (categoriasVistas.Add(e.Categoria))
                Categorias.Add(e.Categoria);
        }

        EntradasView.Refresh();
    }

    // ── Filtro ────────────────────────────────────────────────────────────────

    private bool FiltrarEntrada(object obj)
    {
        if (obj is not SqlEntry e) return false;

        if (_categoriaFiltro != "Todas" && e.Categoria != _categoriaFiltro)
            return false;

        if (string.IsNullOrWhiteSpace(_textoBusca))
            return true;

        var busca = _textoBusca.Trim();
        return e.Nome.Contains(busca, StringComparison.OrdinalIgnoreCase)
            || e.Categoria.Contains(busca, StringComparison.OrdinalIgnoreCase)
            || e.Descricao.Contains(busca, StringComparison.OrdinalIgnoreCase);
    }
}
