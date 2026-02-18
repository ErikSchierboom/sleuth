using Sleuth;

const string repoDirectoryPath = "/Users/erik/Code/cito/Construction.Platform";
var codebaseDirectoryPath = Path.Combine(repoDirectoryPath, "Backend");
var fileAnalyses = await Analyzer.Analyze(repoDirectoryPath, codebaseDirectoryPath);

await Output.WriteToJson("/Users/erik/Code/sleuth/analysis.json", fileAnalyses);
await Output.WriteToCsv("/Users/erik/Code/sleuth/analysis.csv", fileAnalyses);
