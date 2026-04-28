# Sprint 1 - Planejamento de UI: Tela "Executar Eco"

## 1. Objetivo da Sprint

Construir a estrutura base de navegacao do EcoUtils e a primeira funcionalidade completa:
gerenciamento e execucao de instancias do sistema ECO.

Ao final da sprint o usuario devera conseguir:
- Navegar pela sidebar entre abas futuras.
- Criar instancias ECO (par executavel + banco + apelido).
- Visualizar as instancias criadas em lista.
- Executar, editar e excluir instancias.

---

## 2. Layout geral da janela

```
+----------------------------------------------------------+
|  [Barra de titulo da janela - sem chrome customizado]    |
+----------+-----------------------------------------------+
|          |                                               |
| SIDEBAR  |           WORKSPACE PRINCIPAL                 |
|          |                                               |
| > Exec.  |   (conteudo da aba selecionada)               |
|   ECO    |                                               |
|          |                                               |
| (futuras)|                                               |
|          |                                               |
+----------+-----------------------------------------------+
```

- A sidebar ocupa a faixa esquerda com largura fixa.
- O workspace ocupa o restante da janela e troca de conteudo conforme a aba ativa.
- A aba ativa na sidebar deve ter destaque visual claro (cor de fundo ou indicador lateral).

---

## 3. Sidebar

### 3.1 Estrutura
- Lista de itens de navegacao dispostos verticalmente.
- Cada item possui icone + rotulo.
- Clique em um item define a aba ativa e carrega o View correspondente no workspace.

### 3.2 Itens da Sprint 1
| Rotulo        | View alvo             | Status    |
|---------------|-----------------------|-----------|
| Executar ECO  | ExecutarEcoView       | SPRINT 1  |
| (futuras)     | -                     | FUTURO    |

### 3.3 Consideracoes tecnicas
- Implementar como lista com `ItemsControl` ou `ListBox` na MainWindow.
- Aba ativa controlada por propriedade `AbaAtiva` no `MainViewModel`.
- Troca de View via `ContentControl` com `DataTemplate` por tipo de ViewModel.
  Esse e o padrao MVVM puro em WPF: cada ViewModel tem um DataTemplate mapeado para seu View.

---

## 4. Tela: Executar ECO

### 4.1 Estrutura da tela
```
+-----------------------------------------------+
|  Executar ECO                        [ + ]     |
+-----------------------------------------------+
|  Apelido       | Executavel  | Base   | Acoes  |
+----------------+-------------+--------+--------+
|  Producao      | Eco_650_10  | PRD.eco| [>][e][x]|
|  Homologacao   | Eco_640_05  | HML.eco| [>][e][x]|
|  (vazia)       |             |        |          |
+-----------------------------------------------+
```

- Cabecalho com titulo da tela e botao "+" alinhado a direita.
- Lista/tabela de instancias cadastradas.
- Cada linha representa uma instancia com colunas: Apelido, Executavel, Base, Acoes.
- Se nao houver instancias, exibir mensagem centralizada: "Nenhuma instancia cadastrada. Clique em + para adicionar."
- Botoes de acao por linha: Executar [>], Editar [lapsis], Excluir [x].

### 4.2 Modelo de dados: Instancia ECO
Cada instancia armazena:
- `Id` (Guid): identificador unico interno.
- `Apelido` (string): nome amigavel dado pelo usuario.
- `ExecutavelPath` (string): caminho completo do .exe selecionado.
- `ExecutavelNome` (string): nome do arquivo sem extensao (ex.: Eco_650_10).
- `BasePath` (string): caminho completo do arquivo .eco selecionado.
- `BaseNome` (string): nome do arquivo .eco sem extensao (para exibicao).
- `IniValido` (bool): indica se o .ini correspondente foi encontrado na verificacao.

### 4.3 Persistencia das instancias
As instancias criadas precisam sobreviver ao fechamento do app.

Opcao recomendada: arquivo JSON no AppData local do usuario.
- Caminho: `%LOCALAPPDATA%\EcoUtils\instancias.json`
- Gerenciado pelo `InstanceRepository` (leitura e escrita de lista de instancias).
- Nao salvar em C:/ecosis para evitar dependencia de permissao de admin na escrita de config.

---

## 5. Flyout: Adicionar / Editar instancia

### 5.1 Comportamento
- Abre centralizado sobre a janela principal ao clicar em "+" (adicionar) ou no botao editar.
- O fundo da janela principal deve ser escurecido (overlay semitransparente).
- O flyout e um painel flutuante, nao uma janela separada (implementado como `Border`/`Grid`
  sobreposto ao layout principal via z-index, ou como `Popup` / `Overlay` customizado).

### 5.2 Conteudo do flyout

```
+-----------------------------------------------+
|  Nova Instancia ECO                      [ X ] |
+-----------------------------------------------+
|                                               |
|  Apelido:   [______________________________]  |
|                                               |
|  Executavel:                                  |
|  [ComboBox com versoes disponiveis    v ]      |
|  Status .ini: [OK - Eco_650_10.ini encontrado]|
|            ou [AVISO - .ini nao encontrado]   |
|                                               |
|  Base de dados:                               |
|  [ComboBox com bancos .eco disponiveis  v ]   |
|                                               |
|          [ Cancelar ]   [ Confirmar ]         |
+-----------------------------------------------+
```

### 5.3 Campo: Executavel
- Exibe apenas os arquivos .exe encontrados em C:/ecosis/windows que seguem o padrao `Eco_{versao}_{build}.exe`.
- Arquivos fora do padrao sao ignorados silenciosamente.
- Ao selecionar um executavel, o app verifica imediatamente se existe o `eco.ini` padrao em C:/ecosis/windows.
  - `eco.ini` encontrado: indicador verde "Template eco.ini encontrado".
  - `eco.ini` ausente: indicador vermelho bloqueando confirmacao, pois sem ele nao e possivel gerar o .ini da instancia.
- O verificador do .ini especifico da instancia e irrelevante aqui: o EcoUtils GERA esse arquivo (ver secao 5.8).
- Se C:/ecosis/windows nao for encontrada ao abrir o flyout, exibir erro e bloquear campo.

### 5.4 Campo: Base de dados
- Lista todos os arquivos .eco encontrados em C:/ecosis/dados/.
- Exibe o nome do arquivo sem extensao para facilitar leitura.
- Se C:/ecosis/dados/ nao for encontrada ao abrir o flyout, exibir erro e bloquear campo.

### 5.5 Campo: Apelido
- Texto livre, obrigatorio.
- Nao permite confirmar com apelido vazio.
- Nao precisa ser unico (o usuario pode ter duas instancias com o mesmo apelido se quiser).

### 5.6 Validacao antes de confirmar
- Apelido preenchido: obrigatorio.
- Executavel selecionado: obrigatorio.
- Base selecionada: obrigatorio.
- `eco.ini` padrao ausente em C:/ecosis/windows: BLOQUEIA confirmacao, pois e o template de geracao.

### 5.8 Geracao do .ini da instancia (CONFIRMADO - DP-1)
Ao confirmar a criacao (ou edicao) de uma instancia, o EcoUtils executa:
1. Ler C:/ecosis/windows/eco.ini (arquivo padrao imutavel, nunca alterado pelo EcoUtils).
2. Localizar a linha que contem a chave `dados=` dentro da secao `[windows]`.
3. Substituir o valor pelo caminho completo do arquivo .eco selecionado.
   Exemplo: `dados=127.0.0.1:C:\ecosis\dados\PRD.eco`
4. Gravar o resultado como `Eco_{versao}_{build}.ini` em C:/ecosis/windows.
   O nome do .ini gerado e sempre identico ao nome do .exe escolhido.
5. O caminho do .ini gerado e armazenado no modelo da instancia.

Regras de seguranca do eco.ini padrao:
- O EcoUtils NUNCA escreve no eco.ini padrao, apenas le.
- Se ao editar uma instancia o executavel for alterado, o .ini antigo e removido e um novo e gerado.
- Se ao excluir uma instancia o .ini correspondente existir em disco, ele e removido junto.
  (os arquivos .ini gerenciados pelo EcoUtils tem ciclo de vida atrelado a instancia)

### 5.7 Modo edicao
- Ao clicar em "Editar" em uma instancia, o flyout abre preenchido com os dados da instancia.
- Os campos seguem as mesmas regras de validacao.
- O botao de confirmacao exibe "Salvar" em vez de "Confirmar".

---

## 6. Acoes das instancias

### 6.1 Executar [>]
1. Validar que o .exe ainda existe no caminho salvo.
2. Validar que o .eco ainda existe no caminho salvo.
3. Validar que o .ini gerado pelo EcoUtils ainda existe em C:/ecosis/windows.
4. Se qualquer arquivo estiver ausente, exibir mensagem de erro descritiva indicando qual esta faltando.
5. Se tudo ok, iniciar o processo do executavel sem argumentos adicionais.
   O ECO le automaticamente o .ini de mesmo nome que esta na mesma pasta, que ja contem o caminho do banco correto.
6. Exibir feedback visual de que o ECO foi iniciado (notificacao breve ou indicador de status na linha).

### 6.2 Editar [lapis]
- Abre o flyout de edicao preenchido com os dados da instancia selecionada.

### 6.3 Excluir [x]
- Exibir confirmacao antes de excluir: "Deseja excluir a instancia '{Apelido}'?"
- Confirmado:
  - Remover da lista e persistir alteracao no JSON.
  - Remover o .ini gerado pelo EcoUtils em C:/ecosis/windows (se ainda existir).
- Cancelado: nenhuma acao.
- O .exe em C:/ecosis/windows NUNCA e removido pelo EcoUtils (pertence a instalacao do ECO).

---

## 7. Estrutura de componentes WPF / MVVM

### 7.1 Views
| View                     | Responsabilidade                                  |
|--------------------------|---------------------------------------------------|
| MainWindow               | Shell: sidebar + ContentControl do workspace      |
| ExecutarEcoView          | Lista de instancias + botao +                     |
| InstanceFlyoutView       | Flyout de adicionar/editar instancia (UserControl)|

### 7.2 ViewModels
| ViewModel                | Responsabilidade                                  |
|--------------------------|---------------------------------------------------|
| MainViewModel            | Controle de navegacao (AbaAtiva, lista de abas)   |
| ExecutarEcoViewModel     | Lista de instancias, comando Adicionar            |
| InstanceFlyoutViewModel  | Estado do flyout, campos, validacao, confirmacao  |

### 7.3 Models
| Model                    | Responsabilidade                                  |
|--------------------------|---------------------------------------------------|
| EcoInstance              | Representa uma instancia cadastrada               |
| EcoExecutavel            | Representa um .exe valido encontrado em /windows  |
| EcoDatabase              | Representa um .eco encontrado em /dados           |

### 7.4 Services (Sprint 1)
| Service                  | Responsabilidade                                                        |
|--------------------------|-------------------------------------------------------------------------|
| VersionCatalogService    | Listar .exe validos em C:/ecosis/windows                                |
| DatabaseDiscoveryService | Listar .eco em C:/ecosis/dados                                          |
| IniGeneratorService      | Ler eco.ini padrao, substituir chave dados= e gravar .ini da instancia  |
| InstanceRepository       | Ler/escrever instancias.json no AppData                                 |
| LaunchService            | Validar existencia dos tres arquivos e iniciar o processo do ECO        |

---

## 8. Registro de decisoes

### DP-1: Mecanismo de passagem do banco para o ECO - CONFIRMADO
O ECO nao recebe argumentos de linha de comando. O banco e definido dentro do arquivo .ini.
Estrutura relevante do eco.ini padrao:
```
[windows]
dados=127.0.0.1:C:\ecosis\dados\ecodados.eco
```
Fluxo definido:
- O EcoUtils le o eco.ini padrao (imutavel) como template.
- Substitui apenas a linha `dados=` com o caminho do banco selecionado.
- Grava o resultado como `Eco_{versao}_{build}.ini` (mesmo nome base do .exe da instancia).
- Ao executar, o ECO le o .ini de mesmo nome e encontra o banco correto automaticamente.
- O IniGeneratorService e responsavel por toda essa operacao.

### DP-2: Tamanho e comportamento da janela - CONFIRMADO
- Tamanho inicial: 1024 x 680.
- Janela redimensionavel com tamanho minimo fixo (sugerido: 800 x 520).
- Barra de titulo sera customizada em sprint futura (WindowChrome proprio no estilo VSCode).
  Por ora usar barra de titulo nativa do Windows.

### DP-3: Estilo visual / tema - CONFIRMADO
- Design system proprio desenvolvido no WPF com ResourceDictionaries.
- Inspiracao visual: VSCode (dark theme, cores neutras, sidebar escura, workspace cinza escuro,
  tipografia monoespaco para detalhes tecnicos, icones simples).
- Todos os estilos e templates base serao centralizados em ResourceDictionaries compartilhados
  antes de construir os primeiros componentes.
- Nenhuma biblioteca de terceiros de UI sera usada (Material Design, MahApps, etc.).
- O design system sera documentado em sprint propria antes de ser aplicado nos componentes.

---

## 9. Criterios de aceite da Sprint 1

- [ ] Layout sidebar + workspace funcional com navegacao por aba.
- [ ] Aba "Executar ECO" carregada como tela inicial.
- [ ] Lista de instancias exibida (ou mensagem de lista vazia).
- [ ] Botao "+" abre flyout centralizado com overlay.
- [ ] Flyout lista executaveis validos de C:/ecosis/windows no padrao Eco_{versao}_{build}.exe.
- [ ] Flyout verifica existencia do eco.ini padrao e bloqueia confirmacao se ausente.
- [ ] Flyout lista bancos .eco de C:/ecosis/dados.
- [ ] Flyout valida campos obrigatorios antes de confirmar.
- [ ] Ao confirmar, IniGeneratorService gera Eco_{versao}_{build}.ini com caminho do banco correto.
- [ ] Instancia criada aparece na lista apos confirmacao.
- [ ] Instancias persistidas entre sessoes (AppData JSON).
- [ ] Botao editar abre flyout preenchido; ao salvar com executavel diferente, .ini antigo e substituido.
- [ ] Botao excluir solicita confirmacao, remove da lista e remove o .ini gerado do disco.
- [ ] Botao executar valida .exe, .eco e .ini gerado antes de iniciar o processo.
- [ ] Erros de pasta ausente (C:/ecosis/windows ou /dados) exibem mensagem descritiva.
- [ ] App executa com permissao de administrador (manifest requireAdministrator).
- [ ] eco.ini padrao nunca e modificado pelo EcoUtils em nenhum fluxo.
