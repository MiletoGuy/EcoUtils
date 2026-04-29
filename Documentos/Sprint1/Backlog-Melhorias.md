# Backlog de Melhorias — Sprint 1

> Registrar aqui qualquer ponto de melhoria identificado durante a execução da Sprint 1.
> Esses itens NÃO devem ser implementados nesta sprint — apenas documentados para sprint futura.

---

## Template

```
### M-<N> — <Titulo curto>

**Identificado em:** Commit N — `<tipo>: <descricao>`
**Prioridade sugerida:** Alta / Média / Baixa

#### Contexto
(descrever onde o ponto foi identificado e por que é uma melhoria)

#### O que melhorar
(descrever o que deveria ser diferente)

#### Impacto estimado
(escalabilidade, UX, manutenção, performance)
```

---

### M-1 — Substituir MessageBox por diálogo customizado

**Identificado em:** Commit 11 — `feat: ações das instâncias`
**Prioridade sugerida:** Média

#### Contexto
O diálogo de confirmação de exclusão e mensagens de erro usam `MessageBox.Show` nativo do Windows,
que quebra a identidade visual do design system e não suporta estilização.

#### O que melhorar
Criar um componente de diálogo modal próprio (overlay + painel + botões estilizados)
dentro do design system, substituindo todos os usos de `MessageBox`.

#### Impacto estimado
UX: consistência visual total com o design system.
Escalabilidade: componente reutilizável em todas as sprints futuras.

---

### M-2 — Sistema de log estruturado

**Identificado em:** Commit 4 — `services: VersionCatalogService e DatabaseDiscoveryService`
**Prioridade sugerida:** Média

#### Contexto
Erros de IO nos services (pasta não encontrada, permissão negada) são silenciados
e retornam lista vazia sem nenhum registro. Dificulta diagnóstico em produção.

#### O que melhorar
Implementar um `ILogService` simples que grava em `C:/ecosis/logs/ecoutils.log`,
injetá-lo nos services e registrar exceções capturadas com timestamp e contexto.

#### Impacto estimado
Manutenção: facilita diagnóstico de problemas em campo pelo analista de suporte.

---

### M-3 — Estilização completa do ComboBox (ControlTemplate)

**Identificado em:** Commit 2 — `design: ResourceDictionaries base`
**Prioridade sugerida:** Baixa

#### Contexto
O `ComboBox` em `Controls.xaml` recebeu apenas estilo de propriedades (`Background`, `BorderBrush`, `Foreground`).
O visual do dropdown, da seta e dos itens selecionados ainda usa o template padrão do Windows,
quebrando a identidade visual do design system, especialmente no modo escuro.

#### O que melhorar
Definir um `ControlTemplate` completo para `ComboBox` e `ComboBoxItem` em `Controls.xaml`,
cobrindo: toggle button com seta personalizada, popup com fundo `PanelBackground`,
itens com hover `SidebarItemHover` e seleção `SidebarItemActive`.

#### Impacto estimado
UX: aparência totalmente integrada ao design system.
Manutenção: sem dependência do tema padrão do Windows (importante em Windows 11 com Mica/Acrylic).

#### Análise de viabilidade
**Alta.** Todo o trabalho é XAML puro em `Controls.xaml` — zero impacto em C# ou ViewModels.
O `ControlTemplate` de um `ComboBox` é verboso (~80–120 linhas) mas bem documentado.
Pode ser incluído como um item isolado em sprint 2 sem risco de regressão.
Referência: [WPF ComboBox Styles and Templates (MSDN)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/combobox-styles-and-templates).

---

### M-4 — DI Container para injeção de dependências

**Identificado em:** Commit 8 — `shell: MainWindow + MainViewModel`
**Prioridade sugerida:** Alta

#### Contexto
`MainViewModel` instancia todos os cinco services com `new` diretamente no construtor.
Isso viola o princípio de que ViewModels não devem conhecer implementações concretas,
impossibilita mocks em testes unitários e torna difícil trocar implementações sem modificar ViewModels.

#### O que melhorar
Adicionar `Microsoft.Extensions.DependencyInjection` (já presente no SDK do .NET 10).
Construir o `ServiceProvider` em `App.xaml.cs` no evento `OnStartup`, registrar todas as
interfaces e implementações, e injetar `MainViewModel` no `MainWindow` via construtor.
Remover a instanciação direta do `DataContext` em `MainWindow.xaml` (substituir por `Startup` em `App.xaml`).

#### Impacto estimado
Testabilidade: ViewModels passam a ser testáveis com mocks de qualquer service.
Escalabilidade: adicionar novo service em qualquer sprint = registrar no container, sem tocar ViewModels.
Manutenção: um único ponto de composição da aplicação (`App.xaml.cs`).

#### Análise de viabilidade
**Alta.** `Microsoft.Extensions.DependencyInjection` não exige NuGet adicional em .NET 10 — está no SDK.
A mudança principal é mover a composição de `MainViewModel` para `App.xaml.cs` e ajustar
`MainWindow.xaml` para não instanciar o DataContext em XAML (mover para constructor injection).
Esforço estimado: ~2h. Risco de regressão: baixo (apenas reorganização de composição).

---

### M-5 — Tratamento de exceção no ConfirmarCommand (async void)

**Identificado em:** Commit 10 — `feat: InstanceFlyoutView + InstanceFlyoutViewModel`
**Prioridade sugerida:** Alta

#### Contexto
`ConfirmarCommand` usa `RelayCommand` com um lambda `async _` que chama `ConfirmarAsync()`.
`RelayCommand` aceita `Action<object?>` — o `async lambda` se torna `async void`.
Qualquer exceção lançada dentro de `ConfirmarAsync` (ex.: `InvalidOperationException` de
`GerarIniAsync` quando `dados=` não for encontrado, ou `IOException` ao gravar o `.ini`)
não será capturada e encerrará o processo silenciosamente.

#### O que melhorar
Duas opções complementares:
1. **Curto prazo**: adicionar `try/catch` em `ConfirmarAsync` com feedback visual — uma propriedade
   `string? ErroConfirmacao` no `InstanceFlyoutViewModel` exibida como `TextBlock` de erro no flyout.
2. **Longo prazo**: implementar `AsyncRelayCommand` (ver M-6) e substituir o padrão async void.

#### Impacto estimado
Confiabilidade: erros de disco ou de configuração do `eco.ini` passam a ter feedback visível ao usuário.
UX: mensagem de erro contextualizada no próprio flyout, sem fechar o formulário.

#### Análise de viabilidade
**Alta (curto prazo).** Adicionar `ErroConfirmacao` + `try/catch` é uma mudança de ~15 linhas.
O TextBlock de erro no flyout é mais uma linha no XAML. Pode ser feito no Commit 11 ou em sprint 2.

---

## Melhorias adicionais identificadas durante análise do código

---

### M-6 — AsyncRelayCommand para eliminar padrão async void

**Identificado durante:** análise pós-Sprint 1
**Prioridade sugerida:** Alta

#### Contexto
`RelayCommand` aceita apenas `Action<object?>` (síncrono). Em vários pontos do código,
comandos que precisam ser `async` usam o padrão fire-and-forget (`_ = MinhaTask()`),
o que silencia exceções e não oferece controle de estado de execução (ex.: desabilitar
o botão enquanto o comando está rodando).

Locais afetados atualmente:
- `ExecutarEcoViewModel.CarregarInstanciasAsync()` — fire-and-forget no construtor.
- `ExecutarEcoViewModel.AbrirFlyoutNovo()` — `_ = _instanceRepository.SalvarAsync(...)` no callback.
- `InstanceFlyoutViewModel.ConfirmarAsync()` — async void via RelayCommand.

#### O que melhorar
Criar `AsyncRelayCommand` em `Commands/AsyncRelayCommand.cs` que aceita `Func<object?, Task>`,
expõe `bool IsExecuting` e garante que exceções sejam propagadas (ou encaminhadas para `ILogService`).

#### Impacto estimado
Confiabilidade: exceções de I/O nunca mais silenciosas.
UX: possibilidade de mostrar loading/spinner enquanto o comando executa.
Manutenção: padrão uniforme para todos os comandos async do projeto.

#### Análise de viabilidade
**Alta.** `AsyncRelayCommand` é um padrão bem documentado na comunidade WPF/MVVM.
Implementação completa com `IsExecuting` e re-entrância protegida: ~40 linhas.
Substituição nos ViewModels existentes é mecânica. Sem impacto em XAML.
Alternativa: adotar `CommunityToolkit.Mvvm` (NuGet) que já inclui `AsyncRelayCommand`,
`ObservableObject` e `[RelayCommand]` source generator — eliminaria ~60% do boilerplate atual.

---

### M-7 — Escrita atômica no InstanceRepository

**Identificado durante:** análise pós-Sprint 1
**Prioridade sugerida:** Alta

#### Contexto
`SalvarAsync` usa `File.Create(arquivo)` diretamente. Se o processo for encerrado, o disco
encher ou outra exceção ocorrer durante a serialização, o arquivo `instancias.json` ficará
truncado ou corrompido — e `CarregarAsync` não conseguirá desserializá-lo, retornando lista vazia.
O usuário perderia todas as instâncias cadastradas silenciosamente.

#### O que melhorar
Escrever em um arquivo temporário `instancias.json.tmp` e, após a serialização completa e
bem-sucedida, substituir atomicamente com `File.Move(tmp, destino, overwrite: true)`.
`File.Move` é atômico no nível do sistema operacional (dentro do mesmo volume).

#### Impacto estimado
Confiabilidade: impossibilidade de corrupção do arquivo de dados principal.
Manutenção: proteção passiva, sem custo de UX ou performance perceptível.

#### Análise de viabilidade
**Alta / imediata.** Mudança de ~5 linhas em `InstanceRepository.SalvarAsync`.
`File.Move` com `overwrite: true` está disponível desde .NET 3.0. Sem dependência externa.
Risco de regressão: zero — é uma substituição direta da escrita.

---

### M-8 — Validação de apelido duplicado no flyout

**Identificado durante:** análise pós-Sprint 1
**Prioridade sugerida:** Média

#### Contexto
`InstanceFlyoutViewModel` não valida se o `Apelido` informado já está em uso por outra
instância cadastrada. O usuário pode criar duas instâncias "PRD" sem nenhum aviso,
causando confusão na lista.

#### O que melhorar
Passar para o `InstanceFlyoutViewModel` via construtor uma lista (ou `IEnumerable`) dos
apelidos já existentes. Adicionar `ApelidoDuplicado` como propriedade derivada e incluí-la
no cálculo de `PodeConfirmar`. Exibir mensagem inline no flyout quando duplicado.
Em modo edição, excluir o apelido da própria instância da verificação.

#### Impacto estimado
UX: eliminação de instâncias com nomes duplicados.
Manutenção: evita bugs sutis onde o usuário executa a instância errada.

#### Análise de viabilidade
**Alta.** A mudança é localizada em `InstanceFlyoutViewModel` e em `ExecutarEcoViewModel.AbrirFlyoutNovo/Editar`.
Passar `IEnumerable<string> apelidosExistentes` no construtor é direto.
A propriedade `ApelidoDuplicado` segue o mesmo padrão derivado já usado em `PodeConfirmar`.

---

### M-9 — Extrair formato `dados=127.0.0.1:` como constante

**Identificado durante:** análise pós-Sprint 1
**Prioridade sugerida:** Média

#### Contexto
`IniGeneratorService.GerarIniAsync` grava `dados=127.0.0.1:{basePath}` com o IP `127.0.0.1`
hardcoded dentro do método. Se o ECO em alguma versão futura precisar de um IP diferente
(outro servidor, configuração de rede), a mudança exige encontrar e editar a string dentro do service.

#### O que melhorar
Extrair `"127.0.0.1"` para `EcoPathConstants.EcoServerHost` (ou similar).
O valor padrão continua `"127.0.0.1"`, mas fica em um único lugar auditável.

#### Impacto estimado
Manutenção: zero risco de string esquecida se o IP precisar mudar.
Escalabilidade: abre caminho para tornar o host configurável via arquivo de settings.

#### Análise de viabilidade
**Alta / trivial.** Uma constante em `EcoPathConstants` + atualização de uma linha em `IniGeneratorService`.
Esforço: 5 minutos. Pode ser incluído no Commit 11 sem criar commit separado.

---

### M-10 — `CanExecuteChanged` manual no ConfirmarCommand

**Identificado durante:** análise pós-Sprint 1
**Prioridade sugerida:** Baixa

#### Contexto
`RelayCommand` liga `CanExecuteChanged` ao evento global `CommandManager.RequerySuggested`,
que dispara em qualquer interação do usuário com a UI (mouse move, foco, digitação).
Para o `ConfirmarCommand` — cuja habilitação depende de `PodeConfirmar` — isso funciona,
mas é ineficiente: o WPF reavalia `CanExecute` desnecessariamente a cada evento de UI.

#### O que melhorar
Adicionar um método `RaiseCanExecuteChanged()` em `RelayCommand` que invoca `CanExecuteChanged`
diretamente. Chamar esse método nas propriedades de `InstanceFlyoutViewModel` que afetam
`PodeConfirmar` (`Apelido`, `ExecutavelSelecionado`, `BancoSelecionado`, `EcoIniValido`),
em vez de depender do ciclo global.

#### Impacto estimado
Performance: redução de chamadas de `CanExecute` em formulários com muitos bindings.
Manutenção: torna o contrato de habilitação de cada comando explícito e rastreável.

#### Análise de viabilidade
**Alta.** Uma linha em `RelayCommand` + quatro chamadas nos setters de `InstanceFlyoutViewModel`.
Requer acesso à instância do command (não pode ser `ICommand` anônimo) — já é o caso no ViewModel.
Risco de regressão: baixo. Pode ser combinado com M-6 (AsyncRelayCommand) para não criar dois PRs de Commands.

<!-- Novos itens serão adicionados abaixo conforme identificados durante a sprint -->
