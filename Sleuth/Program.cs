using Sleuth;

var analyze = SourceCodeLines.AnalyzeFile("/Users/erik/Code/sleuth/Sleuth.Example/Program.cs");
Console.WriteLine(analyze);
var frequentlyChangedFiles = VersionControl.MostFrequentlyChangedFiles("/Users/erik/Code/sleuth");

foreach (var (file, count) in frequentlyChangedFiles)
    Console.WriteLine($">{file}: {count}");