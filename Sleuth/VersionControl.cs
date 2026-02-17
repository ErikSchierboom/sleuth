using LibGit2Sharp;

namespace Sleuth;

internal record VersionControlFileAnalysis(string File, int NumberOfTimesChanged, int NumberOfAuthors);

internal record VersionControlRepositoryAnalysis(string Directory, VersionControlFileAnalysis[] Files);

internal static class VersionControl
{
    public static VersionControlRepositoryAnalysis AnalyzeRepository(string directoryPath)
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
        var fileAnalyses = files.Select(x => new VersionControlFileAnalysis(x, changesPerFile.GetValueOrDefault(x, 0), authorsPerFile.GetValueOrDefault(x, []).Count));

        return new VersionControlRepositoryAnalysis(directoryPath, [..fileAnalyses]);
    }
}