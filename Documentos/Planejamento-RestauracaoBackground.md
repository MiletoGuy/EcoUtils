# Planejamento — Restauração de Bases em Segundo Plano

**Data:** 02/05/2026  
**Status:** Aguardando implementação

---

## 1. Contexto e Motivação

Atualmente, ao importar um backup no flyout de nova instância, o `InstanceFlyoutViewModel`
chama `RestaurarAsync` de forma bloqueante — o flyout permanece travado com uma barra de
progresso indeterminada durante todo o processo do `gbak.exe`, que pode levar vários minutos.

O objetivo é permitir que a restauração ocorra em segundo plano, liberando o usuário para
continuar usando o restante do aplicativo enquanto aguarda. A tabela de instâncias deve
exibir o status de cada base em restauração e sinalizar a conclusão (ou falha).

---

## 2. Visão Geral da Solução

```
┌─ InstanceFlyoutViewModel ─────────────────────────────────────────┐
│  AdicionarBancoAsync (backup):                                    │
│  → _restoreJobService.Iniciar(backup, destino, apelido)           │
│  → Retorna imediatamente. EcoDatabase criado com caminho destino. │
│  → Flyout pode ser fechado normalmente.                           │
└───────────────────────────────────────────────────────────────────┘
         ↓ (em background, Task.Run)
┌─ RestoreJobService ────────────────────────────────────────────────┐
│  → IRestoreService.RestaurarAsync(backup, destino, progress, ct)   │
│  → Atualiza RestoreJobEntry.UltimaMensagem via Dispatcher          │
│  → Ao concluir: entry.Status = Concluido/Falhou + JobFinalizado    │
└────────────────────────────────────────────────────────────────────┘
         ↓ (evento JobFinalizado)
┌─ ExecutarEcoViewModel ────────────────────────────────────────────┐
│  → Vincula job à EcoInstance por BasePath                         │
│  → Propaga StatusRestauracao e UltimaMensagemRestauracao           │
│  → Após 30s de Concluido: limpa status                            │
└───────────────────────────────────────────────────────────────────┘
         ↓ (binding INPC)
┌─ ExecutarEcoView.xaml ────────────────────────────────────────────┐
│  Coluna "Status":                                                  │
│  • Restaurando → spinner + tooltip com última mensagem             │
│  • Concluido   → ícone ✓ verde                                     │
│  • Falhou      → ícone ✕ vermelho + tooltip do erro                │
│  Botão ▶ desabilitado enquanto Restaurando                        │
└───────────────────────────────────────────────────────────────────┘
```

---

## 3. Novos Componentes

### 3.1 `Models/RestoreJobStatus.cs`

Enum simples com os três estados possíveis de um job:

```csharp
public enum RestoreJobStatus { Restaurando, Concluido, Falhou }
```

### 3.2 `Models/RestoreJobEntry.cs`

Representa um job de restauração em andamento ou finalizado.
Implementa `INotifyPropertyChanged` (via `ViewModelBase`) para que o ViewModel
possa fazer binding nas suas propriedades.

```csharp
public class RestoreJobEntry : ViewModelBase
{
    public Guid   Id            { get; init; } = Guid.NewGuid();
    public string Apelido       { get; init; } = string.Empty;
    public string ArquivoBackup { get; init; } = string.Empty;
    public string DestinoEco    { get; init; } = string.Empty;

    // Mutável — notifica observers ao mudar
    public RestoreJobStatus Status        { get; set; }  // SetProperty
    public string UltimaMensagem          { get; set; }  // SetProperty
    public string? Erro                   { get; set; }  // SetProperty

    // Token de cancelamento — exposto para permitir cancelar da UI
    public CancellationTokenSource Cts { get; } = new();
}
```

### 3.3 `Services/Interfaces/IRestoreJobService.cs`

```csharp
public interface IRestoreJobService
{
    /// Inicia a restauração em background. Retorna imediatamente com o job criado.
    RestoreJobEntry Iniciar(string arquivoBackup, string destinoEco, string apelido);

    /// Retorna o job ativo para aquele caminho destino, ou null.
    RestoreJobEntry? ObterPorDestino(string destinoEco);

    /// Atalho: true se há job com Status == Restaurando para aquele destino.
    bool EstaRestaurando(string destinoEco);

    /// Cancela o job (mata o gbak e deleta o .eco parcial).
    void Cancelar(string destinoEco);

    /// Disparado quando um job muda para Concluido ou Falhou.
    event EventHandler<RestoreJobEntry> JobFinalizado;
}
```

### 3.4 `Services/RestoreJobService.cs`

Implementação singleton.

**Responsabilidades:**
- Mantém `List<RestoreJobEntry>` em memória (jobs persistem durante a sessão).
- Em `Iniciar()`: cria `RestoreJobEntry`, adiciona à lista, dispara `Task.Run` e retorna.
- Dentro do `Task.Run`:
  - Chama `IRestoreService.RestaurarAsync` com `IProgress` que atualiza
    `entry.UltimaMensagem` via `Application.Current.Dispatcher.Invoke`.
  - Em caso de sucesso: `entry.Status = Concluido`.
  - Em caso de `OperationCanceledException`: `entry.Status = Falhou`, `entry.Erro = "Cancelado"`.
  - Em caso de outra exceção: `entry.Status = Falhou`, `entry.Erro = ex.Message`.
  - Sempre dispara `JobFinalizado` no Dispatcher ao finalizar.
- Em `Cancelar()`: chama `entry.Cts.Cancel()`.

---

## 4. Componentes Modificados

### 4.1 `Models/EcoInstance.cs`

Atualmente é uma classe POCO sem `INotifyPropertyChanged`. Precisa ser modificada para
que a tabela reflita mudanças de status em tempo real.

**Mudanças:**
- Implementar `INotifyPropertyChanged` (herdar `ViewModelBase` ou implementar manualmente
  com `[JsonIgnore]` nos campos novos para não quebrar serialização).
- Adicionar dois campos somente em memória:

```csharp
[JsonIgnore]
public RestoreJobStatus? StatusRestauracao { get; set; }  // SetProperty

[JsonIgnore]
public string? UltimaMensagemRestauracao  { get; set; }  // SetProperty

[JsonIgnore]
public string? ErroRestauracao            { get; set; }  // SetProperty
```

> Os campos existentes (`Apelido`, `BasePath`, etc.) continuam como propriedades simples —
> não precisam notificar pois são imutáveis após a criação da instância.

### 4.2 `Services/Interfaces/IRestoreService.cs` e `Services/RestoreService.cs`

Sem alterações. O `RestoreJobService` os consome internamente.

### 4.3 `ViewModels/InstanceFlyoutViewModel.cs`

**Injeção:** Adicionar `IRestoreJobService _restoreJobService` no construtor.

**Mudança em `AdicionarBancoAsync` (caminho backup):**

| Antes | Depois |
|---|---|
| Confirma via dialog "Restaurar agora?" | Mantém o dialog de confirmação |
| Solicita nome da base via dialog | Mantém o dialog de nome |
| `await _restoreService.RestaurarAsync(...)` — bloqueia flyout | `_restoreJobService.Iniciar(backup, destino, apelido)` |
| Progress bar visível durante todo o gbak | Mensagem rápida: *"Restauração iniciada em segundo plano"* |
| `EcoDatabase` criado ao finalizar | `EcoDatabase` criado imediatamente com caminho pré-calculado |
| Barra de erro se falhar | Falha visível apenas na tabela de instâncias |

**Observação sobre o `EcoDatabase` imediato:** O arquivo `.eco` não precisa existir para
criar o objeto em memória. O `ImplantarAsync` apenas escreve o caminho no `.ini`. A base
estará disponível para uso assim que o gbak concluir.

**Mudança em `ConfirmarAsync`:**
- Após salvar a instância, verificar se `BasePath` tem job ativo via
  `_restoreJobService.ObterPorDestino(inst.BasePath)`.
- Se sim, vincular imediatamente: `instancia.StatusRestauracao = RestoreJobStatus.Restaurando`.
  O `ExecutarEcoViewModel` fará o restante quando receber o callback `onConfirmado`.

### 4.4 `ViewModels/ExecutarEcoViewModel.cs`

**Injeção:** Adicionar `IRestoreJobService _restoreJobService` no construtor.

**Em `CarregarInstanciasAsync`** (após popular `Instancias`):
```csharp
foreach (var inst in Instancias)
{
    var job = _restoreJobService.ObterPorDestino(inst.BasePath);
    if (job != null)
        VincularJobAInstancia(inst, job);
}
```

**Inscrição global no construtor:**
```csharp
_restoreJobService.JobFinalizado += OnJobFinalizado;
```

**Novo método `VincularJobAInstancia(EcoInstance inst, RestoreJobEntry job)`:**
```csharp
inst.StatusRestauracao       = job.Status;
inst.UltimaMensagemRestauracao = job.UltimaMensagem;
inst.ErroRestauracao         = job.Erro;

// Propaga atualizações futuras do job → instância
job.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(RestoreJobEntry.UltimaMensagem))
        inst.UltimaMensagemRestauracao = job.UltimaMensagem;
    if (e.PropertyName == nameof(RestoreJobEntry.Status))
        inst.StatusRestauracao = job.Status;
    if (e.PropertyName == nameof(RestoreJobEntry.Erro))
        inst.ErroRestauracao = job.Erro;
};
```

**Novo método `OnJobFinalizado`:**
```csharp
private async void OnJobFinalizado(object? sender, RestoreJobEntry job)
{
    var inst = Instancias.FirstOrDefault(i => i.BasePath == job.DestinoEco);
    if (inst == null) return;

    inst.StatusRestauracao = job.Status;
    inst.ErroRestauracao   = job.Erro;

    if (job.Status == RestoreJobStatus.Concluido)
    {
        await Task.Delay(TimeSpan.FromSeconds(30));
        inst.StatusRestauracao         = null;
        inst.UltimaMensagemRestauracao = null;
    }
}
```

**Modificar `ExecutarCommand`:**
- Verificar `inst.StatusRestauracao != RestoreJobStatus.Restaurando` antes de executar.
- Caso esteja restaurando: exibir notificação ou simplesmente não executar (o botão
  já estará desabilitado no XAML).

**Modificar callback `onConfirmado` em `AbrirFlyoutNovo` e `AbrirFlyoutEditar`:**
- Após `Instancias.Add(instancia)`, verificar job ativo e vincular:
  ```csharp
  var job = _restoreJobService.ObterPorDestino(instancia.BasePath);
  if (job != null) VincularJobAInstancia(instancia, job);
  ```

### 4.5 `Views/ExecutarEcoView.xaml`

**Nova coluna "Status"** inserida entre "Versão" e "Ações":

- Header: `Status` (sem ordenação por enquanto).
- Visibilidade controlada por propriedade `MostrarStatus` no ViewModel
  (opcional, pode começar sempre visível).
- Célula: `DataTemplate` com `DataTrigger` em `StatusRestauracao`:

| Status | Visual |
|---|---|
| `null` | Célula vazia |
| `Restaurando` | Spinner animado (`ProgressBar IsIndeterminate`) + `ToolTip` com `UltimaMensagemRestauracao` |
| `Concluido` | Ícone ✓ (`Path` verde, estilo dos demais ícones do app) + texto "Concluído" |
| `Falhou` | Ícone ✕ (`Path` vermelho) + `ToolTip` com `ErroRestauracao` |

**Botão Executar (▶):**
```xml
IsEnabled="{Binding StatusRestauracao,
    Converter={StaticResource RestaurandoParaDesabilitado}}"
ToolTip="{Binding StatusRestauracao,
    Converter={StaticResource RestaurandoParaTooltip}}"
```

Ou via `DataTrigger` diretamente no `Style` do botão se preferir evitar conversor.

**Botão Cancelar** (dentro da célula de Status, visível somente quando `Restaurando`):
- Chama `_restoreJobService.Cancelar(inst.BasePath)` via `ICommand` no ViewModel.

### 4.6 `App.xaml.cs`

Registrar `RestoreJobService` como **singleton** no container de DI:

```csharp
services.AddSingleton<IRestoreJobService, RestoreJobService>();
```

---

## 5. Edge Cases

### 5.1 App fechado durante restauração

O `RestoreService` já deleta o `.eco` parcial ao cancelar. Se o app fechar de forma
abrupta (sem cancelar), o arquivo `.eco` incompleto permanece no disco.

**Tratamento no startup (`CarregarInstanciasAsync`):**
- Para cada instância, verificar se `BasePath` existe com `File.Exists`.
- Se não existir: marcar `inst.StatusRestauracao = RestoreJobStatus.Falhou` e
  `inst.ErroRestauracao = "Arquivo de base não encontrado. A restauração pode ter sido interrompida."`.
- O usuário poderá editar a instância e reimportar o backup.

### 5.2 Flyout de edição durante restauração ativa

Se o usuário abrir o flyout de edição de uma instância que está restaurando:
- Exibir aviso no flyout: *"Esta base está sendo restaurada. Aguarde a conclusão."*
- Desabilitar o botão "Salvar/Confirmar" até o job terminar.
- Oferecer botão "Cancelar Restauração" no flyout.

### 5.3 Concorrência de jobs no mesmo destino

`RestoreJobService.Iniciar` deve rejeitar (ou sobrescrever após confirmação) se já
houver um job `Restaurando` para o mesmo `DestinoEco`. Evita dois `gbak` escrevendo
no mesmo arquivo.

---

## 6. Divisão em Commits

A implementação é extensa. A divisão abaixo permite validar e commitar em partes
independentes, sem deixar o app em estado quebrado entre commits.

---

### Commit 1 — `feat: modelos e serviço de jobs de restauração em background`

**Arquivos criados:**
- `Models/RestoreJobStatus.cs`
- `Models/RestoreJobEntry.cs`
- `Services/Interfaces/IRestoreJobService.cs`
- `Services/RestoreJobService.cs`

**Arquivos modificados:**
- `App.xaml.cs` — registrar `RestoreJobService` como singleton

**Critério de conclusão:** Build verde. Nenhuma funcionalidade visível alterada ainda.

---

### Commit 2 — `feat: EcoInstance com suporte a status de restauração em memória`

**Arquivos modificados:**
- `Models/EcoInstance.cs` — implementar `INotifyPropertyChanged` e adicionar
  `StatusRestauracao`, `UltimaMensagemRestauracao`, `ErroRestauracao` com `[JsonIgnore]`

**Critério de conclusão:** Build verde. Serialização para `instancias.json` inalterada
(verificar com teste manual de adicionar e reabrir o app).

---

### Commit 3 — `feat: flyout dispara restauração em segundo plano`

**Arquivos modificados:**
- `ViewModels/InstanceFlyoutViewModel.cs` — injetar `IRestoreJobService`, alterar
  `AdicionarBancoAsync` para chamar `Iniciar` em vez de aguardar `RestaurarAsync`,
  criar `EcoDatabase` imediatamente, e vincular status no `ConfirmarAsync`

**Critério de conclusão:** Importar um backup no flyout fecha o flyout normalmente.
O gbak continua rodando em background (verificável via Task Manager).

---

### Commit 4 — `feat: tabela de instâncias reflete status de restauração`

**Arquivos modificados:**
- `ViewModels/ExecutarEcoViewModel.cs` — injetar `IRestoreJobService`, implementar
  `VincularJobAInstancia`, `OnJobFinalizado`, limpeza após 30s, e checagem de arquivo
  ausente no startup
- `ViewModels/InstanceFlyoutViewModel.cs` (ajuste menor) — vincular job no callback
  `onConfirmado`

**Critério de conclusão:** Após fechar o flyout com backup, a linha da instância na
tabela mostra `StatusRestauracao == Restaurando`.

---

### Commit 5 — `feat: UI de status de restauração na tabela de instâncias`

**Arquivos modificados:**
- `Views/ExecutarEcoView.xaml` — adicionar coluna Status com spinner/check/erro,
  desabilitar botão ▶ durante restauração, adicionar botão Cancelar na célula de status
- `ViewModels/ExecutarEcoViewModel.cs` — adicionar `CancelarRestauracaoCommand`
- `Converters/` — adicionar conversor `RestoreJobStatusToVisibilityConverter` se necessário
  (ou usar `DataTrigger` direto no XAML)

**Critério de conclusão:** Spinner visível na tabela durante restauração. Botão ▶
desabilitado. Ícone ✓ aparece ao concluir e some após 30s.

---

### Commit 6 (opcional) — `feat: tratamento de edge cases de restauração`

**Arquivos modificados:**
- `ViewModels/ExecutarEcoViewModel.cs` — checagem de `File.Exists` no startup para
  bases com restore interrompido
- `ViewModels/InstanceFlyoutViewModel.cs` — aviso e bloqueio de "Salvar" quando instância
  está em restauração ativa; rejeição de job duplicado no mesmo destino

**Critério de conclusão:** App reiniciado com `.eco` ausente exibe status "Falhou" com
mensagem explicativa na tabela.

---

## 7. Dependências entre Commits

```
Commit 1 (modelos + serviço)
    └── Commit 2 (EcoInstance INPC)
            └── Commit 3 (flyout dispara em background)
                    └── Commit 4 (ViewModel vincula jobs)
                            └── Commit 5 (UI da tabela)
                                    └── Commit 6 (edge cases)
```

Cada commit depende do anterior. Não há paralelização possível nesta sequência.
