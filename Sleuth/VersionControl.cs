using System.Diagnostics;

namespace Sleuth;

internal sealed record VersionControlFileAnalysis(int NumberOfTimesChanged, HashSet<string> Authors, DateTimeOffset LastChangedAt);

internal sealed class VersionControl(DirectoryInfo repoDirectory, DirectoryInfo codebaseDirectory)
{
    public static async Task<Dictionary<string, VersionControlFileAnalysis>> Analyze(DirectoryInfo repoDirectory, DirectoryInfo codebaseDirectory) =>
        await new VersionControl(repoDirectory, codebaseDirectory).Analyze();

    private async Task<Dictionary<string, VersionControlFileAnalysis>> Analyze()
        {
            var changesPerFile = new Dictionary<string, int>();
            var authorsPerFile = new Dictionary<string, HashSet<string>>();
            var lastChangedDatePerFile = new Dictionary<string, DateTimeOffset>();

            // Use git log with numstat for efficient file-level analysis
            var path = Path.GetRelativePath(repoDirectory.FullName, codebaseDirectory.FullName);
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"--no-pager log --format=\"%an%x09%aI\" --numstat --no-renames --no-merges --remove-empty --shortstat --all -- {path}",
                WorkingDirectory = repoDirectory.FullName,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git process");

            while (true)
            {
                var header = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrEmpty(header))
                    break;

                var parts = header.Split('\t');
                var author = parts[0];
                var date = DateTimeOffset.Parse(parts[1]);
                
                await process.StandardOutput.ReadLineAsync(); // Skip blank line after commit header

                while (true)
                {
                    var fileChangeSummary = await process.StandardOutput.ReadLineAsync();
                    if (string.IsNullOrEmpty(fileChangeSummary) || fileChangeSummary.StartsWith(' '))
                        break;
                    
                    var relativeFilePath = fileChangeSummary[(fileChangeSummary.LastIndexOf('\t') + 1)..];
                    var filePath = Path.GetRelativePath(codebaseDirectory.FullName, Path.Combine(repoDirectory.FullName, relativeFilePath));
                    
                    authorsPerFile.TryAdd(filePath, []);
                    authorsPerFile[filePath].Add(author);
                        
                    changesPerFile[filePath] = changesPerFile.GetValueOrDefault(filePath, 0) + 1;
                    lastChangedDatePerFile.TryAdd(filePath, date);
                }
            }

            await process.WaitForExitAsync();

            Debug.Assert(changesPerFile.Count == authorsPerFile.Count);
            Debug.Assert(lastChangedDatePerFile.Count == authorsPerFile.Count);

            return changesPerFile.Keys
                .Select(file => (file, new VersionControlFileAnalysis(
                    changesPerFile.GetValueOrDefault(file, 0), 
                    authorsPerFile.GetValueOrDefault(file, []),
                    lastChangedDatePerFile.GetValueOrDefault(file, DateTimeOffset.MinValue))))
                .ToDictionary();
        }
}