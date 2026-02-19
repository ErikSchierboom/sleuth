using System.CommandLine;

namespace Sleuth;

internal static class Program
{   
    public static async Task<int> Main(string[] args)
    {
        var pathArg = new Argument<DirectoryInfo>("path")
        {
            Description = "The path to the codebase to analyze."
        };
        var outputFormatOption = new Option<OutputFormat>("--output")
        {
            Description = "The format of the output file.",
            DefaultValueFactory = _ => OutputFormat.Csv,
        };
        var outputDirectoryOption = new Option<DirectoryInfo>("--output-dir")
        {
            Description = "The format of the output file.",
            DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory())
        };
        
        var rootCommand = new RootCommand("Analyze a C# codebase for metrics.");
        rootCommand.Arguments.Add(pathArg);
        rootCommand.Options.Add(outputFormatOption);
        rootCommand.Options.Add(outputDirectoryOption);

        rootCommand.SetAction(async parseResult =>
        {
            var codebaseDirectory = parseResult.GetValue(pathArg)!;
            var fileAnalyses = await Analyzer.Analyze(codebaseDirectory);
            
            var outputFormat = parseResult.GetRequiredValue<OutputFormat>(outputFormatOption.Name);
            var outputDirectory = parseResult.GetRequiredValue<DirectoryInfo>(outputDirectoryOption.Name);
            var outputFilePath = Path.Combine(outputDirectory.FullName, "analysis.csv");
            await Output.WriteToFile(outputFilePath, fileAnalyses, outputFormat);
            
            return 0;
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}