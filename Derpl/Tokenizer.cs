namespace Derpl;
public static class Tokenizer
{
    static char[] identifierStartinCharacters;
    static char[] identifierCharacters;
    static char[] whitespace;
    static char[] numbers;
    static char[] trueString;
    static char[] falseString;

    static Tokenizer()
    {
        identifierStartinCharacters = (new int[] { '_' }).Concat(Enumerable.Range('a', 'z' - 'a' + 1)).Concat(Enumerable.Range('A', 'Z' - 'A' + 1)).Select(n => (char)n).ToArray();
        identifierCharacters = identifierStartinCharacters.Concat(Enumerable.Range('0', '9' - '0' + 1).Select(n => (char)n)).ToArray();
        whitespace = new[] { ' ', '\t', '\r', '\n' };
        numbers = new[] { '.' }.Concat(Enumerable.Range('0', '9' - '0' + 1).Select(n => (char)n)).ToArray();
        trueString = "true".ToArray();
        falseString = "false".ToArray();
    }

    public static bool TryReadTokenNode(ReadOnlySpan<char> input, int offset, out TokenNode node, out int end)
    {
        var tempNode = ReadTokenNode(input.Slice(offset));

        if (tempNode.TokenKind == TokenKind.Unknown)
        {
            node = default;
            end = offset;
            return false;
        }

        node = new TokenNode(tempNode.TokenKind, new TextSpan(tempNode.Range.Start + offset, tempNode.Range.Length));
        end = offset + node.Range.Length;

        return tempNode.TokenKind != TokenKind.Unknown;
    }

    private static TokenNode ReadTokenNode(ReadOnlySpan<char> input)
    {
        int current = 0;
        var working = input;

        if (working.IsEmpty)
            return default;

        if (identifierStartinCharacters.Contains(working[0]))
        {
            // identifier
            var next = working.IndexOfAnyExcept(identifierCharacters);
            if (next == -1)
            {
                next = working.Length;
            }

            var range = new TextSpan(current, next);

            if (MemoryExtensions.Equals(range.ApplyTo(input), trueString, StringComparison.OrdinalIgnoreCase)) // .ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return new TokenNode(TokenKind.TrueLiteral, range);
            }
            else if (MemoryExtensions.Equals(range.ApplyTo(input), falseString, StringComparison.OrdinalIgnoreCase))
            {
                return new TokenNode(TokenKind.FalseLiteral, range);
            }
            else
            {
                return new TokenNode(TokenKind.Identifier, range);
            }
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

            return new TokenNode(TokenKind.StringLiteral, new TextSpan(current, index + 1));
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

            return new TokenNode(TokenKind.NumericLiteral, new TextSpan(current, next));
        }
        else if (char.IsWhiteSpace(working[0]))
        {
            var next = working.IndexOfAnyExcept(whitespace);
            return new TokenNode(TokenKind.Whitespace, new TextSpan(current, next));
        }
        else if (working.Length >= 2 && working[0] == '=' && working[1] == '>')
        {
            return new TokenNode(TokenKind.LambdaOperator, new TextSpan(current, 2));
        }
        else
        {
            var kind = working[0] switch
            {
                ',' => TokenKind.Comma,
                '.' => TokenKind.DotOperator,
                '(' => TokenKind.OpenParenthesis,
                ')' => TokenKind.CloseParenthesis,
                '[' => TokenKind.OpenSquareBracket,
                ']' => TokenKind.CloseSquareBracket,
                _ => TokenKind.Unknown
            };

            return new TokenNode(kind, new TextSpan(current, 1));
        }
    }

    /// <summary>
    /// Parses the given input into a collection of token nodes.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static TokenCollection Parse(ReadOnlySpan<char> input)
    {
        var syntaxNodes = new List<TokenNode>();
        var store = new TokenStore(input);

        foreach (var node in store)
        {
            syntaxNodes.Add(node);
        }

        return new TokenCollection(syntaxNodes);
    }
}

public readonly ref struct TokenStore
{
    private readonly ReadOnlySpan<char> input;

    public TokenStore(ReadOnlySpan<char> input)
    {
        this.input = input;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public ref struct Enumerator
    {
        private TokenStore syntaxStore;
        private TokenNode current;

        public Enumerator(TokenStore syntaxStore)
        {
            this.syntaxStore = syntaxStore;
            this.current = default;
        }

        public bool MoveNext()
        {
            if (Tokenizer.TryReadTokenNode(syntaxStore.input, current.Range.Start + current.Range.Length, out var node, out var end))
            {
                current = node;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            // No Op
        }

        public TokenNode Current => current;
    }
}
