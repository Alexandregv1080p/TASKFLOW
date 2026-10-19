package com.taskflow.mobile.ui

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.background
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.taskflow.mobile.data.AppNotification
import com.taskflow.mobile.data.ForYouItem
import com.taskflow.mobile.data.ForYouResponse
import com.taskflow.mobile.data.LiveEvent
import com.taskflow.mobile.data.LiveUpdates
import com.taskflow.mobile.data.NotificationList
import com.taskflow.mobile.data.NotificationType
import com.taskflow.mobile.data.ProjectSummary
import com.taskflow.mobile.data.TaskFlowRepository
import com.taskflow.mobile.domain.formatDateTime
import com.taskflow.mobile.domain.taskKey
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import androidx.lifecycle.viewModelScope

// Abas do "Para você" (as mesmas do site, menos Trabalhado e Visto).
enum class ForYouFilter(val label: String) { Recommended("Recomendado"), Assigned("Atribuído a mim"), Starred("Com estrela") }

class ForYouViewModel(private val repository: TaskFlowRepository, live: LiveUpdates) : LoadViewModel<ForYouResponse>() {
    val filter = MutableStateFlow(ForYouFilter.Recommended)

    init {
        reload()
        live.updates.onEach { if (it is LiveEvent.Connected) reload() }.launchIn(viewModelScope)
    }

    fun select(next: ForYouFilter) {
        filter.value = next
        reload()
    }

    override suspend fun load() = repository.forYou(filter.value.name)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ForYouScreen(viewModel: ForYouViewModel, openTask: (projectId: String, taskId: String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val filter by viewModel.filter.collectAsState()
    val refreshing by viewModel.refreshing.collectAsState()

    Column(Modifier.fillMaxSize()) {
        LazyRow(contentPadding = androidx.compose.foundation.layout.PaddingValues(horizontal = 16.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            items(ForYouFilter.entries) { option ->
                FilterChip(selected = option == filter, onClick = { viewModel.select(option) }, label = { Text(option.label) })
            }
        }

        PullToRefreshBox(isRefreshing = refreshing, onRefresh = { viewModel.reload(userAsked = true) }, modifier = Modifier.fillMaxSize()) {
            LoadContent(state, onRetry = { viewModel.reload() }) { data ->
                if (data.items.isEmpty()) {
                    EmptyMessage(
                        when (filter) {
                            ForYouFilter.Recommended -> "Nada pedindo sua atenção agora."
                            ForYouFilter.Assigned -> "Nenhuma tarefa aberta atribuída a você."
                            ForYouFilter.Starred -> "Você ainda não marcou nenhuma tarefa com estrela."
                        },
                    )
                } else {
                    LazyColumn(Modifier.fillMaxSize()) {
                        items(data.items, key = { it.taskId }) { item ->
                            ForYouRow(item) { openTask(item.projectId, item.taskId) }
                            HorizontalDivider(color = MaterialTheme.colorScheme.outline)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun ForYouRow(item: ForYouItem, onClick: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().clickable(onClick = onClick).padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Column(Modifier.weight(1f)) {
            Text(item.title, maxLines = 2, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.bodyLarge)
            Text(
                "${taskKey(item.projectName, item.number)} · ${item.projectName}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        StatusPill(item.status)
        item.assigneeName?.let { Avatar(it) }
    }
}

class ProjectsViewModel(private val repository: TaskFlowRepository) : LoadViewModel<List<ProjectSummary>>() {
    init {
        reload()
    }

    override suspend fun load() = repository.projects()
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProjectsScreen(viewModel: ProjectsViewModel, openProject: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val refreshing by viewModel.refreshing.collectAsState()

    PullToRefreshBox(isRefreshing = refreshing, onRefresh = { viewModel.reload(userAsked = true) }, modifier = Modifier.fillMaxSize()) {
        LoadContent(state, onRetry = { viewModel.reload() }) { projects ->
            if (projects.isEmpty()) {
                EmptyMessage("Você ainda não tem projetos. Crie o primeiro pelo site; ele aparece aqui na hora.")
            } else {
                LazyColumn(Modifier.fillMaxSize()) {
                    items(projects, key = { it.id }) { project ->
                        Column(
                            Modifier.fillMaxWidth().clickable { openProject(project.id) }.padding(horizontal = 16.dp, vertical = 14.dp),
                            verticalArrangement = Arrangement.spacedBy(6.dp),
                        ) {
                            Text(project.name, style = MaterialTheme.typography.titleMedium)
                            Text(
                                listOfNotNull(project.spaceName, "${project.doneCount} de ${project.taskCount} concluídas").joinToString(" · "),
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                            LinearProgressIndicator(
                                progress = { if (project.taskCount == 0) 0f else project.doneCount.toFloat() / project.taskCount },
                                modifier = Modifier.fillMaxWidth(),
                                // Barra contínua, igual à do site (sem o vão e o ponto final do Material 3).
                                gapSize = 0.dp,
                                drawStopIndicator = {},
                            )
                        }
                        HorizontalDivider(color = MaterialTheme.colorScheme.outline)
                    }
                }
            }
        }
    }
}

// Fica no nível da navegação: alimenta o contador da barra inferior e a aba de notificações.
class NotificationsViewModel(private val repository: TaskFlowRepository, live: LiveUpdates) : LoadViewModel<NotificationList>() {
    init {
        reload()
        live.updates.onEach { if (it is LiveEvent.Notified || it is LiveEvent.Connected) reload() }.launchIn(viewModelScope)
    }

    override suspend fun load() = repository.notifications()

    fun open(notification: AppNotification) {
        if (!notification.read) act { repository.markRead(notification.id) }
    }

    fun markAllRead() = act { repository.markAllRead() }
}

private val NotificationType.action: String
    get() = when (this) {
        NotificationType.Assigned -> "atribuiu a você"
        NotificationType.Mentioned -> "mencionou você em"
        NotificationType.Commented -> "comentou em"
    }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NotificationsScreen(viewModel: NotificationsViewModel, openTask: (projectId: String, taskId: String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val refreshing by viewModel.refreshing.collectAsState()

    PullToRefreshBox(isRefreshing = refreshing, onRefresh = { viewModel.reload(userAsked = true) }, modifier = Modifier.fillMaxSize()) {
        LoadContent(state, onRetry = { viewModel.reload() }) { data ->
            if (data.items.isEmpty()) {
                EmptyMessage("Nada por aqui ainda. Você recebe um aviso quando alguém atribui uma tarefa a você, menciona você com @ ou comenta em uma tarefa sua.")
            } else {
                LazyColumn(Modifier.fillMaxSize()) {
                    if (data.unreadCount > 0) {
                        item {
                            Box(Modifier.fillMaxWidth().padding(horizontal = 8.dp), contentAlignment = Alignment.CenterEnd) {
                                TextButton(onClick = viewModel::markAllRead) { Text("Marcar todas como lidas") }
                            }
                        }
                    }
                    items(data.items, key = { it.id }) { n ->
                        Row(
                            Modifier.fillMaxWidth()
                                .clickable {
                                    viewModel.open(n)
                                    openTask(n.projectId, n.taskId)
                                }
                                .padding(horizontal = 16.dp, vertical = 12.dp)
                                .semantics { if (!n.read) contentDescription = "Não lida" },
                            horizontalArrangement = Arrangement.spacedBy(12.dp),
                        ) {
                            Avatar(n.actorName)
                            Column(Modifier.weight(1f)) {
                                Text(
                                    "${n.actorName} ${n.type.action} ${taskKey(n.projectName, n.taskNumber)} ${n.taskTitle}",
                                    fontWeight = if (n.read) FontWeight.Normal else FontWeight.SemiBold,
                                    style = MaterialTheme.typography.bodyMedium,
                                )
                                Text(
                                    "${n.projectName} · ${formatDateTime(n.createdAt)}",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            }
                            if (!n.read) {
                                Spacer(Modifier.width(4.dp))
                                Box(Modifier.padding(top = 6.dp).size(8.dp).background(MaterialTheme.colorScheme.primary, CircleShape))
                            }
                        }
                        HorizontalDivider(color = MaterialTheme.colorScheme.outline)
                    }
                }
            }
        }
    }
}
