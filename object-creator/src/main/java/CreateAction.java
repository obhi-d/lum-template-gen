package main.java;

import com.intellij.execution.ui.ConsoleViewContentType;
import com.intellij.openapi.actionSystem.AnAction;
import com.intellij.openapi.actionSystem.AnActionEvent;
import com.intellij.openapi.actionSystem.CommonDataKeys;
import com.intellij.openapi.project.Project;
import com.intellij.openapi.ui.ComboBox;
import com.intellij.openapi.ui.DialogWrapper;
import com.intellij.openapi.ui.Messages;
import com.intellij.openapi.wm.ToolWindow;
import com.intellij.openapi.wm.ToolWindowManager;
import com.intellij.pom.Navigatable;
import org.intellij.sdk.settings.AppSettingsState;
import org.intellij.sdk.toolwindow.PluginLog;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import javax.swing.*;
import java.awt.*;
import java.io.*;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.Comparator;

public class CreateAction extends AnAction {
    public static class Template {
        String name;
        int weight;

        Template(String name) {
            this.name = name;
            if (name.matches("^[0-9].*$")) {
                this.weight = Integer.parseInt(name.substring(0, name.indexOf('.')));
            } else
                this.weight = 0;
        }

        public String dispName() {
            if (name.matches("^[0-9].*$"))
                return name.substring(name.indexOf('.') + 1);
            return name;
        }
    }

    String selection;
    String objectName;
    Template[] templates;
    
    public  Template k_templateSource;
    public  Template k_templateLocalHeader;
    public  Template k_templateHeader;
    public  Template k_templateClass;


    public static class FrameworkModule {
        String framework;
        String module;
        FrameworkModule(String f, String m) {
            framework = f;
            module = m;
        }
    }

    public static class TypeDialogWrapper extends DialogWrapper {
        ComboBox<String> comboBox;
        JTextField name;

        public TypeDialogWrapper() {
            super(true); // use current window as parent
            init();
            setTitle("Lumiere: Select Template");
        }

        public void setTemplates(Template[] names) {
            for (Template template : names) {
                comboBox.addItem(template.dispName());
            }
        }

        @Override
        public @Nullable JComponent getPreferredFocusedComponent() {
            return name;
        }

        @Nullable
        @Override
        protected JComponent createCenterPanel() {
            JPanel dialogPanel = new JPanel(new BorderLayout());
            comboBox = new ComboBox<>();
            name = new JTextField();

            dialogPanel.add(comboBox, BorderLayout.CENTER);
            dialogPanel.add(name, BorderLayout.AFTER_LAST_LINE);

            return dialogPanel;
        }
    }

    static public Path kScript = Paths.get("Scripts","build_system", "build_utils", "from_template.py");
    static public Path kScriptRoot = Paths.get("Scripts");


    @Override
    public void update(AnActionEvent e) {
        e.getPresentation().setEnabledAndVisible(true);
    }

    private void setTemplateLocation(Path templateLoc) {
        File folder = new File(templateLoc.toUri());
        File[] list = folder.listFiles();
        assert list != null;
        templates = new Template[list.length + 1];
        templates[0] = new Template("Auto");
        for (int f = 0; f < list.length; ++f) {
            Template templ = new Template(list[f].getName());
            if (templ.name.contains("Source("))
                this.k_templateSource = templ;
            else if (templ.name.contains("LocalHeader("))
                this.k_templateLocalHeader = templ;
            else if (templ.name.contains("Header("))
                this.k_templateHeader = templ;
            else if (templ.name.contains(".Class("))
                this.k_templateClass = templ;
            templates[f + 1] = templ;
        }
        Arrays.sort(templates, 1, templates.length, Comparator.comparingInt(o -> o.weight));
    }

    private String determineType(String selectionPath,
                                Template template,
                                String objectName) {
        if (template.name.equals("Auto")) {
            int index = objectName.indexOf(':');
            if (index >= 0) {
                for (int i = 0; i < this.templates.length; ++i) {
                    String header = Integer.toString(i + 1) + ':';
                    if (objectName.startsWith(header))
                        return this.templates[i].name;
                    int open = this.templates[i].name.lastIndexOf('(');
                    int close = this.templates[i].name.lastIndexOf(')');
                    if (open >= 0 && close >= 0) {
                        header = this.templates[i].name.substring(open + 1, close) + ':';
                        if (objectName.startsWith(header))
                            return this.templates[i].name;
                    }
                }
            }
            if (selectionPath.contains(File.separator + "src") || objectName.endsWith(".cpp") ||
                    objectName.endsWith(".cxx"))
                return this.k_templateSource.name;
            if (selectionPath.contains(File.separator + "local_include"))
                return this.k_templateLocalHeader.name;
            if (selectionPath.contains(File.separator + "include") ||
                    objectName.endsWith(".h") ||
                    objectName.endsWith(".hpp") ||
                    objectName.endsWith(".hxx"))
                return this.k_templateHeader.name;
            return this.k_templateClass.name;
        } else
            return template.name;

    }
    
    private String getPlacementLocation(String selPath) {
        String search = "Frameworks" + File.separator;
        int index = selPath.indexOf(search);
        if (index >= 0) {
            index = selPath.indexOf(File.separator, index + search.length());
            if (index >= 0) {
                int module = selPath.indexOf(File.separator, index + 1);
                if (module >= 0) {
                    return selPath.substring(0, module);
                }
                else if (index + 1 < selPath.length())
                    return selPath;
                else
                    return selPath.substring(0, index);
            }
        }
        return selPath;
    }
    
    private String sanitizeName() {
        String sanitized = this.objectName;
        int index = sanitized.indexOf(':');
        if (index >= 0) {
            sanitized = sanitized.substring(index + 1);
        }
        index = sanitized.indexOf('.');
        if (index >= 0) {
            sanitized = sanitized.substring(0, index);
        }
        return sanitized.trim();
    }

    private FrameworkModule frameworkAndModule(String selPath) {
        String frameworkName = "";
        String moduleName = "";
        String search = "Frameworks" + File.separator;
        int index = selPath.indexOf(search);
        if (index >= 0) {
            String code = selPath.substring(index + search.length());
            index = code.indexOf(File.separator);
            if (index >= 0) {
                frameworkName = code.substring(0, index);
                code = code.substring(index + 1);
                index = code.indexOf(File.separator);
                if (index >= 0) {
                    moduleName = code.substring(0, index);
                }
                else
                    moduleName = code;
            }
            else
                frameworkName = code;
        }
        return new FrameworkModule(frameworkName, moduleName);
    }

    private void generateEnum(String path, @Nullable Project project) {
        selection = path.substring("PsiFile:".length());
        int index = selection.indexOf("Frameworks");
        if (index > 0) {

            String source = selection.substring(0, index);
            Path script = Paths.get(source, kScriptRoot.toString());
            String program = "python3";
            if (System.getProperty("os.name").contains("Windows"))
                program = "python";
            try {
                Runtime.getRuntime().exec(program + " --version");
            } catch (Exception exception) {
                program = "python";
            }

            String command = program + " -m build_system.build_utils.enums --auto " + selection.toString();

            try {
                Process process = Runtime.getRuntime().exec(command, null, new File(script.toUri()));
                BufferedReader in = new BufferedReader(new InputStreamReader(process.getInputStream()));

                ToolWindowManager.getInstance(project).getToolWindow("Lumiere");
                String ret = in.readLine();
                while(ret != null && PluginLog.view != null) {
                    PluginLog.view.print(ret + "\n", ConsoleViewContentType.NORMAL_OUTPUT);
                    ret = in.readLine();
                }

                in = new BufferedReader(new InputStreamReader(process.getErrorStream()));
                ret = in.readLine();
                while(ret != null && PluginLog.view != null) {
                    PluginLog.view.print(ret + "\n", ConsoleViewContentType.ERROR_OUTPUT);
                    ret = in.readLine();
                }
            }catch (Exception exception) {
                StringWriter errors = new StringWriter();
                exception.printStackTrace(new PrintWriter(errors));
                String msg = errors.toString();
                if (msg == null)
                    msg = exception.toString();
                if (msg != null)
                    Messages.showMessageDialog("Failed to execute:\n" + command + "\nException: " + msg, "Lumiere Error", Messages.getErrorIcon());
            }
        }
    }

    private void parsePath(String path, @Nullable Project project) {
        selection = path.substring("PsiDirectory:".length());
        int index = selection.indexOf("Frameworks");
        if (index > 0) {

            String source = selection.substring(0, index);
            Path script = Paths.get(source, kScript.toString());
            Path templateLoc = Paths.get(source, "Templates");
            Path templateRules = Paths.get(source, "template-rules.json");

            setTemplateLocation(templateLoc);

            TypeDialogWrapper dialog = new TypeDialogWrapper();
            dialog.setTemplates(templates);
            if(dialog.showAndGet()) {
                objectName = dialog.name.getText();
                Template template = templates[dialog.comboBox.getSelectedIndex()];
                String type = this.determineType(this.selection, template, this.objectName);
                String sanName = this.sanitizeName();
                FrameworkModule itemLoc = this.frameworkAndModule(this.selection);

                AppSettingsState settings = AppSettingsState.getInstance();

                if (type.contains("Module"))
                    itemLoc.module = sanName;

                String program = "python3";
                if (System.getProperty("os.name").contains("Windows"))
                    program = "python";
                try {
                    Runtime.getRuntime().exec(program + " --version");
                } catch (Exception exception) {
                    program = "python";
                }

                String command = program + " " + script.toString() + " --name=\"" + sanName +
                "\" --type=\"" + type +
                        "\" --author=\"" + settings.userName +
                        "\" --email=\"" + settings.userEmail +
                        "\" --templates=\"" + templateLoc.toString() +
                        "\" --framework=\"" + itemLoc.framework +
                        "\" --module=\"" + itemLoc.module +
                        "\" --file=\"" + this.objectName +
                        "\" --rules=\"" + templateRules.toString() +
                        "\" --destroot=\"" + this.getPlacementLocation(this.selection) + "\"";

                try {
                    Process process = Runtime.getRuntime().exec(command);
                    BufferedReader in = new BufferedReader(new InputStreamReader(process.getInputStream()));

                    ToolWindowManager.getInstance(project).getToolWindow("Lumiere");
                    String ret = in.readLine();
                    while(ret != null && PluginLog.view != null) {
                        PluginLog.view.print(ret + "\n", ConsoleViewContentType.NORMAL_OUTPUT);
                        ret = in.readLine();
                    }

                    in = new BufferedReader(new InputStreamReader(process.getErrorStream()));
                    ret = in.readLine();
                    while(ret != null && PluginLog.view != null) {
                        PluginLog.view.print(ret + "\n", ConsoleViewContentType.ERROR_OUTPUT);
                        ret = in.readLine();
                    }
                }catch (Exception exception) {
                    StringWriter errors = new StringWriter();
                    exception.printStackTrace(new PrintWriter(errors));
                    String msg = errors.toString();
                    if (msg == null)
                        msg = exception.toString();
                    if (msg != null)
                        Messages.showMessageDialog("Failed to execute:\n" + command + "\nException: " + msg, "Lumiere Error", Messages.getErrorIcon());
                }

            }
        }
    }

    @Override
    public void actionPerformed(@NotNull AnActionEvent event) {

        // Using the event, create and show a dialog
        // If an element is selected in the editor, add info about it.
        Navigatable nav = event.getData(CommonDataKeys.NAVIGATABLE);
        if (nav != null) {
            String path = nav.toString();
            if (path.startsWith("PsiDirectory:")) {
                parsePath(path, event.getProject());
            } else if (path.startsWith("PsiFile:") && path.endsWith("Enums.json")) {
                generateEnum(path, event.getProject());
            }
        }
    }

}
