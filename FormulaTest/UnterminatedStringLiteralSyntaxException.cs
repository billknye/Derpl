namespace FormulaTest;

public class UnterminatedStringLiteralSyntaxException : StringLiteralSyntaxException
{
    public string Text { get; }

    public UnterminatedStringLiteralSyntaxException(TextSpan location, string text) : base(location, GetMessage(location, text))
    {
        Text = text;
    }

    static string GetMessage(TextSpan location, string text)
    {
        return $"Unterminated string literal @ {location}: {text}";
    }
}