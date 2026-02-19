using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sleuth;

public enum OutputFormat
{
    Csv,
    Json
}

internal static class Output
{
    public static async Task WriteToFile(string filePath, FileAnalysis[] analyses, OutputFormat format)
    {
        if (format == OutputFormat.Csv)
            await WriteToCsv(filePath, analyses);
        else if (format == OutputFormat.Json)
            await WriteToJson(filePath, analyses);
        else
            throw new ArgumentOutOfRangeException(nameof(format));
    }

    private static async Task WriteToJson(string filePath, FileAnalysis[] analyses)
    {
        await using var outputStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(outputStream, analyses, _jsonSerializerOptions);
    }
    
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    private static async Task WriteToCsv(string filePath, FileAnalysis[] analyses)
    {
        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap(new FileAnalysisMap());
        await csv.WriteRecordsAsync(analyses);
    }
    
    private sealed class FileAnalysisMap : ClassMap<FileAnalysis>
    {
        public FileAnalysisMap()
        {
            var index = 0;
            
            Map(m => m.FilePath).Name("File").Index(index++);
            Map(m => m.VersionControl.NumberOfTimesChanged).Name("Git.NumberOfTimesChanged").Index(index++);
            Map(m => m.VersionControl.Authors.Count).Name("Git.NumberOfAuthors").Index(index++);
            Map(m => m.VersionControl.LastChangedAt).Name("Git.LastChangedAt").Index(index++);
            Map(m => m.Codebase.LineCounters.Code).Name("Lines.Code").Index(index++);
            Map(m => m.Codebase.LineCounters.Comments).Name("Lines.Comments").Index(index++);
            Map(m => m.Codebase.LineCounters.Empty).Name("Lines.Empty").Index(index++);
            Map(m => m.Codebase.Indentation.Average).Name("Indent.Avg").Index(index++);
            Map(m => m.Codebase.Indentation.Min).Name("Indent.Min").Index(index++);
            Map(m => m.Codebase.Indentation.Max).Name("Indent.Max").Index(index);
        }
    }
}