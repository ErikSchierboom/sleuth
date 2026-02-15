using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileSystemGlobbing;

internal record FileCodeCount(string File, int Code, int Comments, int Empty);

internal static class CodeCount
{
    public static async Task<FileCodeCount[]> Count(string directoryPath)
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*.cs");
        matcher.AddExclude("**/bin/");
        matcher.AddExclude("**/obj/");

        var result = matcher.GetResultsInFullPath(directoryPath);
        return await Task.WhenAll(result.Select(CountFile));
    }

    private static async Task<FileCodeCount> CountFile(string filePath)
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

        var linesCount = root.GetLocation().GetLineSpan().EndLinePosition.Line + 1;
        return new FileCodeCount(filePath, codeLines.Count, commentLines.Count, linesCount - codeLines.Count - commentLines.Count);
    }
}