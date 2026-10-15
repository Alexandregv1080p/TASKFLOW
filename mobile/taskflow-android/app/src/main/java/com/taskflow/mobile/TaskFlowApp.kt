package com.taskflow.mobile

import android.app.Application
import com.taskflow.mobile.data.LiveUpdates
import com.taskflow.mobile.data.PrefsSessionStorage
import com.taskflow.mobile.data.SessionManager
import com.taskflow.mobile.data.TaskFlowRepository
import kotlinx.coroutines.MainScope

// Injeção de dependência manual: três objetos, criados uma vez, sem framework.
class TaskFlowApp : Application() {
    lateinit var session: SessionManager
    lateinit var repository: TaskFlowRepository
    lateinit var live: LiveUpdates

    override fun onCreate() {
        super.onCreate()
        session = SessionManager(PrefsSessionStorage(this))
        repository = TaskFlowRepository.create(BuildConfig.API_BASE_URL, session)
        live = LiveUpdates("${BuildConfig.API_BASE_URL}hubs/board", session, MainScope()).also { it.start() }
    }
}
