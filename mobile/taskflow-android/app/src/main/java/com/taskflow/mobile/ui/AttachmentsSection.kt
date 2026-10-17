package com.taskflow.mobile.ui

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.provider.OpenableColumns
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import com.taskflow.mobile.data.Attachment
import com.taskflow.mobile.data.Member
import com.taskflow.mobile.data.StorageUsage
import com.taskflow.mobile.domain.ATTACHMENT_MIME_TYPES
import com.taskflow.mobile.domain.MAX_ATTACHMENT_BYTES
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import com.taskflow.mobile.domain.formatDateTime
import com.taskflow.mobile.domain.formatSize
import java.io.File

// Anexos da tarefa: lista com o espaço usado pelo projeto, anexar pelo seletor de arquivos do sistema,
// abrir no app do celular que lê aquele tipo e excluir (quem anexou ou o dono do projeto).
@Composable
fun AttachmentsSection(
    attachments: List<Attachment>,
    usage: StorageUsage?,
    members: List<Member>,
    currentUserId: String,
    isProjectOwner: Boolean,
    readOnly: Boolean,
    onUpload: (fileName: String, mimeType: String?, bytes: ByteArray) -> Unit,
    onOpen: (Attachment, File, (File) -> Unit) -> Unit,
    onDelete: (Attachment) -> Unit,
    onError: (String) -> Unit,
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    var deleting by remember { mutableStateOf<Attachment?>(null) }

    val picker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) scope.launch { readPicked(context, uri, onUpload, onError) }
    }

    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        if (attachments.isEmpty()) {
            Text("Nenhum anexo.", color = MaterialTheme.colorScheme.onSurfaceVariant)
        }

        attachments.forEach { attachment ->
            val uploader = members.firstOrNull { it.id == attachment.uploadedById }?.name ?: "Alguém"
            val canDelete = !readOnly && (attachment.uploadedById == currentUserId || isProjectOwner)
            Row(
                Modifier.fillMaxWidth()
                    .clickable { onOpen(attachment, context.cacheDir) { file -> view(context, file, attachment.contentType, onError) } }
                    .padding(vertical = 6.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(attachment.fileName, maxLines = 1, overflow = TextOverflow.Ellipsis, color = MaterialTheme.colorScheme.primary)
                    Text(
                        "${formatSize(attachment.sizeBytes)} · $uploader · ${formatDateTime(attachment.createdAt)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                if (canDelete) {
                    IconButton(onClick = { deleting = attachment }) {
                        Icon(Icons.Filled.Delete, contentDescription = "Excluir o anexo ${attachment.fileName}", tint = MaterialTheme.colorScheme.error)
                    }
                }
            }
            HorizontalDivider(color = MaterialTheme.colorScheme.outline)
        }

        if (!readOnly) {
            TextButton(onClick = { picker.launch(ATTACHMENT_MIME_TYPES) }) { Text("Anexar arquivo") }
        }

        usage?.let {
            val high = it.quotaBytes > 0 && it.usedBytes.toDouble() / it.quotaBytes >= 0.8
            Text(
                "Espaço do projeto: ${formatSize(it.usedBytes)} de ${formatSize(it.quotaBytes)}" + if (high) " (quase cheio)" else "",
                style = MaterialTheme.typography.bodySmall,
                color = if (high) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }

    deleting?.let { attachment ->
        AlertDialog(
            onDismissRequest = { deleting = null },
            title = { Text("Excluir o anexo?") },
            text = { Text(attachment.fileName) },
            confirmButton = {
                TextButton(onClick = {
                    onDelete(attachment)
                    deleting = null
                }) { Text("Excluir", color = MaterialTheme.colorScheme.error) }
            },
            dismissButton = { TextButton(onClick = { deleting = null }) { Text("Cancelar") } },
        )
    }
}

// Lê o arquivo escolhido (fora da thread principal): nome pelo provedor de documentos, tamanho conferido
// antes de carregar na memória.
private suspend fun readPicked(
    context: Context,
    uri: Uri,
    onUpload: (String, String?, ByteArray) -> Unit,
    onError: (String) -> Unit,
) {
    val resolver = context.contentResolver
    var name = "arquivo"
    var tooBig = false
    val bytes = withContext(Dispatchers.IO) {
        try {
            var size = -1L
            resolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE), null, null, null)?.use { cursor ->
                if (cursor.moveToFirst()) {
                    name = cursor.getString(0) ?: name
                    if (!cursor.isNull(1)) size = cursor.getLong(1)
                }
            }
            tooBig = size > MAX_ATTACHMENT_BYTES
            if (tooBig) null else resolver.openInputStream(uri)?.use { it.readBytes() }
        } catch (e: Exception) {
            // Provedor sumiu, permissão revogada, erro de leitura.
            if (e is CancellationException) throw e
            null
        }
    }

    when {
        tooBig -> onError("\"$name\" passa de 10 MB.")
        bytes == null -> onError("Não foi possível ler o arquivo.")
        else -> onUpload(name, resolver.getType(uri), bytes)
    }
}

// Abre o arquivo baixado no app do celular que lê aquele tipo (o arquivo sai do cache do app via FileProvider).
private fun view(context: Context, file: File, mimeType: String, onError: (String) -> Unit) {
    val uri = FileProvider.getUriForFile(context, "${context.packageName}.files", file)
    val intent = Intent(Intent.ACTION_VIEW).setDataAndType(uri, mimeType).addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)

    try {
        context.startActivity(intent)
    } catch (_: ActivityNotFoundException) {
        onError("Nenhum app deste celular abre arquivos desse tipo.")
    }
}
