using Sleuth;

var codebaseAnalysis = await Codebase.Analyze("/Users/erik/Code/sleuth");
foreach (var codebaseFileAnalysis in codebaseAnalysis.Files)
    Console.WriteLine($">{codebaseFileAnalysis.File}: code: {codebaseFileAnalysis.LineCounters.Code}, comments: {codebaseFileAnalysis.LineCounters.Comments}, empty: {codebaseFileAnalysis.LineCounters.Empty}, lines: {codebaseFileAnalysis.LineCounters.Empty}");

var repositoryAnalysis = VersionControl.AnalyzeRepository("/Users/erik/Code/sleuth");
foreach (var repositoryFileAnalysis in repositoryAnalysis.Files)
    Console.WriteLine($">{repositoryFileAnalysis.File}: times changed: {repositoryFileAnalysis.NumberOfTimesChanged}, number of authors: {repositoryFileAnalysis.Authors.Count}");