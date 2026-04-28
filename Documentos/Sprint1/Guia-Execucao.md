# Guia de Execução — Sprint 1

Referência: [Planejamento-UI.md](./Planejamento-UI.md)
Registro de progresso: [Registro-Commits.md](./Registro-Commits.md)
Melhorias identificadas: [Backlog-Melhorias.md](./Backlog-Melhorias.md)

---

## Princípios que guiam toda a Sprint

### Código
- Nenhuma lógica em code-behind de View (.xaml.cs). Apenas inicialização de DataContext se necessário.
- Nenhum estilo inline no XAML. Toda estilização referencia uma chave do ResourceDictionary.
- Nenhuma string mágica no código C#. Caminhos de pasta definidos como constantes em uma classe dedicada.
- Services nunca instanciam outros services internamente. Dependências são recebidas via construtor.
- Toda operação de I/O (disco, processo) deve ser async/await.
- ObservableCollection para qualquer lista exibida na UI.
- ICommand sempre via RelayCommand. Nunca evento de botão no code-behind.

### Escalabilidade
- Interfaces definem o contrato de cada service antes da implementação.
  Isso permite trocar implementações no futuro sem alterar ViewModels.
- Models são POCOs puros: sem lógica de UI, sem referência a WPF.
- ViewModels conhecem apenas services via interface, nunca implementações concretas.
- ResourceDictionaries separados por responsabilidade (cores, tipografia, controles).

### Isolamento entre sprints
- Tudo que esta sprint não implementa completamente deve expor apenas a interface,
  nunca a implementação parcial. Uma sprint não quebra outra.
- Se um ponto de melhoria for identificado durante a execução, registrar em
  [Backlog-Melhorias.md](./Backlog-Melhorias.md) e seguir sem desviar do escopo do commit atual.

---

## Checklist pré-commit (aplicar antes de cada commit)

- [ ] O código compila sem erros (`dotnet build`).
- [ ] Nenhum estilo inline foi introduzido.
- [ ] Nenhuma lógica de negócio está em code-behind.
- [ ] Nenhuma string mágica de caminho no código (usar `EcoPathConstants`).
- [ ] O commit não introduz dependência de código de sprint futura.
- [ ] Registrar o commit em [Registro-Commits.md](./Registro-Commits.md) após o push.

---

## Commit 1 — `manifest: configurar requireAdministrator`

### Objetivo
Garantir que o EcoUtils sempre execute com permissão de administrador,
necessário para acessar e escrever em `C:/ecosis/windows`.

### Arquivos a criar/modificar
| Ação    | Arquivo                        |
|---------|-------------------------------|
| Criar   | `app.manifest`                 |
| Modificar | `EcoUtils.csproj`            |

### Instruções

**1. Criar `app.manifest` na raiz do projeto com o conteúdo:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="EcoUtils.app"/>
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

**2. Referenciar o manifest no `EcoUtils.csproj`:**
```xml
<ApplicationManifest>app.manifest</ApplicationManifest>
```

### Validação
- Executar `dotnet build`.
- Ao rodar o `.exe` gerado em `bin/`, o Windows deve exibir o prompt UAC.

### Ponto de atenção
Durante desenvolvimento no VS Code o prompt UAC pode não aparecer se o próprio
VS Code já estiver rodando como admin. Validar com o executável final.

---

## Commit 2 — `design: ResourceDictionaries base (Design System)`

### Objetivo
Criar o design system próprio do EcoUtils inspirado no VSCode antes de qualquer
View ser construída. Toda estilização da Sprint 1 em diante deve referenciar esses tokens.

### Arquivos a criar/modificar
| Ação      | Arquivo                                      |
|-----------|----------------------------------------------|
| Criar dir | `Themes/`                                    |
| Criar     | `Themes/Colors.xaml`                         |
| Criar     | `Themes/Typography.xaml`                     |
| Criar     | `Themes/Controls.xaml`                       |
| Modificar | `App.xaml`                                   |

### Paleta de cores (VSCode Dark)
| Token                        | Hex       | Uso                            |
|------------------------------|-----------|--------------------------------|
| `AppBackground`              | #1e1e1e   | Fundo principal (workspace)    |
| `SidebarBackground`          | #252526   | Fundo da sidebar               |
| `SidebarItemHover`           | #2a2d2e   | Hover no item de navegação     |
| `SidebarItemActive`          | #37373d   | Item ativo na sidebar          |
| `SidebarActiveIndicator`     | #0078d4   | Barra lateral azul do item ativo|
| `PanelBackground`            | #252526   | Fundo de painéis/flyout        |
| `PanelBorder`                | #3c3c3c   | Bordas de painéis              |
| `InputBackground`            | #3c3c3c   | Fundo de TextBox e ComboBox    |
| `InputBorder`                | #555555   | Borda de inputs                |
| `InputBorderFocus`           | #0078d4   | Borda de input com foco        |
| `ButtonPrimaryBackground`    | #0078d4   | Botão primário (Confirmar)     |
| `ButtonPrimaryHover`         | #1a8fd1   | Hover botão primário           |
| `ButtonSecondaryBackground`  | #3c3c3c   | Botão secundário (Cancelar)    |
| `ButtonSecondaryHover`       | #4a4a4a   | Hover botão secundário         |
| `ButtonDangerBackground`     | #c72e2e   | Botão de exclusão              |
| `ButtonDangerHover`          | #d94040   | Hover botão danger             |
| `TextPrimary`                | #d4d4d4   | Texto principal                |
| `TextSecondary`              | #8a8a8a   | Texto secundário/label         |
| `TextDisabled`               | #555555   | Texto desabilitado             |
| `TextLink`                   | #4ec9b0   | Texto de link/destaque         |
| `StatusSuccess`              | #4ec9b0   | Indicador de sucesso           |
| `StatusWarning`              | #ce9178   | Indicador de aviso             |
| `StatusError`                | #f44747   | Indicador de erro              |
| `OverlayBackground`          | #00000099 | Overlay semitransparente       |
| `TableHeaderBackground`      | #2d2d2d   | Cabeçalho da tabela            |
| `TableRowHover`              | #2a2d2e   | Hover da linha da tabela       |
| `TableRowAlternate`          | #222222   | Linha alternada da tabela      |

### Tipografia
- Fonte base: `Segoe UI`, tamanho 13.
- Fonte de detalhes técnicos (caminhos, versões): `Consolas`, tamanho 12.

### Controles base a estilizar neste commit
- `Window` (fundo e cor de texto globais)
- `Button` (primário, secundário, danger, ícone)
- `TextBox`
- `ComboBox`
- `ScrollViewer`
- `ListBox` / `ListBoxItem`

### Instrução de merge no App.xaml
```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Themes/Colors.xaml"/>
      <ResourceDictionary Source="Themes/Typography.xaml"/>
      <ResourceDictionary Source="Themes/Controls.xaml"/>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### Validação
- `dotnet build` sem erros.
- Executar o app: a janela deve aparecer com o fundo escuro (#1e1e1e).

---

## Commit 3 — `models: EcoInstance, EcoExecutavel, EcoDatabase`

### Objetivo
Definir os modelos de dados puros da sprint. POCOs sem referência a WPF.

### Arquivos a criar
| Arquivo                        | Conteúdo                                          |
|-------------------------------|---------------------------------------------------|
| `Models/EcoInstance.cs`       | Instância cadastrada pelo usuário                 |
| `Models/EcoExecutavel.cs`     | Executável válido encontrado em /windows          |
| `Models/EcoDatabase.cs`       | Banco .eco encontrado em /dados                   |
| `Infrastructure/EcoPathConstants.cs` | Caminhos de pasta como constantes         |

### Propriedades de cada model

**EcoInstance**
```
Guid   Id
string Apelido
string ExecutavelPath
string ExecutavelNome
string BasePath
string BaseNome
string IniPath
```

**EcoExecutavel**
```
string NomeCompleto     // "Eco_650_10"
string ExePath          // "C:/ecosis/windows/Eco_650_10.exe"
bool   IniPadraoPresente // eco.ini padrão existe na pasta?
```

**EcoDatabase**
```
string NomeCompleto   // "PRD"
string EcoPath        // "C:/ecosis/dados/PRD.eco"
```

**EcoPathConstants**
```csharp
public static class EcoPathConstants
{
    public const string WindowsDir = @"C:\ecosis\windows";
    public const string DadosDir   = @"C:\ecosis\dados";
    public const string EcoIniPadrao = @"C:\ecosis\windows\eco.ini";
    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoUtils");
}
```

### Instrução de qualidade
- Nenhum model deve importar `System.Windows` ou qualquer namespace WPF.
- Nenhum model deve ter lógica de validação ou IO. Isso pertence aos services.

### Validação
- `dotnet build` sem erros.

---

## Commit 4 — `services: interfaces e VersionCatalogService + DatabaseDiscoveryService`

### Objetivo
Definir os contratos (interfaces) de todos os services da sprint e implementar
os dois services de descoberta. Interfaces primeiro, implementações depois.

### Arquivos a criar
| Arquivo                                          |
|--------------------------------------------------|
| `Services/Interfaces/IVersionCatalogService.cs`  |
| `Services/Interfaces/IDatabaseDiscoveryService.cs`|
| `Services/Interfaces/IIniGeneratorService.cs`    |
| `Services/Interfaces/IInstanceRepository.cs`    |
| `Services/Interfaces/ILaunchService.cs`         |
| `Services/VersionCatalogService.cs`             |
| `Services/DatabaseDiscoveryService.cs`          |

### Contratos das interfaces

**IVersionCatalogService**
```csharp
Task<IReadOnlyList<EcoExecutavel>> ListarExecutaveisAsync();
```

**IDatabaseDiscoveryService**
```csharp
Task<IReadOnlyList<EcoDatabase>> ListarBancosAsync();
```

**IIniGeneratorService**
```csharp
Task<string> GerarIniAsync(string exeNome, string basePath);
void RemoverIni(string iniPath);
```

**IInstanceRepository**
```csharp
Task<IReadOnlyList<EcoInstance>> CarregarAsync();
Task SalvarAsync(IReadOnlyList<EcoInstance> instancias);
```

**ILaunchService**
```csharp
Task<(bool Sucesso, string? Erro)> ExecutarAsync(EcoInstance instancia);
```

### Lógica do VersionCatalogService
- Verificar se `EcoPathConstants.WindowsDir` existe. Se não, retornar lista vazia.
- Listar arquivos `.exe` no diretório.
- Filtrar pelo regex: `^Eco_\d+_\d+\.exe$` (case-insensitive).
- Para cada match, verificar se `eco.ini` padrão existe no mesmo diretório.
- Retornar lista de `EcoExecutavel`.

### Lógica do DatabaseDiscoveryService
- Verificar se `EcoPathConstants.DadosDir` existe. Se não, retornar lista vazia.
- Listar arquivos `.eco` no diretório.
- Retornar lista de `EcoDatabase`.

### Instrução de qualidade
- Todo acesso a disco deve usar `async/await` com `Task`.
- Nunca lançar exceção genérica. Capturar `IOException` e `UnauthorizedAccessException`
  e retornar lista vazia com log (quando o sistema de log existir).

### Validação
- `dotnet build` sem erros.

---

## Commit 5 — `services: InstanceRepository`

### Objetivo
Persistir e recuperar a lista de instâncias em JSON no AppData local.

### Arquivos a criar
| Arquivo                           |
|-----------------------------------|
| `Services/InstanceRepository.cs`  |

### Lógica
- Caminho do arquivo: `Path.Combine(EcoPathConstants.AppDataDir, "instancias.json")`.
- Criar o diretório no AppData se não existir (não exige permissão de admin).
- Serializar/desserializar com `System.Text.Json`.
- `CarregarAsync`: retornar lista vazia se o arquivo não existir ainda.
- `SalvarAsync`: escrever a lista serializada sobrescrevendo o arquivo.

### Instrução de qualidade
- Usar `JsonSerializerOptions` com `WriteIndented = true` para facilitar depuração manual.
- Nunca salvar em `C:/ecosis` — apenas em AppData.

### Validação
- `dotnet build` sem erros.

---

## Commit 6 — `services: IniGeneratorService`

### Objetivo
Ler o `eco.ini` padrão, substituir a chave `dados=` e gravar o `.ini` da instância.
Este service é o coração do mecanismo de seleção de banco do ECO.

### Arquivos a criar
| Arquivo                           |
|-----------------------------------|
| `Services/IniGeneratorService.cs` |

### Lógica de GerarIniAsync
1. Ler todas as linhas de `EcoPathConstants.EcoIniPadrao`.
2. Localizar linha que começa com `dados=` dentro da seção `[windows]`.
3. Substituir o valor: `dados=127.0.0.1:{basePath}`.
   - `basePath` é o caminho completo do `.eco` (ex: `C:\ecosis\dados\PRD.eco`).
4. Gravar as linhas resultantes em `C:\ecosis\windows\{exeNome}.ini`.
5. Retornar o caminho do `.ini` gerado.

### Lógica de RemoverIni
- Verificar se o arquivo existe antes de remover.
- Remover apenas se o arquivo seguir o padrão `Eco_*.ini` (proteção para nunca remover `eco.ini`).

### Instrução de qualidade
- Nunca modificar `eco.ini`. Qualquer escrita deve ser em arquivo de nome diferente.
- Usar `File.ReadAllLinesAsync` e `File.WriteAllLinesAsync`.
- A substituição da linha `dados=` deve ser feita por busca de chave, não por índice de linha.

### Validação
- `dotnet build` sem erros.

---

## Commit 7 — `services: LaunchService`

### Objetivo
Validar os três arquivos necessários e iniciar o processo do ECO.

### Arquivos a criar
| Arquivo                    |
|----------------------------|
| `Services/LaunchService.cs`|

### Lógica de ExecutarAsync
1. Verificar se `instancia.ExecutavelPath` existe em disco.
2. Verificar se `instancia.BasePath` existe em disco.
3. Verificar se `instancia.IniPath` existe em disco.
4. Se qualquer um estiver ausente, retornar `(false, "mensagem descritiva")`.
5. Se todos presentes: `Process.Start(instancia.ExecutavelPath)`.
6. Retornar `(true, null)`.

### Instrução de qualidade
- Usar `Process.Start` com `ProcessStartInfo` explícito (sem usar o overload simplificado).
- Não aguardar o processo terminar (ECO é uma aplicação desktop independente).

### Validação
- `dotnet build` sem erros.

---

## Commit 8 — `shell: MainWindow + MainViewModel (sidebar + navegação)`

### Objetivo
Construir o shell da aplicação: sidebar fixa à esquerda com item "Executar ECO"
e ContentControl no workspace que troca de View via DataTemplate por tipo de ViewModel.

### Arquivos a criar/modificar
| Ação      | Arquivo                             |
|-----------|-------------------------------------|
| Modificar | `MainWindow.xaml`                   |
| Modificar | `MainWindow.xaml.cs`                |
| Modificar | `ViewModels/MainViewModel.cs`       |
| Criar     | `ViewModels/NavItem.cs`             |

### Estrutura do layout MainWindow
```
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="220"/>       <!-- Sidebar -->
    <ColumnDefinition Width="*"/>         <!-- Workspace -->
  </Grid.ColumnDefinitions>

  <!-- Sidebar -->
  <ListBox Grid.Column="0"
           ItemsSource="{Binding Abas}"
           SelectedItem="{Binding AbaAtiva}"/>

  <!-- Workspace -->
  <ContentControl Grid.Column="1"
                  Content="{Binding AbaAtiva.ViewModel}"/>
</Grid>
```

### NavItem
```csharp
public class NavItem
{
    public string Rotulo   { get; init; }
    public string Icone    { get; init; }  // caractere Unicode ou path de recurso futuro
    public ViewModelBase ViewModel { get; init; }
}
```

### MainViewModel
```csharp
public ObservableCollection<NavItem> Abas { get; }
public NavItem? AbaAtiva { get; set; }
// construtor inicializa Abas com ExecutarEcoViewModel e define AbaAtiva = Abas[0]
```

### DataTemplate no App.xaml
Mapear `ExecutarEcoViewModel` → `ExecutarEcoView`:
```xml
<DataTemplate DataType="{x:Type vm:ExecutarEcoViewModel}">
  <views:ExecutarEcoView/>
</DataTemplate>
```

### Instrução de qualidade
- O `ContentControl` não sabe qual View vai exibir — apenas exibe o DataContext.
  Toda a lógica de mapeamento ViewModel→View fica no ResourceDictionary, não no code-behind.
- `MinWidth="800"` e `MinHeight="520"` na janela.
- Tamanho inicial: `Width="1024"` `Height="680"`.

### Validação
- `dotnet build` sem erros.
- Executar o app: sidebar visível com item "Executar ECO" e workspace vazio (View ainda não criada).

---

## Commit 9 — `feat: ExecutarEcoView + ExecutarEcoViewModel`

### Objetivo
Tela principal da sprint: cabeçalho com botão `+`, tabela de instâncias e
estado de lista vazia com mensagem orientativa.

### Arquivos a criar
| Arquivo                                   |
|-------------------------------------------|
| `Views/ExecutarEcoView.xaml`              |
| `Views/ExecutarEcoView.xaml.cs`           |
| `ViewModels/ExecutarEcoViewModel.cs`      |

### ExecutarEcoViewModel
```csharp
public ObservableCollection<EcoInstance> Instancias { get; }
public bool ListaVazia => !Instancias.Any();
public ICommand AdicionarCommand { get; }
public ICommand EditarCommand    { get; }    // recebe EcoInstance
public ICommand ExcluirCommand   { get; }    // recebe EcoInstance
public ICommand ExecutarCommand  { get; }    // recebe EcoInstance
// flyout state:
public bool FlyoutAberto { get; set; }
public InstanceFlyoutViewModel? FlyoutVM { get; set; }
```

### Instrução de qualidade
- `ListaVazia` deve ser recalculado automaticamente via `OnPropertyChanged`
  sempre que `Instancias` mudar.
- Carregar instâncias no construtor via `IInstanceRepository` (async via `Task.Run` ou
  sobrescrever padrão com método `InicializarAsync` chamado pelo ViewModel).
- `AdicionarCommand` apenas define `FlyoutVM` e `FlyoutAberto = true`.
  A lógica de criação fica no `InstanceFlyoutViewModel`.

### Validação
- `dotnet build` sem erros.
- Executar o app: tela com cabeçalho "Executar ECO", botão `+` e mensagem de lista vazia.

---

## Commit 10 — `feat: InstanceFlyoutView + InstanceFlyoutViewModel`

### Objetivo
Flyout centralizado com overlay para adicionar e editar instâncias.
Inclui toda a lógica de validação e geração do `.ini` ao confirmar.

### Arquivos a criar
| Arquivo                                       |
|-----------------------------------------------|
| `Views/InstanceFlyoutView.xaml`               |
| `Views/InstanceFlyoutView.xaml.cs`            |
| `ViewModels/InstanceFlyoutViewModel.cs`       |

### InstanceFlyoutViewModel
```csharp
public string Titulo { get; }      // "Nova Instância ECO" ou "Editar Instância"
public string TextoBotaoConfirmar { get; }  // "Confirmar" ou "Salvar"
public string Apelido { get; set; }
public ObservableCollection<EcoExecutavel> Executaveis { get; }
public EcoExecutavel? ExecutavelSelecionado { get; set; }
public ObservableCollection<EcoDatabase>  Bancos { get; }
public EcoDatabase? BancoSelecionado { get; set; }
public string StatusIni { get; }        // mensagem de status do eco.ini padrão
public bool EcoIniValido { get; }       // controla se Confirmar está habilitado
public bool PodeConfirmar { get; }      // todos os campos + EcoIniValido
public ICommand ConfirmarCommand { get; }
public ICommand CancelarCommand  { get; }
```

### Lógica ao selecionar executável
- Verificar se `eco.ini` padrão existe em `EcoPathConstants.WindowsDir`.
- Atualizar `StatusIni` e `EcoIniValido`.
- Notificar `PodeConfirmar`.

### Lógica do ConfirmarCommand
1. Validar `PodeConfirmar`.
2. Chamar `IIniGeneratorService.GerarIniAsync`.
3. Construir `EcoInstance` com todos os dados.
4. Invocar callback/evento `OnConfirmado(EcoInstance)` para o `ExecutarEcoViewModel`.
5. Fechar flyout.

### Flyout como overlay no XAML da ExecutarEcoView
```xml
<!-- Overlay escuro -->
<Grid Visibility="{Binding FlyoutAberto, Converter={...BoolToVisibility}}">
  <Rectangle Fill="{StaticResource OverlayBackground}"/>
  <!-- Painel centralizado -->
  <Border HorizontalAlignment="Center" VerticalAlignment="Center" Width="480">
    <views:InstanceFlyoutView DataContext="{Binding FlyoutVM}"/>
  </Border>
</Grid>
```

### Instrução de qualidade
- O flyout é um `UserControl`, não uma `Window`. Fica no mesmo contexto visual.
- `PodeConfirmar` é calculado como propriedade derivada, nunca campo manual.
- O `ConfirmarCommand` tem `CanExecute` ligado a `PodeConfirmar` para desabilitar o botão automaticamente.

### Validação
- `dotnet build` sem erros.
- Executar o app: clicar em `+` abre o flyout com overlay. Campos aparecem corretamente.
- Testar validação: botão Confirmar desabilitado com campos vazios.

---

## Commit 11 — `feat: ações das instâncias (executar, editar, excluir)`

### Objetivo
Completar o ciclo CRUD das instâncias: wiring dos três botões de ação com
feedback de execução, confirmação de exclusão e remoção do `.ini` do disco.

### Arquivos a modificar
| Arquivo                              |
|--------------------------------------|
| `ViewModels/ExecutarEcoViewModel.cs` |
| `Views/ExecutarEcoView.xaml`         |

### Fluxo ExcluirCommand
1. Exibir diálogo de confirmação: `"Deseja excluir a instância '{Apelido}'?"`.
2. Se confirmado:
   - Chamar `IIniGeneratorService.RemoverIni(instancia.IniPath)`.
   - Remover da `ObservableCollection`.
   - Persistir via `IInstanceRepository.SalvarAsync`.

### Fluxo EditarCommand
1. Criar `InstanceFlyoutViewModel` com os dados da instância existente (modo edição).
2. Abrir flyout.
3. Ao confirmar:
   - Se o executável mudou: remover `.ini` antigo, gerar novo.
   - Atualizar a instância na `ObservableCollection` (mesmo `Id`).
   - Persistir.

### Fluxo ExecutarCommand
1. Chamar `ILaunchService.ExecutarAsync(instancia)`.
2. Se `Sucesso = false`: exibir mensagem de erro com o campo `Erro` retornado.
3. Se `Sucesso = true`: exibir notificação breve na linha ("ECO iniciado").

### Instrução de qualidade
- O diálogo de confirmação de exclusão deve ser implementado via `MessageBox.Show`
  por ora. Quando o design system de diálogos for definido em sprint futura,
  registrar melhoria no backlog.
- Toda persistência após ação do usuário deve ser `await` para não bloquear a UI.

### Validação final da Sprint 1 (checklist completo)
- [ ] Layout sidebar + workspace funcional com navegação por aba.
- [ ] Aba "Executar ECO" carregada como tela inicial.
- [ ] Lista de instâncias exibida (ou mensagem de lista vazia).
- [ ] Botão "+" abre flyout centralizado com overlay.
- [ ] Flyout lista executáveis válidos de C:/ecosis/windows no padrão Eco_{versao}_{build}.exe.
- [ ] Flyout verifica eco.ini padrão e bloqueia confirmação se ausente.
- [ ] Flyout lista bancos .eco de C:/ecosis/dados.
- [ ] Flyout valida campos obrigatórios antes de confirmar.
- [ ] IniGeneratorService gera Eco_{versao}_{build}.ini com caminho do banco correto.
- [ ] Instância criada aparece na lista após confirmação.
- [ ] Instâncias persistidas entre sessões (AppData JSON).
- [ ] Botão editar abre flyout preenchido; ao salvar com executável diferente, .ini antigo é substituído.
- [ ] Botão excluir solicita confirmação, remove da lista e remove o .ini gerado do disco.
- [ ] Botão executar valida .exe, .eco e .ini gerado antes de iniciar o processo.
- [ ] Erros de pasta ausente exibem mensagem descritiva.
- [ ] App executa com permissão de administrador.
- [ ] eco.ini padrão nunca é modificado pelo EcoUtils em nenhum fluxo.
