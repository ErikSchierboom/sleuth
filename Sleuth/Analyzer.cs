namespace Sleuth;

internal sealed record FileAnalysis(string FilePath, VersionControlFileAnalysis VersionControl, CodebaseFileAnalysis Codebase);

internal static class Analyzer
{
    public static async Task<FileAnalysis[]> Analyze(DirectoryInfo repoDirectory, DirectoryInfo codebaseDirectory)
    {
        var versionControlFileAnalyses = await VersionControl.Analyze(repoDirectory, codebaseDirectory);
        var codebaseFileAnalyses = await Codebase.Analyze(codebaseDirectory);

        return codebaseFileAnalyses
            .Select(kv => new FileAnalysis(kv.Key, versionControlFileAnalyses[kv.Key], kv.Value))
            .OrderBy(fileAnalysis => fileAnalysis.FilePath)
            .ToArray();
    }
}