using LibGit2Sharp;

namespace Sleuth;

internal record FileChanges(string File, int Count);

internal static class VersionControl
{
    public static FileChanges[] MostFrequentlyChangedFiles(string directoryPath)
    {
        using var repo = new Repository(directoryPath);

        return repo.Commits
            .QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Time | CommitSortStrategies.Reverse })
            .TakeWhile(commit => commit.Committer.When >= DateTimeOffset.Now.AddYears(-1))
            .SelectMany(commit =>
                commit.Parents
                    .SelectMany(parent => repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree)
                        .Select(change => change.OldPath)))
            .GroupBy(path => path)
            .Select(g => new FileChanges(g.Key, g.Count()))
            .OrderByDescending(f => f.Count)
            .ToArray();
    }
}