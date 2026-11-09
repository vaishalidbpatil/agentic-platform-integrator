# PlatformDataIntegrator
reusable .NET ingestion framework with templated pluggable source adapters and idempotent loads, ingesting sample records into SQL with job tracking.

## MCP Server Architecture

This project uses [Claude Code](https://claude.com/claude-code) with MCP (Model Context Protocol) servers for AI-assisted development. The architecture diagram below shows how the components interact.

**Interactive diagram:** [`mcp-architecture-diagram.html`](./mcp-architecture-diagram.html) — open in a browser for the full view.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    EXTERNAL SERVICES & APIs                      │
│  [Anthropic API]  [Atlassian Cloud]  [GitHub]  [Observability]  │
└──────────────┬───────────────┬───────────────┬──────────────────┘
               │ HTTPS/SSE     │ REST/OAuth     │ REST
┌──────────────▼───────────────▼───────────────▼──────────────────┐
│                       MCP SERVERS  (JSON-RPC 2.0 / stdio)        │
│  [Atlassian MCP]  [Sentinel MCP]  [IDE MCP]  [Custom MCP]       │
│      Jira/Confluence   200+ tools   executeCode  user-defined    │
└──────────────────────────────┬──────────────────────────────────┘
                               │ stdio / JSON-RPC
┌──────────────────────────────▼──────────────────────────────────┐
│                    CLAUDE CODE CLI  (Agent Core)                  │
│  Agent Loop │ Tool Executor │ Memory System │ Skills/Workflows   │
└─────────────────────────────┬────────────────────────────────────┘
                              ↕ stdin/stdout
┌─────────────────────────────▼────────────────────────────────────┐
│                  VISUAL STUDIO CODE  (IDE Layer)                  │
│  VS Code Host │ Claude Code Extension │ Language Servers │ DAP   │
└──────────────────────────────┬──────────────────────────────────┘
                               │ I/O / spawn
┌──────────────────────────────▼──────────────────────────────────┐
│               OPERATING SYSTEM  (Windows 11)                     │
│  File System │ Bash Shell │ Node.js Runtime │ Git │ Sandbox      │
└─────────────────────────────────────────────────────────────────┘
```

### Layers

| Layer | Role |
|---|---|
| **OS** | Hosts all processes; provides file system, shell, Node.js runtime for MCP servers |
| **VS Code** | IDE with Claude Code extension providing the chat panel, inline diffs, and slash commands |
| **Claude Code CLI** | The agent loop — dispatches tools, manages context, runs skills and multi-agent workflows |
| **MCP Servers** | Pluggable tool providers connected over `stdio` using JSON-RPC 2.0 |
| **External Services** | Anthropic LLM API, Atlassian, GitHub, observability stack, CI/CD pipelines |

### MCP Servers in Use

| Server | Tools | Status |
|---|---|---|
| `plugin:atlassian:atlassian` | Jira, Confluence, Loom, Atlas | Active |
| `plugin:scdp-dev-tools:scdp-sentinel` | GitHub, Jenkins, New Relic, Slack, Snowflake, n8n, PagerDuty, TeamCity | Active |
| `plugin:scdp-dev-tools:scdp-github` | Repo management, PRs, Actions | Connection closed |
| `mcp__ide` | `executeCode`, `getDiagnostics` (VS Code bridge) | Active |
