using Derpl;

namespace TestProject1;

public class SyntaxVisitorTests
{
    [Theory]
    [InlineData("'foobar", typeof(UnterminatedStringLiteralSyntaxException))]
    public void Test(string input, Type exceptionType)
    {
        Assert.Throws(exceptionType, () =>
        {
            var syntax = SyntaxVisitor.Parse(input);
        });
    }
}