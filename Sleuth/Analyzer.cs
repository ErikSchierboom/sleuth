namespace Sleuth;

internal sealed record FileAnalysis(string FilePath, VersionControlFileAnalysis VersionControl, CodebaseFileAnalysis Codebase);

internal static class Analyzer
{
    public static async Task<FileAnalysis[]> Analyze(DirectoryInfo codebaseDirectory)
    {
        var repoDirectory = new DirectoryInfo(codebaseDirectory.FullName);
        
        while (repoDirectory.Exists && !Directory.Exists(Path.Combine(repoDirectory.FullName, ".git")))
            repoDirectory = repoDirectory.Parent ?? throw new InvalidOperationException("Could not find .git directory in specified directory or its parent directories.");
        
        return await Analyze(codebaseDirectory, repoDirectory);
    }

    private static async Task<FileAnalysis[]> Analyze(DirectoryInfo codebaseDirectory, DirectoryInfo repoDirectory)
    {
        var versionControlFileAnalyses = await VersionControl.Analyze(repoDirectory, codebaseDirectory);
        var codebaseFileAnalyses = await Codebase.Analyze(codebaseDirectory);

        return codebaseFileAnalyses
            .Select(kv => new FileAnalysis(kv.Key, versionControlFileAnalyses[kv.Key], kv.Value))
            .OrderBy(fileAnalysis => fileAnalysis.FilePath)
            .ToArray();
    }
}