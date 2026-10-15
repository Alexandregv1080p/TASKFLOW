package com.taskflow.mobile.data

import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

// Payload do evento BoardChanged (lido pelo Gson do cliente SignalR).
class BoardChange(val projectId: String? = null, val taskId: String? = null)

sealed interface LiveEvent {
    data class BoardChanged(val projectId: String) : LiveEvent
    data object Notified : LiveEvent
    // A conexão (re)abriu: avisos podem ter se perdido enquanto estava fora, então quem escuta recarrega.
    data object Connected : LiveEvent
}

// Tempo real pelo mesmo hub do site (/hubs/board). Os avisos não trazem dados: as telas recarregam pela API.
// Fica conectado enquanto houver sessão e tenta de novo a cada 5 s se a conexão cair.
// ponytail: a conexão continua aberta com o app em segundo plano enquanto o processo vive; para economizar
// bateria, ligar/desligar com o ciclo de vida do app (ProcessLifecycleOwner).
class LiveUpdates(private val hubUrl: String, private val session: SessionManager, private val scope: CoroutineScope) {
    private val events = MutableSharedFlow<LiveEvent>(extraBufferCapacity = 32)
    val updates = events.asSharedFlow()

    private val isConnected = MutableStateFlow(false)
    val connected = isConnected.asStateFlow()

    @Volatile private var connection: HubConnection? = null

    // Projetos abertos em alguma tela, com quantas telas cada um (quadro e detalhe da tarefa olham o mesmo).
    private val watched = mutableMapOf<String, Int>()

    fun start() {
        scope.launch {
            session.session.collectLatest { if (it != null) keepConnected() }
        }
    }

    fun watch(projectId: String) {
        val first = synchronized(watched) { (watched.merge(projectId, 1, Int::plus) ?: 1) == 1 }
        if (first) invoke("JoinProject", projectId)
    }

    fun unwatch(projectId: String) {
        val last = synchronized(watched) {
            val count = (watched[projectId] ?: return) - 1
            if (count == 0) watched.remove(projectId) else watched[projectId] = count
            count == 0
        }
        if (last) invoke("LeaveProject", projectId)
    }

    private fun invoke(method: String, projectId: String) {
        val current = connection ?: return // sem conexão: entra no grupo quando conectar
        scope.launch(Dispatchers.IO) { runCatching { current.invoke(method, projectId).blockingAwait() } }
    }

    private suspend fun keepConnected() {
        while (currentCoroutineContext().isActive) {
            val hub = HubConnectionBuilder.create(hubUrl)
                .withAccessTokenProvider(Single.defer { Single.just(session.token.orEmpty()) })
                .build()
            val closed = CompletableDeferred<Unit>()

            hub.on("BoardChanged", { change: BoardChange ->
                change.projectId?.let { events.tryEmit(LiveEvent.BoardChanged(it)) }
            }, BoardChange::class.java)
            hub.on("Notified", { _: Any -> events.tryEmit(LiveEvent.Notified) }, Any::class.java)
            hub.onClosed { closed.complete(Unit) }

            try {
                withContext(Dispatchers.IO) {
                    hub.start().blockingAwait()
                    synchronized(watched) { watched.keys.toList() }.forEach { runCatching { hub.invoke("JoinProject", it).blockingAwait() } }
                }
                connection = hub
                isConnected.value = true
                events.tryEmit(LiveEvent.Connected)
                closed.await()
            } catch (e: CancellationException) {
                throw e
            } catch (_: Exception) {
                // API fora do ar ou sem rede: tenta de novo abaixo.
            } finally {
                isConnected.value = false
                connection = null
                withContext(NonCancellable + Dispatchers.IO) { runCatching { hub.stop().blockingAwait() } }
            }

            delay(5_000)
        }
    }
}
