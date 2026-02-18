namespace Sleuth;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var codebaseDirectoryPath = args.FirstOrDefault();
        if (codebaseDirectoryPath is null)
        {
            await Console.Error.WriteLineAsync("Please specify a directory to analyze.");
            return 1;
        }
        
        var codebaseDirectory = new DirectoryInfo(codebaseDirectoryPath);
        if (!codebaseDirectory.Exists)
        {
            await Console.Error.WriteLineAsync($"Specified directory does not exist: {codebaseDirectoryPath}");
            return 2;
        }
        
        var repoDirectory = new DirectoryInfo(codebaseDirectory.FullName);
        while (repoDirectory.Exists && !Directory.Exists(Path.Combine(repoDirectory.FullName, ".git")))
        {
            if (repoDirectory.Parent is null)
            {
                await Console.Error.WriteLineAsync("Could not find .git directory in specified directory or its parent directories.");
                return 2;
            }
        
            repoDirectory = repoDirectory.Parent;
        }
        
        var fileAnalyses = await Analyzer.Analyze(repoDirectory, codebaseDirectory);

        await Output.WriteToJson("/Users/erik/Code/sleuth/analysis.json", fileAnalyses);
        await Output.WriteToCsv("/Users/erik/Code/sleuth/analysis.csv", fileAnalyses);
        return 0;
    }
}