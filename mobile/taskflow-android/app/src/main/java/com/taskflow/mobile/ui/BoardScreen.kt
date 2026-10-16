package com.taskflow.mobile.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.PrimaryTabRow
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Tab
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewModelScope
import com.taskflow.mobile.data.LiveEvent
import com.taskflow.mobile.data.LiveUpdates
import com.taskflow.mobile.data.Member
import com.taskflow.mobile.data.Project
import com.taskflow.mobile.data.Task
import com.taskflow.mobile.data.TaskFlowRepository
import com.taskflow.mobile.data.TaskPriority
import com.taskflow.mobile.data.TaskStatus
import com.taskflow.mobile.data.TaskType
import com.taskflow.mobile.domain.label
import com.taskflow.mobile.domain.moves
import com.taskflow.mobile.domain.taskKey
import com.taskflow.mobile.ui.theme.Done
import com.taskflow.mobile.ui.theme.High
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach

data class Board(val project: Project, val tasks: List<Task>, val members: List<Member>, val types: List<TaskType>)

// Quadro de um projeto: recarrega sozinho quando alguém muda algo nele (tempo real).
class BoardViewModel(
    private val repository: TaskFlowRepository,
    private val live: LiveUpdates,
    private val projectId: String,
) : LoadViewModel<Board>() {
    init {
        reload()
        live.watch(projectId)
        live.updates
            .onEach { if (it == LiveEvent.Connected || (it is LiveEvent.BoardChanged && it.projectId == projectId)) reload() }
            .launchIn(viewModelScope)
    }

    override suspend fun load() = coroutineScope {
        val project = async { repository.project(projectId) }
        val tasks = async { repository.tasks(projectId) }
        val members = async { repository.members(projectId) }
        val types = async { repository.taskTypes(projectId) }
        Board(project.await(), tasks.await(), members.await(), types.await())
    }

    fun move(task: Task, status: TaskStatus) = act { repository.changeStatus(task.id, status) }

    override fun onCleared() = live.unwatch(projectId)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BoardScreen(
    viewModel: BoardViewModel,
    live: LiveUpdates,
    currentUserId: String,
    onBack: () -> Unit,
    openTask: (String) -> Unit,
    openTypes: () -> Unit,
) {
    val state by viewModel.state.collectAsState()
    val refreshing by viewModel.refreshing.collectAsState()
    val message by viewModel.message.collectAsState()
    val connected by live.connected.collectAsState()
    var tab by rememberSaveable { mutableIntStateOf(0) }
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(message) {
        message?.let {
            snackbar.showSnackbar(it)
            viewModel.message.value = null
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text((state as? Load.Ready)?.data?.project?.name ?: "Quadro", maxLines = 1) },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar") }
                },
                actions = {
                    // Só o dono gerencia os tipos (a API responde 403 para os outros).
                    val project = (state as? Load.Ready)?.data?.project
                    if (project != null && project.ownerId == currentUserId && project.archivedAt == null) {
                        TextButton(onClick = openTypes) { Text("Tipos") }
                    }
                    if (connected) Text("● Ao vivo", color = Done, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(end = 16.dp))
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbar) },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = refreshing,
            onRefresh = { viewModel.reload(userAsked = true) },
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            LoadContent(state, onRetry = { viewModel.reload() }) { board ->
                val readOnly = board.project.archivedAt != null
                val status = TaskStatus.entries[tab]
                val column = board.tasks.filter { it.status == status }

                Column(Modifier.fillMaxSize()) {
                    // No celular, uma coluna por vez em abas; mover é pelos botões do card.
                    PrimaryTabRow(selectedTabIndex = tab) {
                        TaskStatus.entries.forEachIndexed { i, s ->
                            Tab(
                                selected = tab == i,
                                onClick = { tab = i },
                                unselectedContentColor = MaterialTheme.colorScheme.onSurfaceVariant,
                                text = {
                                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                                        Text(s.label, maxLines = 1)
                                        Text("${board.tasks.count { it.status == s }}", style = MaterialTheme.typography.labelSmall)
                                    }
                                },
                            )
                        }
                    }

                    if (readOnly) {
                        Text(
                            "Este projeto está arquivado e é somente leitura.",
                            modifier = Modifier.padding(16.dp),
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }

                    if (column.isEmpty()) {
                        EmptyMessage("Nenhuma tarefa em ${status.label}.")
                    } else {
                        LazyColumn(
                            Modifier.fillMaxSize(),
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(10.dp),
                        ) {
                            items(column, key = { it.id }) { task ->
                                TaskCard(
                                    task = task,
                                    key = taskKey(board.project.name, task.number),
                                    assignee = board.members.firstOrNull { it.id == task.assignedToId }?.name,
                                    type = board.types.firstOrNull { it.id == task.typeId },
                                    subtasks = board.tasks.filter { it.parentId == task.id },
                                    parentKey = task.parentId?.let { id -> board.tasks.firstOrNull { it.id == id } }?.let { taskKey(board.project.name, it.number) },
                                    readOnly = readOnly,
                                    onOpen = { openTask(task.id) },
                                    onMove = { viewModel.move(task, it) },
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun TaskCard(
    task: Task,
    key: String,
    assignee: String?,
    type: TaskType?,
    subtasks: List<Task>,
    parentKey: String?,
    readOnly: Boolean,
    onOpen: () -> Unit,
    onMove: (TaskStatus) -> Unit,
) {
    Card(
        onClick = onOpen,
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        border = BorderStroke(1.dp, MaterialTheme.colorScheme.outline),
    ) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text(task.title, style = MaterialTheme.typography.bodyLarge)
            if (type != null || parentKey != null || subtasks.isNotEmpty()) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    type?.let { TypeChip(it) }
                    parentKey?.let { Text("↳ $it", style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                    if (subtasks.isNotEmpty()) {
                        Text(
                            "${subtasks.count { it.status == TaskStatus.Done }}/${subtasks.size} subtarefas",
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
            }
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(key, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
                if (task.priority == TaskPriority.High) Text("Prioridade alta", style = MaterialTheme.typography.labelMedium, color = High)
                Text("", Modifier.weight(1f))
                assignee?.let { Avatar(it) }
            }
            if (!readOnly) {
                Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                    task.status.moves().forEach { target ->
                        val back = target.ordinal < task.status.ordinal
                        TextButton(onClick = { onMove(target) }) {
                            if (back) Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = null)
                            Text(if (back) target.label else "Mover para ${target.label}")
                            if (!back) Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = null)
                        }
                    }
                }
            }
        }
    }
}
