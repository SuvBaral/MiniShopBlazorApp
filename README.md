# Git Buddy

> A Visual Studio Team Explorer-style Git panel for VS Code — do everything with natural language or one click, no git commands required.

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)
![VS Code](https://img.shields.io/badge/VS%20Code-1.107%2B-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)

Git Buddy is a VS Code extension that puts a full Git panel directly in your sidebar. Type plain English like *"merge feature into main"* or click through a familiar UI to stage files, commit, view history, manage branches, and resolve conflicts — all without leaving your editor.

---

## Features

### Natural Language Commands
Type git instructions in plain English. A 3-tier engine (pattern matching → fuzzy matching → GitHub Copilot) translates them into real git commands before execution.

```
switch to main            → git checkout main
create branch feature/x   → git checkout -b feature/x
merge develop             → git merge develop
undo last commit          → git reset --soft HEAD~1
push to origin            → git push
```

### Interactive Staging
Stage and unstage individual files with a single click. Open a VS Code diff editor for any file directly from the panel.

### Commit Composer
- Built-in commit message templates (feat, fix, chore, docs…)
- AI-generated commit message from your staged diff (uses Copilot if available)
- Keyboard shortcut: `Ctrl+Enter` to commit

### Branch Management
- See all local and remote branches at a glance
- Checkout, create, rename, delete branches from a right-click menu
- Ahead/behind sync indicator on the current branch

### Commit History
- Scrollable log of recent commits with author, date, and message
- One-click cherry-pick
- Copy commit hash or message to clipboard

### Tag Management
- Create lightweight or annotated tags
- Delete tags locally
- Push individual tags to remote

### Merge Conflict Resolution
- Dedicated panel appears automatically when conflicts exist
- Accept ours / Accept theirs / Mark resolved per file
- Abort merge or continue after resolution

### Safety by Default
- Every command is classified as **safe**, **moderate**, or **dangerous**
- Moderate operations (merge, rebase, checkout) ask for confirmation
- Dangerous operations (hard reset, force push) auto-stash your work first and show a prominent warning
- All classification is local, instant, and offline — no AI needed

### Quick Actions Bar
Fetch, Pull, Push, Sync, and Stash are always one click away at the top.

---

## Screenshots

> Screenshots will be added after the first public release.

---

## Installation

### From VSIX (current release)

1. Download `git-buddy-1.0.0.vsix` from the [Releases](https://github.com/YOUR-USERNAME/GitBuddy/releases) page
2. In VS Code: `Extensions (Ctrl+Shift+X)` → `···` menu → `Install from VSIX…`
3. Select the downloaded file

### From VS Code Marketplace

> Not yet published. Follow the repository for updates.

---

## Requirements

| Requirement | Version |
|-------------|---------|
| VS Code (or compatible fork) | 1.107+ |
| Git | Any recent version |
| .NET (for building from source) | 9.0 SDK |
| GitHub Copilot (optional) | For AI commit messages and NL fallback |

---

## Usage

### Open the panel
Click the Git Buddy icon in the Activity Bar (branch icon), or run `View → Git Buddy`.

### Natural language bar
The text box at the top accepts plain English. Press **Enter** to translate and execute. Safe commands run immediately; risky ones show a confirmation dialog first.

Examples:
```
fetch all remotes
show me the last 10 commits
stash my changes with message "wip: login"
cherry-pick abc123f
push tags
```

### Keyboard shortcut
`Ctrl+Shift+G Ctrl+Shift+N` — focuses the natural language input from anywhere.

### Settings
Open `Ctrl+,` and search for **Git Buddy**:

| Setting | Default | Description |
|---------|---------|-------------|
| `gitBuddy.nlProvider` | `auto` | AI provider: `auto`, `copilot`, `offline` |
| `gitBuddy.confirmModerate` | `true` | Confirm before merge / rebase / checkout |
| `gitBuddy.autoBackupOnDangerous` | `true` | Auto-stash before dangerous operations |

---

## Building from Source

```bash
# 1. Clone the repository
git clone https://github.com/YOUR-USERNAME/GitBuddy.git
cd GitBuddy

# 2. Install Node dependencies
cd src/extension
npm install
cd ../..

# 3. Build Blazor UI + copy assets + compile TypeScript
powershell -File build.ps1

# 4. Package the extension
cd src/extension
npx vsce package
```

The output is `git-buddy-1.0.0.vsix` in `src/extension/`.

### Running Tests

```bash
# .NET / Blazor unit tests (xUnit)
dotnet test tests/GitSimple.UI.Tests/

# TypeScript unit tests (Jest)
cd tests/extension.tests
npm test
```

---

## Architecture

```
GitBuddy/
├── src/
│   ├── extension/               # VS Code extension host (TypeScript)
│   │   ├── extension.ts         # Webview provider, git runner (execFile — no shell injection)
│   │   ├── nlTranslator.ts      # 3-tier NL → git command translation
│   │   ├── riskClassifier.ts    # Safe / Moderate / Dangerous classification
│   │   └── gitPatterns.ts       # Offline pattern library
│   ├── GitSimple.Core/          # Shared models and parsers (C# / .NET 9)
│   └── GitSimple.UI/            # Blazor WASM UI (runs inside VS Code webview)
│       └── Components/          # Razor components for each panel
└── tests/
    ├── extension.tests/         # Jest tests for NL translation, patterns, risk
    └── GitSimple.UI.Tests/      # xUnit tests for parsers and state
```

**Key design decisions:**
- All git commands use `child_process.execFile` (not `exec`) — arguments are passed directly, no shell injection possible
- Blazor WASM communicates with the extension host via a `postMessage` bridge over the VS Code webview API
- The NL translation pipeline runs entirely offline unless Copilot is available and opted-in
- Risk classification is deterministic keyword-based, never requires a network call

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for the full guide.

Quick version:
1. Fork the repo and create a branch: `git checkout -b feat/your-feature`
2. Make your changes and add tests where appropriate
3. Run `dotnet test` and `npm test` — both must pass
4. Open a Pull Request against `master`

Please follow [Conventional Commits](https://www.conventionalcommits.org/) for commit messages.

---

## License

MIT — see [LICENSE](LICENSE).

---

*Git Buddy is an independent open-source project and is not affiliated with Microsoft, GitHub, or the VS Code team.*
