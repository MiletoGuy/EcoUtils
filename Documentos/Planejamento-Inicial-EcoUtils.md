# Planejamento Inicial de Funcionalidades - EcoUtils

## 1. Objetivo
Este documento define o planejamento das primeiras funcionalidades do EcoUtils:
- Operação sobre banco de dados ECO.
- Suporte ao fluxo de alteracao de versao do sistema ECO.
- Mapeamento de decisoes tecnicas pendentes antes da implementacao final.

## 2. Premissas Confirmadas

### 2.1 Banco de dados alvo
- O banco ECO sera sempre um arquivo com extensao .eco.
- Caminho padrao esperado: C:/ecosis/dados/
- O banco roda em Firebird 2.5.
- Credenciais padrao:
  - Usuario: SYSDBA
  - Senha: masterkey

### 2.2 Estrutura de executaveis do sistema ECO
- O sistema ECO sempre possui:
  - Um executavel .exe.
  - Um arquivo .ini com o mesmo nome base.
- O executavel considera o .ini de mesmo nome para suas configuracoes.

### 2.3 Padrao de nomenclatura de versao (CONFIRMADO)
- Formato: Eco_{versao}_{build}.exe / Eco_{versao}_{build}.ini
- Exemplo: Eco_650_10.exe e Eco_650_10.ini
- O campo "versao" e o numero principal do sistema. O campo "build" e o subindice incremental.
- Esse padrao e obrigatorio para que o EcoUtils reconheca e liste os arquivos corretamente.

## 3. Funcao 1 - Operacao sobre banco ECO

### 3.1 Escopo inicial sugerido
- Localizar automaticamente arquivos .eco no caminho C:/ecosis/dados/.
- Permitir selecao de um banco quando houver mais de um arquivo.
- Testar conexao Firebird com parametros configurados.
- Exibir status claro da conexao (sucesso, erro de acesso, erro de credencial, arquivo inexistente).

### 3.2 Estrategia tecnica
1. Validar se a pasta C:/ecosis/dados/ existe.
2. Listar arquivos .eco.
3. Montar string de conexao para Firebird 2.5.
4. Testar abertura e fechamento de conexao.
5. Registrar logs tecnicos para diagnostico.

### 3.3 Ponto de atencao
Firebird 2.5 pode ter variacoes de provider .NET e dependencia de cliente nativo instalado na maquina. Isso deve ser validado cedo para evitar retrabalho.

## 4. Funcao 2 - Gerenciamento de versoes (MVP)

### 4.1 Objetivo (MVP)
Permitir que o analista de suporte selecione qual versao do sistema ECO deseja executar, combinando um banco .eco e uma versao disponivel localmente.

### 4.2 Premissa do MVP
O download e a preparacao dos arquivos de versao SAO responsabilidade do usuario do app no MVP.
O fluxo esperado e:
1. O usuario baixa manualmente o par de arquivos da versao desejada.
2. O usuario os coloca em C:/ecosis/windows ja com o nome correto no padrao Eco_{versao}_{build}.
3. O EcoUtils le a pasta e lista as versoes disponiveis automaticamente.

### 4.3 Requisitos funcionais do MVP
- Ler a pasta C:/ecosis/windows e identificar todos os pares .exe/.ini validos no padrao Eco_{versao}_{build}.
- Listar as versoes encontradas para o usuario selecionar.
- Combinar selecao de banco .eco (C:/ecosis/dados/) com selecao de versao para abrir o sistema ECO.
- Validar que o par .exe/.ini existe e esta completo antes de permitir execucao.
- Se C:/ecosis/windows nao existir, exibir mensagem de erro indicativa (nao criar a pasta).

### 4.4 Fluxo funcional do MVP
1. EcoUtils inicia e valida C:/ecosis/windows e C:/ecosis/dados/.
2. EcoUtils lista bancos .eco disponiveis.
3. EcoUtils lista versoes disponiveis (pares .exe/.ini validos no padrao correto).
4. Usuario seleciona banco + versao.
5. EcoUtils valida o par selecionado.
6. EcoUtils lanca o executavel com o banco como parametro (ou mecanismo compativel com o ECO).

### 4.5 FUTURO - Download automatico de versoes
A funcionalidade de download automatico (acesso a pagina web, login, download de pacotes, descompactacao e deploy em C:/ecosis/windows) esta prevista para implementacao futura.
Decisoes relacionadas a essa funcionalidade (mecanismo de login, formato dos pacotes, seguranca de credenciais e rollback de deploy) serao definidas nessa fase.

## 5. Arquitetura sugerida para o EcoUtils

### 5.1 Modulos do MVP
- DatabaseService:
  - Descoberta e listagem de bancos .eco em C:/ecosis/dados/.
  - Teste de conexao Firebird 2.5 com credenciais configuradas.
- VersionCatalogService:
  - Leitura de C:/ecosis/windows.
  - Identificacao de pares .exe/.ini validos no padrao Eco_{versao}_{build}.
  - Listagem de versoes para a interface.
- LaunchService:
  - Validacao do par selecionado antes de executar.
  - Inicializacao do executavel ECO com o banco selecionado.
- SettingsService:
  - Leitura e escrita das configuracoes editaveis (credenciais Firebird, etc.).

### 5.2 Modulos reservados para implementacao futura
- AuthAndDownloadService (login web + download de versoes).
- PackageService (descompactacao e validacao de pacotes).
- VersionDeploymentService (copia, renomeacao e rollback de deploy).

### 5.3 Estruturas de apoio
- Pasta de versoes: C:/ecosis/windows (deve existir previamente).
- Pasta de bancos: C:/ecosis/dados/.
- Arquivo de log do app: C:/ecosis/logs/ecoutils.log.
- Arquivo de configuracoes do app: appdata local do usuario (não em C:/ecosis para evitar necessidade de admin para config).

## 6. Registro de decisoes

### DECISAO 1 - Fonte web e mecanismo de login
Status: ADIADO PARA FASE FUTURA.
No MVP o usuario realiza o download manualmente e posiciona os arquivos na pasta correta.

### DECISAO 2 - Formato dos arquivos de versao
Status: ADIADO PARA FASE FUTURA.
Sera definido quando a funcionalidade de download automatico for implementada.

### DECISAO 3 - Padrao de nomenclatura de versao
Status: CONFIRMADO.
Padrao: Eco_{versao}_{build}.exe e Eco_{versao}_{build}.ini
Exemplo: Eco_650_10.exe + Eco_650_10.ini
O EcoUtils usara esse padrao para identificar e listar versoes validas em C:/ecosis/windows.
Arquivos que nao seguirem esse padrao serao ignorados.

### DECISAO 4 - Selecao de banco .eco
Status: CONFIRMADO.
A selecao do banco sera uma funcionalidade central do app.
O perfil de usuario e o de analista de suporte, que pode ter varios bancos para testes e cenarios distintos.
O fluxo principal sera: selecionar banco + selecionar versao para abrir o sistema ECO.
Nao ha banco padrao automatico; a escolha e sempre explicita pelo usuario.

### DECISAO 5 - Credenciais Firebird
Status: CONFIRMADO.
As credenciais (usuario e senha do Firebird) serao editaveis na tela de configuracoes do EcoUtils.
Os valores padrao sao SYSDBA / masterkey e na pratica sera desnecessario alterar.
As configuracoes serao armazenadas localmente no perfil do usuario do Windows.

### DECISAO 6 - Politica de seguranca de credenciais web
Status: ADIADO PARA FASE FUTURA.
Sera definido junto com a funcionalidade de download automatico.

### DECISAO 7 - Politica de rollback
Status: NAO APLICAVEL AO MVP. REVISAO NECESSARIA NA FASE FUTURA.
No MVP o EcoUtils nao realiza deploy de arquivos, apenas le versoes ja existentes em C:/ecosis/windows.
Como todos os pares de versao coexistem na pasta simultaneamente, o "rollback" no MVP e simplesmente
selecionar uma versao anterior na lista — nao ha operacao tecnica a desfazer.
Quando o download automatico for implementado, a politica de rollback de deploy precisara ser definida:
- Desfazer copia em caso de falha durante o deploy.
- Registrar historico de versoes instaladas automaticamente.
- Permitir retorno para versao anterior em caso de problema.

### DECISAO 8 - Compatibilidade operacional
Status: CONFIRMADO.
- O EcoUtils DEVE rodar sempre como administrador (manifest de execucao com requireAdministrator).
- C:/ecosis/windows deve existir previamente pois e a instalacao do sistema ECO.
- Se a pasta nao for encontrada ao iniciar, o EcoUtils exibe mensagem de erro indicativa e nao permite prosseguir.
- O EcoUtils NAO cria a pasta C:/ecosis/windows automaticamente.

## 7. Proximos passos

### 7.1 Para o MVP
1. Configurar manifest de execucao como requireAdministrator no projeto.
2. Implementar DatabaseService: descoberta de bancos .eco e teste de conexao Firebird 2.5.
3. Implementar VersionCatalogService: leitura de C:/ecosis/windows e listagem de pares validos no padrao Eco_{versao}_{build}.
4. Implementar tela principal: selecao de banco + selecao de versao.
5. Implementar LaunchService: validacao e execucao do ECO com o banco selecionado.
6. Implementar tela de configuracoes: edicao de credenciais Firebird.
7. Validar comportamento quando C:/ecosis/windows ou C:/ecosis/dados/ nao existem.

### 7.2 Para fase futura
1. Definir mecanismo de login da pagina web (apos entendimento tecnico do endpoint).
2. Implementar AuthAndDownloadService.
3. Implementar PackageService.
4. Implementar VersionDeploymentService com politica de rollback a ser definida.
5. Definir politica de seguranca para armazenamento de credenciais web.
