package com.taskflow.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.taskflow.mobile.ui.TaskFlowNavigation
import com.taskflow.mobile.ui.theme.TaskFlowTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            TaskFlowTheme {
                TaskFlowNavigation(application as TaskFlowApp)
            }
        }
    }
}
