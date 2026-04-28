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
