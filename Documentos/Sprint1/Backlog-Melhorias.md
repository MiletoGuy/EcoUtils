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

<!-- Novos itens serão adicionados abaixo conforme identificados durante a sprint -->
