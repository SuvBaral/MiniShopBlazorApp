import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as path from 'path';
import { translateNL, getAutocompleteSuggestions } from './nlTranslator';
import { classifyRisk } from './riskClassifier';

export function activate(context: vscode.ExtensionContext) {
    const provider = new GitBuddyProvider(context);

    context.subscriptions.push(
        vscode.window.registerWebviewViewProvider('gitBuddy.mainView', provider),
        vscode.commands.registerCommand('gitBuddy.refresh', () => provider.notifyRepoChanged()),
        vscode.commands.registerCommand('gitBuddy.nlCommand', () => provider.focusNLBar())
    );

    // Watch git state changes
    const watcher = vscode.workspace.createFileSystemWatcher('**/.git/{HEAD,refs/**,index}');
    watcher.onDidChange(() => provider.notifyRepoChanged());
    watcher.onDidCreate(() => provider.notifyRepoChanged());
    context.subscriptions.push(watcher);
}

class GitBuddyProvider implements vscode.WebviewViewProvider {
    private _view?: vscode.WebviewView;

    constructor(private ctx: vscode.ExtensionContext) {}

    resolveWebviewView(view: vscode.WebviewView) {
        this._view = view;
        view.webview.options = {
            enableScripts: true,
            localResourceRoots: [vscode.Uri.joinPath(this.ctx.extensionUri, 'blazor-app')]
        };
        view.webview.html = this.getHtml(view.webview);

        view.webview.onDidReceiveMessage(async (msg) => {
            let result: any;
            try {
                switch (msg.command) {
                    // === Workspace diagnostics ===
                    case 'get-workspace': {
                        const cwd = this.getCwd();
                        const folders = vscode.workspace.workspaceFolders?.map(f => f.uri.fsPath) ?? [];
                        result = { success: !!cwd, output: cwd ?? '', error: cwd ? null : 'No workspace folder open', folders };
                        break;
                    }
                    // === Git operations ===
                    case 'get-branches':
                    case 'get-status':
                    case 'get-sync-info':
                    case 'checkout':
                    case 'pull':
                    case 'push':
                    case 'fetch':
                    case 'merge':
                    case 'rebase':
                    case 'stash-save':
                    case 'stash-apply':
                    case 'stash-drop':
                    case 'stash-list':
                    case 'create-branch':
                    case 'delete-branch':
                    case 'rename-branch':
                    case 'get-repo-name':
                    case 'get-current-branch':
                    case 'sync':
                    // === Merge Conflict Resolution ===
                    case 'get-conflicts':
                    case 'get-conflict-diff':
                    case 'resolve-conflict':
                    case 'abort-merge':
                    case 'continue-merge':
                    // === Commit Composer ===
                    case 'get-staged-changes':
                    case 'get-unstaged-changes':
                    case 'commit':
                    case 'commit-amend':
                    // === Interactive Staging ===
                    case 'stage-file':
                    case 'unstage-file':
                    case 'stage-all':
                    case 'unstage-all':
                    case 'stage-hunk':
                    case 'unstage-hunk':
                    case 'get-file-diff':
                    case 'discard-file-changes':
                    // === Git Graph / History ===
                    case 'get-commit-log':
                    case 'get-commit-detail':
                    case 'get-graph-data':
                    case 'cherry-pick':
                    // === Tag Management ===
                    case 'get-tags':
                    case 'create-tag':
                    case 'delete-tag':
                    case 'push-tag':
                    // === Blame ===
                    case 'get-blame':
                        result = await this.execGit(msg.command, msg.payload);
                        break;

                    // === View Diff in VS Code editor ===
                    case 'view-diff':
                        result = await this.handleViewDiff(msg.payload);
                        break;

                    // === AI Commit Message ===
                    case 'generate-commit-message':
                        result = await this.handleGenerateCommitMessage();
                        break;

                    // === NL Translation ===
                    case 'nl-translate':
                        result = await this.handleNLTranslate(msg.payload.text);
                        break;

                    case 'nl-execute':
                        result = await this.handleNLExecute(msg.payload.command);
                        break;

                    case 'nl-autocomplete':
                        result = await this.handleAutocomplete(msg.payload.text);
                        break;

                    // === Open file in VS Code editor ===
                    case 'open-file': {
                        const cwd = this.getCwd();
                        const file: string = msg.payload?.file || '';
                        if (cwd && file) {
                            const uri = vscode.Uri.file(path.join(cwd, file));
                            await vscode.commands.executeCommand('vscode.open', uri);
                            result = { success: true };
                        } else {
                            result = { success: false, error: 'No workspace or file provided' };
                        }
                        break;
                    }

                    default:
                        result = { error: `Unknown command: ${msg.command}` };
                }
            } catch (e: any) {
                result = { success: false, error: e.message };
            }

            view.webview.postMessage({ type: 'response', requestId: msg.requestId, data: result });
        });
    }

    // --- Resolve the working directory, preferring the active file's workspace folder ---
    private getCwd(): string | undefined {
        // Prefer the workspace folder that contains the currently active file
        const activeUri = vscode.window.activeTextEditor?.document.uri;
        if (activeUri) {
            const folder = vscode.workspace.getWorkspaceFolder(activeUri);
            if (folder) { return folder.uri.fsPath; }
        }
        // Fall back to first workspace folder
        return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
    }

    // --- Core git runner: no shell, args passed directly to git process ---
    private spawnGit(args: string[], cwd?: string, timeout: number = 30000): Promise<any> {
        cwd = cwd || this.getCwd();

        return new Promise(resolve => {
            cp.execFile('git', args, { cwd, timeout }, (err, stdout, stderr) => {
                let errorMsg = err ? (stderr.trim() || err.message) : null;
                // Detect auth failures and guide the user
                if (errorMsg && /authentication failed|could not read username|permission denied|403|401|invalid credentials|remote: repository not found/i.test(errorMsg)) {
                    errorMsg = `Authentication failed. Make sure git credentials are configured:\n• Run: git config --global credential.helper manager\n• Or use SSH: git remote set-url origin git@github.com:user/repo.git\n\nOriginal error: ${errorMsg}`;
                }
                resolve({
                    success: !err,
                    output: stdout.trim(),
                    error: errorMsg
                });
            });
        });
    }

    // --- Parse a "git <args>" string into an args array, handling quoted tokens ---
    private parseGitCommand(cmd: string): string[] | null {
        const trimmed = cmd.trim();
        if (!/^git(\s|$)/i.test(trimmed)) { return null; }

        const rest = trimmed.slice(3).trim();
        if (!rest) { return []; }

        const args: string[] = [];
        let current = '';
        let inQuote: string | null = null;

        for (const char of rest) {
            if (inQuote) {
                if (char === inQuote) { inQuote = null; }
                else { current += char; }
            } else if (char === '"' || char === "'") {
                inQuote = char;
            } else if (char === ' ' || char === '\t') {
                if (current) { args.push(current); current = ''; }
            } else {
                current += char;
            }
        }
        if (current) { args.push(current); }
        return args;
    }

    // --- NL Translation (3-Tier) ---
    private async handleNLTranslate(text: string): Promise<any> {
        const cwd = this.getCwd();
        if (!cwd) { return { error: 'No workspace' }; }

        // Get repo context for LLM tier
        const branchesRes = await this.spawnGit(['branch', '-a', '--format=%(refname:short)'], cwd);
        const currentBranchRes = await this.spawnGit(['branch', '--show-current'], cwd);
        const statusRes = await this.spawnGit(['status', '--short'], cwd);

        const context = {
            currentBranch: currentBranchRes.output.trim(),
            branches: branchesRes.output.split('\n').map((b: string) => b.trim()).filter(Boolean),
            hasUncommittedChanges: statusRes.output.trim().length > 0,
            statusSummary: statusRes.output.trim()
        };

        // 3-tier translation
        const translation = await translateNL(text, context);

        // Classify risk (deterministic, keyword-based)
        if (translation.command) {
            const risk = classifyRisk(translation.command);
            translation.risk = risk.level;
            translation.requiresConfirmation = risk.requiresConfirmation;
            translation.warning = risk.warning ?? undefined;

            // Auto-backup for dangerous commands
            const config = vscode.workspace.getConfiguration('gitBuddy');
            if (risk.level === 'dangerous' && config.get<boolean>('autoBackupOnDangerous', true)) {
                await this.spawnGit(['stash', 'push', '-m', `auto-backup-${Date.now()}`], cwd);
            }
        }

        // Save to history
        const history = this.ctx.workspaceState.get<any[]>('nlHistory', []);
        history.unshift({ nl: text, git: translation.command, time: Date.now() });
        await this.ctx.workspaceState.update('nlHistory', history.slice(0, 100));

        return translation;
    }

    // --- NL Execute: safely parse and run the translated git command ---
    private async handleNLExecute(rawCmd: string): Promise<any> {
        const cwd = this.getCwd();
        if (!cwd) { return { success: false, error: 'No workspace open. Open a folder with a git repository first.' }; }

        // Support compound commands chained with &&
        if (rawCmd.includes('&&')) {
            const parts = rawCmd.split('&&').map(s => s.trim());
            let lastResult: any;
            for (const part of parts) {
                const args = this.parseGitCommand(part);
                if (!args) {
                    return { success: false, error: `Only git commands are allowed: "${part}"` };
                }
                lastResult = await this.spawnGit(args, cwd, 300000);
                if (!lastResult.success) { return lastResult; }
            }
            return lastResult;
        }

        const args = this.parseGitCommand(rawCmd);
        if (!args) {
            return { success: false, error: 'Only git commands are allowed' };
        }
        return this.spawnGit(args, cwd, 300000);
    }

    private async handleAutocomplete(partial: string): Promise<any[]> {
        const cwd = this.getCwd();
        if (!cwd) { return []; }
        return getAutocompleteSuggestions(partial, cwd);
    }

    // --- Git Execution ---
    private async execGit(command: string, payload: any): Promise<any> {
        const cwd = this.getCwd();
        if (!cwd) { return { success: false, error: 'No workspace open' }; }

        if (command === 'get-sync-info') {
            try {
                const res = await this.spawnGit(['rev-list', '--left-right', '--count', 'HEAD...@{upstream}'], cwd);
                if (res.success && res.output && !res.output.includes('fatal')) {
                    return res;
                }
            } catch {}
            return { success: true, output: '0\t0', error: null };
        }

        if (command === 'get-conflict-diff') {
            const file = payload?.file || '';
            // Try working-tree file first — it contains the <<<<<<</<=======/>>>>>>> markers
            try {
                const fs = require('fs');
                const content = fs.readFileSync(require('path').join(cwd, file), 'utf8');
                return { success: true, output: content, error: null };
            } catch {
                // Fall back to git show :3: (their side) if the file can't be read
                const gitRes = await this.spawnGit(['show', `:3:${file}`], cwd);
                if (gitRes.success && gitRes.output && !gitRes.output.includes('fatal')) {
                    return gitRes;
                }
                return { success: false, error: `Could not read conflict file: ${file}` };
            }
        }

        if (command === 'sync') {
            const pullRes = await this.spawnGit(['pull'], cwd, 300000);
            if (!pullRes.success) { return pullRes; }
            return this.spawnGit(['push'], cwd, 300000);
        }

        type GitArgsEntry = string[] | ((p: any) => string[]);
        const cmds: Record<string, GitArgsEntry> = {
            'get-branches': ['branch', '-a', `--format=%(refname:short)\x1f%(objectname:short)\x1f%(upstream:short)\x1f%(HEAD)`],
            'get-status': ['status', '--porcelain'],
            'checkout': (p) => ['checkout', p?.branch],
            'pull': ['pull'],
            'push': ['push'],
            'fetch': ['fetch', '--all', '--prune'],
            'merge': (p) => ['merge', p?.branch],
            'rebase': (p) => ['rebase', p?.branch],
            'stash-list': ['stash', 'list', '--format=\x1f%gs\x1f%ci'],
            'stash-save': (p) => ['stash', 'push', '-m', p?.message || 'stash'],
            'stash-apply': (p) => ['stash', 'apply', `stash@{${p?.index ?? 0}}`],
            'stash-drop': (p) => ['stash', 'drop', `stash@{${p?.index ?? 0}}`],
            'create-branch': (p) => p?.from
                ? ['checkout', '-b', p.name, p.from]
                : ['checkout', '-b', p?.name],
            'delete-branch': (p) => ['branch', '-d', p?.branch],
            'rename-branch': (p) => ['branch', '-m', p?.oldName, p?.newName],
            'get-repo-name': ['rev-parse', '--show-toplevel'],
            'get-current-branch': ['branch', '--show-current'],
            // === Merge Conflict Resolution ===
            'get-conflicts': ['diff', '--name-only', '--diff-filter=U'],
            'resolve-conflict': (p) => ['add', p?.file],
            'abort-merge': ['merge', '--abort'],
            'continue-merge': ['commit', '--no-edit'],
            // === Commit Composer ===
            'get-staged-changes': ['diff', '--cached', '--name-status'],
            'get-unstaged-changes': ['status', '--porcelain', '--untracked-files=all'],
            'commit': (p) => ['commit', '-m', p?.message || ''],
            'commit-amend': (p) => ['commit', '--amend', '-m', p?.message || ''],
            // === Interactive Staging ===
            'stage-file': (p) => ['add', p?.file],
            'unstage-file': (p) => ['reset', 'HEAD', p?.file],
            'stage-all': ['add', '-A'],
            'unstage-all': ['reset', 'HEAD'],
            'stage-hunk': (p) => ['add', '-p', p?.file],
            'unstage-hunk': (p) => ['reset', '-p', p?.file],
            'get-file-diff': (p) => p?.staged
                ? ['diff', '--cached', p?.file]
                : ['diff', p?.file],
            'discard-file-changes': (p) => ['checkout', '--', p?.file],
            // === Git Graph / History ===
            'get-commit-log': (p) => {
                const args = ['log', '--format=%H|%s|%an|%ae|%ci|%P|%D', `-${p?.count || 50}`];
                if (p?.branch) { args.push(p.branch); }
                return args;
            },
            'get-commit-detail': (p) => ['show', '--stat', '--format=%H|%s|%an|%ae|%ci|%P|%D|%B', p?.hash || 'HEAD'],
            'get-graph-data': (p) => ['log', '--format=%H|%s|%an|%ae|%ci|%P|%D', '--graph', `-${p?.count || 50}`],
            'cherry-pick': (p) => ['cherry-pick', p?.hash || ''],
            // === Tag Management ===
            'get-tags': ['tag', '-l', '--format=%(refname:short)|%(objectname:short)|%(contents:subject)|%(taggername)|%(creatordate:short)|%(objecttype)'],
            'create-tag': (p) => p?.message
                ? ['tag', '-a', p.name, '-m', p.message, ...(p?.commit ? [p.commit] : [])]
                : ['tag', p?.name || '', ...(p?.commit ? [p.commit] : [])],
            'delete-tag': (p) => ['tag', '-d', p?.name || ''],
            'push-tag': (p) => ['push', 'origin', p?.name || ''],
            // === Blame ===
            'get-blame': (p) => ['blame', '--porcelain', p?.file],
        };

        const entry = cmds[command];
        if (!entry) { return { success: false, error: `Unknown: ${command}` }; }

        const rawArgs = typeof entry === 'function' ? entry(payload) : entry;
        const args = rawArgs.filter((a): a is string => a != null && a !== '');

        const isNetworkOp = ['pull', 'push', 'fetch'].includes(command);
        return this.spawnGit(args, cwd, isNetworkOp ? 300000 : 30000);
    }

    // --- Open VS Code diff editor for a file ---
    private async handleViewDiff(payload: any): Promise<any> {
        const cwd = this.getCwd();
        const file: string = payload?.file || '';
        const staged: boolean = !!payload?.staged;

        if (!cwd || !file) {
            return { success: false, error: 'No workspace or file provided' };
        }

        const fileName = path.basename(file);

        if (staged) {
            // Staged diff: compare HEAD vs index
            const headRes = await this.spawnGit(['show', `HEAD:${file}`], cwd);

            // If HEAD doesn't have this file, it's a new file being staged for the first time
            let headContent = '';
            let isNewFile = false;
            if (!headRes.success) {
                headContent = '';
                isNewFile = true;
            } else {
                headContent = headRes.output;
            }

            const indexRes = await this.spawnGit(['show', `:${file}`], cwd);

            const headDoc = await vscode.workspace.openTextDocument({
                content: headContent
            });
            const indexDoc = await vscode.workspace.openTextDocument({
                content: indexRes.success ? indexRes.output : ''
            });

            const title = isNewFile
                ? `${fileName} (New File — Staged)`
                : `${fileName} (Staged Changes)`;

            await vscode.commands.executeCommand(
                'vscode.diff',
                headDoc.uri,
                indexDoc.uri,
                title
            );
        } else {
            // Unstaged diff: compare index vs working tree
            const indexRes = await this.spawnGit(['show', `:${file}`], cwd);
            const indexDoc = await vscode.workspace.openTextDocument({
                content: indexRes.success ? indexRes.output : ''
            });

            const workspaceUri = vscode.Uri.file(path.join(cwd, file));
            await vscode.commands.executeCommand(
                'vscode.diff',
                indexDoc.uri,
                workspaceUri,
                `${fileName} (Changes)`
            );
        }

        return { success: true };
    }

    // --- AI Commit Message Generation ---
    private async handleGenerateCommitMessage(): Promise<any> {
        const cwd = this.getCwd();
        if (!cwd) { return { success: false, error: 'No workspace' }; }

        const diffStatRes = await this.spawnGit(['diff', '--cached', '--stat'], cwd);
        const diffRes = await this.spawnGit(['diff', '--cached'], cwd);
        const diff = diffStatRes.output;
        const diffContent = diffRes.output;

        if (!diff.trim()) { return { success: false, error: 'No staged changes' }; }

        // Try VS Code LM API first
        try {
            const models = await vscode.lm.selectChatModels({ vendor: 'copilot', family: 'gpt-4o-mini' });
            if (models.length > 0) {
                const prompt = `Generate a concise git commit message for these changes. Use conventional commit format (type: description). Return ONLY the commit message, nothing else.\n\nDiff stat:\n${diff.slice(0, 500)}\n\nDiff:\n${diffContent.slice(0, 2000)}`;
                const messages = [vscode.LanguageModelChatMessage.User(prompt)];
                const response = await models[0].sendRequest(messages, {}, new vscode.CancellationTokenSource().token);
                let result = '';
                for await (const chunk of response.text) { result += chunk; }
                return { success: true, output: result.trim().replace(/^["']|["']$/g, '') };
            }
        } catch { /* fallback to heuristic */ }

        // Fallback: heuristic commit message from diff stat
        const lines = diff.split('\n').filter((l: string) => l.includes('|'));
        const files = lines.map((l: string) => l.split('|')[0].trim());
        const additions = diff.match(/(\d+) insertion/);
        const deletions = diff.match(/(\d+) deletion/);

        let type = 'chore';
        if (files.some((f: string) => f.includes('test'))) { type = 'test'; }
        else if (files.some((f: string) => f.includes('.css') || f.includes('.html') || f.includes('.razor'))) { type = 'style'; }
        else if (files.some((f: string) => f.includes('fix') || f.includes('bug'))) { type = 'fix'; }
        else if (additions && parseInt(additions[1]) > 50) { type = 'feat'; }

        const scope = files.length === 1 ? files[0].split('/').pop()?.replace(/\.[^.]+$/, '') : '';
        const desc = `update ${files.length} file${files.length > 1 ? 's' : ''}`;
        const msg = scope ? `${type}(${scope}): ${desc}` : `${type}: ${desc}`;

        return { success: true, output: msg };
    }

    notifyRepoChanged() { this._view?.webview.postMessage({ type: 'repoChanged' }); }
    focusNLBar() { this._view?.webview.postMessage({ type: 'focusNLBar' }); }

    private getHtml(wv: vscode.Webview): string {
        const base = wv.asWebviewUri(vscode.Uri.joinPath(this.ctx.extensionUri, 'blazor-app'));
        return `<!DOCTYPE html><html><head>
<meta charset="utf-8"/>
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval' ${wv.cspSource}; style-src 'unsafe-inline' ${wv.cspSource}; font-src ${wv.cspSource}; connect-src ${wv.cspSource} blob:; img-src ${wv.cspSource} blob: data:; worker-src blob: ${wv.cspSource};"/>
<link href="${base}/css/app.css" rel="stylesheet"/>
</head><body>
<div id="app"><div id="loading-msg" style="padding:16px;color:var(--vscode-foreground)">Loading Git Buddy...</div></div>
<script>
// VS Code bridge
const vscode = acquireVsCodeApi();
let dn = null;
window.bridge = {
    init: h => { dn = h; },
    send: m => { vscode.postMessage(m); }
};
window.addEventListener('message', e => {
    const m = e.data;
    if (m.type === 'response' && dn) dn.invokeMethodAsync('OnResponse', m.requestId, JSON.stringify(m.data));
    if (m.type === 'repoChanged' && dn) dn.invokeMethodAsync('OnRepoChange');
    if (m.type === 'focusNLBar' && dn) dn.invokeMethodAsync('OnFocusNLBarRequest');
});
// Show errors inside the panel for easier debugging
window.onerror = function(msg, src, line, col, err) {
    const el = document.getElementById('app');
    if (el) el.innerHTML = '<pre style="color:#f44;padding:16px;white-space:pre-wrap;font-size:12px">Git Buddy error:\\n' + msg + (err ? '\\n' + err.stack : '') + '</pre>';
};
</script>
<script src="${base}/_framework/blazor.webassembly.js" autostart="false"></script>
<script>
// Use loadBootResource so Blazor fetches files via explicit webview URIs
// instead of resolving against document.baseURI (which breaks in webview context)
(function startBlazor() {
    if (typeof Blazor === 'undefined') {
        setTimeout(startBlazor, 50);
        return;
    }
    Blazor.start({
        loadBootResource: function(type, name, defaultUri, integrity) {
            return '${base}/_framework/' + name;
        }
    });
})();
</script>
</body></html>`;
    }
}

export function deactivate() {}
