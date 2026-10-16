package com.taskflow.mobile.data

import kotlinx.serialization.Serializable

// Espelham os DTOs da API (TaskFlow.Application). Campos que o app não usa ficam de fora (ignoreUnknownKeys).

@Serializable
data class User(val id: String, val name: String)

@Serializable
data class AuthResponse(val accessToken: String, val user: User)

@Serializable
data class LoginRequest(val email: String, val password: String)

@Serializable
data class RegisterRequest(val name: String, val email: String, val password: String)

@Serializable
enum class TaskStatus { Todo, InProgress, Done }

@Serializable
enum class TaskPriority { Low, Medium, High }

@Serializable
data class ForYouItem(
    val taskId: String,
    val projectId: String,
    val projectName: String,
    val number: Int,
    val title: String,
    val status: TaskStatus,
    val priority: TaskPriority,
    val assigneeName: String? = null,
)

@Serializable
data class ForYouResponse(val assignedCount: Int = 0, val items: List<ForYouItem>)

@Serializable
data class ProjectSummary(
    val id: String,
    val name: String,
    val spaceName: String? = null,
    val taskCount: Int,
    val doneCount: Int,
)

@Serializable
data class Project(val id: String, val name: String, val archivedAt: String? = null)

@Serializable
data class Task(
    val id: String,
    val projectId: String,
    val number: Int,
    val title: String,
    val description: String? = null,
    val status: TaskStatus,
    val priority: TaskPriority,
    val dueDate: String? = null,
    val assignedToId: String? = null,
    val createdAt: String,
    val typeId: String? = null,
    // Tarefa-pai, quando esta é uma subtarefa.
    val parentId: String? = null,
)

// Tipo da tarefa (Bug, Integração...): cada projeto tem os seus, com cor em #RRGGBB.
@Serializable
data class TaskType(val id: String, val name: String, val color: String)

@Serializable
data class Member(val id: String, val name: String, val isOwner: Boolean = false)

@Serializable
data class Comment(val id: String, val authorId: String? = null, val body: String, val createdAt: String)

@Serializable
data class StatusRequest(val status: TaskStatus)

// userId nulo some do JSON (explicitNulls = false) e a API entende como "sem responsável".
@Serializable
data class AssignRequest(val userId: String?)

@Serializable
data class CommentRequest(val body: String)

@Serializable
data class TypeRequest(val typeId: String?)

@Serializable
data class ParentRequest(val parentId: String?)

@Serializable
enum class NotificationType { Assigned, Mentioned, Commented }

@Serializable
data class AppNotification(
    val id: String,
    val type: NotificationType,
    val actorName: String,
    val taskId: String,
    val projectId: String,
    val projectName: String,
    val taskNumber: Int,
    val taskTitle: String,
    val createdAt: String,
    val read: Boolean,
)

@Serializable
data class NotificationList(val unreadCount: Int, val items: List<AppNotification>)

// Formato de erro da API: { status, title, message, timestamp }.
@Serializable
data class ApiErrorBody(val message: String? = null)
