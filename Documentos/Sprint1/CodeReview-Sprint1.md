# Code Review — Sprint 1

**Data:** 2026-04-29
**Escopo:** Toda a codebase implementada na Sprint 1 (commits 1–12+)
**Revisor:** GitHub Copilot

---

## Legenda de Prioridade

| Símbolo | Prioridade | Critério |
|---------|-----------|----------|
| 🔴 P0 | **Crítico** | Bug confirmado ou risco de perda de dado / crash em produção |
| 🟠 P1 | **Alto** | Funcionalidade incorreta, comportamento inesperado ou falha de segurança |
| 🟡 P2 | **Médio** | Qualidade de código, manutenibilidade, comportamento silencioso problemático |
| 🟢 P3 | **Baixo** | Polish de UX, otimização, housekeeping |

---

## 🔴 P0 — Crítico

---

### CR-01 — `RemoverIni` nunca é chamado ao excluir instância ✅ Resolvido

**Arquivo:** `Services/LaunchService.cs`, `ViewModels/ExecutarEcoViewModel.cs`

**Problema:**
`IIniGeneratorService.RemoverIni(string iniPath)` foi implementado com lógica defensiva completa
(validação por regex `Eco_.+\.ini$`, verificação de existência) e está declarado na interface —
mas nunca é chamado. Ao excluir uma instância em `ExcluirInstanciaAsync`, apenas o objeto
`EcoInstance` é removido da coleção e o JSON é atualizado; o arquivo `.ini` gerado em
`C:\ecosis\windows\` **permanece no disco indefinidamente**.

```csharp
// ExecutarEcoViewModel.cs — ExcluirInstanciaAsync
// AUSENTE: _iniGeneratorService.RemoverIni(instancia.IniPath);
Instancias.Remove(instancia);
await _instanceRepository.SalvarAsync(new List<EcoInstance>(Instancias));
```

**Impacto:** Vazamento de arquivos de configuração. Com uso prolongado, arquivos `.ini` órfãos
se acumulam na pasta `C:\ecosis\windows`, podendo causar confusão ao ECO ao carregar configurações
de instâncias que não existem mais.

**Correção:**
1. Injetar `IIniGeneratorService` em `ExecutarEcoViewModel` (já está disponível no DI container).
2. Chamar `_iniGeneratorService.RemoverIni(instancia.IniPath)` **antes** de remover da coleção.

> **✅ Resolvido — commit `f786983`** — `IIniGeneratorService` substituído por `IInstanceSetupService`. `ExcluirInstanciaAsync` agora chama `_instanceSetupService.Remover(instancia.ExecutavelPath, instancia.IniPath)` antes de remover da coleção, eliminando `.exe` e `.ini` implantados.

---

### CR-02 — `CarregarInstanciasAsync` fire-and-forget sem tratamento para erros inesperados ✅ Resolvido

**Arquivo:** `ViewModels/ExecutarEcoViewModel.cs`

**Problema:**
O construtor de `ExecutarEcoViewModel` dispara `CarregarInstanciasAsync()` com o padrão
`_ = Task`. `InstanceRepository.CarregarAsync()` filtra erros esperados (`IOException`,
`JsonException`, `UnauthorizedAccessException`), mas o método `CarregarInstanciasAsync`
percorre o resultado em um `foreach` sem qualquer `try/catch`. Qualquer exceção não prevista
(ex.: `OutOfMemoryException` em um JSON corrompido enorme) resultará em uma `UnobservedTaskException`
que pode encerrar o processo sem feedback ao usuário.

Padrão idêntico ocorre em `InstanceFlyoutViewModel.CarregarDadosAsync()`.

```csharp
// Construtor — sem tratamento de erro
_ = CarregarInstanciasAsync();
```

**Impacto:** Crash silencioso em produção. Lista de instâncias e ComboBoxes permanecem
vazios sem nenhuma mensagem ao usuário.

**Correção:** Envolver o corpo dos dois métodos em `try/catch (Exception ex)` e logar/exibir
o erro. No caso de `CarregarInstanciasAsync`, notificar via `_dialogService.Notificar`.

> **✅ Resolvido** — `CarregarInstanciasAsync` em `ExecutarEcoViewModel` agora captura qualquer exceção, loga via `_log.Error` e notifica o usuário com `_dialogService.Notificar`. `CarregarDadosAsync` em `InstanceFlyoutViewModel` captura e expõe via `ErroConfirmacao` (exibido inline na view).

---

## 🟠 P1 — Alto

---

### CR-03 — Caminhos hardcoded em `EcoPathConstants` ✅ Resolvido

**Arquivo:** `Infrastructure/EcoPathConstants.cs`

**Problema:**
Todos os caminhos críticos da aplicação são constantes em tempo de compilação:

```csharp
public const string WindowsDir    = @"C:\ecosis\windows";
public const string DadosDir      = @"C:\ecosis\dados";
public const string EcoIniPadrao  = @"C:\ecosis\windows\eco.ini";
public const string EcoServerHost = "127.0.0.1";
```

Se o ECO estiver instalado em outro drive ou diretório (ex.: `D:\ecosis` ou
`C:\Sistemas\ecosis`), a aplicação não funciona e não exibe nenhuma mensagem
explicativa — simplesmente mostra listas vazias.

**Impacto:** Portabilidade zero. Deployment em qualquer máquina fora do padrão exige
recompilação. O `EcoServerHost` hardcoded impede uso com servidor remoto.

**Correção sugerida:**
- Criar um arquivo `appsettings.json` (lido em `App.xaml.cs`) com as configurações de
  caminho e host.
- Ou realizar lookup no registro do Windows (onde o ECO normalmente registra seu diretório
  de instalação).
- Expor um painel de configurações mínimo para o usuário definir os caminhos.

> **✅ Resolvido** — `EcoPathConstants` convertido de `const` para `static { get; set; }` com defaults hardcoded. Caminhos derivados (`UtilsDir`, `EcoIniPadrao`, `LogPath`) viram propriedades computadas. Novo `EcoSettings.cs` (POCO) e `appsettings.json` (copiado para output com `PreserveNewest`). `App.xaml.cs` chama `CarregarConfiguracoes()` antes do DI: lê o JSON com `System.Text.Json` e aplica cada campo não-vazio sobre `EcoPathConstants`, ignorando silenciosamente arquivos ausentes ou inválidos.

---

### CR-04 — `IniGeneratorService.GerarIniAsync` sobrescreve `.ini` sem verificação ao editar ✅ Resolvido

**Arquivo:** ~~`Services/IniGeneratorService.cs`~~ → `Services/InstanceSetupService.cs`, `ViewModels/InstanceFlyoutViewModel.cs`

**Problema:**
`ConfirmarAsync` em `InstanceFlyoutViewModel` sempre chama `GerarIniAsync`, inclusive ao
**editar** uma instância sem alterar o executável ou o banco. O arquivo `.ini` existente
é sobrescrito silenciosamente. Se o `.ini` foi modificado manualmente (ex.: parâmetros
customizados adicionados ao arquivo), essas modificações são perdidas.

Adicionalmente, o nome do arquivo `.ini` gerado é `{exeNome}.ini`. Duas instâncias que
usam o mesmo executável mas bancos diferentes compartilham o mesmo arquivo `.ini` —
a segunda instância confirmada sobrescreve a configuração da primeira.

```csharp
// Dois registros com Eco_650_10 e bancos diferentes → mesmo arquivo Eco_650_10.ini
var iniDestino = Path.Combine(EcoPathConstants.WindowsDir, $"{exeNome}.ini");
```

**Impacto:** Perda silenciosa de configuração. Bug funcional ao registrar duas instâncias
com o mesmo executável.

**Correção:**
- Incluir o `Apelido` (sanitizado) no nome do arquivo: `$"{exeNome}_{apelido}.ini"`.
- Ao editar, só regenerar o `.ini` se `ExecutavelSelecionado` ou `BancoSelecionado`
  mudar em relação à instância existente.

> **✅ Resolvido — commits `de83602`–`f786983`** — `InstanceSetupService.ImplantarAsync` usa nomenclatura sequencial `eco_{v}_{b}_{seq}.exe/.ini`, garantindo par de arquivos exclusivo por instância. `ConfirmarAsync` compara `ExecutavelFontePath` e `BasePath` para decidir se reimplanta, preservando os arquivos quando não há mudança.

---

### CR-05 — `AtualizarStatusIni` valida existência do arquivo, não seu conteúdo ✅ Resolvido

**Arquivo:** `ViewModels/InstanceFlyoutViewModel.cs`, ~~`Services/IniGeneratorService.cs`~~ → `Services/InstanceSetupService.cs`

**Problema:**
O feedback "eco.ini padrão encontrado." e a habilitação do botão Confirmar (`EcoIniValido = true`)
dependem apenas de `File.Exists(EcoPathConstants.EcoIniPadrao)`. Um arquivo `eco.ini` sem a
seção `[windows]` ou sem a chave `dados=` passará na validação, mas lançará
`InvalidOperationException` dentro de `GerarIniAsync`, exibindo uma mensagem de erro
confusa ("Chave 'dados=' não encontrada na seção [windows]") somente **após** o usuário
clicar em Confirmar.

**Impacto:** UX: validação antecipada incompleta; o usuário preenche todo o formulário
e só descobre o problema ao confirmar.

**Correção:**
- Adicionar método `bool ValidarEcoIniAsync()` em `IIniGeneratorService` que verifica a
  presença da chave `dados=` na seção `[windows]`.
- Chamar em `AtualizarStatusIni` e ajustar `StatusIni` com a mensagem específica do problema.

> **✅ Resolvido** — `IInstanceSetupService` recebeu `ValidarEcoIniAsync()`, que lê o template e verifica a presença de `dados=` na seção `[windows]`. `AtualizarStatusIni` foi convertido em `AtualizarStatusIniAsync` e chama este método no setter de `ExecutavelSelecionado` (fire-and-forget), exibindo mensagem distinta para cada caso: arquivo não encontrado, conteúdo inválido, ou válido. O botão Confirmar só é habilitado quando o conteúdo é válido.

---

### CR-06 — Duas instâncias com o mesmo executável compartilham o `.ini` (duplicata de CR-04 — detalhe de colisão) ✅ Resolvido

Ver CR-04. Registrado separadamente por ser um bug funcional independente do comportamento
de sobrescrita na edição.

> **✅ Resolvido — commits `de83602`–`f786983`** — Resolvido como parte do CR-04. Cada instância recebe um par exclusivo `eco_{v}_{b}_{seq}.exe/.ini` com numeração sequencial, eliminando qualquer colisão entre instâncias que usam o mesmo executável.

---

## 🟡 P2 — Médio

---

### CR-07 — `ListBox` sem `ItemContainerStyle`: seleção nativa conflita com dark theme

**Arquivo:** `Views/ExecutarEcoView.xaml`

**Problema:**
O `ListBox` de instâncias não define `ItemContainerStyle`. O WPF aplica o estilo padrão do
sistema, que em Windows 10/11 usa fundo azul ou cinza claro no item selecionado — completamente
incompatível com o dark theme do design system. Ao clicar em qualquer linha para usar os
botões de ação, a linha fica com uma cor de destaque indesejada.

**Correção:**
Adicionar `ItemContainerStyle` ao `ListBox` desabilitando ou re-estilizando a seleção:

```xml
<ListBox.ItemContainerStyle>
    <Style TargetType="ListBoxItem">
        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
        <Setter Property="Focusable" Value="False"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ListBoxItem">
                    <Border x:Name="Bd" Background="Transparent" Padding="16,0">
                        <ContentPresenter/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bd" Property="Background"
                                    Value="{StaticResource TableRowHover}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ListBox.ItemContainerStyle>
```

---

### CR-08 — `LogService` usa `DateTime.Now` (hora local) nos logs

**Arquivo:** `Services/LogService.cs`

**Problema:**
```csharp
var linha = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ...";
```
Em ambientes com múltiplos fusos horários ou ao correlacionar logs com eventos do servidor,
timestamps locais causam ambiguidade. A convenção de ferramentas de log (Serilog, NLog, etc.)
é sempre usar UTC.

**Correção:** Substituir `DateTime.Now` por `DateTime.UtcNow` e adicionar sufixo `Z` ao formato:
```csharp
$"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z] ..."
```

---

### CR-09 — `InstanceFlyoutViewModel.CarregarDadosAsync` sem indicador de carregamento

**Arquivo:** `ViewModels/InstanceFlyoutViewModel.cs`, `Views/InstanceFlyoutView.xaml`

**Problema:**
Os ComboBoxes de Executável e Banco de Dados aparecem vazios enquanto `CarregarDadosAsync`
está em execução. Em máquinas lentas ou com discos travados, o usuário pode clicar em
Confirmar com os campos "vazios" e receber um erro, sem entender que os dados ainda estavam
carregando. O botão Confirmar fica desabilitado (`PodeConfirmar`), mas não há feedback visual
explicando o motivo.

**Correção:**
- Adicionar propriedade `bool IsCarregando` ao ViewModel.
- Desabilitar os ComboBoxes e exibir um `TextBlock` "Carregando..." enquanto `IsCarregando == true`.
- Setar `IsCarregando = true` antes do `await` e `false` no `finally`.

---

### CR-10 — Larguras de coluna duplicadas entre header e rows da tabela

**Arquivo:** `Views/ExecutarEcoView.xaml`

**Problema:**
As larguras `180 / 160 / 160 / *` estão definidas duas vezes — uma no `Border` do cabeçalho
e outra no `DataTemplate` das linhas. Se uma largura for alterada em um lugar e esquecida no
outro, o alinhamento visual quebrará silenciosamente.

```xml
<!-- Duplicação: Border cabeçalho e DataTemplate linhas usam os mesmos valores hardcoded -->
<ColumnDefinition Width="180"/>
<ColumnDefinition Width="160"/>
<ColumnDefinition Width="160"/>
<ColumnDefinition Width="*"/>
```

**Correção:** Usar um `Grid` com `SharedSizeGroup` (via `Grid.IsSharedSizeScope`) para que
header e linhas compartilhem as mesmas definições de coluna automaticamente.

---

### CR-11 — `VersionCatalogService.IniPadraoPresente` calculado uma única vez e não atualizado ✅ Resolvido

**Arquivo:** `Services/VersionCatalogService.cs`

**Problema:**
A presença do `eco.ini` é verificada no momento em que `ListarExecutaveisAsync` é chamado
e armazenada em cada `EcoExecutavel.IniPadraoPresente`. Se o `eco.ini` for criado ou
deletado enquanto o flyout está aberto, o status exibido não se atualiza — o usuário pode
ver "eco.ini não encontrado" mesmo depois de ter criado o arquivo, ou ver "eco.ini encontrado"
após deletá-lo.

**Correção:**
- `IniPadraoPresente` pode ser removido do modelo `EcoExecutavel`, pois a verificação já é
  feita diretamente em `AtualizarStatusIni` com `File.Exists(EcoPathConstants.EcoIniPadrao)`.
- Adicionar um botão "Recarregar" ou `FileSystemWatcher` para monitorar a pasta.

> **✅ Resolvido — commit `de83602`** — `IniPadraoPresente` removido de `EcoExecutavel` e de `VersionCatalogService` (ver CR-18). `AtualizarStatusIni` já verificava `File.Exists` diretamente no momento da seleção do executável, garantindo status sempre atualizado.

---

### CR-12 — `InstanceRepository.SalvarAsync` sem tratamento de exceção

**Arquivo:** `Services/InstanceRepository.cs`

**Problema:**
`SalvarAsync` não tem `try/catch`. Em caso de falha de disco ou permissão ao escrever o
arquivo `.tmp`, a exceção propaga para o chamador (`ConfirmarAsync` no flyout, onde é
capturada e exibida via `ErroConfirmacao`). Para os comandos de excluir, a exceção é
capturada pelo `AsyncRelayCommand.onError` e apenas logada — **sem feedback ao usuário**.

```csharp
// ExcluirCommand — erro de I/O é apenas logado, sem notificação ao usuário
ExcluirCommand = new AsyncRelayCommand(
    async inst => await ExcluirInstanciaAsync((EcoInstance)inst!),
    onError: ex => _log.Error(nameof(ExcluirInstanciaAsync), ex));  // sem _dialogService.Notificar
```

Se o salvamento após exclusão falhar, a instância foi removida da UI mas permanece no JSON
em disco — **dessincronização silenciosa de estado**.

**Correção:** No `ExcluirCommand.onError`, além de logar, chamar
`_dialogService.Notificar("Erro ao salvar", ...)` para informar o usuário.

> **⚠️ Agravante introduzido pelo fix de CR-01** — Com `Remover` chamado antes de `SalvarAsync`, uma falha de I/O no `SalvarAsync` resulta em inconsistência mais grave: os arquivos `.exe`/`.ini` são deletados com sucesso, mas a instância permanece no JSON e reaparece na lista no próximo carregamento com referências a arquivos inexistentes.

---

## 🟢 P3 — Baixo

---

### CR-13 — `ListBox` sem virtualização de painel

**Arquivo:** `Views/ExecutarEcoView.xaml`

**Problema:**
O `ListBox` renderiza todos os `ListBoxItem` simultaneamente. Para uso com muitas instâncias
(>50), o scroll ficará lento.

**Correção:** Garantir que `VirtualizingPanel.IsVirtualizing="True"` e
`VirtualizingPanel.VirtualizationMode="Recycling"` estejam definidos no `ListBox`.
(Por padrão o `ListBox` virtualiza, mas o `DataTemplate` com `Grid.ColumnDefinitions`
pode desabilitar a virtualização implicitamente se não houver `ItemContainerStyle` com
`HorizontalContentAlignment="Stretch"`.)

---

### CR-14 — Colunas da tabela com largura fixa truncam nomes longos

**Arquivo:** `Views/ExecutarEcoView.xaml`

**Problema:**
A coluna "Apelido" tem `Width="180"` e a de "Executável" tem `Width="160"`. Nomes longos
(ex.: `Eco_2024_Homologacao_Completo`) são truncados sem `TextTrimming` e sem `ToolTip`.

**Correção:**
- Adicionar `TextTrimming="CharacterEllipsis"` nos `TextBlock` das colunas de texto.
- Adicionar `ToolTip="{Binding Apelido}"` (e equivalentes) para exibir o texto completo
  no hover.

---

### CR-15 — Flyout não protege contra abertura simultânea em clique rápido

**Arquivo:** `ViewModels/ExecutarEcoViewModel.cs`

**Problema:**
`AbrirFlyoutNovo` e `AbrirFlyoutEditar` não verificam se `FlyoutAberto == true` antes de
criar um novo `FlyoutVM`. Um clique rápido duplo no botão "+" cria dois ViewModels de flyout,
com o segundo sobrescrevendo o primeiro. Qualquer dado preenchido no primeiro formulário é
perdido silenciosamente.

**Correção:**
```csharp
private void AbrirFlyoutNovo()
{
    if (FlyoutAberto) return;
    // ...
}
```

---

### CR-16 — `DialogService` acessa `Application.Current.MainWindow` sem verificação de nulo

**Arquivo:** `Services/DialogService.cs`

**Problema:**
```csharp
Owner = Application.Current.MainWindow
```
Em shutdown ou em testes de integração, `Application.Current.MainWindow` pode ser `null`,
causando `NullReferenceException` na criação do `ConfirmDialog`.

**Correção:**
```csharp
Owner = Application.Current?.MainWindow
```

---

### CR-17 — `InstanceFlyoutViewModel` reconstrói lista de apelidos existentes no momento da abertura

**Arquivo:** `ViewModels/ExecutarEcoViewModel.cs`

**Problema:**
`apelidosExistentes` é capturado como snapshot no momento em que o flyout é aberto. Se duas
janelas (ou a mesma em uso rápido) adicionarem instâncias concorrentemente, a validação de
duplicata pode ter falso negativo. Baixo risco em uso típico (app single-window), mas vale
registrar.

**Correção:** Passar uma referência `Func<IEnumerable<string>>` ao invés de uma cópia da lista,
avaliada no momento da validação em tempo real.

---

### CR-18 — `EcoExecutavel.IniPadraoPresente` é propriedade do modelo mas nunca usada no ViewModel ou na View ✅ Resolvido

**Arquivo:** `Models/EcoExecutavel.cs`, `ViewModels/InstanceFlyoutViewModel.cs`

**Problema:**
`IniPadraoPresente` é populada em `VersionCatalogService` mas `InstanceFlyoutViewModel`
faz sua própria verificação via `File.Exists` em `AtualizarStatusIni`. A propriedade
no modelo é portanto redundante e pode causar confusão sobre qual fonte de verdade usar.

**Correção:** Remover `IniPadraoPresente` de `EcoExecutavel` e manter apenas a verificação
em `AtualizarStatusIni`, que é a que efetivamente controla o estado do formulário.

> **✅ Resolvido — commit `de83602`** — Propriedade `IniPadraoPresente` removida de `EcoExecutavel` e da atribuição em `VersionCatalogService.ListarExecutaveisAsync`.

---

## Resumo por Prioridade

| ID | Prioridade | Status | Descrição Resumida | Arquivo Principal |
|----|-----------|--------|-------------------|-------------------|
| CR-01 | 🔴 P0 | ✅ Resolvido | `RemoverIni` nunca chamado ao excluir | `ExecutarEcoViewModel.cs` |
| CR-02 | 🔴 P0 | ✅ Resolvido | Fire-and-forget sem catch para erros inesperados | `ExecutarEcoViewModel.cs` |
| CR-03 | 🟠 P1 | ✅ Resolvido | Caminhos hardcoded — zero portabilidade | `EcoPathConstants.cs` |
| CR-04 | 🟠 P1 | ✅ Resolvido | `.ini` sobrescrito ao editar; colisão por nome de exe | `InstanceSetupService.cs` |
| CR-05 | 🟠 P1 | ✅ Resolvido | Validação do `eco.ini` incompleta (só existência) | `InstanceFlyoutViewModel.cs` |
| CR-06 | 🟠 P1 | ✅ Resolvido | Duas instâncias com mesmo exe compartilham `.ini` | `InstanceSetupService.cs` |
| CR-07 | 🟡 P2 | 🔓 Aberto | Seleção nativa do `ListBox` conflita com dark theme | `ExecutarEcoView.xaml` |
| CR-08 | 🟡 P2 | 🔓 Aberto | Log usa `DateTime.Now` em vez de UTC | `LogService.cs` |
| CR-09 | 🟡 P2 | 🔓 Aberto | ComboBoxes sem loading state no flyout | `InstanceFlyoutViewModel.cs` |
| CR-10 | 🟡 P2 | 🔓 Aberto | Larguras de coluna duplicadas — risco de desalinhamento | `ExecutarEcoView.xaml` |
| CR-11 | 🟡 P2 | ✅ Resolvido | `IniPadraoPresente` calculado uma vez, não atualizado | `VersionCatalogService.cs` |
| CR-12 | 🟡 P2 | 🔓 Aberto | Erro de I/O na exclusão não notifica o usuário | `ExecutarEcoViewModel.cs` |
| CR-13 | 🟢 P3 | 🔓 Aberto | `ListBox` sem virtualização explícita | `ExecutarEcoView.xaml` |
| CR-14 | 🟢 P3 | 🔓 Aberto | Colunas com largura fixa truncam nomes longos | `ExecutarEcoView.xaml` |
| CR-15 | 🟢 P3 | 🔓 Aberto | Flyout abre duplicado em clique duplo rápido | `ExecutarEcoViewModel.cs` |
| CR-16 | 🟢 P3 | 🔓 Aberto | `MainWindow` acessado sem null-check no `DialogService` | `DialogService.cs` |
| CR-17 | 🟢 P3 | 🔓 Aberto | Lista de apelidos capturada como snapshot | `ExecutarEcoViewModel.cs` |
| CR-18 | 🟢 P3 | ✅ Resolvido | `EcoExecutavel.IniPadraoPresente` redundante | `EcoExecutavel.cs` |

---

## Pontos Positivos Identificados

Esta seção registra o que está bem implementado e deve ser mantido como padrão.

- **Arquitetura MVVM bem aplicada:** ViewModels sem referência a tipos do WPF, separação
  clara entre View e lógica de negócio.
- **DI via `Microsoft.Extensions.DependencyInjection`:** Composição centralizada em
  `App.xaml.cs`, sem instanciação direta em ViewModels. Testabilidade preservada.
- **`AsyncRelayCommand` com `onError` callback:** Padrão sólido para comandos assíncronos —
  evita `async void` sem tratamento.
- **`InstanceRepository` com arquivo temporário (`.tmp`):** Padrão de escrita atômica correto
  — evita corrupção do JSON em caso de crash durante a gravação.
- **`IniGeneratorService.RemoverIni` com regex de segurança:** A validação `Eco_.+\.ini$`
  previne deleção acidental de arquivos não gerados pela aplicação. Boa prática.
- **Design System com ResourceDictionaries:** Separação correta em `Colors.xaml`,
  `Controls.xaml` e `Typography.xaml`. `ControlTemplate` customizado para `Button` e
  `ComboBox` já implementados.
- **`ConfirmDialog` customizado:** Substituição do `MessageBox` nativo concluída com
  componente integrado ao design system (M-1 do Backlog entregue).
- **`LogService` thread-safe com `lock`:** Proteção contra escrita concorrente no arquivo
  de log.
- **Validação de apelido duplicado em tempo real** (`ApelidoDuplicado`) com feedback inline
  no formulário antes do submit.
