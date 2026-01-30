using System.Diagnostics;

namespace GLA.Services;

/// <summary>
/// Manages a sandboxed Git repository for safe learning
/// </summary>
public class SandboxService : IDisposable
{
    private readonly string _sandboxPath;
    private bool _isInitialized;

    public string SandboxPath => _sandboxPath;

    public SandboxService(string? basePath = null)
    {
        var baseDir = basePath ?? Path.GetTempPath();
        _sandboxPath = Path.Combine(baseDir, "gla-sandbox", Guid.NewGuid().ToString("N")[..8]);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        // Create sandbox directory
        Directory.CreateDirectory(_sandboxPath);

        // Initialize git repository
        await ExecuteGitCommandAsync("init");

        // Configure git for the sandbox
        await ExecuteGitCommandAsync("config user.email \"learner@gitlearning.local\"");
        await ExecuteGitCommandAsync("config user.name \"Git Learner\"");

        _isInitialized = true;
    }

    public async Task<(bool Success, string Output, string Error)> ExecuteGitCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _sandboxPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (process.ExitCode == 0, output.Trim(), error.Trim());
    }

    public async Task CreateFileAsync(string fileName, string content)
    {
        var filePath = Path.Combine(_sandboxPath, fileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task<string> ReadFileAsync(string fileName)
    {
        var filePath = Path.Combine(_sandboxPath, fileName);
        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath);
        }
        return string.Empty;
    }

    public bool FileExists(string fileName)
    {
        return File.Exists(Path.Combine(_sandboxPath, fileName));
    }

    public string[] GetFiles(string searchPattern = "*")
    {
        return Directory.GetFiles(_sandboxPath, searchPattern, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_sandboxPath, f))
            .ToArray();
    }

    public async Task<string> GetStatusAsync()
    {
        var result = await ExecuteGitCommandAsync("status --short");
        return result.Output;
    }

    public async Task<string> GetLogAsync(int count = 5)
    {
        var result = await ExecuteGitCommandAsync($"log --oneline -n {count}");
        return result.Output;
    }

    public async Task ResetAsync()
    {
        // Clean up the sandbox and reinitialize
        if (Directory.Exists(_sandboxPath))
        {
            // Remove .git directory attributes to allow deletion
            var gitDir = Path.Combine(_sandboxPath, ".git");
            if (Directory.Exists(gitDir))
            {
                SetAttributesNormal(new DirectoryInfo(gitDir));
            }
            Directory.Delete(_sandboxPath, true);
        }
        _isInitialized = false;
        await InitializeAsync();
    }

    public async Task SetupScenarioFilesAsync(Dictionary<string, string> files)
    {
        foreach (var (fileName, content) in files)
        {
            await CreateFileAsync(fileName, content);
        }
    }

    private static void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
        {
            SetAttributesNormal(subDir);
        }
        foreach (var file in dir.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_sandboxPath))
            {
                var gitDir = Path.Combine(_sandboxPath, ".git");
                if (Directory.Exists(gitDir))
                {
                    SetAttributesNormal(new DirectoryInfo(gitDir));
                }
                Directory.Delete(_sandboxPath, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
