using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Sleuth;

internal record FileIndentation(float Median, float Max, float Min);
internal record LineCounters(int Code, int Comments, int Empty);
internal record CodebaseFileAnalysis(string FilePath, LineCounters LineCounters, FileIndentation Indentation);
internal record DirectoryAnalysis(string DirectoryPath, int NumberOfFiles, LineCounters LineCounters);
internal record CodebaseAnalysis(string DirectoryPath, CodebaseFileAnalysis[] Files, DirectoryAnalysis[] Directories);

internal class Codebase(string directoryPath)
{
    public static async Task<CodebaseAnalysis> Analyze(string directoryPath) =>
        await new Codebase(directoryPath).Analyze();
    
    public async Task<CodebaseAnalysis> Analyze()
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*.cs");
        matcher.AddExclude("**/bin/");
        matcher.AddExclude("**/obj/");

        var fileAnalysesTasks = matcher.GetResultsInFullPath(directoryPath).Select(AnalyzeFile);
        var fileAnalyses = await Task.WhenAll(fileAnalysesTasks);
        var directoryAnalyses = AnalyzeDirectories(directoryPath, fileAnalyses);
        return new CodebaseAnalysis(directoryPath, fileAnalyses, directoryAnalyses);
    }

    private static async Task<CodebaseFileAnalysis> AnalyzeFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var lineCounters = CountLines(root);
        var indentation = CalculateIndentationStats(code);
        return new CodebaseFileAnalysis(filePath, lineCounters, indentation);
    }

    private static LineCounters CountLines(SyntaxNode root)
    {
        var codeLines = new HashSet<int>();
        foreach (var token in root.DescendantTokens())
        {
            var span = token.GetLocation().GetLineSpan();
            for (var line = span.StartLinePosition.Line; line <= span.EndLinePosition.Line; line++)
                codeLines.Add(line);
        }

        var commentLines = new HashSet<int>();
        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: false))
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) &&
                !trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) &&
                !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                continue;

            var span = trivia.GetLocation().GetLineSpan();
            for (var line = span.StartLinePosition.Line; line <= span.EndLinePosition.Line; line++)
                commentLines.Add(line);
        }
        commentLines.ExceptWith(codeLines);

        var lineCount = root.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
        return new LineCounters(codeLines.Count, commentLines.Count, lineCount - codeLines.Count - commentLines.Count);
    }

    private static FileIndentation CalculateIndentationStats(string code)
    {
        var indentations = new List<int>();
        foreach (var line in code.EnumerateLines())
        {
            var indentation = 0;

            foreach (var c in line)
            {
                if (!char.IsWhiteSpace(c))
                    break;
                   
                indentation += c == '\t' ? 4 : 1;
            }
            
            indentations.Add(indentation);
        }
        
        if (indentations.Count == 0)
            return new FileIndentation(0, 0, 0);
        
        return new FileIndentation(indentations[indentations.Count / 2], indentations.Max(), indentations.Min());
    }

    private static DirectoryAnalysis[] AnalyzeDirectories(string rootDirectory, CodebaseFileAnalysis[] fileAnalyses)
    {
        var directoryStats = new Dictionary<string, (int Files, int Code, int Comments, int Empty)>();

        foreach (var file in fileAnalyses)
        {
            var directory = Path.GetDirectoryName(file.FilePath);

            while (directory is not null)
            {
                if (!directoryStats.ContainsKey(directory))
                    directoryStats[directory] = (0, 0, 0, 0);

                var stats = directoryStats[directory];
                directoryStats[directory] = (
                    stats.Files + 1,
                    stats.Code + file.LineCounters.Code,
                    stats.Comments + file.LineCounters.Comments,
                    stats.Empty + file.LineCounters.Empty
                );
                
                if (directory == rootDirectory)
                    break;

                directory = Path.GetDirectoryName(directory);
            }
        }

        return directoryStats
            .Select(kv => new DirectoryAnalysis(kv.Key, kv.Value.Files, new LineCounters(kv.Value.Code, kv.Value.Comments, kv.Value.Empty)))
            .OrderBy(d => d.DirectoryPath)
            .ToArray();
    }
}