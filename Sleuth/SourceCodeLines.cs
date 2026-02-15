using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal sealed record SourceCodeLines(int Code, int Comments, int Empty)
{
    public static SourceCodeLines AnalyzeFile(string path) =>
        Analyze(File.ReadAllText(path));
    
    public static SourceCodeLines Analyze(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
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
        return new SourceCodeLines(codeLines.Count, commentLines.Count, linesCount - codeLines.Count - commentLines.Count);
    }
}