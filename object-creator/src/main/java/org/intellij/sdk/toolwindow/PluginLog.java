package org.intellij.sdk.toolwindow;
import com.intellij.execution.filters.TextConsoleBuilderFactory;
import com.intellij.execution.ui.ConsoleView;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.openapi.wm.ToolWindowFactory;
import com.intellij.openapi.wm.ToolWindowManager;
import com.intellij.ui.content.Content;
import com.intellij.ui.content.ContentFactory;
import org.jetbrains.annotations.NotNull;

public class PluginLog implements ToolWindowFactory {
    public static ConsoleView view;
    // Create the tool window content.
    public void createToolWindowContent(@NotNull Project project, @NotNull ToolWindow toolWindow) {
        ConsoleView consoleView = TextConsoleBuilderFactory.getInstance().createBuilder(project).getConsole();
        view = consoleView;
        Content content = toolWindow.getContentManager().getFactory().createContent(consoleView.getComponent(), "Lumiere Output", false);
        toolWindow.getContentManager().addContent(content);
    }

}