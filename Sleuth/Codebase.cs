using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Sleuth;

internal record LineCounters(int Code, int Comments, int Empty);
internal record CodebaseFileAnalysis(string File, LineCounters LineCounters);
internal record DirectoryAnalysis(string Directory, int NumberOfFiles, int NumberOfLinesOfCode);
internal record CodebaseAnalysis(string Directory, CodebaseFileAnalysis[] Files);

internal static class Codebase
{
    public static async Task<CodebaseAnalysis> Analyze(string directoryPath)
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*.cs");
        matcher.AddExclude("**/bin/");
        matcher.AddExclude("**/obj/");

        var fileAnalysesTasks = matcher.GetResultsInFullPath(directoryPath).Select(AnalyzeFile);
        var fileAnalyses = await Task.WhenAll(fileAnalysesTasks);
        return new CodebaseAnalysis(directoryPath, fileAnalyses);
    }

    private static async Task<CodebaseFileAnalysis> AnalyzeFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

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
        var lineCounters = new LineCounters(codeLines.Count, commentLines.Count, lineCount - codeLines.Count - commentLines.Count);
        return new CodebaseFileAnalysis(filePath, lineCounters);
    }
}