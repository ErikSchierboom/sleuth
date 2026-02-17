using Sleuth;

var codeCounts = await Codebase.CountLines("/Users/erik/Code/sleuth");
foreach (var codeCount in codeCounts)
    Console.WriteLine($">{codeCount.File}: code: {codeCount.Code}, comments: {codeCount.Comments}, empty: {codeCount.Empty}, lines: {codeCount.Empty}");

var repositoryAnalysis = VersionControl.AnalyzeRepository("/Users/erik/Code/sleuth");
foreach (var repositoryFileAnalysis in repositoryAnalysis.Files)
    Console.WriteLine($">{repositoryFileAnalysis.File}: {repositoryFileAnalysis.NumberOfTimesChanged}");