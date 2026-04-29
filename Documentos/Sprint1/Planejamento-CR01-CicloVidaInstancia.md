# Planejamento — CR-01: Ciclo de Vida Completo de Executável e .ini por Instância

**Relacionado a:** CR-01 do CodeReview-Sprint1.md  
**Data:** 2026-04-29  
**Status:** Aguardando implementação

---

## 1. Contexto e Motivação

O comportamento atual cria um único arquivo `.ini` cujo nome é baseado apenas no executável
(`Eco_650_10.ini`). Isso causa dois problemas confirmados no CR-04/CR-06:

1. Duas instâncias usando o mesmo executável e bancos diferentes **compartilham o mesmo `.ini`**,
   com a segunda sobrescrevendo a configuração da primeira.
2. Ao excluir uma instância, o `.ini` nunca é removido do disco (CR-01).

A nova abordagem resolve ambos os problemas e adiciona isolamento total entre instâncias:
cada instância possui **seu próprio par `(.exe, .ini)`** dedicado no diretório de execução.

---

## 2. Nova Arquitetura de Pastas

```
C:\ecosis\windows\
│
├── eco.exe              ← executável padrão do ECO (não gerenciado pelo EcoUtils)
├── eco.ini              ← eco.ini padrão/template (lido pelo EcoUtils, nunca modificado)
│
├── eco_650_10_1.exe       ← cópia implantada pelo EcoUtils (instância A)
├── eco_650_10_1.ini       ← .ini gerado pelo EcoUtils (instância A)
├── eco_650_10_2.exe       ← cópia implantada pelo EcoUtils (instância B, mesmo exe, banco diferente)
├── eco_650_10_2.ini       ← .ini gerado pelo EcoUtils (instância B)
│
└── Utils\
    ├── Eco_650_10.exe     ← fonte das versões (adicionado manualmente pelo usuário)
    ├── Eco_700_05.exe
    └── ...
```

**Regras:**
- `C:\ecosis\windows\Utils\` — somente leitura pelo EcoUtils (nunca escreve nem deleta aqui).
- `eco.exe` e `eco.ini` em `C:\ecosis\windows\` — nunca tocados pelo EcoUtils.
- Arquivos `eco_{versao}_{build}_{seq}.*` — criados e deletados exclusivamente pelo EcoUtils.

---

## 3. Nomenclatura dos Arquivos Implantados

| Componente | Valor | Exemplo |
|---|---|---|
| Prefixo | `eco_` (minúsculo) | — |
| Versão+Build | extraído do nome da fonte (`Eco_650_10` → `650_10`) | `650_10` |
| Sequência | menor inteiro positivo disponível em `WindowsDir` para aquele `versao_build` | `1`, `2`, `3`... |
| Extensão | `.exe` ou `.ini` | — |
| **Resultado final** | `eco_{versao}_{build}_{seq}.exe` | `eco_650_10_1.exe`, `eco_650_10_2.exe` |

> **Como o número sequencial é determinado?** Em `ImplantarAsync`, o service escaneia
> `WindowsDir` em busca de arquivos `eco_{versao}_{build}_*.exe` e extrai os sufixos
> numéricos dos nomes existentes. O próximo número é `max(existentes) + 1`;
> se não houver nenhum, começa em `1`. A busca é feita com `File.Exists` no número candidato
> para garantir atomicidade em cenários de criação paralela.

> **Por que prefixo minúsculo `eco_` vs `Eco_` das fontes?** Distinção intencional: permite
> identificar visualmente e programaticamente quais arquivos são gerenciados pelo EcoUtils
> (prefixo `eco_`) vs quais são as fontes originais (prefixo `Eco_`). Também facilita o
> regex de segurança na deleção.

---

## 4. Fluxo de Criação de Instância (novo)

```
Usuário clica Confirmar no flyout
        │
        ▼
InstanceFlyoutViewModel.ConfirmarAsync()
        │
        ├─ chama: IInstanceSetupService.ImplantarAsync(exeFontePath, basePath, instanceId)
        │           │
        │           ├─ 1. Lê eco.ini template (C:\ecosis\windows\eco.ini)
        │           ├─ 2. Valida presença de [windows] e chave dados=   ← resolve CR-05
        │           ├─ 3. Substitui dados= com host:basePath
        │           ├─ 4. Determina próximo número sequencial disponível em WindowsDir
        │           ├─ 5. Copia exeFontePath → C:\ecosis\windows\eco_{v}_{b}_{seq}.exe
        │           ├─ 6. Grava C:\ecosis\windows\eco_{v}_{b}_{seq}.ini
        │           └─ 7. Retorna (exeDestPath, iniDestPath)
        │
        └─ cria EcoInstance com ExePath = exeDestPath, IniPath = iniDestPath
```

---

## 5. Fluxo de Exclusão de Instância (novo)

```
Usuário clica ✕ e confirma o diálogo
        │
        ▼
ExecutarEcoViewModel.ExcluirInstanciaAsync(instancia)
        │
        ├─ chama: IInstanceSetupService.RemoverAsync(instancia.ExecutavelPath, instancia.IniPath)
        │           │
        │           ├─ 1. Valida que o nome do exe bate com regex ^eco_[^_]+_[^_]+_\d+\.exe$
        │           ├─ 2. Valida que o nome do ini bate com regex ^eco_[^_]+_[^_]+_\d+\.ini$
        │           ├─ 3. File.Delete(exePath)    ← só se regex OK
        │           └─ 4. File.Delete(iniPath)    ← só se regex OK
        │
        ├─ Instancias.Remove(instancia)
        └─ _instanceRepository.SalvarAsync(...)
```

**Garantia de segurança:** O regex `^eco_[^_]+_[^_]+_\d+\.(exe|ini)$` assegura
que apenas arquivos com o padrão exato gerado pelo EcoUtils são deletados. `eco.exe`, `eco.ini`
e os arquivos em `Utils\` nunca correspondem ao padrão.

---

## 6. Mudanças Necessárias por Arquivo

### 6.1 `Infrastructure/EcoPathConstants.cs`

**Adicionar:**
```csharp
public const string UtilsDir = @"C:\ecosis\windows\Utils";
```

**Sem alterações** em `WindowsDir`, `EcoIniPadrao`, `DadosDir`.

---

### 6.2 `Services/Interfaces/IIniGeneratorService.cs` → renomear para `IInstanceSetupService.cs`

A responsabilidade do service cresce além de gerar `.ini`: agora também copia o executável.
Renomear a interface é mais expressivo do que acumular métodos com nomes de domínios diferentes.

**Nova interface:**
```csharp
public interface IInstanceSetupService
{
    /// <summary>
    /// Copia o executável fonte para WindowsDir, gera o .ini correspondente e retorna
    /// os caminhos dos arquivos implantados. O número sequencial é determinado
    /// internamente pelo service com base nos arquivos já existentes em WindowsDir.
    /// </summary>
    Task<(string ExePath, string IniPath)> ImplantarAsync(
        string exeFontePath,
        string basePath);

    /// <summary>
    /// Remove os arquivos .exe e .ini de uma instância implantada.
    /// Operação segura: valida o padrão do nome antes de deletar.
    /// </summary>
    void Remover(string exePath, string iniPath);
}
```

> **Nota:** O método antigo `GerarIniAsync(exeNome, basePath)` é absorvido por `ImplantarAsync`.
> `RemoverIni` é absorvido e expandido por `Remover`.

---

### 6.3 `Services/IniGeneratorService.cs` → renomear para `InstanceSetupService.cs`

**Lógica de `ImplantarAsync`:**
1. Extrair `versaoBuild` do nome da fonte: `Path.GetFileNameWithoutExtension(exeFontePath)` → remover prefixo `Eco_` → resultado é `{v}_{b}` (ex.: `650_10`).
2. Determinar o próximo número sequencial: escanear `WindowsDir` com padrão `eco_{v}_{b}_*.exe`, extrair os sufixos numéricos dos nomes encontrados e calcular `max + 1` (ou `1` se nenhum existir).
3. Ler `EcoPathConstants.EcoIniPadrao` e validar presença de `dados=` em `[windows]` (resolve CR-05 como efeito colateral).
4. `File.Copy(exeFontePath, exeDestPath, overwrite: false)` — `overwrite: false` protege contra condição de corrida em criação paralela.
5. Substituir `dados=` e gravar `iniDestPath`.
6. Retornar `(exeDestPath, iniDestPath)`.

**Regex de segurança para `Remover`:**
```csharp
private static readonly Regex _implantadoRegex =
    new(@"^eco_[^_]+_[^_]+_\d+\.(exe|ini)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
```

---

### 6.4 `Models/EcoExecutavel.cs`

Adicionar propriedade para rastrear o caminho fonte (em `Utils\`):

```csharp
public string FontePath { get; set; } = string.Empty;  // C:\ecosis\windows\Utils\Eco_650_10.exe
```

`ExePath` permanece, mas agora representa o arquivo fonte em `Utils\`.
`IniPadraoPresente` pode ser **removido** nesta oportunidade (CR-18 — propriedade redundante).

---

### 6.5 `Services/VersionCatalogService.cs`

Alterar o diretório de scan de `WindowsDir` para `UtilsDir`:

```csharp
// Antes:
if (!Directory.Exists(EcoPathConstants.WindowsDir)) ...
Directory.EnumerateFiles(EcoPathConstants.WindowsDir, "*.exe")

// Depois:
if (!Directory.Exists(EcoPathConstants.UtilsDir)) ...
Directory.EnumerateFiles(EcoPathConstants.UtilsDir, "*.exe")
```

O regex de filtro `^Eco_\d+_\d+\.exe$` **permanece igual** — as fontes em `Utils\`
seguem o mesmo padrão de nomenclatura `Eco_{versao}_{build}.exe`.

Remover o campo `IniPadraoPresente` do `new EcoExecutavel { ... }` (CR-18).

---

### 6.6 `Models/EcoInstance.cs`

Adicionar propriedade para rastrear a fonte usada na criação:

```csharp
public string ExecutavelFontePath { get; set; } = string.Empty;
// ex.: C:\ecosis\windows\Utils\Eco_650_10.exe
```

`ExecutavelPath` passa a armazenar o **caminho do exe implantado** em `WindowsDir`
(`eco_650_10_<guid32>.exe`) — comportamento já esperado pela `LaunchService`.

---

### 6.7 `ViewModels/InstanceFlyoutViewModel.cs`

**`ConfirmarAsync`:** substituir a chamada a `_iniGeneratorService.GerarIniAsync(...)` pela
chamada a `_instanceSetupService.ImplantarAsync(exeFontePath, basePath)`.

```csharp
var (exePath, iniPath) = await _instanceSetupService.ImplantarAsync(
    ExecutavelSelecionado!.ExePath,   // caminho em Utils\
    BancoSelecionado!.EcoPath);

var instancia = new EcoInstance
{
    Id                   = _instanciaExistente?.Id ?? Guid.NewGuid(),
    Apelido              = Apelido.Trim(),
    ExecutavelPath       = exePath,          // caminho implantado em WindowsDir
    ExecutavelFontePath  = ExecutavelSelecionado.ExePath,
    ExecutavelNome       = ExecutavelSelecionado.NomeCompleto,
    BasePath             = BancoSelecionado.EcoPath,
    BaseNome             = BancoSelecionado.NomeCompleto,
    IniPath              = iniPath
};
```

**Ao editar:** se `ExecutavelSelecionado` ou `BancoSelecionado` **não mudaram** em relação
a `_instanciaExistente`, não chamar `ImplantarAsync` — reutilizar os caminhos existentes.
Isso evita copiar o exe desnecessariamente e perder customizações manuais no `.ini`.

```csharp
bool exeIgual  = _instanciaExistente?.ExecutavelFontePath == ExecutavelSelecionado!.ExePath;
bool bancoIgual = _instanciaExistente?.BasePath == BancoSelecionado!.EcoPath;

if (_instanciaExistente is not null && exeIgual && bancoIgual)
{
    // reutilizar caminhos implantados existentes
    exePath = _instanciaExistente.ExecutavelPath;
    iniPath = _instanciaExistente.IniPath;
}
else
{
    // nova implantação
    (exePath, iniPath) = await _instanceSetupService.ImplantarAsync(...);
    // se editando e exe/banco mudou: remover arquivos antigos antes
    if (_instanciaExistente is not null)
        _instanceSetupService.Remover(
            _instanciaExistente.ExecutavelPath,
            _instanciaExistente.IniPath);
}
```

**Dependência injetada:** substituir `IIniGeneratorService` por `IInstanceSetupService`
no construtor.

---

### 6.8 `ViewModels/ExecutarEcoViewModel.cs`

**`ExcluirInstanciaAsync`:** adicionar chamada a `_instanceSetupService.Remover(...)`:

```csharp
private async Task ExcluirInstanciaAsync(EcoInstance instancia)
{
    if (!_dialogService.Confirmar("Excluir instância", $"Excluir \"{instancia.Apelido}\"?", "Excluir"))
        return;

    _instanceSetupService.Remover(instancia.ExecutavelPath, instancia.IniPath);

    Instancias.Remove(instancia);
    await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
}
```

**Dependência injetada:** substituir `IIniGeneratorService` por `IInstanceSetupService`.

---

### 6.9 `App.xaml.cs`

Atualizar o registro no DI container:

```csharp
// Antes:
sc.AddSingleton<IIniGeneratorService, IniGeneratorService>();

// Depois:
sc.AddSingleton<IInstanceSetupService, InstanceSetupService>();
```

---

### 6.10 `Services/Interfaces/IVersionCatalogService.cs` e `Views/InstanceFlyoutView.xaml`

Sem alterações de contrato. O `ComboBox` de executáveis continua exibindo `NomeCompleto`
(`Eco_650_10`), que é o nome do arquivo fonte em `Utils\` — comportamento visual inalterado.

---

## 7. Casos de Borda

| Caso | Comportamento esperado |
|---|---|
| `Utils\` não existe na primeira execução | `VersionCatalogService` retorna lista vazia (já tratado pelo `Directory.Exists`). Exibir mensagem de dica. |
| Arquivo `.exe` fonte deletado de `Utils\` enquanto instância existe | Instância continua funcionando — usa o exe implantado em `WindowsDir`, não o fonte. Edição futura que trocar o exe detectará fonte inexistente. |
| Edição com mudança de exe ou banco | Arquivos antigos são removidos via `Remover` antes de chamar `ImplantarAsync` novamente (ver fluxo de edição, seção 6.7). O novo arquivo recebe o próximo número disponível. |
| Disco cheio ao copiar o `.exe` | `File.Copy` lança `IOException`, capturada em `ConfirmarAsync` → exibida em `ErroConfirmacao`. Nenhum arquivo parcial fica em disco (o `.ini` só é criado após a cópia). |
| `Remover` chamado com path que não bate o regex | Método retorna sem ação — sem exceção, sem deleção. Comportamento idêntico ao atual `RemoverIni`. |
| Criação paralela de instâncias com o mesmo exe | Ambas leem o mesmo `max`, tentam criar o mesmo número sequencial. `overwrite: false` em `File.Copy` lançará `IOException` na segunda — o usuário verá erro em `ErroConfirmacao` e poderá tentar novamente. Risco mínimo em app single-window. |

---

## 8. O Que Não Muda

- `LaunchService` — já usa `instancia.ExecutavelPath`; com a nova abordagem esse path aponta para
  o exe implantado, sem mudança de contrato.
- `InstanceRepository` — serialização/deserialização de `EcoInstance` em JSON;
  o novo campo `ExecutavelFontePath` é incluído automaticamente pelo `JsonSerializer`.
- `VersionCatalogService` regex `^Eco_\d+_\d+\.exe$` — as fontes em `Utils\` seguem o mesmo padrão.
- Design system e Views — nenhuma alteração visual necessária para este conjunto de mudanças.
- `ConfirmDialog`, `DialogService` — sem alterações.

---

## 9. Plano de Commits

### Análise de viabilidade de commit único

Tecnicamente possível — todas as mudanças compilam juntas. Porém não é recomendável:
são **9 arquivos alterados em 4 camadas** (infra, models, service, ViewModels + DI),
sem ponto de rollback intermediário e difíceis de revisar de forma coesa.

A interface `IIniGeneratorService` é injetada em `ExecutarEcoViewModel`,
`InstanceFlyoutViewModel` e `App.xaml.cs`. Sua remoção força todos os consumidores a
mudarem na mesma operação. A estratégia adotada é o padrão **Strangler Fig**: criar o
novo service ao lado do antigo, migrar os consumidores e só então remover o legado —
produzindo 3 commits que compilam e fazem sentido individualmente.

---

### Commit 1 — `refactor: preparar modelos e infra para ciclo de vida por instância`

**Arquivos alterados (4):**

| Arquivo | Mudança |
|---|---|
| `Infrastructure/EcoPathConstants.cs` | + constante `UtilsDir` |
| `Models/EcoExecutavel.cs` | + `FontePath`; - `IniPadraoPresente` |
| `Models/EcoInstance.cs` | + `ExecutavelFontePath` |
| `Services/VersionCatalogService.cs` | Scan em `UtilsDir`; remover atribuição de `IniPadraoPresente` |

**Status após commit:**
- ✅ Compila
- ⚠️ ComboBox de executáveis ficará vazio até a pasta `Utils\` existir na máquina — aceitável em branch de desenvolvimento

---

### Commit 2 — `feat: InstanceSetupService — implantação sequencial e remoção segura de binários`

**Arquivos criados/alterados (3):**

| Arquivo | Mudança |
|---|---|
| `Services/Interfaces/IInstanceSetupService.cs` | Criado (nova interface) |
| `Services/InstanceSetupService.cs` | Criado (nova implementação) |
| `App.xaml.cs` | Registrar `IInstanceSetupService` **ao lado** do `IIniGeneratorService` existente |

**Status após commit:**
- ✅ Compila
- ⚠️ Novo service existe no container mas ainda não é consumido pelos ViewModels
- O `IIniGeneratorService` permanece registrado temporariamente para não quebrar os ViewModels

---

### Commit 3 — `refactor: migrar ViewModels para IInstanceSetupService; remover IIniGeneratorService`

**Arquivos alterados/removidos (5):**

| Arquivo | Mudança |
|---|---|
| `ViewModels/InstanceFlyoutViewModel.cs` | Substituir `IIniGeneratorService` por `IInstanceSetupService`; reescrever `ConfirmarAsync` |
| `ViewModels/ExecutarEcoViewModel.cs` | Substituir `IIniGeneratorService` por `IInstanceSetupService`; adicionar `Remover` na exclusão |
| `App.xaml.cs` | Remover registro de `IIniGeneratorService` |
| `Services/Interfaces/IIniGeneratorService.cs` | **Deletado** |
| `Services/IniGeneratorService.cs` | **Deletado** |

**Status após commit:**
- ✅ Compila
- ✅ Feature completa e funcional

---

## 10. Resumo das Alterações

| Arquivo | Tipo de mudança |
|---|---|
| `EcoPathConstants.cs` | + constante `UtilsDir` |
| `IIniGeneratorService.cs` → `IInstanceSetupService.cs` | Renomear + expandir contrato |
| `IniGeneratorService.cs` → `InstanceSetupService.cs` | Renomear + reimplementar |
| `EcoExecutavel.cs` | + `FontePath`; - `IniPadraoPresente` |
| `EcoInstance.cs` | + `ExecutavelFontePath` |
| `VersionCatalogService.cs` | Scan em `UtilsDir`; remover `IniPadraoPresente` |
| `InstanceFlyoutViewModel.cs` | Substituir `IIniGeneratorService` por `IInstanceSetupService`; lógica de edição |
| `ExecutarEcoViewModel.cs` | + `IInstanceSetupService`; chamar `Remover` na exclusão |
| `App.xaml.cs` | Atualizar registro no DI |
