package com.taskflow.mobile.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material3.AssistChip
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewModelScope
import com.taskflow.mobile.data.ApiException
import com.taskflow.mobile.data.Attachment
import com.taskflow.mobile.data.Comment
import com.taskflow.mobile.data.StorageUsage
import java.io.File
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import com.taskflow.mobile.data.LiveEvent
import com.taskflow.mobile.data.LiveUpdates
import com.taskflow.mobile.data.Member
import com.taskflow.mobile.data.Project
import com.taskflow.mobile.data.Task
import com.taskflow.mobile.data.TaskFlowRepository
import com.taskflow.mobile.data.TaskStatus
import com.taskflow.mobile.data.TaskType
import androidx.compose.foundation.clickable
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.ui.text.style.TextOverflow
import com.taskflow.mobile.domain.completeMention
import com.taskflow.mobile.domain.formatDateTime
import com.taskflow.mobile.domain.label
import com.taskflow.mobile.domain.mentionQuery
import com.taskflow.mobile.domain.mentionRanges
import com.taskflow.mobile.domain.moves
import com.taskflow.mobile.domain.taskKey
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach

// tasks: todas do projeto, para achar a tarefa-pai, as subtarefas e quem pode ser pai.
data class TaskDetail(
    val task: Task,
    val project: Project,
    val members: List<Member>,
    val comments: List<Comment>,
    val tasks: List<Task>,
    val types: List<TaskType>,
    val attachments: List<Attachment>,
    val storage: StorageUsage?,
)

class TaskViewModel(
    private val repository: TaskFlowRepository,
    private val live: LiveUpdates,
    private val projectId: String,
    private val taskId: String,
) : LoadViewModel<TaskDetail>() {
    init {
        reload()
        live.watch(projectId)
        live.updates
            .onEach { if (it == LiveEvent.Connected || (it is LiveEvent.BoardChanged && it.projectId == projectId)) reload() }
            .launchIn(viewModelScope)
    }

    override suspend fun load() = coroutineScope {
        val task = async { repository.task(taskId) }
        val project = async { repository.project(projectId) }
        val members = async { repository.members(projectId) }
        val comments = async { repository.comments(taskId) }
        val tasks = async { repository.tasks(projectId) }
        val types = async { repository.taskTypes(projectId) }
        val attachments = async { repository.attachments(taskId) }
        // Uso do espaço é só informativo: se falhar, a tela abre sem ele.
        val storage = async { runCatching { repository.storage(projectId) }.getOrNull() }
        TaskDetail(
            task.await(), project.await(), members.await(), comments.await(), tasks.await(), types.await(),
            attachments.await(), storage.await(),
        )
    }

    fun move(status: TaskStatus) = act { repository.changeStatus(taskId, status) }

    fun assign(userId: String?) = act { repository.assign(taskId, userId) }

    fun changeType(typeId: String?) = act { repository.changeType(taskId, typeId) }

    fun changeParent(parentId: String?) = act { repository.changeParent(taskId, parentId) }

    fun uploadAttachment(fileName: String, mimeType: String?, bytes: ByteArray) =
        act { repository.uploadAttachment(taskId, fileName, mimeType, bytes) }

    fun deleteAttachment(attachment: Attachment) = act { repository.deleteAttachment(attachment.id) }

    // Baixa para o cache do app (pasta própria por anexo, com o nome original) e entrega o arquivo para abrir.
    fun openAttachment(attachment: Attachment, cacheDir: File, onReady: (File) -> Unit) {
        viewModelScope.launch {
            try {
                val bytes = repository.downloadAttachment(attachment.id)
                val file = withContext(Dispatchers.IO) {
                    File(cacheDir, "attachments/${attachment.id}").apply { mkdirs() }.resolve(File(attachment.fileName).name)
                        .apply { writeBytes(bytes) }
                }
                onReady(file)
            } catch (e: ApiException) {
                message.value = e.message
            }
        }
    }

    fun createSubtask(parent: Task, title: String, onCreated: () -> Unit) = act {
        repository.createSubtask(parent, title)
        onCreated()
    }

    fun comment(body: String, onSent: () -> Unit) = act {
        repository.addComment(taskId, body)
        onSent()
    }

    override fun onCleared() = live.unwatch(projectId)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TaskScreen(viewModel: TaskViewModel, currentUserId: String, onBack: () -> Unit, openTask: (String) -> Unit) {
    val state by viewModel.state.collectAsState()
    val message by viewModel.message.collectAsState()
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(message) {
        message?.let {
            snackbar.showSnackbar(it)
            viewModel.message.value = null
        }
    }

    Scaffold(
        topBar = {
            val detail = (state as? Load.Ready)?.data
            TopAppBar(
                title = { Text(detail?.let { taskKey(it.project.name, it.task.number) } ?: "Tarefa") },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar") }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbar) },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding).imePadding()) {
            LoadContent(state, onRetry = { viewModel.reload() }) { detail ->
                TaskContent(detail, currentUserId, viewModel, openTask)
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
private fun TaskContent(detail: TaskDetail, currentUserId: String, viewModel: TaskViewModel, openTask: (String) -> Unit) {
    val task = detail.task
    val keyOf = { t: Task -> taskKey(detail.project.name, t.number) }
    val parent = detail.tasks.firstOrNull { it.id == task.parentId }
    val subtasks = detail.tasks.filter { it.parentId == task.id }
    val readOnly = detail.project.archivedAt != null || detail.members.any { it.id == currentUserId && it.isViewer }
    val nameOf = { id: String? -> detail.members.firstOrNull { it.id == id }?.name }

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text(task.title, style = MaterialTheme.typography.headlineSmall)
        Text(detail.project.name, color = MaterialTheme.colorScheme.onSurfaceVariant)

        parent?.let {
            TextButton(onClick = { openTask(it.id) }) { Text("↳ Subtarefa de ${keyOf(it)} ${it.title}") }
        }

        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            StatusPill(task.status)
            detail.types.firstOrNull { it.id == task.typeId }?.let { TypeChip(it) }
            Text("Prioridade ${task.priority.label.lowercase()}", style = MaterialTheme.typography.bodyMedium)
        }

        if (!readOnly) {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                task.status.moves().forEach { target ->
                    OutlinedButton(onClick = { viewModel.move(target) }) { Text("Mover para ${target.label}") }
                }
            }
        }

        PickerField(
            "Responsável",
            listOf(null to "Sem responsável") + detail.members.filter { !it.isViewer }.map { it.id to it.name },
            task.assignedToId,
            enabled = !readOnly,
            onPick = viewModel::assign,
        )
        if (!readOnly && task.assignedToId != currentUserId) {
            TextButton(onClick = { viewModel.assign(currentUserId) }) { Text("Atribuir a mim") }
        }

        PickerField(
            "Tipo",
            listOf(null to "Sem tipo") + detail.types.map { it.id to it.name },
            task.typeId,
            enabled = !readOnly,
            onPick = viewModel::changeType,
        )

        // Um nível só: quem tem subtarefas não vira subtarefa, e subtarefa não é pai.
        PickerField(
            "Tarefa-pai",
            listOf(null to "Nenhuma") + detail.tasks.filter { it.id != task.id && it.parentId == null }.map { it.id to "${keyOf(it)} ${it.title}" },
            task.parentId,
            enabled = !readOnly && subtasks.isEmpty(),
            onPick = viewModel::changeParent,
            supporting = if (subtasks.isNotEmpty()) "Tem subtarefas, então não pode virar subtarefa." else null,
        )

        task.dueDate?.let { Text("Data limite: ${it.take(10).split('-').reversed().joinToString("/")}") }

        Section("Descrição") {
            Text(task.description ?: "Sem descrição.", color = if (task.description == null) MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.onSurface)
        }

        Section("Anexos") {
            AttachmentsSection(
                attachments = detail.attachments,
                usage = detail.storage,
                members = detail.members,
                currentUserId = currentUserId,
                isProjectOwner = detail.project.ownerId == currentUserId,
                readOnly = readOnly,
                onUpload = viewModel::uploadAttachment,
                onOpen = viewModel::openAttachment,
                onDelete = viewModel::deleteAttachment,
                onError = { viewModel.message.value = it },
            )
        }

        // Um nível só: subtarefa não tem a seção.
        if (task.parentId == null && (subtasks.isNotEmpty() || !readOnly)) {
            Section("Subtarefas") {
                if (subtasks.isNotEmpty()) {
                    val done = subtasks.count { it.status == TaskStatus.Done }
                    Text("$done de ${subtasks.size} concluídas", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    LinearProgressIndicator(progress = { done.toFloat() / subtasks.size }, modifier = Modifier.fillMaxWidth())
                }
                subtasks.forEach { sub ->
                    Row(
                        Modifier.fillMaxWidth().clickable { openTask(sub.id) }.padding(vertical = 8.dp),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        Text(keyOf(sub), style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
                        Text(sub.title, modifier = Modifier.weight(1f), maxLines = 1, overflow = TextOverflow.Ellipsis)
                        StatusPill(sub.status)
                    }
                    HorizontalDivider(color = MaterialTheme.colorScheme.outline)
                }
                if (!readOnly) SubtaskComposer { title, onCreated -> viewModel.createSubtask(task, title, onCreated) }
            }
        }

        Section("Comentários") {
            if (!readOnly) CommentComposer(detail.members.filter { it.id != currentUserId }, viewModel)

            if (detail.comments.isEmpty()) {
                Text("Nenhum comentário ainda.", color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            val names = detail.members.map { it.name }
            detail.comments.forEach { comment ->
                val author = nameOf(comment.authorId) ?: "Alguém"
                Row(horizontalArrangement = Arrangement.spacedBy(10.dp), modifier = Modifier.padding(vertical = 6.dp)) {
                    Avatar(author)
                    Column {
                        Text("$author · ${formatDateTime(comment.createdAt)}", style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
                        Text(withMentions(comment.body, names))
                    }
                }
                HorizontalDivider(color = MaterialTheme.colorScheme.outline)
            }
        }
    }
}

@Composable
private fun Section(title: String, content: @Composable () -> Unit) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        content()
    }
}

@Composable
private fun withMentions(body: String, names: List<String>) = buildAnnotatedString {
    var last = 0
    mentionRanges(body, names).forEach { range ->
        append(body.substring(last, range.first))
        withStyle(SpanStyle(color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.SemiBold)) { append(body.substring(range)) }
        last = range.last + 1
    }
    append(body.substring(last))
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
// Campo de escolha (responsável, tipo, tarefa-pai). options: (id ou null, texto); a primeira é a opção "vazia".
private fun PickerField(
    label: String,
    options: List<Pair<String?, String>>,
    selected: String?,
    enabled: Boolean,
    onPick: (String?) -> Unit,
    supporting: String? = null,
) {
    var expanded by remember { mutableStateOf(false) }
    val current = options.firstOrNull { it.first == selected }?.second ?: options.first().second

    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { if (enabled) expanded = it }) {
        OutlinedTextField(
            value = current,
            onValueChange = {},
            readOnly = true,
            enabled = enabled,
            label = { Text(label) },
            supportingText = supporting?.let { { Text(it) } },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
            modifier = Modifier.fillMaxWidth().menuAnchor(MenuAnchorType.PrimaryNotEditable),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { (id, text) ->
                DropdownMenuItem(text = { Text(text) }, onClick = { expanded = false; if (id != selected) onPick(id) })
            }
        }
    }
}

// Nova subtarefa pelo título; o campo limpa quando a API confirma.
@Composable
private fun SubtaskComposer(onCreate: (title: String, onCreated: () -> Unit) -> Unit) {
    var title by rememberSaveable { mutableStateOf("") }

    OutlinedTextField(
        value = title,
        onValueChange = { title = it.take(200) },
        placeholder = { Text("Nova subtarefa") },
        singleLine = true,
        modifier = Modifier.fillMaxWidth(),
        trailingIcon = {
            TextButton(onClick = { onCreate(title) { title = "" } }, enabled = title.isNotBlank()) { Text("Criar") }
        },
    )
}

// Comentário com @: enquanto o texto termina em "@começo", sugere as pessoas do projeto.
@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun CommentComposer(people: List<Member>, viewModel: TaskViewModel) {
    var draft by rememberSaveable { mutableStateOf("") }
    val query = mentionQuery(draft)
    val suggestions = if (query == null) emptyList() else people.filter { it.name.contains(query, ignoreCase = true) }.take(5)

    // Sugestões acima do campo e enviar dentro dele: com o teclado aberto, o que fica abaixo do campo some.
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        if (suggestions.isNotEmpty()) {
            FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                suggestions.forEach { person ->
                    AssistChip(
                        onClick = { draft = completeMention(draft, query.orEmpty(), person.name) },
                        label = { Text(person.name) },
                        leadingIcon = { Avatar(person.name) },
                    )
                }
            }
        }
        OutlinedTextField(
            value = draft,
            onValueChange = { draft = it },
            placeholder = { Text("Adicionar um comentário… Use @ para mencionar") },
            modifier = Modifier.fillMaxWidth(),
            minLines = 2,
            trailingIcon = {
                IconButton(onClick = { viewModel.comment(draft) { draft = "" } }, enabled = draft.isNotBlank()) {
                    Icon(Icons.AutoMirrored.Filled.Send, contentDescription = "Comentar")
                }
            },
        )
    }
}
