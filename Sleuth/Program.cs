using Sleuth;

var codeCounts = await Codebase.CountLines("/Users/erik/Code/sleuth");
foreach (var codeCount in codeCounts)
    Console.WriteLine($">{codeCount.File}: code: {codeCount.Code}, comments: {codeCount.Comments}, empty: {codeCount.Empty}, lines: {codeCount.Empty}");

var frequentlyChangedFiles = VersionControl.MostFrequentlyChangedFiles("/Users/erik/Code/sleuth");
foreach (var frequentlyChangedFile in frequentlyChangedFiles)
    Console.WriteLine($">{frequentlyChangedFile.File}: {frequentlyChangedFile.Count}");