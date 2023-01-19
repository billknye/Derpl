
namespace Derpl;

public enum TokenKind
{
    Unknown,
    Whitespace,
    Identifier,
    StringLiteral,
    NumericLiteral,
    TrueLiteral,
    FalseLiteral,
    DotOperator,
    Comma,
    LambdaOperator,
    OpenParenthesis,
    CloseParenthesis,
    OpenSquareBracket,
    CloseSquareBracket,
}
