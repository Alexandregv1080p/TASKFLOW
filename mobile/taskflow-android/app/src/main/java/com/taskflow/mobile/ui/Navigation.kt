package com.taskflow.mobile.ui

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.taskflow.mobile.TaskFlowApp

private enum class HomeTab(val title: String, val icon: ImageVector) {
    ForYou("Para você", Icons.Filled.Home),
    Projects("Projetos", Icons.AutoMirrored.Filled.List),
    Notifications("Notificações", Icons.Filled.Notifications),
}

// Sem sessão: login. Com sessão: abas (Para você, Projetos, Notificações) e as telas de quadro e tarefa.
@Composable
fun TaskFlowNavigation(app: TaskFlowApp) {
    val session by app.session.session.collectAsState()
    val current = session

    if (current == null) {
        LoginScreen(app.repository)
        return
    }

    val nav = rememberNavController()
    // Chave por usuário: trocar de conta recria as telas com os dados da nova sessão.
    val notifications = viewModel(key = "notifications-${current.userId}") { NotificationsViewModel(app.repository, app.live) }

    NavHost(nav, startDestination = "home") {
        composable("home") {
            HomeScaffold(app, notifications, current.userId, openProject = { nav.navigate("board/$it") }) { projectId, taskId ->
                nav.navigate("task/$projectId/$taskId")
            }
        }
        composable("board/{projectId}") { entry ->
            val projectId = entry.arguments?.getString("projectId").orEmpty()
            BoardScreen(
                viewModel { BoardViewModel(app.repository, app.live, projectId) },
                app.live,
                onBack = { nav.popBackStack() },
                openTask = { nav.navigate("task/$projectId/$it") },
            )
        }
        composable("task/{projectId}/{taskId}") { entry ->
            val projectId = entry.arguments?.getString("projectId").orEmpty()
            val taskId = entry.arguments?.getString("taskId").orEmpty()
            TaskScreen(
                viewModel { TaskViewModel(app.repository, app.live, projectId, taskId) },
                currentUserId = current.userId,
                onBack = { nav.popBackStack() },
                openTask = { nav.navigate("task/$projectId/$it") },
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun HomeScaffold(
    app: TaskFlowApp,
    notifications: NotificationsViewModel,
    userId: String,
    openProject: (String) -> Unit,
    openTask: (projectId: String, taskId: String) -> Unit,
) {
    var tab by rememberSaveable { mutableStateOf(HomeTab.ForYou) }
    val notificationState by notifications.state.collectAsState()
    val unread = (notificationState as? Load.Ready)?.data?.unreadCount ?: 0

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(tab.title) },
                actions = {
                    IconButton(onClick = { app.repository.logout() }) {
                        Icon(Icons.AutoMirrored.Filled.ExitToApp, contentDescription = "Sair")
                    }
                },
            )
        },
        bottomBar = {
            NavigationBar {
                HomeTab.entries.forEach { item ->
                    NavigationBarItem(
                        selected = tab == item,
                        onClick = { tab = item },
                        label = { Text(item.title) },
                        icon = {
                            BadgedBox(badge = {
                                if (item == HomeTab.Notifications && unread > 0) Badge { Text(if (unread > 9) "9+" else "$unread") }
                            }) {
                                Icon(item.icon, contentDescription = null)
                            }
                        },
                    )
                }
            }
        },
    ) { padding ->
        val modifier = Modifier.fillMaxSize().padding(padding)
        androidx.compose.foundation.layout.Box(modifier) {
            when (tab) {
                HomeTab.ForYou -> ForYouScreen(viewModel(key = "foryou-$userId") { ForYouViewModel(app.repository, app.live) }, openTask)
                HomeTab.Projects -> ProjectsScreen(viewModel(key = "projects-$userId") { ProjectsViewModel(app.repository) }, openProject)
                HomeTab.Notifications -> NotificationsScreen(notifications, openTask)
            }
        }
    }
}
