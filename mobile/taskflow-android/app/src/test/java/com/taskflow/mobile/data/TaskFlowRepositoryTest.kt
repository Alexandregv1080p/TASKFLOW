package com.taskflow.mobile.data

import kotlinx.coroutines.test.runTest
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.fail
import org.junit.Before
import org.junit.Test

// A API de verdade substituída por um servidor local que responde o que o teste mandar.
class TaskFlowRepositoryTest {
    private val server = MockWebServer()
    private val storage = MemoryStorage()
    private lateinit var session: SessionManager
    private lateinit var repository: TaskFlowRepository

    private class MemoryStorage : SessionStorage {
        var saved: Session? = null
        override fun load() = saved
        override fun save(session: Session?) {
            saved = session
        }
    }

    @Before
    fun setUp() {
        server.start()
        session = SessionManager(storage)
        repository = TaskFlowRepository.create(server.url("/").toString(), session)
    }

    @After
    fun tearDown() = server.shutdown()

    private fun json(code: Int, body: String) = MockResponse().setResponseCode(code).setHeader("Content-Type", "application/json").setBody(body)

    private suspend fun expectError(block: suspend () -> Unit): ApiException {
        try {
            block()
        } catch (e: ApiException) {
            return e
        }
        fail("Era esperado um ApiException")
        throw AssertionError()
    }

    @Test
    fun `login guarda a sessao e as proximas chamadas levam o token`() = runTest {
        server.enqueue(json(200, """{"accessToken":"jwt-123","expiresAt":"2026-10-15T12:00:00Z","user":{"id":"u1","name":"Ana","email":"ana@x.test"}}"""))
        server.enqueue(json(200, """[{"id":"p1","name":"Website","ownerId":"u1","taskCount":3,"doneCount":1,"extra":"ignorado"}]"""))

        repository.login(" ana@x.test ", "senha-segura")
        val projects = repository.projects()

        assertEquals(Session("jwt-123", "u1", "Ana"), storage.saved)
        val login = server.takeRequest()
        assertNull(login.getHeader("Authorization"))
        assertEquals("""{"email":"ana@x.test","password":"senha-segura"}""", login.body.readUtf8())

        val list = server.takeRequest()
        assertEquals("/api/projects?scope=All", list.path)
        assertEquals("Bearer jwt-123", list.getHeader("Authorization"))
        assertEquals("Website", projects.single().name)
    }

    @Test
    fun `login com senha errada vira mensagem em portugues`() = runTest {
        server.enqueue(json(401, """{"status":401,"title":"Unauthorized","message":"Invalid email or password."}"""))

        val error = expectError { repository.login("ana@x.test", "errada") }

        assertEquals(401, error.code)
        assertEquals("E-mail ou senha inválidos.", error.message)
        assertNull(session.token)
    }

    @Test
    fun `token expirado encerra a sessao`() = runTest {
        session.signIn(Session("vencido", "u1", "Ana"))
        server.enqueue(json(401, """{"status":401,"message":"Authentication is required."}"""))

        expectError { repository.notifications() }

        assertNull(session.session.value)
        assertNull(storage.saved)
    }

    @Test
    fun `erro da API usa a mensagem do corpo e sem rede vira aviso de conexao`() = runTest {
        session.signIn(Session("jwt", "u1", "Ana"))
        server.enqueue(json(400, """{"status":400,"title":"Validation Error","message":"This project is archived."}"""))
        assertEquals("This project is archived.", expectError { repository.changeStatus("t1", TaskStatus.Done) }.message)
        assertEquals("""{"status":"Done"}""", server.takeRequest().body.readUtf8())

        server.enqueue(json(404, """{"status":404,"message":"TaskItem was not found."}"""))
        assertEquals(404, expectError { repository.task("t1") }.code)

        server.shutdown()
        assertEquals(0, expectError { repository.projects() }.code)
    }

    @Test
    fun `tipos do projeto e mudanca de tarefa-pai`() = runTest {
        session.signIn(Session("jwt", "u1", "Ana"))
        server.enqueue(json(200, """[{"id":"ty1","name":"Bug","color":"#C9372C"}]"""))
        val task = """{"id":"t2","projectId":"p1","number":2,"title":"B","status":"Todo","priority":"Low","createdAt":"2026-10-16T10:00:00Z","typeId":"ty1","parentId":"t1"}"""
        server.enqueue(json(200, task))
        server.enqueue(json(200, task.replace("\"parentId\":\"t1\"", "\"parentId\":null")))

        assertEquals("Bug", repository.taskTypes("p1").single().name)
        assertEquals("/api/projects/p1/task-types", server.takeRequest().path)

        val moved = repository.changeParent("t2", "t1")
        assertEquals("t1", moved.parentId)
        assertEquals("ty1", moved.typeId)
        val request = server.takeRequest()
        assertEquals("/api/tasks/t2/parent", request.path)
        assertEquals("""{"parentId":"t1"}""", request.body.readUtf8())

        // Voltar a ser tarefa comum: parentId nulo some do JSON, e a API entende como "sem pai".
        assertNull(repository.changeParent("t2", null).parentId)
        assertEquals("{}", server.takeRequest().body.readUtf8())
    }

    @Test
    fun `remover responsavel manda o corpo sem userId`() = runTest {
        session.signIn(Session("jwt", "u1", "Ana"))
        server.enqueue(
            json(200, """{"id":"t1","projectId":"p1","number":1,"title":"A","status":"Todo","priority":"Low","createdAt":"2026-10-15T10:00:00Z"}"""),
        )

        repository.assign("t1", null)

        val request = server.takeRequest()
        assertEquals("PATCH", request.method)
        assertEquals("{}", request.body.readUtf8())
    }
}
