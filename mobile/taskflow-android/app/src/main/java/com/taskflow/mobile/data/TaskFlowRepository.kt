package com.taskflow.mobile.data

import java.io.IOException
import java.util.concurrent.TimeUnit
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.RequestBody.Companion.toRequestBody
import retrofit2.HttpException
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory

class ApiException(val code: Int, message: String) : Exception(message)

// Acesso à API: põe o token nas requisições, encerra a sessão em 401 e traduz erros em mensagens para a tela.
class TaskFlowRepository(private val api: TaskFlowApi, private val session: SessionManager) {

    suspend fun login(email: String, password: String) {
        val auth = call(unauthorized = "E-mail ou senha inválidos.") { api.login(LoginRequest(email.trim(), password)) }
        session.signIn(Session(auth.accessToken, auth.user.id, auth.user.name))
    }

    suspend fun register(name: String, email: String, password: String) {
        val auth = call { api.register(RegisterRequest(name.trim(), email.trim(), password)) }
        session.signIn(Session(auth.accessToken, auth.user.id, auth.user.name))
    }

    fun logout() = session.signOut()

    suspend fun forYou(filter: String) = call { api.forYou(filter) }
    suspend fun projects() = call { api.projects() }
    suspend fun project(id: String) = call { api.project(id) }
    suspend fun tasks(projectId: String) = call { api.tasks(projectId) }
    suspend fun members(projectId: String) = call { api.members(projectId) }
    suspend fun task(id: String) = call { api.task(id) }
    suspend fun changeStatus(taskId: String, status: TaskStatus) = call { api.changeStatus(taskId, StatusRequest(status)) }
    suspend fun assign(taskId: String, userId: String?) = call { api.assign(taskId, AssignRequest(userId)) }
    suspend fun taskTypes(projectId: String) = call { api.taskTypes(projectId) }
    suspend fun createTaskType(projectId: String, name: String, color: String) =
        call { api.createTaskType(projectId, TaskTypeRequest(name.trim(), color)) }
    suspend fun updateTaskType(id: String, name: String, color: String) = call { api.updateTaskType(id, TaskTypeRequest(name.trim(), color)) }
    suspend fun deleteTaskType(id: String) = call { api.deleteTaskType(id) }

    // Subtarefa nova: herda o tipo da tarefa-pai; prioridade e o resto se ajustam depois.
    suspend fun createSubtask(parent: Task, title: String) =
        call { api.createTask(parent.projectId, CreateTaskRequest(title.trim(), TaskPriority.Medium, typeId = parent.typeId, parentId = parent.id)) }
    suspend fun changeType(taskId: String, typeId: String?) = call { api.changeType(taskId, TypeRequest(typeId)) }
    suspend fun changeParent(taskId: String, parentId: String?) = call { api.changeParent(taskId, ParentRequest(parentId)) }
    suspend fun comments(taskId: String) = call { api.comments(taskId) }
    suspend fun attachments(taskId: String) = call { api.attachments(taskId) }
    suspend fun storage(projectId: String) = call { api.storage(projectId) }
    suspend fun deleteAttachment(id: String) = call { api.deleteAttachment(id) }

    // O tipo de conteúdo enviado é só informativo: a API decide pela extensão do nome.
    suspend fun uploadAttachment(taskId: String, fileName: String, mimeType: String?, bytes: ByteArray) = call {
        val body = bytes.toRequestBody((mimeType ?: "application/octet-stream").toMediaTypeOrNull())
        api.uploadAttachment(taskId, MultipartBody.Part.createFormData("file", fileName, body))
    }

    // O corpo (até 10 MB) é lido fora da thread principal.
    suspend fun downloadAttachment(id: String): ByteArray =
        call { api.downloadAttachment(id).use { withContext(Dispatchers.IO) { it.bytes() } } }
    suspend fun addComment(taskId: String, body: String) = call { api.addComment(taskId, CommentRequest(body.trim())) }
    suspend fun notifications() = call { api.notifications() }
    suspend fun markRead(id: String) = call { api.markRead(id) }
    suspend fun markAllRead() = call { api.markAllRead() }

    private suspend fun <T> call(unauthorized: String? = null, block: suspend () -> T): T =
        try {
            block()
        } catch (e: HttpException) {
            val message = when {
                e.code() == 401 && unauthorized != null -> unauthorized
                // A API responde 404 também para o que existe mas não é seu: não revela nada.
                e.code() == 404 -> "Não encontrado: pode ter sido excluído ou você não tem mais acesso."
                else -> errorMessage(e)
            }
            throw ApiException(e.code(), message)
        } catch (e: IOException) {
            throw ApiException(0, "Não foi possível conectar ao TaskFlow. Confira se a API está no ar.")
        }

    private fun errorMessage(e: HttpException): String {
        val body = e.response()?.errorBody()?.string()
        val message = body?.let { runCatching { json.decodeFromString<ApiErrorBody>(it).message }.getOrNull() }

        return message ?: "Erro ${e.code()} ao falar com o TaskFlow."
    }

    companion object {
        val json = Json {
            ignoreUnknownKeys = true // a API pode ganhar campos sem quebrar o app
            explicitNulls = false
        }

        fun create(baseUrl: String, session: SessionManager): TaskFlowRepository {
            val client = OkHttpClient.Builder()
                .addInterceptor { chain ->
                    val token = session.token
                    val request = chain.request().newBuilder()
                        .apply { if (token != null) header("Authorization", "Bearer $token") }
                        .build()
                    val response = chain.proceed(request)

                    // Token expirado ou inválido: volta para o login. O token dura 60 min e a API não tem refresh.
                    if (response.code == 401 && token != null && session.token == token) session.signOut()
                    response
                }
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(20, TimeUnit.SECONDS)
                .build()

            val api = Retrofit.Builder()
                .baseUrl(baseUrl)
                .client(client)
                .addConverterFactory(json.asConverterFactory("application/json; charset=UTF-8".toMediaType()))
                .build()
                .create(TaskFlowApi::class.java)

            return TaskFlowRepository(api, session)
        }
    }
}
