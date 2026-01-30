# GLA - Git Learning Assistant

A .NET console application that helps users learn Git through **AI-guided, hands-on scenarios** powered by the GitHub Copilot SDK.

## ✨ Features

- **AI-Driven Learning** - GitHub Copilot dynamically creates learning scenarios and guides you step-by-step
- **Safe Sandbox Environment** - Practice Git commands in an isolated temporary repository
- **Interactive Feedback** - Get real-time explanations for commands, errors, and Git concepts
- **Adaptive Teaching** - Copilot adjusts to your skill level and learning pace
- **Hands-On Practice** - Learn by doing, not just reading

## 🏗️ Architecture

The app gives GitHub Copilot SDK **full control** of the learning experience through custom tools:

```
User ⟷ Program.cs ⟷ CopilotLearningService ⟷ GitHub Copilot SDK
                              ↓
                    Custom Tools (7 total)
                              ↓
                    SandboxService (Git environment)
```

### Copilot Tools

| Tool | Description |
|------|-------------|
| `create_file` | Create files in the sandbox |
| `run_git_command` | Execute any Git command |
| `get_git_status` | Check working tree status |
| `get_git_log` | View commit history |
| `list_files` | List sandbox contents |
| `read_file` | Read file contents |
| `reset_sandbox` | Start fresh |

## 📁 Project Structure

```
gla/
├── src/
│   ├── Program.cs                      # Main entry point & UI
│   └── Services/
│       ├── CopilotLearningService.cs   # AI-driven learning with tools
│       └── SandboxService.cs           # Safe Git sandbox environment
├── gla.csproj
└── README.md
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- [GitHub CLI](https://cli.github.com/) with Copilot extension
- Active [GitHub Copilot subscription](https://github.com/features/copilot)

### Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd git-learning-app
   ```

2. Ensure you're authenticated with GitHub:
   ```bash
   gh auth login
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

### Running the Application

```bash
dotnet run
```

## Usage

Once the app starts, you can:

- **Type Git commands** directly (e.g., `git status`, `git add .`, `git commit -m "message"`)
- **Ask questions** in plain English (e.g., "What is a branch?", "How do I undo a commit?")
- **Choose a topic** from the menu for guided learning

### Special Commands

| Command | Description |
|---------|-------------|
| `menu` | Show topic selection menu |
| `reset` | Reset sandbox to fresh state |
| `help` | Show help message |
| `exit` | Quit the application |

### Learning Topics

- **Beginner**: Repository init, staging, committing, viewing history
- **Intermediate**: Branching, merging, working with remotes
- **Advanced**: Rebasing, undoing changes, stashing

## How It Works

1. **You start the app** → Copilot greets you and offers guidance
2. **You pick a topic or ask a question** → Copilot uses its tools to:
   - Create files in the sandbox
   - Set up the learning scenario (branches, commits, etc.)
   - Guide you through exercises step-by-step
3. **You practice commands** → Copilot:
   - Executes them in the sandbox
   - Provides feedback on what happened
   - Helps you understand errors
   - Suggests next steps