package com.taskflow.mobile.data

import android.content.Context
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

data class Session(val token: String, val userId: String, val userName: String)

// Onde a sessão fica guardada entre aberturas do app; nos testes, em memória.
interface SessionStorage {
    fun load(): Session?
    fun save(session: Session?)
}

// Sessão atual. Sem sessão o app mostra o login; um 401 da API também encerra a sessão.
class SessionManager(private val storage: SessionStorage) {
    private val current = MutableStateFlow(storage.load())
    val session: StateFlow<Session?> = current.asStateFlow()
    val token: String? get() = current.value?.token

    fun signIn(session: Session) {
        storage.save(session)
        current.value = session
    }

    fun signOut() {
        storage.save(null)
        current.value = null
    }
}

// ponytail: o token (JWT de 60 min, sem refresh) fica em SharedPreferences privadas, fora de backup
// (allowBackup=false). Se a API ganhar refresh token de longa duração, cifrá-lo com o Android Keystore.
class PrefsSessionStorage(context: Context) : SessionStorage {
    private val prefs = context.getSharedPreferences("session", Context.MODE_PRIVATE)

    override fun load(): Session? {
        val token = prefs.getString("token", null) ?: return null
        val userId = prefs.getString("userId", null) ?: return null
        return Session(token, userId, prefs.getString("userName", null).orEmpty())
    }

    override fun save(session: Session?) {
        prefs.edit().apply {
            if (session == null) clear()
            else putString("token", session.token).putString("userId", session.userId).putString("userName", session.userName)
        }.apply()
    }
}
