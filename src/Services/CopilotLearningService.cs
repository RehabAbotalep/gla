using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;

namespace GLA.Services;

/// <summary>
/// Copilot SDK-driven Git learning service - AI has full control of the learning experience
/// </summary>
public class CopilotLearningService : IAsyncDisposable
{
    private readonly CopilotClient _client;
    private CopilotSession? _session;
    private readonly SandboxService _sandbox;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public CopilotLearningService(SandboxService sandbox)
    {
        _sandbox = sandbox;
        _client = new CopilotClient(new CopilotClientOptions
        {
            LogLevel = "error",
            AutoStart = true
        });
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _client.StartAsync();
        await _sandbox.InitializeAsync();

        // Create session with tools that let Copilot control the sandbox
        _session = await _client.CreateSessionAsync(new SessionConfig
        {
            Model = "gpt-4.1",
            Streaming = true,
            Tools = CreateSandboxTools(),
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Replace,
                Content = GetSystemPrompt()
            }
        });

        _isInitialized = true;
    }

    private string GetSystemPrompt() => $"""
        You are an interactive Git tutor. You have FULL CONTROL over a sandboxed Git environment.
        Your job is to teach users Git through hands-on, practical exercises.

        ## Your Capabilities
        You have tools to:
        - Create files in the sandbox (create_file)
        - Execute Git commands to set up scenarios (run_git_command)
        - Check the current Git status (get_git_status)
        - View commit history (get_git_log)
        - List files in the sandbox (list_files)
        - Read file contents (read_file)

        ## Sandbox Location
        The sandbox is at: {_sandbox.SandboxPath}
        It's already initialized with `git init`.

        ## Teaching Approach
        1. When user wants to learn, CREATE a practical scenario using your tools
        2. Set up the environment (create files, make commits, create branches as needed)
        3. Explain what you've set up and give the user a TASK to complete
        4. Wait for the user to type their Git command
        5. The user's command will be executed automatically - you'll see the result
        6. Provide feedback on what happened and guide them to the next step
        7. Adapt difficulty based on user's responses

        ## Important Rules
        - ALWAYS use your tools to set up scenarios - don't just describe them
        - After setting up, clearly tell the user what command to try
        - When user enters a command, analyze the result and provide educational feedback
        - Be encouraging but also explain mistakes clearly
        - Progress from simple to complex concepts

        ## Topics You Can Teach
        - Basic: init, status, add, commit, log
        - Intermediate: branches, checkout, merge, diff
        - Advanced: rebase, cherry-pick, stash, reset, revert

        Start by greeting the user and asking what they'd like to learn about Git!
        """;

    private List<AIFunction> CreateSandboxTools()
    {
        return new List<AIFunction>
        {
            // Tool: Create a file in the sandbox
            AIFunctionFactory.Create(
                async ([Description("The file name/path to create")] string fileName,
                       [Description("The content to write to the file")] string content) =>
                {
                    await _sandbox.CreateFileAsync(fileName, content);
                    return $"Created file: {fileName}";
                },
                "create_file",
                "Create a file in the Git sandbox with the specified content"),

            // Tool: Execute a Git command
            AIFunctionFactory.Create(
                async ([Description("Git command arguments (without 'git' prefix)")] string command) =>
                {
                    var (success, output, error) = await _sandbox.ExecuteGitCommandAsync(command);
                    if (success)
                        return string.IsNullOrEmpty(output) ? "Command executed successfully." : output;
                    else
                        return $"Error: {error}";
                },
                "run_git_command",
                "Execute a Git command in the sandbox (e.g., 'add .', 'commit -m \"message\"', 'branch feature')"),

            // Tool: Get Git status
            AIFunctionFactory.Create(
                async () =>
                {
                    var status = await _sandbox.GetStatusAsync();
                    return string.IsNullOrEmpty(status) ? "Working tree clean - nothing to commit." : status;
                },
                "get_git_status",
                "Get the current Git status of the sandbox"),

            // Tool: Get Git log
            AIFunctionFactory.Create(
                async ([Description("Number of commits to show")] int count = 5) =>
                {
                    var log = await _sandbox.GetLogAsync(count);
                    return string.IsNullOrEmpty(log) ? "No commits yet." : log;
                },
                "get_git_log",
                "Get the Git commit history"),

            // Tool: List files
            AIFunctionFactory.Create(
                () =>
                {
                    var files = _sandbox.GetFiles();
                    return files.Length == 0 ? "No files in sandbox." : string.Join("\n", files);
                },
                "list_files",
                "List all files in the sandbox"),

            // Tool: Read file content
            AIFunctionFactory.Create(
                async ([Description("The file name to read")] string fileName) =>
                {
                    var content = await _sandbox.ReadFileAsync(fileName);
                    return string.IsNullOrEmpty(content) ? $"File not found: {fileName}" : content;
                },
                "read_file",
                "Read the contents of a file in the sandbox"),

            // Tool: Reset sandbox
            AIFunctionFactory.Create(
                async () =>
                {
                    await _sandbox.ResetAsync();
                    return "Sandbox has been reset to a fresh Git repository.";
                },
                "reset_sandbox",
                "Reset the sandbox to a fresh state (deletes all files and commits)")
        };
    }

    /// <summary>
    /// Start the interactive learning session
    /// </summary>
    public async Task StartLearningSessionAsync(Action<string> onMessage, Action<string>? onToolUse = null)
    {
        if (_session == null)
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");

        // Send initial greeting prompt
        await SendAndStreamAsync("Start the Git learning session. Greet the user and ask what they'd like to learn.", onMessage, onToolUse);
    }

    /// <summary>
    /// Process user input - could be a question OR a Git command
    /// </summary>
    public async Task ProcessUserInputAsync(string userInput, Action<string> onMessage, Action<string>? onToolUse = null)
    {
        if (_session == null)
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");

        // Check if user is trying to run a Git command
        var isGitCommand = userInput.TrimStart().StartsWith("git ", StringComparison.OrdinalIgnoreCase) ||
                          IsLikelyGitCommand(userInput);

        if (isGitCommand)
        {
            // Execute the command and let Copilot analyze the result
            var command = userInput.StartsWith("git ", StringComparison.OrdinalIgnoreCase)
                ? userInput.Substring(4).Trim()
                : userInput.Trim();

            var (success, output, error) = await _sandbox.ExecuteGitCommandAsync(command);

            var prompt = $"""
                The user ran this Git command: git {command}
                
                Result:
                Success: {success}
                Output: {(string.IsNullOrEmpty(output) ? "(no output)" : output)}
                {(string.IsNullOrEmpty(error) ? "" : $"Error: {error}")}
                
                Analyze this result. Provide educational feedback:
                - Was this the right command for the current task?
                - Explain what happened
                - If there was an error, explain why and how to fix it
                - Suggest the next step or give them a new challenge
                
                Use your tools to verify the current state if needed.
                """;

            await SendAndStreamAsync(prompt, onMessage, onToolUse);
        }
        else
        {
            // It's a question or request - let Copilot handle it fully
            await SendAndStreamAsync(userInput, onMessage, onToolUse);
        }
    }

    private bool IsLikelyGitCommand(string input)
    {
        var gitCommands = new[] { "init", "add", "commit", "status", "log", "branch", "checkout", 
                                   "merge", "pull", "push", "fetch", "clone", "diff", "reset", 
                                   "revert", "stash", "rebase", "cherry-pick", "tag" };
        var firstWord = input.Split(' ')[0].ToLower();
        return gitCommands.Contains(firstWord);
    }

    private async Task SendAndStreamAsync(string prompt, Action<string> onMessage, Action<string>? onToolUse)
    {
        var messageBuilder = new System.Text.StringBuilder();

        using var subscription = _session!.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    if (!string.IsNullOrEmpty(delta.Data.DeltaContent))
                    {
                        onMessage(delta.Data.DeltaContent);
                        messageBuilder.Append(delta.Data.DeltaContent);
                    }
                    break;

                case ToolExecutionStartEvent toolStart:
                    onToolUse?.Invoke($"🔧 {toolStart.Data.ToolName}");
                    break;
            }
        });

        await _session.SendAndWaitAsync(new MessageOptions { Prompt = prompt });
    }

    /// <summary>
    /// Ask Copilot to set up a specific scenario
    /// </summary>
    public async Task SetupScenarioAsync(string topic, string difficulty, Action<string> onMessage, Action<string>? onToolUse = null)
    {
        if (_session == null)
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");

        var prompt = $"""
            Set up a {difficulty} level Git learning scenario about: {topic}
            
            Use your tools to:
            1. Reset the sandbox if needed
            2. Create necessary files
            3. Set up any required Git state (commits, branches, etc.)
            4. Explain the scenario to the user
            5. Give them a clear task to complete
            
            Make it practical and hands-on!
            """;

        await SendAndStreamAsync(prompt, onMessage, onToolUse);
    }

    public async ValueTask DisposeAsync()
    {
        if (_session != null)
        {
            await _session.DisposeAsync();
        }
        await _client.StopAsync();
    }
}
