# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-04-07

### Added

- **Natural language command bar** — type plain English git instructions; translated via a 3-tier pipeline (regex patterns → fuzzy matching → GitHub Copilot LLM)
- **Risk classification** — every command is classified as safe / moderate / dangerous; moderate ops confirm, dangerous ops auto-stash and warn before running
- **Interactive staging** — stage / unstage individual files with a single click; open VS Code diff editor per file
- **Commit composer** — commit message textarea with built-in templates, AI message generation from staged diff, `Ctrl+Enter` shortcut, amend support
- **Branch management** — view local and remote branches, checkout, create, rename, delete via right-click context menu; ahead/behind sync badge on current branch
- **Commit history** — recent commit log with cherry-pick, copy hash, and copy message actions
- **Tag management** — create lightweight and annotated tags, delete, push to remote
- **Merge conflict resolution** — automatic panel when conflicts exist; accept ours/theirs per file, abort or continue merge
- **Quick actions bar** — Fetch, Pull, Push, Sync, Stash buttons always visible at the top
- **Stash management** — list, apply, pop, and drop stashes; create named stashes
- **Security** — all git calls use `child_process.execFile` (no shell injection); NL execution validates and sanitizes command strings before running
- **Blazor WASM UI** — full Razor component tree runs inside a VS Code webview via a postMessage bridge
- **Keyboard shortcut** — `Ctrl+Shift+G Ctrl+Shift+N` focuses the NL command bar from anywhere

### Requirements

- VS Code 1.107.0 or later (or a compatible fork)
- Git installed and available on `PATH`
- GitHub Copilot (optional — for AI commit messages and NL fallback)

[Unreleased]: https://github.com/YOUR-USERNAME/GitBuddy/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/YOUR-USERNAME/GitBuddy/releases/tag/v1.0.0
