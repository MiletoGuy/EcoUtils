# Registro de Commits — Sprint 1

> Preencher após cada commit executado.
> Formato: copiar o bloco de template abaixo e preencher.

---

## Template

```
### Commit N — `<tipo>: <descricao>`

**Data:** DD/MM/AAAA
**Hash:** (preencher após git push)

#### O que foi feito
- (listar arquivos criados/modificados e o que cada um faz)

#### Decisões tomadas durante a execução
- (registrar qualquer escolha de implementação não prevista no guia)

#### Pontos de melhoria identificados
- (registrar aqui e também em Backlog-Melhorias.md)

#### Build
- [ ] dotnet build: OK
```

---

<!-- Os blocos preenchidos serão adicionados abaixo conforme os commits forem executados -->

---

### Commit 1 — `manifest: configurar requireAdministrator`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `app.manifest` criado na raiz com `requestedExecutionLevel level="requireAdministrator"`.
- `EcoUtils.csproj` modificado: adicionada tag `<ApplicationManifest>app.manifest</ApplicationManifest>` dentro do `<PropertyGroup>`.

#### Decisões tomadas durante a execução
- Nenhum desvio em relação ao guia.

#### Pontos de melhoria identificados
- Nenhum.

#### Build
- [x] dotnet build: OK

---

### Commit 2 — `design: ResourceDictionaries base (Design System)`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `Themes/Colors.xaml` criado: todos os 28 tokens de cor definidos como `Color` + `SolidColorBrush` correspondente.
- `Themes/Typography.xaml` criado: `FontBase` (Segoe UI 13) e `FontMono` (Consolas 12) com tipos `FontFamily` e `sys:Double`.
- `Themes/Controls.xaml` criado: estilos implícitos/explícitos para `Window`, `Button` (variantes `ButtonPrimary`, `ButtonSecondary`, `ButtonDanger`, `ButtonIcon`), `TextBox`, `ComboBox`, `ScrollViewer`, `ScrollBar`, `ListBox` e `ListBoxItem`.
- `App.xaml` modificado: `<Application.Resources>` substituído por `ResourceDictionary` com `MergedDictionaries` apontando para os três arquivos de tema.

#### Decisões tomadas durante a execução
- O estilo de `Window` foi definido como implícito (sem `x:Key`) para aplicar globalmente sem necessidade de referência manual em cada janela.
- `ScrollBar` recebeu estilo implícito básico (largura 8px, cor de fundo alinhada ao tema) mesmo não estando listado explicitamente no guia, pois é necessário para a aparência coerente do `ScrollViewer`.
- `ButtonBase` definido como estilo-chave interno (não implícito) e usado como `BasedOn` pelas três variantes de botão, evitando repetição de template.

#### Pontos de melhoria identificados
- O `ComboBox` recebeu apenas estilo de propriedades simples; o template completo (dropdown, seta, itens) ainda usa o visual padrão do sistema. Registrado para refinamento futuro.

#### Build
- [x] dotnet build: OK

---

### Commit 3 — `models: EcoInstance, EcoExecutavel, EcoDatabase`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `Models/EcoInstance.cs` criado: POCO com `Id` (Guid), `Apelido`, `ExecutavelPath`, `ExecutavelNome`, `BasePath`, `BaseNome`, `IniPath`.
- `Models/EcoExecutavel.cs` criado: POCO com `NomeCompleto`, `ExePath`, `IniPadraoPresente`.
- `Models/EcoDatabase.cs` criado: POCO com `NomeCompleto`, `EcoPath`.
- `Infrastructure/EcoPathConstants.cs` criado: constantes `WindowsDir`, `DadosDir`, `EcoIniPadrao` e propriedade derivada `AppDataDir`.

#### Decisões tomadas durante a execução
- `System.IO` não é incluído nos implicit usings de projetos WPF — adicionado `using System.IO;` explícito em `EcoPathConstants.cs`. Os demais models não precisam de nenhum using adicional.

#### Pontos de melhoria identificados
- Nenhum.

#### Build
- [x] dotnet build: OK

---

### Commit 4 — `services: interfaces e VersionCatalogService + DatabaseDiscoveryService`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `Services/Interfaces/IVersionCatalogService.cs` criado: contrato `ListarExecutaveisAsync()`.
- `Services/Interfaces/IDatabaseDiscoveryService.cs` criado: contrato `ListarBancosAsync()`.
- `Services/Interfaces/IIniGeneratorService.cs` criado: contratos `GerarIniAsync` e `RemoverIni`.
- `Services/Interfaces/IInstanceRepository.cs` criado: contratos `CarregarAsync` e `SalvarAsync`.
- `Services/Interfaces/ILaunchService.cs` criado: contrato `ExecutarAsync` retornando tupla `(bool Sucesso, string? Erro)`.
- `Services/VersionCatalogService.cs` criado: lista `.exe` em `WindowsDir`, filtra pelo regex `^Eco_\d+_\d+\.exe$`, captura `IOException` e `UnauthorizedAccessException` retornando lista vazia.
- `Services/DatabaseDiscoveryService.cs` criado: lista `.eco` em `DadosDir`, mesma política de tratamento de exceções.

#### Decisões tomadas durante a execução
- `Task.Run` exige tipo genérico explícito (`Task.Run<IReadOnlyList<T>>`) para que o compilador resolva corretamente a sobrecarga quando o lambda retorna `IReadOnlyList<T>`. Sem o tipo explícito, o compilador inferia o lambda como `Func<Task>` (void) e gerava 18 erros de conversão.
- A verificação de `IniPadraoPresente` em `VersionCatalogService` é feita uma única vez antes do loop (não por executável), pois o `eco.ini` padrão é único na pasta e não varia por `.exe`.

#### Pontos de melhoria identificados
- Nenhum.

#### Build
- [x] dotnet build: OK

---

### Commit 5 — `services: InstanceRepository`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `Services/InstanceRepository.cs` criado: implementa `IInstanceRepository` com persistência JSON em `%LOCALAPPDATA%\EcoUtils\instancias.json`.
- `CarregarAsync`: abre o arquivo com `File.OpenRead` e desserializa via `JsonSerializer.DeserializeAsync`. Retorna lista vazia se o arquivo não existir ou em caso de `IOException`, `JsonException` ou `UnauthorizedAccessException`.
- `SalvarAsync`: cria o diretório se necessário via `Directory.CreateDirectory`, depois serializa com `File.Create` + `JsonSerializer.SerializeAsync`.
- `JsonSerializerOptions` configurado com `WriteIndented = true`.

#### Decisões tomadas durante a execução
- O operador `??` não unifica `List<EcoInstance>` com `EcoInstance[]` — fallback de `CarregarAsync` corrigido para `new List<EcoInstance>()` em vez de `Array.Empty<EcoInstance>()`.
- `ArquivoPath` exposto como propriedade estática privada para centralizar o cálculo do caminho sem repetição.

#### Pontos de melhoria identificados
- Nenhum.

#### Build
- [x] dotnet build: OK

---

### Commit 6 — `services: IniGeneratorService`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `Services/IniGeneratorService.cs` criado: implementa `IIniGeneratorService`.
- `GerarIniAsync`: lê todas as linhas de `EcoPathConstants.EcoIniPadrao` com `File.ReadAllLinesAsync`, percorre as linhas rastreando a seção `[windows]` e substitui a linha `dados=` pelo valor `dados=127.0.0.1:{basePath}`. Grava o resultado em `C:\ecosis\windows\{exeNome}.ini` com `File.WriteAllLinesAsync`. Retorna o caminho do `.ini` gerado.
- `RemoverIni`: valida que o arquivo existe e que o nome segue o padrão `Eco_*.ini` via regex antes de deletar — proteção explícita para nunca remover `eco.ini`.

#### Decisões tomadas durante a execução
- Lança `InvalidOperationException` se a chave `dados=` não for encontrada na seção `[windows]` — erro de configuração que deve ser visível, não silenciado.
- A detecção de seção é feita por comparação `OrdinalIgnoreCase` para robustez com variações de casing no `eco.ini`.
- A substituição opera no array de linhas in-place (por índice) antes de gravar, evitando alocação de lista adicional.

#### Pontos de melhoria identificados
- Nenhum.

#### Build
- [x] dotnet build: OK

---

### Commit 7 — `services: LaunchService`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `Services/LaunchService.cs` criado: implementa `ILaunchService`.
- `ExecutarAsync`: verifica existência de `ExecutavelPath`, `BasePath` e `IniPath` com `File.Exists` antes de qualquer tentativa de lançar o processo. Retorna `(false, "mensagem descritiva")` para qualquer arquivo ausente.
- `Process.Start` usado com `ProcessStartInfo` explícito (`UseShellExecute = true`). Não aguarda o processo terminar.
- Toda a operação é envolvida em `Task.Run` para manter o padrão async do contrato.

#### Decisões tomadas durante a execução
- `UseShellExecute = true` é necessário para que o Windows aplique corretamente o contexto de elevação ao executável do ECO, que também requer permissão de administrador.
- O `Task.Run` envolve apenas o `Process.Start`, pois `File.Exists` é síncrono e leve — não justifica overhead de thread para as verificações.

#### Pontos de melhoria identificados
- Nenhum.

#### Build
- [x] dotnet build: OK

---

### Commit 8 — `shell: MainWindow + MainViewModel (sidebar + navegação)`

**Data:** 28/04/2026
**Hash:** (preencher após git push)

#### O que foi feito
- `ViewModels/NavItem.cs` criado: POCO com `Rotulo`, `Icone` e `ViewModel` (usando `init`).
- `ViewModels/MainViewModel.cs` reescrito: `ObservableCollection<NavItem> Abas` + `NavItem? AbaAtiva`; instancia serviços concretos e popula a lista com um `NavItem` para `ExecutarEcoViewModel`.
- `ViewModels/ExecutarEcoViewModel.cs` criado como stub: recebe as cinco interfaces de serviço via construtor (campos privados); será expandido no Commit 9.
- `Views/ExecutarEcoView.xaml` + `.xaml.cs` criados como stub: `UserControl` vazio (grid vazio), necessário para compilar o `DataTemplate` no `App.xaml`.
- `MainWindow.xaml` reescrito: layout de duas colunas (220px sidebar + `*` workspace); `ListBox` vinculado a `Abas`/`AbaAtiva` com `DataTemplate` de ícone + rótulo; `ContentControl` vinculado a `AbaAtiva.ViewModel`; `Width="1024"`, `Height="680"`, `MinWidth="800"`, `MinHeight="520"`.
- `MainWindow.xaml.cs` limpo: removidos todos os usings desnecessários; mantido apenas `using System.Windows` + `InitializeComponent()`.
- `App.xaml` atualizado: adicionados namespaces `vm` e `views`; `DataTemplate` de `ExecutarEcoViewModel → ExecutarEcoView` registrado na `ResourceDictionary` (fora das `MergedDictionaries`).

#### Decisões tomadas durante a execução
- `ExecutarEcoViewModel` e `ExecutarEcoView` precisam existir como stubs neste commit porque `MainViewModel` já os referencia e `App.xaml` já declara o `DataTemplate`. Sem as classes, o build falharia.
- Os serviços são instanciados diretamente no construtor de `MainViewModel` por simplicidade; injeção de dependência formal está registrada no backlog para sprint futura.
- O `DataTemplate` é colocado após o bloco `MergedDictionaries` dentro do mesmo `ResourceDictionary` para que os estilos dos temas fiquem disponíveis dentro do template.

#### Pontos de melhoria identificados
- `MainViewModel` instancia dependências diretamente (new). Considerar DI container (ex.: `Microsoft.Extensions.DependencyInjection`) em sprint futura para facilitar testes e extensibilidade.

#### Build
- [x] dotnet build: OK
