using System.Text.Json;
using Sleuth;

const string repoDirectoryPath = "/Users/erik/Code/cito/Construction.Platform";
var codebaseDirectoryPath = Path.Combine(repoDirectoryPath, "Backend");

var fileAnalyses = await Analyzer.Analyze(repoDirectoryPath, codebaseDirectoryPath);

const string fileName = "/Users/erik/Code/sleuth/analysis.json";
await using var outputStream = File.Create(fileName);
await JsonSerializer.SerializeAsync(outputStream, fileAnalyses, new JsonSerializerOptions { WriteIndented = true });
