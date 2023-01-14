
using System.Runtime.CompilerServices;

namespace FormulaTest;
public static class SyntaxVisitor
{    
    /// <summary>
    /// Parses the given input into a collection of syntax nodes.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static SyntaxCollection Parse(ReadOnlySpan<char> input)
    {
        var syntaxNodes = new List<SyntaxNode>();

        char[] identifierStartinCharacters = (new int[] { '_' }).Concat(Enumerable.Range('a', 'z' - 'a' + 1)).Concat(Enumerable.Range('A', 'Z' - 'A' + 1)).Select(n => (char)n).ToArray();
        char[] identifierCharacters = identifierStartinCharacters.Concat(Enumerable.Range('0', '9' - '0' + 1).Select(n => (char)n)).ToArray();
        char[] whitespace = new[] { ' ', '\t', '\r', '\n' };
        char[] numbers = new[] { '.' }.Concat(Enumerable.Range('0', '9' - '0' + 1).Select(n => (char)n)).ToArray();

        int current = 0;
        var working = input;

        while (!working.IsEmpty)
        {
            if (identifierStartinCharacters.Contains(working[0]))
            {
                // identifier
                var next = working.IndexOfAnyExcept(identifierCharacters);
                if (next == -1)
                {
                    next = working.Length;
                }

                var range = new TextSpan(current, next);

                if (range.ApplyTo(input).ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    syntaxNodes.Add(new SyntaxNode(SyntaxKind.TrueLiteral, range));
                }
                else if (range.ApplyTo(input).ToString().Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    syntaxNodes.Add(new SyntaxNode(SyntaxKind.FalseLiteral, range));
                }
                else
                {
                    syntaxNodes.Add(new SyntaxNode(SyntaxKind.Identifier, range));
                }

                current += next;
            }
            else if (working[0] == '\'')
            {
                // string literal
                var index = 1;
                while (index < working.Length)
                {
                    if (working[index] == '\'' && working[index - 1] != '\\')
                        break;
                    index++;
                }

                if (index >= working.Length)
                {
                    throw new UnterminatedStringLiteralSyntaxException(new TextSpan(current, index), new TextSpan(current, index).ApplyTo(input).ToString());
                }

                syntaxNodes.Add(new SyntaxNode(SyntaxKind.StringLiteral, new TextSpan(current, index + 1)));
                current += index + 1;
            }
#if NET7_0_OR_GREATER
            else if (char.IsAsciiDigit(working[0]))
#else
            else if (working[0].IsAsciiDigit())
#endif
            {
                // numeric literal
                var next = working.IndexOfAnyExcept(numbers);
                if (next == -1)
                {
                    next = working.Length;
                }

                syntaxNodes.Add(new SyntaxNode(SyntaxKind.NumericLiteral, new TextSpan(current, next)));
                current += next;
            }
            else if (char.IsWhiteSpace(working[0]))
            {
                var next = working.IndexOfAnyExcept(whitespace);
                syntaxNodes.Add(new SyntaxNode(SyntaxKind.Whitespace, new TextSpan(current, next)));
                current += next;
            }
            else
            {
                var kind = working[0] switch
                {
                    ',' => SyntaxKind.Comma,
                    '.' => SyntaxKind.DotOperator,
                    '(' => SyntaxKind.OpenParenthesis,
                    ')' => SyntaxKind.CloseParenthesis,
                    '[' => SyntaxKind.OpenSquareBracket,
                    ']' => SyntaxKind.CloseSquareBracket,
                    _ => SyntaxKind.Unknown
                };

                syntaxNodes.Add(new SyntaxNode(kind, new TextSpan(current, 1)));
                current += 1;
            }

            working = input.Slice(current);
        }

        return new SyntaxCollection(syntaxNodes);
    }
}
