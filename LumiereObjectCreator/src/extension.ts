import * as vscode from 'vscode';
import * as path from 'path';
import env from './environment'
import { LumiereObjectCreator } from './lumiereObjectCreator'

export function activate(context: vscode.ExtensionContext) {
    context.subscriptions.push(
        vscode.commands.registerCommand('lumiereObjectCreator.newFile', async function(e: vscode.Uri) {
            try {
                let targetFolderPath = e && e.fsPath ? e.fsPath : vscode.workspace.rootPath;
                let creator = new LumiereObjectCreator(targetFolderPath);
                await creator.run();
            } catch (error) {
                vscode.window.showErrorMessage(`LumiereObjectCreator error: ${error.message}`);
            }
        }),
    );

    env.output = vscode.window.createOutputChannel("Lumiere");
    env.context = context;
}

export function deactivate() {}
