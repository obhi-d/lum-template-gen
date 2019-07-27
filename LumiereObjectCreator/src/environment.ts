

import * as vscode from 'vscode';
import * as path from 'path';
import * as os from 'os';
import * as _ from 'lodash';

export class Environment {
    public context: vscode.ExtensionContext;
    public output: vscode.OutputChannel;

    public get config(): vscode.WorkspaceConfiguration {
        return vscode.workspace.getConfiguration('lumiereObjectCreator');
    }

    public get templatesFolderPath(): string {
        let templPath = this.config.get<string>('templatesPath');
        return templPath ? path.join(vscode.workspace.rootPath, templPath) : path.join(os.homedir(), '.vscode/templates');
    }
}

export default new Environment();
