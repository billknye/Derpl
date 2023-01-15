namespace Derpl;

public class StringLiteralSyntaxException : SyntaxException
{
    public StringLiteralSyntaxException(TextSpan location, string message) : base(location, message)
    {
    }
}
