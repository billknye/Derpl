using FormulaTest;

namespace TestProject1;

public class CompilerTests
{
    [Theory]
    [InlineData("32", 32D)]
    [InlineData("Any([true, false])", true)]
    [InlineData("OrderedAscending([4, 6, 8])", true)]
    [InlineData("OrderedAscending([4, 9, 8])", false)]
    [InlineData("Diff(4, 2)", 2D)]
    [InlineData("All([true, false])", false)]
    [InlineData("All([true, true])", true)]
    public void Test1(string formula, object expected)
    {
        var syntax = SyntaxVisitor.Parse(formula);
        var compiled = ExpressionCompiler.Compile(formula, syntax, new DataSetDefinition());

        Assert.Equal(expected, compiled.Invoke(new DataSetEvaluator(), new DataRow()));
    }
}
