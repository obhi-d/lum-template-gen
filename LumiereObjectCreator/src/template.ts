import * as vscode from 'vscode';
import { once } from './decorators';
import * as path from 'path';

export class Template implements vscode.QuickPickItem {
  public readonly name : string;
  public readonly weight: number;

  public constructor(selection : string) {
    this.name = selection;
    
    let lastSep : number = selection.lastIndexOf(path.sep);
    if (lastSep < 0)
      lastSep = 0;
    this.weight = Number(selection.substr(lastSep, selection.indexOf('.')));
  }

  @once()
  public get label(): string {
    let first : number = this.name.lastIndexOf('.');
    if (first < 0)
      first = 0;
    else
      first = first + 1;
    let last : number = this.name.lastIndexOf('(');
    if (last < 0)
      last = this.name.length - first;  
    else
      last = last - first;
      
    return this.name.substr(first, last);
  }

  @once()
  public get description(): string {
    return this.name;
  }
}
