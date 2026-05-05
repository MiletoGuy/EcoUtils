using System.Collections.ObjectModel;
using System.Windows.Input;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.ViewModels;

public class SqlEditorViewModel : ViewModelBase
{
    private readonly ISqlLibraryService _libraryService;
    private readonly IDialogService     _dialogService;
    private readonly Action             _fechar;   // fecha editor + recarrega lista

    private string? _idExistente;  // null = nova SQL

    // ── Campos editáveis ──────────────────────────────────────────────────────

    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        set => SetProperty(ref _nome, value);
    }

    private string _categoria = string.Empty;
    public string Categoria
    {
        get => _categoria;
        set => SetProperty(ref _categoria, value);
    }

    private string _descricao = string.Empty;
    public string Descricao
    {
        get => _descricao;
        set => SetProperty(ref _descricao, value);
    }

    // CorpoSql sincronizado com AvalonEdit via code-behind
    private string _corpoSql = string.Empty;
    public string CorpoSql
    {
        get => _corpoSql;
        set => SetProperty(ref _corpoSql, value);
    }

    // ── Parâmetros ────────────────────────────────────────────────────────────

    public ObservableCollection<SqlParameterEditItem> Parametros { get; } = [];

    // ── Metadados / UI ────────────────────────────────────────────────────────

    private bool _mostrarAvisoCopia;
    public bool MostrarAvisoCopia
    {
        get => _mostrarAvisoCopia;
        private set => SetProperty(ref _mostrarAvisoCopia, value);
    }

    public ObservableCollection<string> CategoriasDisponiveis { get; } = [];

    public string Titulo => _idExistente is null ? "Nova consulta" : "Editar consulta";

    // ── Salvando ──────────────────────────────────────────────────────────────

    private bool _salvando;
    public bool Salvando
    {
        get => _salvando;
        private set
        {
            if (SetProperty(ref _salvando, value))
                (SalvarCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand SalvarCommand            { get; }
    public ICommand CancelarCommand          { get; }
    public ICommand AdicionarParametroCommand { get; }
    public ICommand RemoverParametroCommand  { get; }

    // ── Ctor ──────────────────────────────────────────────────────────────────

    public SqlEditorViewModel(
        ISqlLibraryService   libraryService,
        IDialogService       dialogService,
        IEnumerable<string>  categoriasDisponiveis,
        Action               fechar)
    {
        _libraryService = libraryService;
        _dialogService  = dialogService;
        _fechar         = fechar;

        foreach (var c in categoriasDisponiveis.Distinct().OrderBy(x => x))
            CategoriasDisponiveis.Add(c);

        SalvarCommand = new AsyncRelayCommand(
            async _ =>
            {
                if (!Validar()) return;

                Salvando = true;
                try
                {
                    var entry = ConstruirEntry();
                    await _libraryService.SalvarCustomAsync(entry);
                    _fechar();
                }
                catch (Exception ex)
                {
                    _dialogService.Notificar("Erro ao salvar", ex.Message);
                }
                finally { Salvando = false; }
            },
            _ => !_salvando);

        CancelarCommand = new RelayCommand(_ => _fechar());

        AdicionarParametroCommand = new RelayCommand(_ => Parametros.Add(new SqlParameterEditItem()));

        RemoverParametroCommand = new RelayCommand(p =>
        {
            if (p is SqlParameterEditItem item)
                Parametros.Remove(item);
        });
    }

    // ── Carga ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Carrega uma SqlEntry no editor.
    /// <para>Passa <c>mostrarAvisoCopia = true</c> quando se trata de fork de built-in.</para>
    /// </summary>
    public void Carregar(SqlEntry? entry, bool mostrarAvisoCopia)
    {
        MostrarAvisoCopia = mostrarAvisoCopia;

        if (entry is null)
        {
            _idExistente = null;
            Nome         = string.Empty;
            Categoria    = string.Empty;
            Descricao    = string.Empty;
            CorpoSql     = string.Empty;
            Parametros.Clear();
        }
        else
        {
            _idExistente = entry.Id;
            Nome         = entry.Nome;
            Categoria    = entry.Categoria;
            Descricao    = entry.Descricao;
            CorpoSql     = entry.CorpoSql;
            Parametros.Clear();
            foreach (var p in entry.Parametros)
                Parametros.Add(new SqlParameterEditItem
                {
                    Nome     = p.Nome,
                    Tipo     = p.Tipo,
                    Descricao = p.Descricao
                });
        }

        OnPropertyChanged(nameof(Titulo));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool Validar()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            _dialogService.Notificar("Campo obrigatório", "O nome da consulta é obrigatório.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(Categoria))
        {
            _dialogService.Notificar("Campo obrigatório", "A categoria é obrigatória.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(CorpoSql))
        {
            _dialogService.Notificar("Campo obrigatório", "O corpo SQL é obrigatório.");
            return false;
        }
        foreach (var p in Parametros)
        {
            if (string.IsNullOrWhiteSpace(p.Nome))
            {
                _dialogService.Notificar("Parâmetro inválido", "Todos os parâmetros devem ter um nome.");
                return false;
            }
        }
        return true;
    }

    private SqlEntry ConstruirEntry() => new()
    {
        Id         = _idExistente ?? $"custom-{Guid.NewGuid():N}",
        Nome       = Nome.Trim(),
        Categoria  = Categoria.Trim(),
        Descricao  = Descricao.Trim(),
        CorpoSql   = CorpoSql.Trim(),
        IsBuiltIn  = false,
        Parametros = Parametros.Select(p => new SqlParameter
        {
            Nome     = p.Nome.Trim(),
            Tipo     = p.Tipo,
            Descricao = p.Descricao.Trim()
        }).ToList()
    };
}
