using LibGit2Sharp;

namespace Sleuth;

internal sealed class VersionControl
{
    public static Dictionary<string, int> MostFrequentlyChangedFiles(string directory)
    {
        using var repo = new Repository(directory);

        return repo.Commits
            .QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Time | CommitSortStrategies.Reverse })
            .TakeWhile(commit => commit.Committer.When >= DateTimeOffset.Now.AddYears(-1))
            .SelectMany(commit =>
                commit.Parents
                    .SelectMany(parent => repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree)
                        .Select(change => change.OldPath)))
            .GroupBy(path => path)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}