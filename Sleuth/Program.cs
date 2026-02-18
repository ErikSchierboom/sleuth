using System.Diagnostics;
using System.Text.Json;
using Sleuth;

const string repoDirectoryPath = "/Users/erik/Code/cito/Construction.Platform";
var codebaseDirectoryPath = Path.Combine(repoDirectoryPath, "Backend");

var sw = Stopwatch.StartNew();
var versionControlRepositoryAnalysis = VersionControl.Analyze(repoDirectoryPath, Path.GetRelativePath(repoDirectoryPath, codebaseDirectoryPath));
Console.WriteLine(sw.ElapsedMilliseconds);
sw = Stopwatch.StartNew();
var codebaseAnalysis = await Codebase.Analyze(codebaseDirectoryPath);
Console.WriteLine(sw.ElapsedMilliseconds);

var analysis = new Analysis(versionControlRepositoryAnalysis, codebaseAnalysis);

const string fileName = "/Users/erik/Code/sleuth/analysis.json";
await using var outputStream = File.Create(fileName);
await JsonSerializer.SerializeAsync(outputStream, analysis, new JsonSerializerOptions { WriteIndented = true });

internal sealed record Analysis(VersionControlRepositoryAnalysis Repository, CodebaseAnalysis Codebase);