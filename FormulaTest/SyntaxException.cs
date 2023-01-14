namespace FormulaTest;

public class SyntaxException : Exception
{
    public TextSpan Location { get; }

    public SyntaxException(TextSpan location, string message) : base(message)
    {
        Location = location;
    }
}
