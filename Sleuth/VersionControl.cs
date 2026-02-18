using System.Diagnostics;

namespace Sleuth;

internal sealed record VersionControlFileAnalysis(string FilePath, int NumberOfTimesChanged, HashSet<string> Authors, DateTimeOffset LastChangedAt);

internal sealed record VersionControlRepositoryAnalysis(string DirectoryPath, VersionControlFileAnalysis[] Files);

internal sealed class VersionControl(string directoryPath, string path)
{
    public static async Task<VersionControlRepositoryAnalysis> Analyze(string directoryPath, string path) =>
        await new VersionControl(directoryPath, path).Analyze();

    private async Task<VersionControlRepositoryAnalysis> Analyze()
        {
            var changesPerFile = new Dictionary<string, int>();
            var authorsPerFile = new Dictionary<string, HashSet<string>>();
            var lastChangedDatePerFile = new Dictionary<string, DateTimeOffset>();

            // Use git log with numstat for efficient file-level analysis
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"--no-pager log --format=\"%an%x09%aI\" --numstat --no-renames --no-merges --remove-empty --shortstat --all -- {path}",
                WorkingDirectory = directoryPath,
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
                    
                    var filePath = fileChangeSummary[fileChangeSummary.LastIndexOf('\t')..];
                    authorsPerFile.TryAdd(filePath, []);
                    authorsPerFile[filePath].Add(author);
                        
                    changesPerFile[filePath] = changesPerFile.GetValueOrDefault(filePath, 0) + 1;
                    lastChangedDatePerFile.TryAdd(filePath, date);
                }
            }

            await process.WaitForExitAsync();

            Debug.Assert(changesPerFile.Count == authorsPerFile.Count);
            Debug.Assert(lastChangedDatePerFile.Count == authorsPerFile.Count);

            var fileAnalyses = changesPerFile.Keys
                .Select(file => new VersionControlFileAnalysis(
                    file,
                    changesPerFile.GetValueOrDefault(file, 0), 
                    authorsPerFile.GetValueOrDefault(file, []),
                    lastChangedDatePerFile.GetValueOrDefault(file, DateTimeOffset.MinValue)))
                .OrderBy(analysis => analysis.FilePath)
                .ToArray();

            return new VersionControlRepositoryAnalysis(directoryPath, fileAnalyses);
        }
}