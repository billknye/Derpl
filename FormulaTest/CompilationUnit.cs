using System.Linq.Expressions;

namespace FormulaTest;

public class CompilationUnit
{
    public ParameterExpression EvaluatorParameter { get; set; }
    public ParameterExpression RowExpression { get; set; }

    public SyntaxCollection Syntax { get; set; }

    public DataSetDefinition DataSetDefinition { get; set; }
}
