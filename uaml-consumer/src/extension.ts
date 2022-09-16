import * as vscode from 'vscode';
import fetch, { Blob, Headers } from 'node-fetch';
import * as fs from 'fs';

const uamlToSvg = "/api/Uaml/GeneratePageAsSvg";
const getPdf = "/api/Uaml/GenerateProject/";

export function activate(context: vscode.ExtensionContext) {

	let cfg: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration("uamlConsumer");

	context.subscriptions.push(
		vscode.commands.registerCommand('uamlConsumer.start', () => {
			const file: vscode.TextDocument | undefined = vscode.window.activeTextEditor?.document;
			if(file?.fileName.endsWith('.uaml')) {
				// Open preview only if file has extension '.uaml'
				let body: string = file.getText();
				getUaml(cfg, body, uamlToSvg, ""). then((data) => {
					UamlConsumerPanel.createOrShow(context.extensionUri, (<string>data));
				});
			} else {
				vscode.window.showErrorMessage('wrong extension file');
			}
		})
	);

	context.subscriptions.push(vscode.commands.registerCommand("uamlConsumer.pdf", () => {
		let filesData: string[] = [];
		
		vscode.workspace.findFiles('**/*.uaml', null).then(res => {
			Promise.all(
				// README: возможно могут быть баги из-за асинхронного вызова документов
				res.map(async (uri)=> {
					const doc = await vscode.workspace.openTextDocument(uri);
					filesData.push(doc.getText());
				})
			).then(() => {
				let workspaceName = "";
				
				if (vscode.workspace.name) {
					workspaceName = vscode.workspace.name;
				}
				
				getUaml(cfg, filesData, getPdf, workspaceName).then((blob) => {
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

	if (vscode.window.registerWebviewPanelSerializer) {
		// Make sure we register a serializer in activation event
		vscode.window.registerWebviewPanelSerializer(UamlConsumerPanel.viewType, {
			async deserializeWebviewPanel(webviewPanel: vscode.WebviewPanel, state: any) {
				console.log(`Got state: ${state}`);
				// Reset the webview options so we use latest uri for `localResourceRoots`.
				webviewPanel.webview.options = getWebviewOptions(context.extensionUri);
				UamlConsumerPanel.revive(webviewPanel, context.extensionUri);
			}
		});
	}
}

function getWebviewOptions(extensionUri: vscode.Uri): vscode.WebviewOptions {
	return {
		// Enable javascript in the webview
		enableScripts: true,

		// And restrict the webview to only loading content from our extension's `media` directory.
		localResourceRoots: [vscode.Uri.joinPath(extensionUri, 'media')]
	};
}

/**
 * Manages svg webview panels
 */
class UamlConsumerPanel {
	/**
	 * Track the currently panel. Only allow a single panel to exist at a time.
	 */
	public static currentPanel: UamlConsumerPanel | undefined;
	
	public static readonly viewType = 'uamlConsumer';
	
	
	//private readonly _currentEditor: vscode.TextEditor;
	private readonly _panel: vscode.WebviewPanel;
	
	private readonly _extensionUri: vscode.Uri;
	private _disposables: vscode.Disposable[] = [];

	public static createOrShow(extensionUri: vscode.Uri, data: string) {
		const column = vscode.window.activeTextEditor
			? vscode.ViewColumn.Beside
			: undefined;

		// If we already have a panel, show it.
		if (UamlConsumerPanel.currentPanel) {
			UamlConsumerPanel.currentPanel._panel.reveal(column);
			this.revive(UamlConsumerPanel.currentPanel._panel, extensionUri, data);
			return;
		}

		// Otherwise, create a new panel.
		const panel = vscode.window.createWebviewPanel(
			UamlConsumerPanel.viewType,
			'Preview',
			vscode.ViewColumn.Beside,
			getWebviewOptions(extensionUri),
		);

		UamlConsumerPanel.currentPanel = new UamlConsumerPanel(panel, extensionUri, data);
	}

	public static revive(panel: vscode.WebviewPanel, extensionUri: vscode.Uri, data?: any) {
		UamlConsumerPanel.currentPanel = new UamlConsumerPanel(panel, extensionUri, data);
	}

	private constructor(panel: vscode.WebviewPanel, extensionUri: vscode.Uri, data?: string) {	
		this._panel = panel;
		this._extensionUri = extensionUri;

		// Set html initial
		this._panel.webview.html = this._getHtmlForWebview(this._panel.webview, data);

		// Listen for when the panel is disposed
		// This happens when the user closes the panel or when the panel is closed programmatically
		this._panel.onDidDispose(() => this.dispose(), null, this._disposables);


		// Handle messages from the webview
		this._panel.webview.onDidReceiveMessage(
			message => {
				switch (message.command) {
					case 'relative':
						vscode.window.showInformationMessage(message.text);
						let path = vscode.Uri.joinPath(vscode.Uri.parse(<string>vscode.workspace.rootPath), message.text);
						console.log(path);
						vscode.workspace.openTextDocument(path).then((doc) => {
							vscode.window.showTextDocument(doc, vscode.ViewColumn.One, false);
						}).then(undefined, console.error);
						return;
				}
			},
			null,
			this._disposables
		);
	}

	public dispose() {
		UamlConsumerPanel.currentPanel = undefined;


		// Clean up our resources
		this._panel.dispose();

		while (this._disposables.length) {
			const x = this._disposables.pop();
			if (x) {
				x.dispose();
			}
		}
	}

	private _getHtmlForWebview(webview: vscode.Webview, data?: any) {
		// Local path to main script run in the webview
		const scriptPathOnDisk = vscode.Uri.joinPath(this._extensionUri, 'media', 'main.js');

		// And the uri we use to load this script in the webview
		const scriptUri = webview.asWebviewUri(scriptPathOnDisk);

		console.log(data);


		return `<!DOCTYPE html>
			<html lang="en">
			<head>
				<meta charset="UTF-8">
				<meta name="viewport" content="width=device-width, initial-scale=1.0">
				<title>Svg webview</title>
				</head>
			<body>
			<span id="zoomValue">1</span>
			<div id="svgContainer">
				${data}
			</div>
			<script src="${scriptUri}"></script>
			</body>
			</html>`;
	}
}

async function getUaml(cfg: vscode.WorkspaceConfiguration, body: any, method: string, workspaceName: string): Promise<string | Blob | undefined> {
	console.log('start requesting to service');

	let headers: any = cfg.get("server.optional.headers");
	let url = cfg.get("server.uri");
	let login = cfg.get("login");
	let password = cfg.get("password");
	
	let _headers = new Headers(headers);
	_headers.set('Authorization', 'Basic ' + Buffer.from(login + ":" + password).toString('base64'));

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
