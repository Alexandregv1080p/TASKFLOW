package com.taskflow.mobile.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

// Fake em memória do storage, sem acesso a SharedPreferences/Android.
private class MemoryStorage(initial: Session? = null) : SessionStorage {
    var saved: Session? = initial
    override fun load() = saved
    override fun save(session: Session?) {
        saved = session
    }
}

class SessionTest {
    @Test
    fun `sem sessao salva, o estado inicial e nulo`() {
        val session = SessionManager(MemoryStorage())

        assertNull(session.session.value)
        assertNull(session.token)
    }

    @Test
    fun `sessao ja salva no storage e carregada no construtor`() {
        val saved = Session("jwt-abc", "u1", "Ana")
        val session = SessionManager(MemoryStorage(saved))

        assertEquals(saved, session.session.value)
        assertEquals("jwt-abc", session.token)
    }

    @Test
    fun `signIn salva no storage e atualiza o estado e o token`() {
        val storage = MemoryStorage()
        val session = SessionManager(storage)
        val novo = Session("jwt-novo", "u2", "Bia")

        session.signIn(novo)

        assertEquals(novo, storage.saved)
        assertEquals(novo, session.session.value)
        assertEquals("jwt-novo", session.token)
    }

    @Test
    fun `signOut limpa o storage e zera o estado e o token`() {
        val storage = MemoryStorage(Session("jwt-abc", "u1", "Ana"))
        val session = SessionManager(storage)

        session.signOut()

        assertNull(storage.saved)
        assertNull(session.session.value)
        assertNull(session.token)
    }

    @Test
    fun `duas instancias nao compartilham estado, cada uma le do seu storage`() {
        val storageA = MemoryStorage()
        val storageB = MemoryStorage()
        val sessionA = SessionManager(storageA)
        val sessionB = SessionManager(storageB)

        sessionA.signIn(Session("jwt-a", "u1", "Ana"))

        assertEquals("jwt-a", sessionA.token)
        assertNull(sessionB.token)
        assertNull(storageB.saved)
    }
}
