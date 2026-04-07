# Contributing to Git Buddy

Thank you for your interest in contributing! This document covers how to set up the project, the coding conventions, and the pull request process.

---

## Table of Contents

- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Running the Extension Locally](#running-the-extension-locally)
- [Running Tests](#running-tests)
- [Coding Conventions](#coding-conventions)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Features](#suggesting-features)

---

## Development Setup

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Node.js | 18+ | Extension host |
| npm | 9+ | JS dependencies |
| .NET SDK | 9.0 | Blazor WASM UI |
| VS Code | 1.107+ | Running the extension |
| PowerShell | 5.1+ | Build script |

### First-time setup

```bash
git clone https://github.com/YOUR-USERNAME/GitBuddy.git
cd GitBuddy

# Install Node.js dependencies
cd src/extension
npm install
cd ../..

# Restore .NET packages
dotnet restore
```

---

## Project Structure

```
src/
├── extension/               # VS Code extension host (TypeScript)
│   ├── src/
│   │   ├── extension.ts     # Main entry point, webview provider, git runner
│   │   ├── nlTranslator.ts  # Natural language → git translation
│   │   ├── riskClassifier.ts
│   │   └── gitPatterns.ts   # Offline pattern library
│   └── package.json
├── GitBuddy.Core/          # Shared C# models and parsers (referenced by both UI and tests)
└── GitBuddy.UI/            # Blazor WASM application
    ├── Components/          # Razor components (one folder per feature panel)
    ├── Services/            # C# services (GitService, BranchParser, etc.)
    └── Models/              # UI-only view models

tests/
├── extension.tests/         # Jest tests for NL translation, risk classification
└── GitBuddy.UI.Tests/      # xUnit tests for parsers and state
```

---

## Running the Extension Locally

### Option A — Full rebuild and install VSIX

```bash
# From repo root
powershell -File build.ps1

cd src/extension
npx vsce package
```

Then install the `.vsix` via `Extensions → ··· → Install from VSIX`.

### Option B — Iterative Blazor development

If you're only changing Razor components or C# services:

```bash
cd src/GitBuddy.UI
dotnet publish -c Release -o ../extension/blazor-app

# Then reload the extension in your editor
```

If you're only changing `extension.ts`:

```bash
cd src/extension
npm run compile
# Reload the VS Code window: Ctrl+Shift+P → "Developer: Reload Window"
```

---

## Running Tests

Always run both test suites before opening a PR.

```bash
# .NET tests
dotnet test tests/GitBuddy.UI.Tests/

# TypeScript tests
cd tests/extension.tests
npm test
```

All tests must pass. Do not open a PR with failing tests.

---

## Coding Conventions

### TypeScript (extension host)

- Use `cp.execFile` (never `cp.exec`) for all git calls — this prevents shell injection
- Every git command must go through `spawnGit(args: string[], ...)` — never concatenate user input into a shell string
- New git operations belong in the `cmds` map in `execGit()`, using the `string[] | ((p) => string[])` pattern
- Keep `extension.ts` focused on the bridge, git runner, and webview HTML. Move new logic to separate `.ts` files

### C# / Blazor

- Models live in `src/GitBuddy.Core/Models/` — must be plain classes with `[JsonConstructor]` and `[DynamicallyAccessedMembers]` attributes (required for `TrimMode=full` Blazor WASM)
- Services live in `src/GitBuddy.UI/Services/` — use `GitService` for all git calls (calls the bridge)
- Payloads passed to `_bridge.SendAsync` must use `JsonObject` (not anonymous types) — this is required for trim-safety in Blazor WASM
- Use `@bind:event="oninput"` + `@bind:after` pattern for textarea binding in webview components
- Follow existing component conventions: `EventCallback` parameters for cross-component communication

### General

- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`
- No `TODO`, `FIXME`, or commented-out code blocks in merged PRs
- No hardcoded paths, credentials, or API keys — ever

---

## Submitting a Pull Request

1. Fork the repository and create a branch from `master`:
   ```bash
   git checkout -b feat/your-feature-name
   ```

2. Write your code. Add or update tests.

3. Run the test suites and confirm they pass.

4. Push your branch and open a PR against `master`.

5. Fill in the PR template — describe the change, link any related issue, and note how you tested it.

PRs are reviewed on a best-effort basis. Small, focused PRs are merged faster than large ones.

---

## Reporting Bugs

Open an issue with:
- VS Code version and OS
- Steps to reproduce
- What you expected vs. what happened
- The error message from the VS Code Output panel (`View → Output → Git Buddy`) if applicable

---

## Suggesting Features

Open an issue with the label `enhancement`. Describe the use case — not just the feature — so we can understand the problem it solves.

---

## Security

If you discover a security vulnerability, please **do not** open a public issue. Email the maintainer directly. See the [README](README.md) for contact information.
