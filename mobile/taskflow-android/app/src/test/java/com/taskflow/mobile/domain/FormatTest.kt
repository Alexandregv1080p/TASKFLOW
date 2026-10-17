package com.taskflow.mobile.domain

import com.taskflow.mobile.data.TaskStatus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class FormatTest {

    // Mesmos casos do site: a chave da tarefa precisa ser igual nos dois clientes.
    @Test
    fun `sigla do projeto igual a do site`() {
        assertEquals("WEB", projectKey("Website"))
        assertEquals("MA", projectKey("Mobile App"))
        assertEquals("PDC", projectKey("Portal do Cliente"))
        assertEquals("RA", projectKey("Revisão anual"))
        assertEquals("ACA", projectKey("Ação"))
        assertEquals("BDP", projectKey("Backlog do produto (v2) extra"))
        assertEquals("TF", projectKey("  !!  "))
        assertEquals("PDC-12", taskKey("Portal do Cliente", 12))
    }

    @Test
    fun `tarefa anda uma coluna por vez`() {
        assertEquals(listOf(TaskStatus.InProgress), TaskStatus.Todo.moves())
        assertEquals(listOf(TaskStatus.Todo, TaskStatus.Done), TaskStatus.InProgress.moves())
        assertEquals(listOf(TaskStatus.InProgress), TaskStatus.Done.moves())
    }

    @Test
    fun `sugestao de mencao so no fim do texto`() {
        assertEquals("", mentionQuery("Pode olhar @"))
        assertEquals("bi", mentionQuery("Pode olhar @bi"))
        assertEquals("ana", mentionQuery("@ana"))
        assertNull(mentionQuery("Pode olhar @Bia Souza "))
        assertNull(mentionQuery("email@empresa"))
        assertEquals("Pode olhar @Bia Souza ", completeMention("Pode olhar @bi", "bi", "Bia Souza"))
    }

    @Test
    fun `mencoes destacadas preferem o nome mais longo`() {
        val body = "Oi @ana paula e @Ana, veja com @Carlos."
        val ranges = mentionRanges(body, listOf("Ana", "Ana Paula"))

        assertEquals(listOf("@ana paula", "@Ana"), ranges.map { body.substring(it) })
        assertEquals(emptyList<IntRange>(), mentionRanges(body, emptyList()))
    }

    @Test
    fun `cor do tipo em hexadecimal`() {
        assertEquals(0xFF1868DBL, hexColor("#1868db"))
        assertNull(hexColor("azul"))
        assertNull(hexColor("#FFF"))
        // Toda cor da paleta do app é aceita pela API (#RRGGBB) e não se repete.
        assertTrue(TYPE_PALETTE.all { hexColor(it) != null })
        assertEquals(TYPE_PALETTE.size, TYPE_PALETTE.toSet().size)
    }

    @Test
    fun `tamanho de arquivo como no site`() {
        assertEquals("812 B", formatSize(812))
        assertEquals("34 KB", formatSize(34 * 1024))
        assertEquals("2 MB", formatSize(2L * 1024 * 1024))
        assertEquals("2,5 MB", formatSize(2_621_440))
        assertEquals("100 MB", formatSize(100L * 1024 * 1024))
    }

    @Test
    fun `iniciais do avatar`() {
        assertEquals("AS", initials("Ana Souza"))
        assertEquals("B", initials("bia"))
        assertEquals("?", initials(" "))
    }
}
