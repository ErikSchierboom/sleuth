using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var analyze = await Lines.Analyze("/Users/erik/Code/sleuth/Sleuth.Example/Program.cs");
Console.WriteLine(analyze);

record Lines(int Code, int Comments, int Empty)
{
    public static async Task<Lines> Analyze(string path)
    {
        var code = await File.ReadAllTextAsync(path);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        
        var codeLines = new HashSet<int>();
        foreach (var token in root.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.EndOfFileToken))
                continue;

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
        return new Lines(codeLines.Count, commentLines.Count, linesCount - codeLines.Count - commentLines.Count);
    }
}