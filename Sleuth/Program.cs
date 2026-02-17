using Sleuth;

var directory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));

var codebaseAnalysis = await Codebase.Analyze(directory);
foreach (var codebaseFileAnalysis in codebaseAnalysis.Files)
{
    Console.WriteLine($">{codebaseFileAnalysis.FilePath}");
    Console.WriteLine($"  Lines: code={codebaseFileAnalysis.LineCounters.Code}, comments={codebaseFileAnalysis.LineCounters.Comments}, empty={codebaseFileAnalysis.LineCounters.Empty}, lines={codebaseFileAnalysis.LineCounters.Empty}");
    Console.WriteLine($"  Indentation: median={codebaseFileAnalysis.Indentation.Median}, max={codebaseFileAnalysis.Indentation.Max}, min={codebaseFileAnalysis.Indentation.Min}");
}

Console.WriteLine();

foreach (var codebaseDirectoryAnalysis in codebaseAnalysis.Directories)
    Console.WriteLine($">{codebaseDirectoryAnalysis.DirectoryPath}: files: {codebaseDirectoryAnalysis.NumberOfFiles}, code: {codebaseDirectoryAnalysis.LineCounters.Code}, comments: {codebaseDirectoryAnalysis.LineCounters.Comments}, empty: {codebaseDirectoryAnalysis.LineCounters.Empty}, lines: {codebaseDirectoryAnalysis.LineCounters.Empty}");

Console.WriteLine();

var repositoryAnalysis = VersionControl.Analyze(directory);
foreach (var repositoryFileAnalysis in repositoryAnalysis.Files)
    Console.WriteLine($">{repositoryFileAnalysis.FilePath}: times changed: {repositoryFileAnalysis.NumberOfTimesChanged}, number of authors: {repositoryFileAnalysis.Authors.Count}");
