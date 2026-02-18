namespace Sleuth;

internal sealed record FileAnalysis(string FilePath, VersionControlFileAnalysis VersionControl, CodebaseFileAnalysis Codebase);

internal class Analyzer
{
    public static async Task<FileAnalysis[]> Analyze(string repoDirectoryPath, string codebaseDirectoryPath)
    {
        var versionControlFileAnalyses = await VersionControl.Analyze(repoDirectoryPath, Path.GetRelativePath(repoDirectoryPath, codebaseDirectoryPath));
        var codebaseFileAnalyses = await Codebase.Analyze(codebaseDirectoryPath);

        return codebaseFileAnalyses
            .Select(kv => new FileAnalysis(kv.Key, versionControlFileAnalyses[kv.Key], kv.Value))
            .OrderBy(fileAnalysis => fileAnalysis.FilePath)
            .ToArray();
    }
}