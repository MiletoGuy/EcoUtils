using System.Data;
using EcoUtils.Commands;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;
using System.Windows.Input;

namespace EcoUtils.ViewModels;

public class SqlResultViewModel : ViewModelBase
{
    private readonly ISqlExportService _exportService;
    private readonly IDialogService    _dialogService;

    private DataTable? _dtCompleto;

    // ── Dados ────────────────────────────────────────────────────────────────

    private DataView? _dataView;
    public DataView? DataView
    {
        get => _dataView;
        private set => SetProperty(ref _dataView, value);
    }

    private string _filtroTexto = string.Empty;
    public string FiltroTexto
    {
        get => _filtroTexto;
        set
        {
            if (SetProperty(ref _filtroTexto, value))
                AplicarFiltro();
        }
    }

    // ── Status ───────────────────────────────────────────────────────────────

    private string _statusTexto = string.Empty;
    public string StatusTexto
    {
        get => _statusTexto;
        private set => SetProperty(ref _statusTexto, value);
    }

    private bool _limiteAtingido;
    public bool LimiteAtingido
    {
        get => _limiteAtingido;
        private set => SetProperty(ref _limiteAtingido, value);
    }

    private int _totalLinhasFiltradas;
    public int TotalLinhasFiltradas
    {
        get => _totalLinhasFiltradas;
        private set
        {
            if (SetProperty(ref _totalLinhasFiltradas, value))
                OnPropertyChanged(nameof(TextoContadorFiltro));
        }
    }

    public string TextoContadorFiltro =>
        _dtCompleto is not null && !string.IsNullOrWhiteSpace(_filtroTexto)
            ? $"{TotalLinhasFiltradas} de {_dtCompleto.Rows.Count}"
            : string.Empty;

    public bool MostrarContadorFiltro =>
        TemResultado && !string.IsNullOrWhiteSpace(_filtroTexto);

    // ── Estados de exibição ──────────────────────────────────────────────────

    private bool _temResultado;
    public bool TemResultado
    {
        get => _temResultado;
        private set
        {
            if (SetProperty(ref _temResultado, value))
                OnPropertyChanged(nameof(MostrarVazio));
        }
    }

    private bool _temErro;
    public bool TemErro
    {
        get => _temErro;
        private set
        {
            if (SetProperty(ref _temErro, value))
                OnPropertyChanged(nameof(MostrarVazio));
        }
    }

    private string? _mensagemErro;
    public string? MensagemErro
    {
        get => _mensagemErro;
        private set => SetProperty(ref _mensagemErro, value);
    }

    public bool MostrarVazio => !_temResultado && !_temErro;

    // ── Commands ─────────────────────────────────────────────────────────────

    public ICommand CopiarTsvCommand    { get; }
    public ICommand ExportarCsvCommand  { get; }

    // ── Ctor ─────────────────────────────────────────────────────────────────

    public SqlResultViewModel(ISqlExportService exportService, IDialogService dialogService)
    {
        _exportService = exportService;
        _dialogService = dialogService;

        CopiarTsvCommand = new RelayCommand(
            _ => _exportService.CopiarTsv(_dataView!),
            _ => _dataView is not null && _dataView.Count > 0);

        ExportarCsvCommand = new AsyncRelayCommand(
            async _ =>
            {
                var path = _dialogService.SalvarArquivo(
                    "Exportar resultados como CSV",
                    "Arquivo CSV (*.csv)|*.csv");
                if (path is null) return;
                await _exportService.ExportarCsvAsync(_dataView!, path);
            },
            _ => _dataView is not null && _dataView.Count > 0);
    }

    // ── Carregamento ─────────────────────────────────────────────────────────

    public void CarregarResultado(SqlExecutionResult result)
    {
        FiltroTexto = string.Empty;

        if (!result.Sucesso)
        {
            TemResultado = false;
            TemErro      = true;
            MensagemErro = result.Erro;
            DataView     = null;
            _dtCompleto  = null;
            StatusTexto  = string.Empty;
            LimiteAtingido       = false;
            TotalLinhasFiltradas = 0;
            return;
        }

        TemErro      = false;
        MensagemErro = null;
        LimiteAtingido = result.LimiteAtingido;

        // Comando de escrita (sem colunas retornadas)
        if (result.Colunas.Count == 0)
        {
            TemResultado = false;
            DataView     = null;
            _dtCompleto  = null;
            TotalLinhasFiltradas = 0;
            var s = result.LinhasAfetadas == 1 ? "1 linha afetada" : $"{result.LinhasAfetadas} linhas afetadas";
            StatusTexto = $"{s} · {FormatarTempo(result.TempoExecucao)}";
            return;
        }

        // SELECT — constrói DataTable com tipos corretos para ordenação nativa
        var dt = new DataTable();
        for (int i = 0; i < result.Colunas.Count; i++)
        {
            var tipo = i < result.TiposColunas.Count ? result.TiposColunas[i] : typeof(string);
            dt.Columns.Add(result.Colunas[i], tipo);
        }

        foreach (var linha in result.Linhas)
        {
            var row = dt.NewRow();
            for (int i = 0; i < result.Colunas.Count; i++)
                row[i] = linha[i] ?? DBNull.Value;
            dt.Rows.Add(row);
        }

        _dtCompleto  = dt;
        DataView     = _dtCompleto.DefaultView;
        TemResultado = true;
        TotalLinhasFiltradas = dt.Rows.Count;
        StatusTexto = $"{result.TotalLinhas} {(result.TotalLinhas == 1 ? "linha" : "linhas")} · {FormatarTempo(result.TempoExecucao)}";

        (CopiarTsvCommand   as RelayCommand)?.RaiseCanExecuteChanged();
        (ExportarCsvCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    public void Limpar()
    {
        FiltroTexto          = string.Empty;
        DataView             = null;
        _dtCompleto          = null;
        TemResultado         = false;
        TemErro              = false;
        MensagemErro         = null;
        StatusTexto          = string.Empty;
        LimiteAtingido       = false;
        TotalLinhasFiltradas = 0;
    }

    // ── Filtro ───────────────────────────────────────────────────────────────

    private void AplicarFiltro()
    {
        OnPropertyChanged(nameof(MostrarContadorFiltro));

        if (_dtCompleto is null) return;

        if (string.IsNullOrWhiteSpace(_filtroTexto))
        {
            DataView             = _dtCompleto.DefaultView;
            TotalLinhasFiltradas = _dtCompleto.Rows.Count;
            return;
        }

        var texto      = _filtroTexto.ToUpperInvariant();
        var dtFiltrado = _dtCompleto.Clone(); // mesma estrutura, sem linhas

        foreach (DataRow row in _dtCompleto.Rows)
        {
            if (row.ItemArray.Any(v =>
                    (v is DBNull ? string.Empty : v?.ToString())
                    ?.ToUpperInvariant().Contains(texto) == true))
                dtFiltrado.ImportRow(row);
        }

        DataView             = dtFiltrado.DefaultView;
        TotalLinhasFiltradas = dtFiltrado.Rows.Count;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatarTempo(TimeSpan t) =>
        t.TotalMilliseconds >= 1000
            ? $"{t.TotalSeconds:F2} s"
            : $"{t.TotalMilliseconds:F0} ms";
}
