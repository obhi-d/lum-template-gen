import env from './environment';
import * as fs from 'fs';
import * as path from 'path';
import { Template } from './template';
import { Input } from './input';
import * as ps from 'child_process'
import * as os from 'os'
import * as vscode from 'vscode';
import {PythonShell} from 'python-shell';

export interface ItemLocation {
  framework?: string;
  module?: string;
}

export class LumiereObjectCreator {

  public objectName: string;
  public selection: string;
  public readonly templates: Template[];
  public readonly k_templateSource: Template;
  public readonly k_templateLocalHeader: Template;
  public readonly k_templateHeader: Template;
  public readonly k_templateClass: Template;

  public constructor(selection: string) {
    this.selection = selection;
    this.templates = fs
      .readdirSync(env.templatesFolderPath)
      .filter(f => !f.startsWith('.'))
      .map(f => new Template(f))
      .sort((a, b) => (a.weight < b.weight && -1) ||
        (a.weight > b.weight && 1) || 0);
    this.templates.unshift(new Template('Auto'));
    for (let i = 0; i < this.templates.length; ++i) {
      if (this.templates[i].name.indexOf("Source(") >= 0)
        this.k_templateSource = this.templates[i];
      else if (this.templates[i].name.indexOf("LocalHeader(") >= 0)
        this.k_templateLocalHeader = this.templates[i];
      else if (this.templates[i].name.indexOf("Header(") >= 0)
        this.k_templateHeader = this.templates[i];
      else if (this.templates[i].name.indexOf(".Class(") >= 0)
        this.k_templateClass = this.templates[i];
    }
  }

  public determineType(selectionPath: string,
    template: Template,
    objectName: string): string {
    if (template.name == 'Auto') {
      let index = objectName.indexOf(':');
      if (index >= 0) {
        for (let i = 0; i < this.templates.length; ++i) {
          let header = (i + 1).toString() + ':';
          if (objectName.startsWith(header))
            return this.templates[i].name;
          let open = this.templates[i].name.lastIndexOf('(');
          let close = this.templates[i].name.lastIndexOf(')');
          if (open >= 0 && close >= 0) {
            header = this.templates[i].name.substr(open + 1, close - (open + 1)) + ':';
            if (objectName.startsWith(header))
              return this.templates[i].name;
          }
        }
      }
      if (selectionPath.indexOf(path.sep + 'src') >= 0 || objectName.endsWith('.cpp') ||
        objectName.endsWith('.cxx'))
        return this.k_templateSource.name;
      if (selectionPath.indexOf(path.sep + 'local_include'))
        return this.k_templateLocalHeader.name;
      if (selectionPath.indexOf(path.sep + 'include') >= 0 ||
        objectName.endsWith('.h') ||
        objectName.endsWith('.hpp') ||
        objectName.endsWith('.hxx'))
        return this.k_templateHeader.name;
      return this.k_templateClass.name;
    } else
      return template.name;

  }

  private get santizeName(): string {
    let sanitized = this.objectName;
    let index = sanitized.indexOf(':');
    if (index >= 0) {
      sanitized = sanitized.substr(index + 1);
    }
    index = sanitized.indexOf('.');
    if (index >= 0) {
      sanitized = sanitized.substr(0, index);
    }
    return sanitized.trim();
  }

  private frameworkAndModule(selPath: string): ItemLocation {
    let frameworkName = "";
    let moduleName = "";
    let search = "Frameworks" + path.sep;
    let index = selPath.indexOf(search);
    if (index >= 0) {
      let code = selPath.substring(index + search.length);
      index = code.indexOf(path.sep);
      if (index >= 0) {
        frameworkName = code.substring(0, index);
        code = code.substring(index + 1);
        index = code.indexOf(path.sep);
        if (index >= 0) {
          moduleName = code.substring(0, index);
        }
        else
          moduleName = code;
      }
      else
        frameworkName = code;
    }
    return { framework: frameworkName, module: moduleName };
  }

  private getScriptsLocation(selPath: string): string {
    let index = selPath.indexOf("Frameworks");
    if (index >= 0) {
      return selPath.substring(0, index) + "Scripts";
    }
    return "";
  }

  private getPlacementLocation(selPath): string {
    let search = "Frameworks" + path.sep;
    let index = selPath.indexOf(search);
    if (index >= 0) {
      index = selPath.indexOf(path.sep, index + search.length);
      if (index >= 0) {
        let module = selPath.indexOf(path.sep, index + 1);
        if (module >= 0) {
          return selPath.substring(0, module);
        }
        else if (index + 1 < selPath.length)
          return selPath;
        else
          return selPath.substring(0, index);
      }
    }
    return selPath;
  }


  private getRulesFile(selPath: string, namespaceRuleLoc: string): string {
    if (path.isAbsolute(namespaceRuleLoc))
      return namespaceRuleLoc;
    let index = selPath.indexOf("Frameworks");
    if (index >= 0) {
      return selPath.substring(0, index) + "template-rules.json";
    }
    return "";
  }

  private processFilesCreated(data: string) {
    let out = data.split('/\r?\n/');
    let list: string[] = [];
    out.forEach(element => {
      if (element.startsWith("[FILE] "))
        list.push(element.substr(7));
    });
    list.forEach(element => {
      let uri = vscode.Uri.file(element);
      vscode.commands.executeCommand('vscode.open', uri);
    });
    env.output.appendLine(data);
  }

  public async run() {
    let template = await this.askTemplate();
    if (!template) {
      return;
    }
    let type = this.determineType(this.selection, template, this.objectName);
    let sanName = this.santizeName;
    let itemLoc = this.frameworkAndModule(this.selection);
    if (type.indexOf("Module") >= 0)
      itemLoc.module = sanName;
    let proc = require('hasbin').sync("python3") ? "python3" : "python";
    let scriptLoc = path.join(
      this.getScriptsLocation(this.selection),
      'build_system',
      'build_utils');

    let args = [
      '--name=' + sanName,
      '--type=' + type,
      '--author=' + env.config.get('fields.author'),
      '--email=' + env.config.get('fields.email'),
      '--templates=' + env.templatesFolderPath,
      '--framework=' + itemLoc.framework,
      '--module=' + itemLoc.module,
      '--file=' + this.objectName,
      '--rules=' + this.getRulesFile(this.selection, env.config.get('fields.rules')),
      '--destroot=' + this.getPlacementLocation(this.selection),
    ];
        
    env.output.appendLine('[COMMAND] python ' + args.join(' '));
    env.output.show();
    PythonShell.run('from_template.py', {
      mode: 'text',
      pythonPath: proc,
      pythonOptions: ['-u'],
      scriptPath: scriptLoc,
      args : args
    }, function (err, results) {
      if (err) throw err;
      // results is an array consisting of messages collected during execution
      results.forEach(element => {
        this.processFilesCreated(element);  
      });
      
    });
  }

  public async askTemplate(): Promise<Template> {
    let inputController = new Input();
    let { template, fileName } = await inputController.run(this.templates);
    if (!template) {
      return;
    }
    this.objectName = fileName;
    return template;
  }
}