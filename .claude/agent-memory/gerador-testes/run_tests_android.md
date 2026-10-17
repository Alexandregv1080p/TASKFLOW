---
name: run-tests-android
description: Comando correto para rodar testes unitários JVM do módulo app em mobile/taskflow-android
metadata:
  type: project
---

Em `mobile/taskflow-android`, a task `./gradlew.bat test --tests "..."` falha com
`Unknown command-line option '--tests'` (a task agregadora `test` de app Android não
aceita essa flag diretamente). Use a task de variante concreta:

```
cd mobile/taskflow-android
./gradlew.bat :app:testDebugUnitTest --tests "com.taskflow.mobile.data.SessionTest"
```

**Why:** o plugin do Android Gradle (AGP) cria `testDebugUnitTest`/`testReleaseUnitTest`
por variante de build; a task `test` plain é só um agregador sem suporte a filtro de
`--tests` nesse setup.

**How to apply:** sempre que for rodar um teste específico neste repo (ou qualquer
app Android com AGP), prefira `:app:testDebugUnitTest --tests "pacote.Classe"` em vez
de `test --tests`. Testes ficam em `app/src/test/java/...`, mesmo pacote da classe
testada; convenção de estilo já usada no projeto: JUnit4, nomes de método em português
entre backticks, fakes de interfaces (ex. `SessionStorage`) implementados como classe
privada em memória dentro do próprio arquivo de teste.
