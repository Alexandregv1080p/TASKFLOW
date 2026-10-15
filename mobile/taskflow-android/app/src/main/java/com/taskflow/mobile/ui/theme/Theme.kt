package com.taskflow.mobile.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

// Mesma paleta do site: neutros frios e o azul como único acento.
private val Light = lightColorScheme(
    primary = Color(0xFF1868DB),
    onPrimary = Color(0xFFFDFEFF),
    primaryContainer = Color(0xFFE9F2FE),
    onPrimaryContainer = Color(0xFF1558BC),
    // Chips selecionados e item ativo da barra inferior: o azul claro do site, não o lilás padrão do Material.
    secondaryContainer = Color(0xFFE9F2FE),
    onSecondaryContainer = Color(0xFF1558BC),
    surfaceContainer = Color(0xFFF7F8F9),
    background = Color(0xFFFEFEFE),
    surface = Color(0xFFFEFEFE),
    surfaceVariant = Color(0xFFF1F2F4),
    onSurface = Color(0xFF172B4D),
    onSurfaceVariant = Color(0xFF626F86),
    outline = Color(0xFFDCDFE4),
    error = Color(0xFFC9372C),
)

private val Dark = darkColorScheme(
    primary = Color(0xFF579DFF),
    onPrimary = Color(0xFF1D2125),
    primaryContainer = Color(0xFF1C2B41),
    onPrimaryContainer = Color(0xFF85B8FF),
    secondaryContainer = Color(0xFF1C2B41),
    onSecondaryContainer = Color(0xFF85B8FF),
    surfaceContainer = Color(0xFF22272B),
    background = Color(0xFF1D2125),
    surface = Color(0xFF22272B),
    surfaceVariant = Color(0xFF161A1D),
    onSurface = Color(0xFFDEE4EA),
    onSurfaceVariant = Color(0xFF8C9BAB),
    outline = Color(0xFF333C44),
    error = Color(0xFFF87168),
)

val Todo = Color(0xFF8590A2)
val InProgress = Color(0xFFCF7A00)
val Done = Color(0xFF22A06B)
val High = Color(0xFFC9372C)

@Composable
fun TaskFlowTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = if (isSystemInDarkTheme()) Dark else Light, content = content)
}
