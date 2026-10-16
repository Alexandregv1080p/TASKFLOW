package com.taskflow.mobile.data

import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

// Só os endpoints que o app usa; os mesmos do site.
interface TaskFlowApi {
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): AuthResponse

    @POST("api/auth/register")
    suspend fun register(@Body request: RegisterRequest): AuthResponse

    @GET("api/for-you")
    suspend fun forYou(@Query("filter") filter: String): ForYouResponse

    @GET("api/projects")
    suspend fun projects(@Query("scope") scope: String = "All"): List<ProjectSummary>

    @GET("api/projects/{id}")
    suspend fun project(@Path("id") id: String): Project

    @GET("api/projects/{id}/tasks")
    suspend fun tasks(@Path("id") projectId: String): List<Task>

    @GET("api/projects/{id}/members")
    suspend fun members(@Path("id") projectId: String): List<Member>

    @GET("api/tasks/{id}")
    suspend fun task(@Path("id") id: String): Task

    @PATCH("api/tasks/{id}/status")
    suspend fun changeStatus(@Path("id") id: String, @Body request: StatusRequest): Task

    @GET("api/projects/{id}/task-types")
    suspend fun taskTypes(@Path("id") projectId: String): List<TaskType>

    @POST("api/projects/{id}/task-types")
    suspend fun createTaskType(@Path("id") projectId: String, @Body request: TaskTypeRequest): TaskType

    @PUT("api/task-types/{id}")
    suspend fun updateTaskType(@Path("id") id: String, @Body request: TaskTypeRequest): TaskType

    @DELETE("api/task-types/{id}")
    suspend fun deleteTaskType(@Path("id") id: String)

    @POST("api/projects/{id}/tasks")
    suspend fun createTask(@Path("id") projectId: String, @Body request: CreateTaskRequest): Task

    @PATCH("api/tasks/{id}/type")
    suspend fun changeType(@Path("id") id: String, @Body request: TypeRequest): Task

    @PATCH("api/tasks/{id}/parent")
    suspend fun changeParent(@Path("id") id: String, @Body request: ParentRequest): Task

    @PATCH("api/tasks/{id}/assignee")
    suspend fun assign(@Path("id") id: String, @Body request: AssignRequest): Task

    @GET("api/tasks/{id}/comments")
    suspend fun comments(@Path("id") taskId: String): List<Comment>

    @POST("api/tasks/{id}/comments")
    suspend fun addComment(@Path("id") taskId: String, @Body request: CommentRequest): Comment

    @GET("api/notifications")
    suspend fun notifications(): NotificationList

    @POST("api/notifications/{id}/read")
    suspend fun markRead(@Path("id") id: String)

    @POST("api/notifications/read-all")
    suspend fun markAllRead()
}
