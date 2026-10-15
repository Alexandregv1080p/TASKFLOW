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

// Iniciais do avatar: "Ana Souza" -> AS.
fun initials(name: String): String =
    name.split(' ').filter { it.isNotBlank() }.take(2).joinToString("") { it.take(1).uppercase(Locale.ROOT) }.ifEmpty { "?" }
