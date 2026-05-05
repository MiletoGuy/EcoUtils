using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class SqlLibraryViewModel : ViewModelBase
{
    private readonly ISqlLibraryService  _libraryService;
    private readonly ISqlExecutionService _executionService;
    private readonly ISqlExportService   _exportService;
    private readonly IDialogService      _dialogService;
    private readonly IUserSettingsService _settingsService;

    // ── Sub-VMs ───────────────────────────────────────────────────────────────

    public SqlDetailViewModel DetalheVM { get; }

    private SqlEditorViewModel? _editorVm;
    public SqlEditorViewModel? EditorVM
    {
        get => _editorVm;
        private set => SetProperty(ref _editorVm, value);
    }

    private bool _editorAberto;
    public bool EditorAberto
    {
        get => _editorAberto;
        private set => SetProperty(ref _editorAberto, value);
    }

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

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand NovaSqlCommand { get; }

    // ── Ctor ──────────────────────────────────────────────────────────────────

    public SqlLibraryViewModel(
        ISqlLibraryService   libraryService,
        ISqlExecutionService executionService,
        ISqlExportService    exportService,
        IDialogService       dialogService,
        IUserSettingsService settingsService)
    {
        _libraryService   = libraryService;
        _executionService = executionService;
        _exportService    = exportService;
        _dialogService    = dialogService;
        _settingsService  = settingsService;

        DetalheVM = new SqlDetailViewModel(
            executionService, exportService, dialogService, settingsService,
            onEditar: AbrirEditorExistente);

        EntradasView = CollectionViewSource.GetDefaultView(_todasEntradas);
        EntradasView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SqlEntry.Categoria)));
        EntradasView.SortDescriptions.Add(new SortDescription(nameof(SqlEntry.Categoria), ListSortDirection.Ascending));
        EntradasView.SortDescriptions.Add(new SortDescription(nameof(SqlEntry.Nome),      ListSortDirection.Ascending));
        EntradasView.Filter = FiltrarEntrada;

        NovaSqlCommand = new RelayCommand(_ => AbrirEditorNovo());

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

    // ── Abertura do editor ────────────────────────────────────────────────────

    private void AbrirEditorNovo()
    {
        var vm = CriarEditorVm();
        vm.Carregar(null, mostrarAvisoCopia: false);
        EditorVM    = vm;
        EditorAberto = true;
    }

    private void AbrirEditorExistente(SqlEntry entry)
    {
        SqlEntry entryParaEditar;
        bool     mostrarAvisoCopia;

        if (entry.IsBuiltIn)
        {
            entryParaEditar    = _libraryService.ForkBuiltIn(entry.Id);
            mostrarAvisoCopia  = true;
        }
        else
        {
            entryParaEditar   = entry;
            mostrarAvisoCopia = false;
        }

        var vm = CriarEditorVm();
        vm.Carregar(entryParaEditar, mostrarAvisoCopia);
        EditorVM    = vm;
        EditorAberto = true;
    }

    private SqlEditorViewModel CriarEditorVm()
    {
        var categorias = _todasEntradas
            .Select(e => e.Categoria)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);

        return new SqlEditorViewModel(
            _libraryService,
            _dialogService,
            categorias,
            fechar: () =>
            {
                EditorAberto = false;
                EditorVM     = null;
                CarregarEntradas();
            });
    }
}
