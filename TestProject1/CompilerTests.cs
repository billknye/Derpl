using ConsoleApp6;

namespace TestProject1;

public class CompilerTests
{
    [Theory]
    [InlineData("32", 32D)]
    [InlineData("Any(true, false)", true)]
    public void Test1(string formula, object expected)
    {
        var syntax = SyntaxVisitor.Parse(formula);
        var compiled = ExpressionCompiler.Compile(formula, syntax, new DataSetDefinition());

        Assert.Equal(expected, compiled.Invoke(new DataSetEvaluator(), new DataRow()));
    }
}

public class SyntaxVisitorTests
{
    [Theory]
    [InlineData("'foobar")]
    public void Test(string input)
    {
        var syntax = SyntaxVisitor.Parse(input);
    }
}