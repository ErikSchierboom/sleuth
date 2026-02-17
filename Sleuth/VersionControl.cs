using LibGit2Sharp;

namespace Sleuth;

internal record VersionControlFileAnalysis(string FilePath, int NumberOfTimesChanged, HashSet<string> Authors);

internal record VersionControlRepositoryAnalysis(string DirectoryPath, VersionControlFileAnalysis[] Files);

internal sealed class VersionControl(string directoryPath)
{
    public static VersionControlRepositoryAnalysis Analyze(string directoryPath) =>
        new VersionControl(directoryPath).Analyze();
    
    public VersionControlRepositoryAnalysis Analyze()
    {
        using var repo = new Repository(directoryPath);

        var changesPerFile = new Dictionary<string, int>();
        var authorsPerFile = new Dictionary<string, HashSet<string>>();

        var compareOptions = new CompareOptions { IncludeUnmodified = false, Algorithm = DiffAlgorithm.Minimal };
        foreach (var commit in repo.Commits)
        {
            var author = commit.Author.Name;

            foreach (var parent in commit.Parents)
            {
                foreach (var change in repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree, compareOptions))
                {
                    changesPerFile[change.Path] = changesPerFile.GetValueOrDefault(change.Path, 0) + 1;

                    authorsPerFile.TryAdd(change.Path, []);
                    authorsPerFile[change.Path].Add(author);
                }
            }
        }
        
        HashSet<string> files = [..changesPerFile.Keys, ..authorsPerFile.Keys];
        var fileAnalyses = files
            .Select(x => new VersionControlFileAnalysis(x, changesPerFile.GetValueOrDefault(x, 0), authorsPerFile.GetValueOrDefault(x, [])))
            .OrderBy(x => x.FilePath);

        return new VersionControlRepositoryAnalysis(directoryPath, [..fileAnalyses]);
    }
}