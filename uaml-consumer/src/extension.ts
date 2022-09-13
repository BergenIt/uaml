// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import fetch, { Blob, Headers } from 'node-fetch';
import * as fs from 'fs';
import { SvgViewerProvider } from './svgView';

const uamlToSvg = "api/Uaml/GeneratePageAsSvg";
const getPdf = "api/Uaml/GenerateProject/";

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext): void {
	
	// Use the console to output diagnostic information (console.log) and errors (console.error)
	// This line of code will only be executed once when your extension is activated
	console.log('Congratulations, your extension "uaml-consumer" is now active!');
	
	let cfg: vscode.WorkspaceConfiguration | undefined = vscode.workspace.getConfiguration("uamlConsumer");

	let headers: Object | undefined = cfg.get("server.optional.headers");

	let currentPanel: vscode.WebviewPanel | undefined = undefined;
	
	
	context.subscriptions.push(SvgViewerProvider.register(context));
	
	context.subscriptions.push(vscode.commands.registerCommand("uaml-consumer.getDataFromOne", () => {

		const columnToShowIn = vscode.window.activeTextEditor
        ? vscode.ViewColumn.Beside
        : undefined;

		let body = vscode.window.activeTextEditor?.document.getText();
		let fName = vscode.window.activeTextEditor?.document.fileName;

		if (fName?.endsWith('.uaml')){
			vscode.window.showInformationMessage("File is .uaml extension");
			if (currentPanel) {
				console.log("already have panel");
				currentPanel.reveal(columnToShowIn);
				getUaml((<vscode.WorkspaceConfiguration>cfg), body, headers, uamlToSvg, '').then(data => {
					if(currentPanel !== undefined) {
						currentPanel.webview.html = getWebview((<string>data));
					}
				});
			} else {
				console.log("create new panel");
				currentPanel = vscode.window.createWebviewPanel(
					'uaml',
					'Preview',
					(<vscode.ViewColumn>columnToShowIn),
					{}
				);
				getUaml((<vscode.WorkspaceConfiguration>cfg), body, headers, uamlToSvg, '').then(data => {
					if(currentPanel !== undefined){
						currentPanel.webview.html = getWebview((<string>data));
					}
				});

				// Reset when the current panel is closed
				currentPanel.onDidDispose(
					() => {
						 currentPanel = undefined;
					},
					null,
					context.subscriptions
				);
			}
		}
}));

	context.subscriptions.push(vscode.commands.registerCommand("uaml-consumer.getDataFromProject", () => {
		let filesData: string[] = [];
		
		vscode.workspace.findFiles('**/*.uaml', null).then(res => {
			Promise.all(
				// README: возможно могут быть баги из-за асинхронного вызова документов
				res.map(async (uri)=> {
					const doc = await vscode.workspace.openTextDocument(uri);
					filesData.push(doc.getText());
				})
			).then(() => {
				let workspaceName = vscode.workspace.name;
				console.log("workspace name: ", workspaceName);
				getUaml((<vscode.WorkspaceConfiguration>cfg), filesData, headers, getPdf, workspaceName).then((blob) => {
					if(blob instanceof Blob && blob !== undefined) {
						(<Blob>blob).arrayBuffer().then((buf) => {
								let path: string | undefined = vscode.workspace.rootPath;
								fs.appendFile(`${path}/${workspaceName}.pdf`, Buffer.from(buf), err => {
								if (err) {
									console.error("can't write data to file: ", err);
									return;
								}
								console.log("file write saccessfuly");
							});
						});
					}
				});
			});
		});
	}));
}

// this method is called when your extension is deactivated
export function deactivate() {}

async function getUaml(cfg: vscode.WorkspaceConfiguration, body?: any, headers?: any, method?: string, workspaceName?: string): Promise<string | Blob | undefined> {
	console.log('start requesting to service');
	
	let _headers = new Headers(headers);
	
	let url = cfg.get("server.uri");
	let login = cfg.get("login");
	let password = cfg.get("password");

	_headers.set('Authorization', 'Basic ' + (login + ":" + password));

	console.log("Headers: ", _headers);
	console.log("BODY: ", body);

	let uri = `${url}${method}${workspaceName}`;
	console.log(uri);

	const response = await fetch(`${uri}`, {
      method: 'PUT',
	  body: JSON.stringify(body),
	  headers: _headers,
	});

	console.log("response status: ", response.status);
	if (!response.ok) {
		console.log("message: ", response.statusText);
		if (response.status === 401) {
			vscode.window.showErrorMessage("Unauthorized");
		}
	}

	switch (method) {
		case uamlToSvg:
			let data: Promise<string> = response.text();
			return data;
		case getPdf:
			//let p = vscode.workspace.rootPath;
			let blob = response.blob();
			return blob;
	}
}

function getWebview(src?: string): string {
	return `<!DOCTYPE html>
	<html lang="en">
	<head>
		<meta charset="UTF-8">
		<meta name="viewport" content="width=device-width, initial-scale=0.5">
		<title>Svg preview</title>
	</head>
	<body>
		${src}
	</body>
	</html>`;
}

