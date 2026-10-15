package com.taskflow.mobile.domain

import com.taskflow.mobile.data.TaskStatus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
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
    fun `iniciais do avatar`() {
        assertEquals("AS", initials("Ana Souza"))
        assertEquals("B", initials("bia"))
        assertEquals("?", initials(" "))
    }
}
