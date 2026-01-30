using GLA.Services;

namespace GLA;

class Program
{
    private static CopilotLearningService? _learningService;
    private static SandboxService? _sandboxService;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintWelcome();

        // Initialize services
        _sandboxService = new SandboxService();
        _learningService = new CopilotLearningService(_sandboxService);

        Console.WriteLine("⏳ Initializing GitHub Copilot SDK...\n");

        try
        {
            await _learningService.InitializeAsync();
            Console.WriteLine("✓ Connected to GitHub Copilot!");
            Console.WriteLine($"✓ Sandbox ready at: {_sandboxService.SandboxPath}\n");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            // Start the AI-driven learning session
            await RunLearningSessionAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Failed to initialize GitHub Copilot SDK: {ex.Message}");
            Console.WriteLine("\nThis app requires the GitHub Copilot CLI to be installed and authenticated.");
            Console.WriteLine("Please ensure:");
            Console.WriteLine("  1. GitHub Copilot CLI is installed (https://docs.github.com/en/copilot)");
            Console.WriteLine("  2. You're authenticated with: gh auth login");
            Console.WriteLine("  3. You have an active GitHub Copilot subscription");
            Console.ResetColor();
        }
        finally
        {
            // Cleanup
            if (_learningService != null)
            {
                await _learningService.DisposeAsync();
            }
            _sandboxService?.Dispose();
        }

        Console.WriteLine("\nThanks for learning Git! Goodbye! 👋");
    }

    static void PrintWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║        🎓 GLA - Git Learning Assistant (Copilot-Powered) 🎓    ║
╠═══════════════════════════════════════════════════════════════╣
║  Learn Git through interactive, AI-guided scenarios!           ║
║  Copilot will create exercises and guide you step-by-step.     ║
║                                                                 ║
║  Commands:                                                      ║
║    • Type any Git command to practice (e.g., 'git status')     ║
║    • Ask questions in plain English                             ║
║    • Type 'menu' for topic selection                           ║
║    • Type 'reset' to start fresh                                ║
║    • Type 'exit' to quit                                        ║
╚═══════════════════════════════════════════════════════════════╝
");
        Console.ResetColor();
    }

    static async Task RunLearningSessionAsync()
    {
        // Start with Copilot greeting
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("🤖 Copilot: ");
        Console.ResetColor();

        await _learningService!.StartLearningSessionAsync(
            onMessage: chunk => Console.Write(chunk),
            onToolUse: tool => {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"\n   [{tool}] ");
                Console.ResetColor();
            }
        );
        Console.WriteLine("\n");

        // Main interaction loop
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("You: ");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            // Handle special commands
            switch (input.ToLower())
            {
                case "exit":
                case "quit":
                    return;

                case "menu":
                    await ShowTopicMenuAsync();
                    continue;

                case "reset":
                    await _sandboxService!.ResetAsync();
                    Console.WriteLine("✓ Sandbox reset!\n");
                    continue;

                case "help":
                    PrintHelp();
                    continue;
            }

            // Send everything else to Copilot
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("🤖 Copilot: ");
            Console.ResetColor();

            try
            {
                await _learningService.ProcessUserInputAsync(
                    input,
                    onMessage: chunk => Console.Write(chunk),
                    onToolUse: tool => {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"\n   [{tool}] ");
                        Console.ResetColor();
                    }
                );
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n");
        }
    }

    static async Task ShowTopicMenuAsync()
    {
        Console.WriteLine("\n📚 LEARNING TOPICS");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  BEGINNER:");
        Console.ResetColor();
        Console.WriteLine("    1. First Steps (init, status)");
        Console.WriteLine("    2. Staging & Committing (add, commit)");
        Console.WriteLine("    3. Viewing History (log, diff)");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  INTERMEDIATE:");
        Console.ResetColor();
        Console.WriteLine("    4. Branching Basics (branch, checkout)");
        Console.WriteLine("    5. Merging Changes (merge)");
        Console.WriteLine("    6. Working with Remotes (remote, fetch, pull)");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  ADVANCED:");
        Console.ResetColor();
        Console.WriteLine("    7. Rewriting History (rebase, amend)");
        Console.WriteLine("    8. Undoing Changes (reset, revert)");
        Console.WriteLine("    9. Stashing Work (stash)");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════");
        Console.Write("Select a topic (1-9) or 'back': ");

        var choice = Console.ReadLine()?.Trim();

        if (choice == "back" || string.IsNullOrEmpty(choice))
            return;

        var (topic, difficulty) = choice switch
        {
            "1" => ("initializing a repository and checking status", "beginner"),
            "2" => ("staging files and making commits", "beginner"),
            "3" => ("viewing commit history and differences", "beginner"),
            "4" => ("creating and switching branches", "intermediate"),
            "5" => ("merging branches and resolving conflicts", "intermediate"),
            "6" => ("working with remote repositories", "intermediate"),
            "7" => ("interactive rebase and amending commits", "advanced"),
            "8" => ("undoing changes with reset and revert", "advanced"),
            "9" => ("stashing work in progress", "advanced"),
            _ => (null, null)
        };

        if (topic == null)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("🤖 Copilot: ");
        Console.ResetColor();

        await _learningService!.SetupScenarioAsync(
            topic,
            difficulty!,
            onMessage: chunk => Console.Write(chunk),
            onToolUse: tool => {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"\n   [{tool}] ");
                Console.ResetColor();
            }
        );
        Console.WriteLine("\n");
    }

    static void PrintHelp()
    {
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                         HELP                                   ║
╠═══════════════════════════════════════════════════════════════╣
║                                                                 ║
║  HOW TO USE THIS APP:                                          ║
║                                                                 ║
║  • Git Commands: Type any git command directly                 ║
║      Example: git status, git add ., git commit -m ""msg""       ║
║                                                                 ║
║  • Questions: Ask anything about Git in plain English          ║
║      Example: ""What is a branch?"", ""How do I undo a commit?""   ║
║                                                                 ║
║  • Guided Learning: Type 'menu' to pick a topic                ║
║      Copilot will create a hands-on scenario for you           ║
║                                                                 ║
║  SPECIAL COMMANDS:                                             ║
║    menu   - Show topic selection menu                          ║
║    reset  - Reset the sandbox to a fresh state                 ║
║    help   - Show this help message                             ║
║    exit   - Quit the application                               ║
║                                                                 ║
╚═══════════════════════════════════════════════════════════════╝
");
    }
}