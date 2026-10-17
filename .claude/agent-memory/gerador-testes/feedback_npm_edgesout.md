---
name: npm-edgesout-bug
description: npm 10.8.2 falha ao instalar vitest (ou outros pacotes com peerDependencies opcionais complexas) neste ambiente Windows
metadata:
  type: feedback
---

`npm install -D vitest` (e provavelmente outros pacotes com muitas optional peerDependencies, como pacotes de browser testing) falha com `TypeError: Cannot read properties of null (reading 'edgesOut')` dentro do Arborist do npm 10.8.2, mesmo com `package-lock.json` saudável (`npm install` puro funciona normalmente).

**Why:** é um bug conhecido do Arborist do npm 10.8.2 ao resolver peer deps opcionais encadeadas (vitest declara várias: `@vitest/browser-playwright`, `@vitest/ui`, `jsdom`, `happy-dom`, etc). Não é problema do lockfile nem do projeto.

**How to apply:** se `npm install -D <pacote>` falhar com esse erro exato, tentar novamente com `--legacy-peer-deps` antes de investigar lockfile/node_modules corrompidos. Funcionou sem efeitos colaterais perceptíveis (instalação limpa, `tsc -b` e testes passaram normalmente depois).
