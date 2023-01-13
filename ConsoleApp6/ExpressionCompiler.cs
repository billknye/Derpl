using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ConsoleApp6;

internal static class ExpressionCompiler
{
    internal static Func<DataSetEvaluator, DataRow, object> Compile(ReadOnlySpan<char> input, SyntaxCollection syntax, DataSetDefinition dataSet)
    {
        var compilationUnit = new CompilationUnit
        {
            Syntax = syntax,
            DataSetDefinition = dataSet,
            EvaluatorParameter = Expression.Parameter(typeof(DataSetEvaluator)),
            RowExpression = Expression.Parameter(typeof(DataRow))
        };

        var expression = ResolveTerm(compilationUnit, input);

        var a = Expression.Lambda<Func<DataSetEvaluator, DataRow, object>>(expression, compilationUnit.EvaluatorParameter, compilationUnit.RowExpression).Compile();

        return a;
    }

    private static Expression ResolveTerm(CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        var term = compilationUnit.Syntax.Peek();
        if (term == null)
        {
            throw new InvalidOperationException();
        }

        if (term.Value.SyntaxKind == SyntaxKind.Identifier)
        {
            // 
            var identifierSet = compilationUnit.Syntax.TakeWhile(SyntaxKind.Identifier, SyntaxKind.DotOperator);
            var identifier = FlattenIdentifier(input, identifierSet);

            var internalMethod = ResolverInternalIdentifier(identifier);
            if (internalMethod != null)
            {
                return ResolveInternalMethod(internalMethod, compilationUnit, input);
            }
            else
            {
                return ResolvePropertyIdentifier(identifier, compilationUnit);
            }
        }
        else if (term.Value.SyntaxKind == SyntaxKind.OpenSquareBracket)
        {
            // read terms in the set []

            _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.OpenSquareBracket);
            var expressions = new List<Expression>();
            while (true)
            {
                expressions.Add(ResolveTerm(compilationUnit, input));

                var next = compilationUnit.Syntax.Peek();
                if (next == null)
                    break;

                if (next?.SyntaxKind == SyntaxKind.Comma)
                {
                    _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.Comma);
                    continue;
                }

                if (next?.SyntaxKind == SyntaxKind.CloseSquareBracket)
                    break;
            }

            _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.CloseSquareBracket);

            var expression = Expression.NewArrayInit(typeof(object), expressions.ToArray());
            return expression;
        }
        else if (term.Value.SyntaxKind == SyntaxKind.NumericLiteral)
        {
            _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.NumericLiteral);
            return Expression.TypeAs(Expression.Constant(double.Parse(term.Value.Range.ApplyTo(input).ToString())), typeof(object));
        }
        else if (term.Value.SyntaxKind == SyntaxKind.StringLiteral)
        {
            _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.StringLiteral);
            return Expression.Constant(term.Value.Range.ApplyTo(input).ToString());
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static Expression ResolveInternalMethod(MethodInfo methodInfo, CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.OpenParenthesis);

        var expression = ResolveTerm(compilationUnit, input);
        
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.CloseParenthesis);

        return Expression.TypeAs(Expression.Call(compilationUnit.EvaluatorParameter, methodInfo, expression), typeof(object));
    }

    private static MethodInfo? ResolverInternalIdentifier(string identifier)
    {
        var methods = typeof(DataSetEvaluator)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public).ToDictionary(n => n.Name, StringComparer.OrdinalIgnoreCase);

        if (methods.TryGetValue($"Evaluate{identifier}", out var method))
        {
            return method;
        }

        return null;
    }

    private static Expression ResolvePropertyIdentifier(string identifier, CompilationUnit compilationUnit)
    {
        var method = typeof(DataSetEvaluator).GetMethod(nameof(DataSetEvaluator.GetPropertyValue));
        
        var property = compilationUnit.DataSetDefinition.Properties.FirstOrDefault(n => n.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        if (property != null)
        {
            return Expression.Call(compilationUnit.EvaluatorParameter, method, compilationUnit.RowExpression, Expression.Constant(property.Name));
        }
        else
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }
    }

    private static string FlattenIdentifier(ReadOnlySpan<char> input, IEnumerable<SyntaxNode> identifierNodes)
    {
        var stringBuilder = new StringBuilder(100);

        foreach (var node in identifierNodes)
        {
            switch (node.SyntaxKind)
            {
                case SyntaxKind.Identifier:
                    stringBuilder.Append(node.Range.ApplyTo(input));
                    break;
                case SyntaxKind.DotOperator:
                    stringBuilder.Append('.');
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        return stringBuilder.ToString();
    }
}
