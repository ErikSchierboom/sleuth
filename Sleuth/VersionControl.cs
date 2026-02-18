namespace Sleuth;

internal sealed record VersionControlFileAnalysis(string FilePath, int NumberOfTimesChanged, HashSet<string> Authors, DateTimeOffset LastChangedAt);

internal sealed record VersionControlRepositoryAnalysis(string DirectoryPath, VersionControlFileAnalysis[] Files);

internal sealed class VersionControl(string directoryPath, string path)
{
    public static VersionControlRepositoryAnalysis Analyze(string directoryPath, string path) =>
        new VersionControl(directoryPath, path).Analyze();

    private VersionControlRepositoryAnalysis Analyze()
        {
            var changesPerFile = new Dictionary<string, int>();
            var authorsPerFile = new Dictionary<string, HashSet<string>>();
            var lastChangedDatePerFile = new Dictionary<string, DateTimeOffset>();

            // Use git log with numstat for efficient file-level analysis
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"log --numstat --format=%H%n%an%n%aI%n --all -- {path}",
                WorkingDirectory = directoryPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) throw new InvalidOperationException("Failed to start git process");

            string? currentHash = null;
            string? currentAuthor = null;
            DateTimeOffset currentDate = default;
            var lineIndex = 0;

            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    lineIndex = 0;
                    continue;
                }

                if (currentHash == null)
                {
                    // Reading commit header (3 lines)
                    switch (lineIndex)
                    {
                        case 0:
                            currentHash = line;
                            break;
                        case 1:
                            currentAuthor = line;
                            break;
                        case 2:
                            currentDate = DateTimeOffset.Parse(line);
                            break;
                    }
                    lineIndex++;
                }
                else
                {
                    // Reading file changes (format: additions deletions filename)
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var filePath = parts[2];
                        
                        authorsPerFile.TryAdd(filePath, []);
                        authorsPerFile[filePath].Add(currentAuthor!);
                        
                        changesPerFile[filePath] = changesPerFile.GetValueOrDefault(filePath, 0) + 1;
                        lastChangedDatePerFile.TryAdd(filePath, currentDate);
                    }
                }
            }

            process.WaitForExit();

            HashSet<string> files = [..changesPerFile.Keys, ..authorsPerFile.Keys];
            var fileAnalyses = files
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