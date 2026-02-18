using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Sleuth;

internal sealed record FileIndentation(float Average, float Min, float Max);
internal sealed record LineCounters(int Code, int Comments, int Empty);
internal sealed record CodebaseFileAnalysis(LineCounters LineCounters, FileIndentation Indentation);

internal class Codebase(string directoryPath)
{
    public static async Task<Dictionary<string,CodebaseFileAnalysis>> Analyze(string directoryPath) =>
        await new Codebase(directoryPath).Analyze();

    private async Task<Dictionary<string,CodebaseFileAnalysis>> Analyze()
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*.cs");
        matcher.AddExclude("**/bin/");
        matcher.AddExclude("**/obj/");

        var fileAnalysesTasks = matcher.GetResultsInFullPath(directoryPath).Select(AnalyzeFile);
        var fileAnalyses = await Task.WhenAll(fileAnalysesTasks);
        return fileAnalyses.ToDictionary();
    }

    private async Task<(string, CodebaseFileAnalysis)> AnalyzeFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var lineCounters = CountLines(root);
        var indentation = CalculateIndentationStats(code);
        
        var relativePath = Path.GetRelativePath(directoryPath, filePath);
        var codebaseFileAnalysis = new CodebaseFileAnalysis(lineCounters, indentation);
        return (relativePath, codebaseFileAnalysis);
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
        var min = 0;
        var max = 0;
        var count = 0;
        var sum = 0;
        
        foreach (var line in code.EnumerateLines())
        {
            if (line.Length == 0)
                continue;
            
            var indentation = 0;

            foreach (var c in line)
            {
                if (!char.IsWhiteSpace(c))
                    break;
                   
                indentation += c == '\t' ? 4 : 1;
            }
            
            count++;
            sum += indentation;
            min = Math.Min(min, indentation);
            max = Math.Max(max, indentation);
        }
        
        var average = count == 0 ? 0 : sum / count;
        return new FileIndentation(average, min, max);
    }
}