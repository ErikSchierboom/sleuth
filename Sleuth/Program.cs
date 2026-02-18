using System.Diagnostics;
using System.Text.Json;
using Sleuth;

const string repoDirectoryPath = "/Users/erik/Code/cito/Construction.Platform";
var codebaseDirectoryPath = Path.Combine(repoDirectoryPath, "Backend");

var startNew = Stopwatch.StartNew();
var versionControlRepositoryAnalysis = VersionControl.Analyze(repoDirectoryPath, Path.GetRelativePath(repoDirectoryPath, codebaseDirectoryPath));
var codebaseAnalysis = Codebase.Analyze(codebaseDirectoryPath);

var analysis = new Analysis(await versionControlRepositoryAnalysis, await codebaseAnalysis);
Console.WriteLine(startNew.ElapsedMilliseconds);

const string fileName = "/Users/erik/Code/sleuth/analysis.json";
await using var outputStream = File.Create(fileName);
await JsonSerializer.SerializeAsync(outputStream, analysis, new JsonSerializerOptions { WriteIndented = true });

internal sealed record Analysis(VersionControlRepositoryAnalysis Repository, CodebaseAnalysis Codebase);