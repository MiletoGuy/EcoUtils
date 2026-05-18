# Planejamento — Suporte Dual Firebird 2.5 / 5.0

**Data:** 14/05/2026 — Revisado: 15/05/2026  
**Objetivo:** Preparar o EcoUtils para operar tanto com Firebird 2.5 quanto com Firebird 5.0, adicionando seleção por instância, configuração de portas, caminhos de DLL e escrita dos novos parâmetros nos arquivos `.ini`.

---

## 1. Resumo das Mudanças

| Área | O que muda |
|---|---|
| `UserSettings` + `ConfiguracoesViewModel` + `ConfiguracoesView` | 4 novas configs: 2 portas FB, 2 caminhos de DLL; ao salvar com porta alterada, propaga para todos os `.ini` existentes |
| `IniPreferencias` | +`FirebirdDllPath` (`Firebird=` em `[preferencias]`) e +`VersaoFirebird` (lida de `[Eco]`) |
| `EcoInstance` | +`VersaoFirebird` (persistida) e +`AvisoDllFirebird` (persistida — sinaliza DLL ausente) |
| `EcoPathConstants` | Caminhos default das DLLs e caminho legado |
| `InstanceSetupService` | Escrita cirúrgica de `[Eco]` (nova ou atualizada), atualização de `dados=` com porta, leitura de `[Eco]`, fallback DLL |
| `IInstanceSetupService` | Novo record `ImplantarOpcoes`; novo método `AtualizarSecoesFbAsync`; `LerPreferenciasAsync` estendido |
| `InstanceFlyoutViewModel` | RadioButton FB, validação/fallback DLL, aviso de DLL ausente, passar versão ao implantar |
| `InstanceFlyoutView.xaml` | UI do RadioButton de versão Firebird |
| `ExecutarEcoView` (tabela) | Indicador visual de aviso DLL ausente na linha da instância |
| `gbak`/`gfix` para FB 5.0 | Endereçado ao final — aguarda aquisição dos binários |

---

## 2. Novos Parâmetros nos `.ini`

### 2.1 Seção `[Eco]` (nova)
```ini
[Eco]
DBServidor=127.0.0.1
DBPorta=3050
DBUser=SYSDBA
DBPassword=masterkey
FirebirdVersao=2.5
```
- `DBPorta` recebe o valor da configuração global correspondente à versão escolhida.
- `FirebirdVersao` recebe `"2.5"` ou `"5.0"` conforme o RadioButton.

### 2.2 Seção `[windows]` (modificação)
Atualmente:
```ini
dados=127.0.0.1:C:\ecosis\dados\base.eco
```
Passa a incluir a porta no formato GDS:
```ini
dados=127.0.0.1/3050:C:\ecosis\dados\base.eco
```

### 2.3 Seção `[preferencias]` (nova chave)
```ini
[preferencias]
Firebird=C:\ecosis\windows\Firebird\2.5\fbclient.dll
```

---

## 3. Mudanças por Artefato

### 3.1 `Models/UserSettings.cs`
Adicionar:
```csharp
public string PortaFirebird25   { get; set; } = "3050";
public string PortaFirebird50   { get; set; } = "3051";
public string DllFirebird25Path { get; set; } = string.Empty;
public string DllFirebird50Path { get; set; } = string.Empty;
```
> `DllFirebird25Path` e `DllFirebird50Path` ficam vazias por padrão — o auto-detect é feito em `ConfiguracoesViewModel.Resetar()`.

---

### 3.2 `Models/IniPreferencias.cs`
Adicionar duas propriedades. `FirebirdDllPath` é escrita em `[preferencias]`; `VersaoFirebird` é lida de `[Eco]` (não escrita por este model — a seção `[Eco]` é gerenciada por `AplicarSecaoEco`):
```csharp
public string FirebirdDllPath { get; set; } = string.Empty;
public string VersaoFirebird  { get; set; } = "2.5";   // populado por LerPreferenciasAsync ao ler [Eco]
```

---

### 3.3 `Models/EcoInstance.cs`
Adicionar duas propriedades persistidas:
```csharp
public string VersaoFirebird    { get; set; } = "2.5";   // default ao deserializar instâncias antigas
public bool   AvisoDllFirebird  { get; set; } = false;   // true quando DLL ausente na criação/edição
```
> `AvisoDllFirebird` é serializado para que o aviso persista entre reinícios do Utils. É limpo automaticamente ao editar a instância caso a DLL seja encontrada.

---

### 3.4 `Infrastructure/EcoPathConstants.cs`
Adicionar constantes:
```csharp
// Estrutura de diretórios Firebird
public static string FirebirdBaseDir  => Path.Combine(WindowsDir, "Firebird");
public static string Firebird25Dir    => Path.Combine(FirebirdBaseDir, "2.5");
public static string Firebird50Dir    => Path.Combine(FirebirdBaseDir, "5.0");

// Caminhos padrão das DLLs
public static string Firebird25DllPadrao => Path.Combine(Firebird25Dir, "fbclient.dll");
public static string Firebird50DllPadrao => Path.Combine(Firebird50Dir, "fbclient.dll");

// Localização legada (Firebird 2.5 antes da nova estrutura de pastas)
public static string FirebirdLegacyDll => Path.Combine(WindowsDir, "fbclient.dll");
```

---

### 3.5 `Services/InstanceSetupService.cs`

#### a) Record de opções de implantação
```csharp
public record ImplantarOpcoes(
    IniPreferencias Preferencias,
    string          VersaoFirebird,   // "2.5" ou "5.0"
    string          PortaFirebird,    // valor resolvido (ex: "3050")
    string          DllFirebirdPath   // caminho resolvido da fbclient.dll (pode ser vazio)
);
```

#### b) `ImplantarAsync` — atualizado
- `dados=` passa para o formato `host/porta:basePath`.
- Chama `AplicarSecaoEco` para inserir/atualizar a seção `[Eco]` no `.ini` gerado.
- `AplicarPreferencias` agora também escreve `Firebird=` via `FirebirdDllPath`.

#### c) Novo `AtualizarSecoesFbAsync(string iniPath, ImplantarOpcoes opcoes)`
Método cirúrgico para uso tanto na edição de instâncias (sem troca de executável/banco) quanto na propagação de mudança de porta. Faz três operações em uma leitura+escrita do arquivo:
1. Atualiza/insere `[Eco]` via `AplicarSecaoEco`.
2. Atualiza `dados=` em `[windows]` com a nova porta.
3. Atualiza `Firebird=` em `[preferencias]` via `AplicarPreferencias` (somente essa chave).

> "Apenas os relevantes": cada método toca somente as chaves que lhe competem, preservando o restante do arquivo intacto.

#### d) `LerPreferenciasAsync` — estendido
Além das chaves de `[preferencias]`, lê também `FirebirdVersao=` da seção `[Eco]` e popula `IniPreferencias.VersaoFirebird`. Uma única passagem pelo arquivo resolve ambas as seções.

#### e) `AplicarPreferencias` — atualizado
Inclui `Firebird` no dicionário de chaves gerenciadas.

#### f) Novo `AplicarSecaoEco(string[] linhas, ImplantarOpcoes opcoes)` → `string[]`
Análogo ao `AplicarPreferencias`. Gerencia as 5 chaves: `DBServidor` (sempre `127.0.0.1`), `DBPorta`, `DBUser` (sempre `SYSDBA`), `DBPassword` (sempre `masterkey`), `FirebirdVersao`.  
Se a seção `[Eco]` não existir no arquivo, ela é **inserida imediatamente antes de `[windows]`** (ou ao final, se `[windows]` também não existir). Os valores `DBServidor`, `DBUser` e `DBPassword` são sempre escritos com os valores padrão fixos — não são configuráveis pelo usuário.

---

### 3.6 `Services/Interfaces/IInstanceSetupService.cs`
- Atualizar assinatura de `ImplantarAsync` para receber `ImplantarOpcoes` (além de `exeFontePath` e `basePath`).
- Adicionar `Task AtualizarSecoesFbAsync(string iniPath, ImplantarOpcoes opcoes)`.
- Manter `AtualizarPreferenciasAsync` para edições que não trocam executável/banco: internamente ele chamará as três atualizações cirúrgicas.
- `LerPreferenciasAsync` mantém a mesma assinatura de retorno (`IniPreferencias`), mas agora também lê `[Eco].FirebirdVersao`.

---

### 3.7 `ViewModels/InstanceFlyoutViewModel.cs`

#### a) Dependência adicional
Injetar `IUserSettingsService` no construtor (para ler portas e dll paths, e gravar o fallback).

#### b) Propriedades de versão Firebird
```csharp
private string _versaoFirebird = "2.5";
public string VersaoFirebird
{
    get => _versaoFirebird;
    set => SetProperty(ref _versaoFirebird, value);
}

// Propriedades bool derivadas para binding de RadioButton
public bool VersaoFirebird25
{
    get => _versaoFirebird == "2.5";
    set { if (value) VersaoFirebird = "2.5"; }
}
public bool VersaoFirebird50
{
    get => _versaoFirebird == "5.0";
    set { if (value) VersaoFirebird = "5.0"; }
}
```
> `SetProperty` em `VersaoFirebird` deve chamar `OnPropertyChanged` para `VersaoFirebird25` e `VersaoFirebird50`.

#### c) Carregamento ao editar
```csharp
VersaoFirebird = _instanciaExistente.VersaoFirebird;
// LerPreferenciasAsync já retorna VersaoFirebird lido de [Eco], mas
// a fonte primária é a propriedade serializada da instância:
// os dois devem ser consistentes; em caso de conflito, o da instância prevalece.
```

#### d) Resolução de porta e DLL + fallback em `ConfirmarAsync`
```
porta = VersaoFirebird == "2.5"
    ? settings.PortaFirebird25    // "3050"
    : settings.PortaFirebird50    // "3051"

dllPath = VersaoFirebird == "2.5"
    ? settings.DllFirebird25Path
    : settings.DllFirebird50Path

// FB 2.5: fallback e migração automática
Se VersaoFirebird == "2.5" E (dllPath vazio OU File.NotExists(dllPath)):
    Se File.Exists(EcoPathConstants.FirebirdLegacyDll):
        Directory.CreateDirectory(EcoPathConstants.Firebird25Dir)
        File.Copy(FirebirdLegacyDll, Firebird25DllPadrao, overwrite: false)
        settings.DllFirebird25Path = Firebird25DllPadrao
        await _userSettingsService.SalvarAsync()
        dllPath = Firebird25DllPadrao
    Senão:
        dllPath = string.Empty   // segue sem bloquear

// FB 5.0: sem fallback
Se VersaoFirebird == "5.0" E (dllPath vazio OU File.NotExists(dllPath)):
    Se File.Exists(EcoPathConstants.Firebird50DllPadrao):
        settings.DllFirebird50Path = Firebird50DllPadrao
        await _userSettingsService.SalvarAsync()
        dllPath = Firebird50DllPadrao
    Senão:
        dllPath = string.Empty
        avisosDll = true   // sinaliza para o EcoInstance.AvisoDllFirebird
```

#### e) Construção das opções e chamada de `ImplantarAsync` / `AtualizarPreferenciasAsync`
```csharp
var opcoes = new ImplantarOpcoes(prefs, VersaoFirebird, porta, dllPath);

if (fonteAlterada)
    (exePath, iniPath) = await _instanceSetupService.ImplantarAsync(exe, banco, opcoes);
else
    await _instanceSetupService.AtualizarPreferenciasAsync(iniPath, opcoes);
```

#### f) Construção do `EcoInstance` (ao final de `ConfirmarAsync`)
```csharp
VersaoFirebird   = VersaoFirebird,
AvisoDllFirebird = avisosDll,
```
> Se `avisosDll == false` e a instância existente tinha `AvisoDllFirebird == true`, o aviso é limpo.

---

### 3.8 `ViewModels/ConfiguracoesViewModel.cs`

#### Dependências adicionais
Injetar `IInstanceRepository` e `IInstanceSetupService` no construtor para propagar mudança de porta.

#### Novas propriedades
```csharp
public string PortaFirebird25   { get; set; }
public string PortaFirebird50   { get; set; }
public string DllFirebird25Path { get; set; }
public string DllFirebird50Path { get; set; }
```

#### Comandos de browse
- `BrowseFirebird25DllCommand` — `OpenFileDialog` filtrado por `fbclient.dll|*.dll`.
- `BrowseFirebird50DllCommand` — idem.

#### Auto-detect em `Resetar()`
```
Se DllFirebird25Path vazio → verificar Firebird25DllPadrao → preencher se existir
Se DllFirebird50Path vazio → verificar Firebird50DllPadrao → preencher se existir
```
> Preenche em memória; o usuário confirma com Salvar.

#### `SalvarCommand` — propagação de porta
Antes de chamar `SalvarAsync`, capturar as portas **antigas** (ainda em `Settings`) para comparação:
```
portaAntiga25 = settings.PortaFirebird25
portaAntiga50 = settings.PortaFirebird50

// salva os novos valores
settings.PortaFirebird25   = PortaFirebird25.Trim()
settings.PortaFirebird50   = PortaFirebird50.Trim()
settings.DllFirebird25Path = DllFirebird25Path.Trim()
settings.DllFirebird50Path = DllFirebird50Path.Trim()
await _userSettingsService.SalvarAsync()

// Propaga mudança de porta para todos os .ini existentes
Se portaAntiga25 != settings.PortaFirebird25:
    Para cada instância em await _instanceRepository.CarregarAsync()
       onde VersaoFirebird == "2.5" E File.Exists(IniPath):
           opcoes = new ImplantarOpcoes(prefs_placeholder, "2.5", settings.PortaFirebird25, settings.DllFirebird25Path)
           await _instanceSetupService.AtualizarSecoesFbAsync(instancia.IniPath, opcoes)

Se portaAntiga50 != settings.PortaFirebird50:
    // mesmo padrão para "5.0"
```
> `prefs_placeholder`: ao chamar `AtualizarSecoesFbAsync` apenas para porta, passamos `null` para `Preferencias` e o método ignora a seção `[preferencias]` nesse caso — ou usamos um overload que só recebe versão e porta. **Ver refinamento na interface.**

> **Atenção:** a propagação ocorre de forma assíncrona no `SalvarCommand`. Se algum `.ini` estiver em uso pelo Eco rodando, `File.WriteAllLines` pode falhar. O erro deve ser capturado por instância (não abortar toda a propagação) e logado via `ILogService`.

---

### 3.9 `Views/InstanceFlyoutView.xaml`

Adicionar bloco de RadioButton no flyout, preferencialmente entre o campo de executável e a seção "Configurações do .ini":

```xml
<!-- Versão Firebird -->
<TextBlock Text="Versão do Firebird"
           Foreground="{StaticResource TextSecondary}"
           Margin="0,12,0,4"/>
<StackPanel Orientation="Horizontal">
    <RadioButton Content="Firebird 2.5"
                 IsChecked="{Binding VersaoFirebird25, Mode=TwoWay}"
                 Margin="0,0,16,0"/>
    <RadioButton Content="Firebird 5.0"
                 IsChecked="{Binding VersaoFirebird50, Mode=TwoWay}"/>
</StackPanel>
```
> Binding via propriedades bool derivadas `VersaoFirebird25` e `VersaoFirebird50` no ViewModel.

---

### 3.10 `Views/ConfiguracoesView.xaml`

Adicionar nova subseção "Firebird" na aba Geral, após a seção do IBExpert:

```xml
<!-- ── Firebird ── -->
<TextBlock Text="Firebird" ... />

<TextBlock Text="Porta Firebird 2.5" ... />
<TextBox Text="{Binding PortaFirebird25}" ... />

<TextBlock Text="Porta Firebird 5.0" ... />
<TextBox Text="{Binding PortaFirebird50}" ... />

<TextBlock Text="fbclient.dll — Firebird 2.5" ... />
<Grid> <!-- TextBox + "..." Button --> </Grid>

<TextBlock Text="fbclient.dll — Firebird 5.0" ... />
<Grid> <!-- TextBox + "..." Button --> </Grid>
```

---

### 3.11 Indicador de aviso DLL na tabela de instâncias

Em `ExecutarEcoView.xaml` (ou onde as colunas da tabela são definidas), adicionar indicador visual na linha de instâncias com `AvisoDllFirebird == true`. Sugestão: ícone de aviso (⚠) com tooltip `"DLL do Firebird não encontrada ao criar/editar esta instância. Verifique o caminho nas Configurações."`.

> O `AvisoDllFirebird` já será propagado via `INotifyPropertyChanged` de `EcoInstance`.

---

## 4. Fluxo de Dados — Resumo

```
[ConfiguracoesView]
  PortaFB25 / PortaFB50 ──→ UserSettings (usersettings.json)
  DllFB25Path / DllFB50Path ─┘
       │ se porta mudou ↓
       └──→ IInstanceSetupService.AtualizarSecoesFbAsync()
                 para cada instância com IniPath existente e VersaoFirebird == versão alterada

[InstanceFlyoutView]
  RadioButton FB version (default: 2.5)
       │
       ├─ FB 2.5: porta ← Settings.PortaFirebird25 (3050)
       │          dll  ← Settings.DllFirebird25Path
       │                  fallback 1: copia de \ecosis\windows\fbclient.dll → \Firebird\2.5\
       │                  fallback 2: dll vazio (aviso, não bloqueia)
       │
       └─ FB 5.0: porta ← Settings.PortaFirebird50 (3051)
                  dll  ← Settings.DllFirebird50Path
                         fallback: verifica Firebird50DllPadrao → atualiza Settings
                         se não existe: dll vazio + AvisoDllFirebird = true

[InstanceSetupService.ImplantarAsync / AtualizarPreferenciasAsync]
  eco.ini (novo ou existente)
       ├─ [Eco]         → DBServidor=127.0.0.1 | DBPorta=<porta> | DBUser=SYSDBA
       │                  DBPassword=masterkey | FirebirdVersao=<versao>
       ├─ [windows]     → dados=127.0.0.1/<porta>:<basePath>
       └─ [preferencias]→ Firebird=<dllPath>   (e demais chaves já gerenciadas)

[EcoInstance]
  VersaoFirebird   → instancias.json (serializado)
  AvisoDllFirebird → instancias.json (serializado, limpo ao editar com DLL presente)
       └──→ tabela de instâncias: ícone ⚠ quando true
```

---

## 5. Ordem de Implementação Sugerida

1. **Modelos e constantes:** `UserSettings`, `EcoInstance`, `IniPreferencias`, `EcoPathConstants`.
2. **Serviço de setup:** `InstanceSetupService` — `AplicarSecaoEco`, extensão de `LerPreferenciasAsync`, atualização de `ImplantarAsync`, novo `AtualizarSecoesFbAsync`, extensão de `AplicarPreferencias`.
3. **Interface:** `IInstanceSetupService` — record `ImplantarOpcoes`, assinaturas atualizadas.
4. **Configurações globais:** `ConfiguracoesViewModel` + `ConfiguracoesView` — portas, DLL, propagação.
5. **Flyout de instância:** `InstanceFlyoutViewModel` + `InstanceFlyoutView` — RadioButton, fallback DLL, aviso.
6. **Tabela de instâncias:** `ExecutarEcoView` — indicador visual `AvisoDllFirebird`.
7. **Testes manuais:**
   - Nova instância FB 2.5 com DLL em `\ecosis\windows` (deve migrar automaticamente).
   - Nova instância FB 2.5 sem nenhuma DLL (deve criar sem bloquear, sem aviso).
   - Nova instância FB 5.0 com DLL no caminho padrão (normal).
   - Nova instância FB 5.0 sem DLL (deve criar com ⚠ na tabela).
   - Editar instância existente (sem trocar exe/banco): verificar que `[Eco]` e `dados=` são atualizados cirurgicamente.
   - Mudar porta no Configurações e salvar: verificar que todos os `.ini` existentes da versão afetada são atualizados.
8. **gbak/gfix para FB 5.0** — ver Seção 6.

---

## 6. Decisões Registradas (Q&A)

| # | Questão | Decisão |
|---|---|---|
| Q1 | Porta padrão do FB 5.0 | **3051** — intencional para coexistência com FB 2.5 (3050) |
| Q2 | Seção `[Eco]` ausente no eco.ini | **Inserir se ausente** (imediatamente antes de `[windows]`) |
| Q3 | Migração de instâncias existentes | **Lazy** — `.ini` atualizado apenas ao editar a instância |
| Q4 | Atualização de `.ini` na edição sem troca de exe/banco | **Cirúrgica** — atualiza somente `[Eco]`, `dados=` e `Firebird=` |
| Q5 | DLL do FB 5.0 ausente ao criar instância | **Aviso + permite criar** — sinaliza `AvisoDllFirebird = true` na instância |
| Q6 | Mudar porta nas configurações | **Propaga para todos os `.ini` existentes** da versão afetada |
| Q7 | DBServidor / DBUser / DBPassword configuráveis | **Não** — sempre escritos com valores fixos padrão |
| Q8 | gbak/gfix para FB 5.0 | **Endereçado ao final** — aguarda aquisição dos binários (ver Seção 7) |
| Q9 | Leitura de `FirebirdVersao` de `[Eco]` | **Estender `LerPreferenciasAsync`** — popula `IniPreferencias.VersaoFirebird` |
| Q10 | Formato `dados=` com porta explícita no FB 2.5 | **Confirmado aceito** — ambas as versões aceitam `host/porta:path` |

---

## 7. Escopo Futuro — gbak/gfix para Firebird 5.0

> **Status: aguardando aquisição dos binários. Implementar após etapa principal.**

Os binários `gbak.exe` e `gfix.exe` embutidos no Utils são compilados para Firebird 2.5. Operações de restauração de backup (`.fbk`) em bancos Firebird 5.0 exigem o `gbak` 5.0 — usar a versão 2.5 contra um servidor 5.0 resultará em erro de protocolo.

### Mudanças necessárias (planejamento preliminar)
- **Aquisição:** obter `gbak.exe` e `gfix.exe` da distribuição Firebird 5.0.
- **`EmbeddedToolsExtractor`:** passar a embutir dois conjuntos de binários (`tools/2.5/` e `tools/5.0/`).
- **`EcoPathConstants`:** `GbakPath` e `GfixPath` se tornam funções que recebem a versão:
  ```csharp
  public static string GbakPath(string versao) => Path.Combine(ToolsDir, versao, "gbak.exe");
  ```
- **`RestoreJobService` / serviço de restore:** receber `VersaoFirebird` da instância alvo e selecionar o binário correspondente.
- **Testes manuais:** restauração de `.fbk` 2.5 via gbak 2.5 e restauração de `.fbk` 5.0 via gbak 5.0.

---

*Fim do documento de planejamento.*
