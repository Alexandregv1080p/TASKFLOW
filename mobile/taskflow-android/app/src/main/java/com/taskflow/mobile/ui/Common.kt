package com.taskflow.mobile.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.taskflow.mobile.data.ApiException
import com.taskflow.mobile.data.TaskStatus
import com.taskflow.mobile.data.TaskType
import com.taskflow.mobile.domain.hexColor
import androidx.compose.ui.graphics.Color
import com.taskflow.mobile.domain.initials
import com.taskflow.mobile.domain.label
import com.taskflow.mobile.ui.theme.Done
import com.taskflow.mobile.ui.theme.InProgress
import com.taskflow.mobile.ui.theme.Todo
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed interface Load<out T> {
    data object Loading : Load<Nothing>
    data class Failed(val message: String) : Load<Nothing>
    data class Ready<T>(val data: T) : Load<T>
}

// Base das telas: carrega, recarrega (puxar para atualizar ou aviso em tempo real) e executa ações.
// Falha ao recarregar com dados na tela não apaga a tela: vira uma mensagem.
abstract class LoadViewModel<T> : ViewModel() {
    private val current = MutableStateFlow<Load<T>>(Load.Loading)
    val state = current.asStateFlow()

    private val isRefreshing = MutableStateFlow(false)
    val refreshing = isRefreshing.asStateFlow()

    val message = MutableStateFlow<String?>(null)

    protected abstract suspend fun load(): T

    fun reload(userAsked: Boolean = false) {
        viewModelScope.launch {
            if (userAsked) isRefreshing.value = true
            try {
                current.value = Load.Ready(load())
            } catch (e: ApiException) {
                if (current.value is Load.Ready) message.value = e.message else current.value = Load.Failed(e.message.orEmpty())
            } finally {
                isRefreshing.value = false
            }
        }
    }

    protected fun act(action: suspend () -> Unit) {
        viewModelScope.launch {
            try {
                action()
                reload()
            } catch (e: ApiException) {
                message.value = e.message
            }
        }
    }
}

@Composable
fun <T> LoadContent(state: Load<T>, onRetry: () -> Unit, content: @Composable (T) -> Unit) {
    when (state) {
        Load.Loading -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) { CircularProgressIndicator() }
        is Load.Failed -> Column(
            Modifier.fillMaxSize().padding(32.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Text(state.message, textAlign = TextAlign.Center, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Button(onClick = onRetry, modifier = Modifier.padding(top = 16.dp)) { Text("Tentar de novo") }
        }
        is Load.Ready -> content(state.data)
    }
}

@Composable
fun EmptyMessage(text: String) {
    Text(
        text,
        modifier = Modifier.fillMaxSize().padding(32.dp),
        textAlign = TextAlign.Center,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
}

@Composable
fun Avatar(name: String, modifier: Modifier = Modifier) {
    Box(
        modifier.size(28.dp).clip(CircleShape).background(MaterialTheme.colorScheme.primaryContainer)
            .clearAndSetSemantics { },
        contentAlignment = Alignment.Center,
    ) {
        Text(initials(name), fontSize = 11.sp, fontWeight = FontWeight.SemiBold, color = MaterialTheme.colorScheme.onPrimaryContainer)
    }
}

// Selo do tipo da tarefa, na cor escolhida pelo dono do projeto.
@Composable
fun TypeChip(type: TaskType) {
    val color = hexColor(type.color)?.let { Color(it) } ?: MaterialTheme.colorScheme.primary
    Row(
        Modifier.clip(RoundedCornerShape(4.dp)).background(color.copy(alpha = 0.14f)).padding(horizontal = 6.dp, vertical = 2.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(type.name, fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = color)
    }
}

@Composable
fun StatusPill(status: TaskStatus) {
    val color = when (status) {
        TaskStatus.Todo -> Todo
        TaskStatus.InProgress -> InProgress
        TaskStatus.Done -> Done
    }
    Row(
        Modifier.clip(RoundedCornerShape(50)).background(color.copy(alpha = 0.16f)).padding(horizontal = 8.dp, vertical = 2.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(status.label, fontSize = 12.sp, fontWeight = FontWeight.SemiBold, color = color)
    }
}
