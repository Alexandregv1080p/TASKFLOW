package com.taskflow.mobile.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.taskflow.mobile.data.ApiException
import com.taskflow.mobile.data.TaskFlowRepository
import kotlinx.coroutines.launch

// Entrar ou criar conta. Ao dar certo a sessão muda e a navegação troca de tela sozinha.
@Composable
fun LoginScreen(repository: TaskFlowRepository) {
    var registering by rememberSaveable { mutableStateOf(false) }
    var name by rememberSaveable { mutableStateOf("") }
    var email by rememberSaveable { mutableStateOf("") }
    var password by rememberSaveable { mutableStateOf("") }
    var error by rememberSaveable { mutableStateOf<String?>(null) }
    var saving by rememberSaveable { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    fun submit() {
        if (saving) return
        if (registering && password.length < 8) {
            error = "A senha precisa ter pelo menos 8 caracteres."
            return
        }
        saving = true
        error = null
        scope.launch {
            try {
                if (registering) repository.register(name, email, password) else repository.login(email, password)
            } catch (e: ApiException) {
                error = e.message
            } finally {
                saving = false
            }
        }
    }

    Column(
        Modifier.fillMaxSize().imePadding().verticalScroll(rememberScrollState()).padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp, androidx.compose.ui.Alignment.CenterVertically),
    ) {
        Text("TaskFlow", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold, color = MaterialTheme.colorScheme.primary)
        Text(if (registering) "Criar sua conta" else "Entrar no TaskFlow", style = MaterialTheme.typography.titleLarge)

        if (registering) {
            OutlinedTextField(
                value = name,
                onValueChange = { name = it },
                label = { Text("Nome") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
            )
        }
        OutlinedTextField(
            value = email,
            onValueChange = { email = it },
            label = { Text("E-mail") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email, imeAction = ImeAction.Next),
        )
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            label = { Text("Senha") },
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password, imeAction = ImeAction.Done),
            supportingText = if (registering) ({ Text("Mínimo de 8 caracteres.") }) else null,
        )

        error?.let {
            Text(it, color = MaterialTheme.colorScheme.error, modifier = Modifier.semantics { liveRegion = LiveRegionMode.Polite })
        }

        Button(
            onClick = ::submit,
            enabled = !saving && email.isNotBlank() && password.isNotBlank() && (!registering || name.isNotBlank()),
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text(if (saving) "Aguarde…" else if (registering) "Criar conta" else "Entrar")
        }

        TextButton(onClick = { registering = !registering; error = null }, modifier = Modifier.fillMaxWidth()) {
            Text(if (registering) "Já tem conta? Entrar" else "Não tem conta? Criar conta")
        }
    }
}
