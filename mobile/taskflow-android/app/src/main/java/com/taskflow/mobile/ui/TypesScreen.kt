package com.taskflow.mobile.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewModelScope
import com.taskflow.mobile.data.LiveEvent
import com.taskflow.mobile.data.LiveUpdates
import com.taskflow.mobile.data.TaskFlowRepository
import com.taskflow.mobile.data.TaskType
import com.taskflow.mobile.domain.TYPE_PALETTE
import com.taskflow.mobile.domain.hexColor
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach

// Tipos de tarefa do projeto (só o dono chega aqui; a API aplica a mesma regra e responde 403 se não for).
class TypesViewModel(
    private val repository: TaskFlowRepository,
    private val live: LiveUpdates,
    private val projectId: String,
) : LoadViewModel<List<TaskType>>() {
    init {
        reload()
        live.watch(projectId)
        live.updates
            .onEach { if (it == LiveEvent.Connected || (it is LiveEvent.BoardChanged && it.projectId == projectId)) reload() }
            .launchIn(viewModelScope)
    }

    override suspend fun load() = repository.taskTypes(projectId)

    fun create(name: String, color: String) = act { repository.createTaskType(projectId, name, color) }

    fun update(type: TaskType, name: String, color: String) = act { repository.updateTaskType(type.id, name, color) }

    fun delete(type: TaskType) = act { repository.deleteTaskType(type.id) }

    override fun onCleared() = live.unwatch(projectId)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TypesScreen(viewModel: TypesViewModel, onBack: () -> Unit) {
    val state by viewModel.state.collectAsState()
    val message by viewModel.message.collectAsState()
    val snackbar = remember { SnackbarHostState() }
    // Tipo sendo editado (null = nenhum) e diálogos de criar e excluir.
    var editing by remember { mutableStateOf<TaskType?>(null) }
    var creating by rememberSaveable { mutableStateOf(false) }
    var deleting by remember { mutableStateOf<TaskType?>(null) }

    LaunchedEffect(message) {
        message?.let {
            snackbar.showSnackbar(it)
            viewModel.message.value = null
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Tipos de tarefa") },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar") }
                },
            )
        },
        floatingActionButton = {
            ExtendedFloatingActionButton(
                onClick = { creating = true },
                icon = { Icon(Icons.Filled.Add, contentDescription = null) },
                text = { Text("Novo tipo") },
            )
        },
        snackbarHost = { SnackbarHost(snackbar) },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            Text(
                "O tipo diz por que o card existe: um bug, uma integração, um desenvolvimento novo. Crie quantos o projeto precisar.",
                modifier = Modifier.padding(16.dp),
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            LoadContent(state, onRetry = { viewModel.reload() }) { types ->
                if (types.isEmpty()) {
                    EmptyMessage("Nenhum tipo ainda. Toque em \"Novo tipo\" para criar o primeiro.")
                } else {
                    LazyColumn(Modifier.fillMaxSize()) {
                        items(types, key = { it.id }) { type ->
                            Row(
                                Modifier.fillMaxWidth().clickable { editing = type }.padding(start = 16.dp, top = 4.dp, bottom = 4.dp),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(12.dp),
                            ) {
                                ColorDot(type.color)
                                Text(type.name, modifier = Modifier.weight(1f), style = MaterialTheme.typography.bodyLarge)
                                IconButton(onClick = { deleting = type }) {
                                    Icon(Icons.Filled.Delete, contentDescription = "Excluir o tipo ${type.name}", tint = MaterialTheme.colorScheme.error)
                                }
                            }
                            HorizontalDivider(color = MaterialTheme.colorScheme.outline)
                        }
                    }
                }
            }
        }
    }

    if (creating) {
        TypeEditor(title = "Novo tipo", initialName = "", initialColor = TYPE_PALETTE[3], onDismiss = { creating = false }) { name, color ->
            viewModel.create(name, color)
            creating = false
        }
    }

    editing?.let { type ->
        TypeEditor(title = "Editar tipo", initialName = type.name, initialColor = type.color, onDismiss = { editing = null }) { name, color ->
            viewModel.update(type, name, color)
            editing = null
        }
    }

    deleting?.let { type ->
        AlertDialog(
            onDismissRequest = { deleting = null },
            title = { Text("Excluir \"${type.name}\"?") },
            text = { Text("As tarefas desse tipo ficam sem tipo; nada mais é apagado.") },
            confirmButton = {
                TextButton(onClick = {
                    viewModel.delete(type)
                    deleting = null
                }) { Text("Excluir", color = MaterialTheme.colorScheme.error) }
            },
            dismissButton = { TextButton(onClick = { deleting = null }) { Text("Cancelar") } },
        )
    }
}

@Composable
private fun ColorDot(hex: String, modifier: Modifier = Modifier) {
    Box(modifier.size(20.dp).background(hexColor(hex)?.let { Color(it) } ?: MaterialTheme.colorScheme.primary, CircleShape))
}

// Nome + cor de uma paleta fixa (o Android não tem seletor de cor nativo; o site aceita qualquer cor).
@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun TypeEditor(
    title: String,
    initialName: String,
    initialColor: String,
    onDismiss: () -> Unit,
    onSave: (name: String, color: String) -> Unit,
) {
    var name by rememberSaveable { mutableStateOf(initialName) }
    var color by rememberSaveable { mutableStateOf(initialColor.uppercase()) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(16.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it.take(40) },
                    label = { Text("Nome") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                Text("Cor", style = MaterialTheme.typography.labelLarge)
                FlowRow(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    // Se a cor atual veio do site e não está na paleta, ela aparece primeiro para não se perder.
                    (listOf(color).filter { it !in TYPE_PALETTE } + TYPE_PALETTE).forEach { option ->
                        val chosen = option == color
                        ColorDot(
                            option,
                            Modifier
                                .size(36.dp)
                                .then(if (chosen) Modifier.border(BorderStroke(3.dp, MaterialTheme.colorScheme.onSurface), CircleShape).padding(5.dp) else Modifier)
                                .clickable { color = option }
                                .semantics {
                                    contentDescription = "Cor $option"
                                    selected = chosen
                                },
                        )
                    }
                }
            }
        },
        confirmButton = { TextButton(onClick = { onSave(name.trim(), color) }, enabled = name.isNotBlank()) { Text("Salvar") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancelar") } },
    )
}
