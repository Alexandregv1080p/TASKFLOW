---
name: frontend-web-testing-setup
description: Como rodar e onde ficam os testes do frontend web (taskflow-web), setup inicial de Vitest
metadata:
  type: project
---

O frontend web (`frontend/taskflow-web`) não tinha nenhum framework de teste até 2026-10-16. Setup mínimo adicionado: apenas `vitest` como devDependency (sem jsdom/@testing-library, pois os únicos testes existentes cobrem `src/utils/format.ts`, que é TS puro sem DOM).

**Why:** o app Android irmão (`mobile/taskflow-android`) já tinha testes JUnit para a função equivalente `projectKey` em `Format.kt`; pedido foi trazer paridade de cobertura para `src/utils/format.ts` no web sem over-engineering (sem instalar dependências de DOM que ainda não serão usadas).

**How to apply:**
- Rodar testes: `npm test` (= `vitest run`) ou `npm run test:watch` dentro de `frontend/taskflow-web`.
- Config do Vitest vive dentro do próprio `vite.config.ts` (bloco `test: { environment: 'node' }`), não há `vitest.config.ts` separado. Precisa da linha `/// <reference types="vitest/config" />` no topo do `vite.config.ts` para o `tsc -b` (que type-checa esse arquivo via `tsconfig.node.json`) não reclamar do campo `test`.
- Testes de datas relativas a "hoje" (`dateGroup`, `formatRelativeDay`, `isOverdue`) usam `vi.useFakeTimers()` + `vi.setSystemTime(...)` para determinismo — ver `src/utils/format.test.ts` como referência de padrão.
- Se quiser testar componentes React/hooks no futuro, aí sim precisará adicionar `jsdom` + `@testing-library/react` — ainda não instalado, fora de escopo da fundação inicial.
- Ver [[npm-edgesout-bug]] se `npm install -D <algo>` falhar ao adicionar a próxima dependência de teste.
