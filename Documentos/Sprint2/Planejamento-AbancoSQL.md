# Sprint 2 - Planejamento: Aba "Banco de Dados / SQL"

**Data:** 05/05/2026  
**Posição na sidebar:** Segunda aba, imediatamente após "Executar ECO"

---

## 1. Visão Geral

A segunda aba do EcoUtils será focada em operações sobre o banco de dados ECO (Firebird 2.5).  
Ela tem duas camadas funcionais:

- **Camada base** — executor SQL livre com visualizador de resultados (filtros, ordenação).
- **Camada principal** — biblioteca de SQLs prontas, voltada para o dia a dia da equipe de suporte do ECO.

A biblioteca de SQLs é o coração da aba. O executor livre é a fundação técnica e também uma funcionalidade acessível ao usuário avançado.

---

## 2. Funcionalidades Planejadas

### 2.1 Seleção de banco de dados ativo

- Antes de qualquer execução, o usuário deve selecionar um banco `.eco` como alvo.
- A aba deve exibir o banco ativo no momento (ex.: `PRD.eco — conectado`).
- Possibilidade de trocar o banco ativo sem sair da aba.
- Testar a conexão ao selecionar e exibir status claro (conectado / erro de acesso / arquivo não encontrado).

> **D1 — RESOLVIDO:** O banco ativo na aba SQL é **independente** do banco selecionado na aba "Executar ECO". Como integração futura, será adicionada ao menu de right-click de cada instância na aba "Executar ECO" a opção **"Abrir banco no SQL"**, que pré-seleciona aquele banco na aba Banco de Dados.

---

### 2.2 Biblioteca de SQLs (foco principal)

#### 2.2.1 SQLs built-in

- Conjunto de SQLs pré-definidas, distribuídas junto com o EcoUtils.
- Organizadas por **categorias** (ex.: Clientes, Fiscal, Estoque, Sistema, Permissões...).
- Cada SQL possui:
  - **Nome** — identificador curto e descritivo.
  - **Categoria** — agrupamento temático.
  - **Descrição** — explicação do que a query faz e quando usar.
  - **Corpo SQL** — a query em si.
  - **Lista de parâmetros** — cada parâmetro com nome, tipo esperado (texto, número, data) e descrição.
- SQLs built-in **não podem ser editadas diretamente** — edição cria uma cópia personalizada.

#### 2.2.2 SQLs personalizadas do usuário

- O usuário pode criar SQLs do zero e salvá-las localmente.
- O usuário pode "forkear" uma SQL built-in: cria uma cópia editável vinculada à original (ou independente).
- SQLs do usuário possuem os mesmos campos das built-in, mais a marcação de origem (criada do zero ou fork de qual built-in).
- Salvamento local — armazenado junto às configurações do usuário.

> **D2 — RESOLVIDO:** SQLs built-in serão **embutidas no assembly** como recurso embarcado (`Resources/built-in-queries.json`). Atualizações ao catálogo de SQLs acompanharão releases do EcoUtils.

> **D3 — RESOLVIDO:** SQLs personalizadas do usuário serão salvas em um **arquivo separado** `custom-queries.json` em `%AppData%\EcoUtils\` (ou equivalente ao caminho já usado pelo app para dados do usuário).

#### 2.2.3 Interface da biblioteca

```
+-----------------------------------------------------------+
|  [Busca: _____________]  [Filtro por categoria: ▼ Todas ] |
+-------------------+---------------------------------------+
|  LISTA DE SQLs    |  PAINEL DE DETALHE                    |
|                   |                                       |
|  > Fiscal         |  Nome: Buscar NF por chave            |
|    Buscar NF ...  |  Categoria: Fiscal                    |
|    Listar notas.. |  Descrição: Retorna dados da NF...    |
|  > Clientes       |                                       |
|    Buscar cliente |  ── Parâmetros ──────────────────     |
|    ...            |  Chave NF-e: [___________________]    |
|                   |                                       |
|  + Nova SQL       |  ── SQL ─────────────────────────     |
|                   |  SELECT ... FROM ...                  |
|                   |  WHERE CHAVE = :chave_nfe             |
|                   |                                       |
|                   |  [Editar]  [Executar]                 |
+-------------------+---------------------------------------+
```

- Painel esquerdo: lista de SQLs agrupadas por categoria, colapsável.
- Campo de busca filtra por nome, categoria e descrição em tempo real.
- Painel direito: detalhe da SQL selecionada, campos de parâmetros e botões de ação.
- Botão `+ Nova SQL` no rodapé da lista abre o editor para criação.

---

### 2.3 Editor de SQL

- Usado tanto para criação de novas SQLs quanto para edição de SQLs personalizadas.
- Ao editar uma SQL built-in, o editor se abre com a cópia e avisa que uma versão personalizada será criada.
- Campos editáveis: Nome, Categoria (selecionável + opção de nova categoria), Descrição, Corpo SQL, Parâmetros.
- Edição de parâmetros: adicionar/remover/reordenar parâmetros com nome, tipo e descrição.
- Botões: `Salvar`, `Cancelar`, `Executar direto` (salva e executa em sequência).

> **D4 — RESOLVIDO:** O editor de SQL terá **highlight de sintaxe SQL** via **AvalonEdit**. A dependência é aceita dado o ganho expressivo de usabilidade.

> **D5 — RESOLVIDO:** Parâmetros usarão a sintaxe **`:nome_param`** — padrão nativo do Firebird. Os parâmetros serão passados como parâmetros reais via ADO.NET (sem interpolação de string), o que elimina risco de SQL injection nas SQLs da biblioteca. No executor livre não há parâmetros — o usuário digita os valores diretamente na query.

---

### 2.4 Executor SQL livre

- Campo de texto livre para digitar qualquer SQL manualmente.
- Botão `Executar` (ou `Ctrl+Enter`).
- Não possui campos de parâmetros — o usuário digita os valores diretamente na query.
- O executor livre ficará em uma seção ou sub-aba separada dentro da aba principal, para não poluir a interface da biblioteca.

- O executor livre será acessível via **botão icon-only na toolbar da aba** (veðr seção 2.6 — Toolbar da aba).

> **D6 — RESOLVIDO:** O executor livre será aberto via **botão icon-only na toolbar** do header da aba, mantendo a área principal limpa para a biblioteca.

> **D7 — RESOLVIDO:** Seguindo o modelo do **IBExpert**, DDL e DML de escrita (`INSERT`, `UPDATE`, `DELETE`, `CREATE`, `ALTER`, `DROP`) exigem **commit explícito**. A toolbar da aba terá botões **Commit** e **Rollback**. A transação é aberta automaticamente quando o primeiro comando não-`SELECT` é executado e permanece aberta até o usuário confirmar ou cancelar. `SELECT` não abre transação e executa imediatamente.

---

### 2.5 Toolbar da aba

O header da aba "Banco de Dados / SQL" conterá uma **toolbar** com os seguintes controles:

```
+-------------------------------------------------------------------+
|  Banco de Dados / SQL                                             |
|  [PRD.eco ▼  conectado]   [Commit ✓]  [Rollback ↺]  [SQL Livre ⋮] |
+-------------------------------------------------------------------+
```

| Controle | Tipo | Descrição |
|---|---|---|
| Seletor de banco | ComboBox + indicador de status | Seleciona o banco ativo e exibe estado da conexão |
| **Commit** | Botão (habilitado somente com transação aberta) | Confirma as alterações pendentes na transação ativa |
| **Rollback** | Botão (habilitado somente com transação aberta) | Desfaz as alterações pendentes da transação ativa |
| **SQL Livre** | Botão icon-only | Abre o painel/flyout do executor SQL livre |

**Estado de transação:**
- `SELECT` executa fora de transação (auto-commit implícito ou leitura direta).
- Qualquer DDL ou DML de escrita abre uma transação explícita se não houver uma aberta.
- Enquanto há transação aberta, a toolbar exibe um indicador visual de "transação pendente" e os botões Commit/Rollback ficam habilitados.
- Trocar de banco com transação aberta deve alertar o usuário e exigir confirmação (com opção de fazer rollback automático).

---

### 2.6 Visualizador de resultados

- DataGrid exibindo os resultados da query executada (da biblioteca ou do executor livre).
- Colunas geradas dinamicamente conforme as colunas retornadas pela query.
- **Ordenação:** clique no cabeçalho de coluna ordena asc/desc.
- **Filtro rápido:** campo de busca que filtra as linhas já retornadas (filtro client-side, não re-executa a query).
- Exibe contagem de linhas retornadas.
- Exibe tempo de execução da query.
- Exibe aviso quando o limite de linhas for atingido, com opção de re-executar sem limite.
- Botões de exportação: **Copiar (CSV/TSV)** e **Exportar .csv**.

> **D8 — RESOLVIDO:** Limite de **1000 linhas** por padrão. Quando a query retornar mais linhas, uma mensagem de confirmação pergunta se o usuário deseja executar assim mesmo sem o limite. O valor do limite é **configurável pela tela de Configurações do EcoUtils**.

> **D9 — RESOLVIDO:** Exportação habilitada em dois formatos: **copiar para clipboard** (formato TSV/CSV) e **exportar arquivo `.csv`**.

---

## 3. Componentes Técnicos a Implementar

### 3.1 Modelos

| Modelo | Responsabilidade |
|---|---|
| `SqlEntry` | Representa uma SQL da biblioteca (built-in ou custom). Campos: Id, Nome, Categoria, Descrição, CorpoSql, Parâmetros, IsBuiltIn, OrigemForkId. |
| `SqlParameter` | Um parâmetro de uma SqlEntry: Nome, Tipo (string/int/date), Descrição. |
| `SqlExecutionResult` | Resultado de uma execução: colunas, linhas, tempo, erro se houver. |

### 3.2 Serviços

| Serviço | Responsabilidade |
|---|---|
| `SqlLibraryService` | Carrega SQLs built-in, gerencia SQLs do usuário (CRUD), salva/carrega do `custom-queries.json`. |
| `SqlExecutionService` | Executa uma query no banco ativo (via FirebirdClient), gerencia transação explícita (open/commit/rollback), retorna `SqlExecutionResult`. |
| `SqlExportService` | Serializa um `SqlExecutionResult` para CSV/TSV (clipboard ou arquivo). |

### 3.3 ViewModels

| ViewModel | Responsabilidade |
|---|---|
| `BancoDadosViewModel` | VM raiz da aba. Gerencia banco ativo, estado de transação (pendente/fechada), habilita/desabilita Commit e Rollback, coordena os sub-VMs. |
| `SqlLibraryViewModel` | Lista de SQLs filtrada, SQL selecionada, lógica de busca/filtro. |
| `SqlDetailViewModel` | Detalhe da SQL selecionada, instâncias de parâmetros preenchidas pelo usuário, dispara execução. |
| `SqlEditorViewModel` | Editor de SQL (nova ou edição), valida campos, salva via SqlLibraryService. |
| `SqlResultViewModel` | Encapsula o `SqlExecutionResult`, expõe colunas e linhas para o DataGrid, lógica de filtro/ordenação client-side, comandos de exportação. |
| `SqlLivreViewModel` | Texto livre da query, dispara execução via `SqlExecutionService`. |

### 3.4 Views

| View | Descrição |
|---|---|
| `BancoDadosView.xaml` | View raiz da aba, com seletor de banco ativo e área principal. |
| `SqlLibraryView.xaml` | Lista de SQLs + painel de detalhe. |
| `SqlDetailView.xaml` | Painel direito: nome, descrição, parâmetros, corpo SQL, botões. |
| `SqlEditorView.xaml` | Tela/flyout de criação e edição de SQLs. |
| `SqlResultView.xaml` | DataGrid de resultados com filtro e contadores. |
| `SqlLivreView.xaml` | Editor livre + botão executar. |

---

## 4. Armazenamento de Dados

### 4.1 SQLs built-in

- Distribuídas como recurso embutido no assembly (`Resources/built-in-queries.json`) **ou** arquivo externo.
- Estrutura JSON proposta:
```json
[
  {
    "id": "fiscal-buscar-nf-chave",
    "nome": "Buscar NF por chave",
    "categoria": "Fiscal",
    "descricao": "Retorna os dados principais de uma NF-e pela chave de acesso.",
    "sql": "SELECT ... FROM ECO_NF WHERE CHAVE_NFE = :chave_nfe",
    "parametros": [
      { "nome": "chave_nfe", "tipo": "string", "descricao": "Chave de acesso da NF-e (44 dígitos)" }
    ]
  }
]
```

### 4.2 SQLs do usuário

- Arquivo `custom-queries.json` em `%AppData%\EcoUtils\` (ou local equivalente já usado pelo app).
- Mesma estrutura JSON, com campos adicionais `isBuiltIn: false` e `forkDeId` (nullable).

---

## 5. Integração com a Estrutura Existente

- Adicionar `BancoDadosViewModel` ao `MainViewModel`, registrado junto às demais abas em `Abas`.
- Adicionar o item "Banco de Dados" à sidebar via `NavItem` com ícone adequado (banco de dados / cilindro).
- Registrar o `DataTemplate` no `MainWindow.xaml` para o mapeamento `BancoDadosViewModel → BancoDadosView`.
- Registrar os novos serviços no contêiner de DI em `App.xaml.cs`.
- O `SqlExecutionService` deve reutilizar o mecanismo de conexão Firebird já existente no app (verificar se `DatabaseDiscoveryService` ou algum serviço adjacente já provê a string de conexão montada).
- **Configurações:** adicionar configuração `LimiteLinhasConsulta` (int, padrão 1000) ao `UserSettings` e exposá-la na tela de Configurações.
- **Right-click na aba Executar ECO:** adicionar opção "Abrir banco no SQL" ao menu de contexto de cada instância. A ação navega para a aba Banco de Dados e pré-seleciona o banco `.eco` da instância clicada (este ponto será implementado como melhoria integrada, possivelmente em momento separado).

---

## 6. Decisões — Resumo

| ID | Questão | Decisão |
|---|---|---|
| D1 | Banco ativo compartilhado entre abas ou independente? | **Independente.** Right-click na aba "Executar ECO" abre banco no SQL. |
| D2 | SQLs built-in: embutidas no assembly ou arquivo externo? | **Embutidas no assembly** como recurso embarcado. |
| D3 | Onde salvar SQLs personalizadas do usuário? | **Arquivo separado** `custom-queries.json` em `%AppData%\EcoUtils\`. |
| D4 | Highlight de sintaxe SQL no editor? | **Sim — AvalonEdit.** |
| D5 | Sintaxe de parâmetros nos SQLs? | **`:nome_param`** — padrão Firebird nativo, via parâmetros ADO.NET. |
| D6 | Executor livre: onde fica? | **Botão icon-only na toolbar** do header da aba. |
| D7 | Restrição de comandos no executor livre? | **Modelo IBExpert:** DDL/DML exigem commit explícito. Toolbar com botões Commit e Rollback. |
| D8 | Limite de linhas retornadas? | **1000 linhas** (padrão). Confirmação para ultrapassar. Configurável em Configurações. |
| D9 | Exportação de resultados? | **Clipboard (TSV/CSV) e exportar `.csv`.** |

---

## 7. Ordem de Implementação Sugerida

1. **Infraestrutura de conexão** — garantir que `SqlExecutionService` consegue abrir, executar e fechar conexão Firebird com o banco selecionado. Validar com query simples (`SELECT CURRENT_TIMESTAMP FROM RDB$DATABASE`).
2. **Gestão de transação** — implementar abertura implícita, commit e rollback explícitos no `SqlExecutionService`. Expor estado de transação ao `BancoDadosViewModel`.
3. **Modelos e serviços base** — `SqlEntry`, `SqlParameter`, `SqlExecutionResult`, `SqlLibraryService`.
4. **Visualizador de resultados** — `SqlResultViewModel` + `SqlResultView` com DataGrid dinâmico, filtro, ordenação, contadores, aviso de limite e botões de exportação.
5. **Executor livre** — `SqlLivreView` + toolbar com Commit/Rollback/SQL Livre + `SqlResultView` integrado. Funcionalidade completa de ponta a ponta.
6. **Configuração de limite de linhas** — `LimiteLinhasConsulta` no `UserSettings` e campo na tela de Configurações.
7. **Biblioteca de SQLs built-in** — estrutura JSON embarcada com primeiras SQLs de suporte, `SqlLibraryService`, `SqlLibraryView` + `SqlDetailView`.
8. **Parâmetros** — formulário dinâmico de preenchimento de parâmetros no `SqlDetailView`, substituição via ADO.NET com `:nome_param`.
9. **Editor de SQLs** — criação e edição de SQLs personalizadas com AvalonEdit, fork de built-ins.
10. **Integração na sidebar** — `BancoDadosViewModel` em `MainViewModel`, `NavItem`, `DataTemplate`.
11. **Right-click "Abrir banco no SQL"** — menu de contexto na aba Executar ECO navegando para o banco correspondente.
12. **Polimento** — estados de loading, tratamento de erros de conexão/query, mensagens de erro amigáveis, indicador visual de transação pendente na toolbar.
