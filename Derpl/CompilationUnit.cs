using System.Linq.Expressions;

namespace Derpl;

public class CompilationUnit
{
    public ParameterExpression EvaluatorParameter { get; set; }
    public ParameterExpression RowExpression { get; set; }

    public SyntaxCollection Syntax { get; set; }

    public DataSetDefinition DataSetDefinition { get; set; }

    // used in lambdas.
    public Dictionary<string, TypedExpression> Variables { get; set; }

    public CompilationUnit()
    {
        Variables = new Dictionary<string, TypedExpression>(StringComparer.OrdinalIgnoreCase);
    }
}
