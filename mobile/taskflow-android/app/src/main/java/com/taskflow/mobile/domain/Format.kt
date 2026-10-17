package com.taskflow.mobile.domain

import com.taskflow.mobile.data.TaskPriority
import com.taskflow.mobile.data.TaskStatus
import java.text.Normalizer
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlin.math.abs

// Regras de apresentação compartilhadas com o site (frontend/src/utils/format.ts), para as chaves e textos baterem.

// Sigla do projeto que monta a chave da tarefa: "Website" -> WEB, "Portal do Cliente" -> PDC.
fun projectKey(name: String): String {
    val words = Normalizer.normalize(name, Normalizer.Form.NFD)
        .replace(Regex("\\p{M}"), "")
        .replace(Regex("[^\\p{L}\\p{N}\\s]"), "")
        .uppercase(Locale.ROOT)
        .split(Regex("\\s+"))
        .filter { it.isNotEmpty() }

    return when {
        words.isEmpty() -> "TF"
        words.size == 1 -> words[0].take(3)
        else -> words.take(3).joinToString("") { it.take(1) }
    }
}

fun taskKey(projectName: String, number: Int) = "${projectKey(projectName)}-$number"

val TaskStatus.label: String
    get() = when (this) {
        TaskStatus.Todo -> "A fazer"
        TaskStatus.InProgress -> "Em andamento"
        TaskStatus.Done -> "Concluído"
    }

val TaskPriority.label: String
    get() = when (this) {
        TaskPriority.Low -> "Baixa"
        TaskPriority.Medium -> "Média"
        TaskPriority.High -> "Alta"
    }

// O domínio só deixa mover uma coluna por vez: A fazer <-> Em andamento <-> Concluído.
fun TaskStatus.moves(): List<TaskStatus> = TaskStatus.entries.filter { abs(it.ordinal - ordinal) == 1 }

// Menção: o texto termina em "@" + começo de um nome? Devolve o que foi digitado depois do @.
fun mentionQuery(text: String): String? = Regex("(?:^|\\s)@([^\\s@]*)$").find(text)?.groupValues?.get(1)

// Troca o "@começo" do fim do texto pelo nome completo.
fun completeMention(text: String, query: String, name: String) = text.dropLast(query.length) + name + " "

// Trechos "@Nome" de quem está no projeto (mesma regra da API para notificar). Nomes mais longos primeiro,
// para "@Ana Paula" não casar só com "@Ana".
fun mentionRanges(body: String, names: List<String>): List<IntRange> {
    if (names.isEmpty()) return emptyList()
    val pattern = names.sortedByDescending { it.length }.joinToString("|") { Regex.escape(it) }

    return Regex("@(?:$pattern)", RegexOption.IGNORE_CASE).findAll(body).map { it.range }.toList()
}

private val dateTime = DateTimeFormatter.ofPattern("d MMM, HH:mm", Locale.forLanguageTag("pt-BR"))

fun formatDateTime(iso: String): String =
    runCatching { OffsetDateTime.parse(iso).atZoneSameInstant(ZoneId.systemDefault()).format(dateTime) }.getOrDefault(iso)

// Cores oferecidas no app para os tipos (o site aceita qualquer uma): as três padrão do projeto e outras da paleta do site.
val TYPE_PALETTE = listOf("#1868DB", "#C9372C", "#6E5DC6", "#22A06B", "#CF7A00", "#1D7F8C", "#E774BB", "#626F86")

// Cor do tipo (#RRGGBB) como ARGB opaco; nulo se vier num formato inesperado.
fun hexColor(hex: String): Long? =
    Regex("^#([0-9A-Fa-f]{6})$").find(hex)?.let { 0xFF000000L or it.groupValues[1].toLong(16) }

// Anexos: mesmo limite e mesmos tipos da API (o seletor do sistema só mostra estes).
const val MAX_ATTACHMENT_BYTES = 10L * 1024 * 1024

val ATTACHMENT_MIME_TYPES = arrayOf(
    "image/*",
    "application/pdf",
    "text/*",
    "application/json",
    "application/zip",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
)

// "812 B", "34 KB", "2,5 MB" (como no site).
fun formatSize(bytes: Long): String = when {
    bytes < 1024 -> "$bytes B"
    bytes < 1024 * 1024 -> "${Math.round(bytes / 1024.0)} KB"
    else -> java.text.DecimalFormat("#,##0.#", java.text.DecimalFormatSymbols(Locale.forLanguageTag("pt-BR"))).format(bytes / 1024.0 / 1024.0) + " MB"
}

// Iniciais do avatar: "Ana Souza" -> AS.
fun initials(name: String): String =
    name.split(' ').filter { it.isNotBlank() }.take(2).joinToString("") { it.take(1).uppercase(Locale.ROOT) }.ifEmpty { "?" }
