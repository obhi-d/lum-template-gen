package org.intellij.sdk.settings;

import com.intellij.ui.components.JBCheckBox;
import com.intellij.ui.components.JBLabel;
import com.intellij.ui.components.JBTextField;
import com.intellij.util.ui.FormBuilder;
import org.jetbrains.annotations.NotNull;

import javax.swing.*;

/**
 * Supports creating and managing a JPanel for the Settings Dialog.
 */
public class AppSettingsComponent {

  private final JPanel myMainPanel;
  private final JBTextField userName = new JBTextField();
  private final JBTextField userEmail = new JBTextField();

  public AppSettingsComponent() {
    myMainPanel = FormBuilder.createFormBuilder()
            .addLabeledComponent(new JBLabel("Enter user name: "), userName, 1, false)
            .addLabeledComponent(new JBLabel("Enter user email: "), userEmail, 1, false)
            .addComponentFillVertically(new JPanel(), 0)
            .getPanel();
  }

  public JPanel getPanel() {
    return myMainPanel;
  }

  public JComponent getPreferredFocusedComponent() {
    return userName;
  }

  @NotNull
  public String getUserName() {
    return userName.getText();
  }
  public void setUserName(@NotNull String newText) {
    userName.setText(newText);
  }

  public String getUserEmail() {
    return userEmail.getText();
  }
  public void setUserEmail(@NotNull String newText) {
    userEmail.setText(newText);
  }
}