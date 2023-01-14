using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ConsoleApp6;

public static class ExpressionCompiler
{
    static Dictionary<DataTypeBase, (Type, MethodInfo)> listType;

    static ExpressionCompiler()
    {
        listType = new Dictionary<DataTypeBase, (Type Type, MethodInfo AddMethod)>();

        listType.Add(DataTypeBase.Number, (typeof(List<double>), typeof(List<double>).GetMethod("Add"))!);
        listType.Add(DataTypeBase.String, (typeof(List<string>), typeof(List<string>).GetMethod("Add"))!);
        listType.Add(DataTypeBase.Bool, (typeof(List<bool>), typeof(List<bool>).GetMethod("Add"))!);

    }

    public static Func<DataSetEvaluator, DataRow, object> Compile(ReadOnlySpan<char> input, SyntaxCollection syntax, DataSetDefinition dataSet)
    {
        var compilationUnit = new CompilationUnit
        {
            Syntax = syntax,
            DataSetDefinition = dataSet,
            EvaluatorParameter = Expression.Parameter(typeof(DataSetEvaluator)),
            RowExpression = Expression.Parameter(typeof(DataRow))
        };

        var expression = ResolveTerm(compilationUnit, input);

        var boxing = Expression.TypeAs(expression.Item1, typeof(object));

        var a = Expression.Lambda<Func<DataSetEvaluator, DataRow, object>>(boxing, compilationUnit.EvaluatorParameter, compilationUnit.RowExpression).Compile();

        return a;
    }

    private static (Expression, DataType) ResolveTerm(CompilationUnit compilationUnit, ReadOnlySpan<char> input)
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
            DataTypeBase? setType = null;
            while (true)
            {
                var type = ResolveTerm(compilationUnit, input);
                if (setType == null)
                {
                    setType = type.Item2.Type;
                }

                if (type.Item2.IsSet)
                {
                    throw new NotSupportedException();
                }

                expressions.Add(type.Item1);

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

            var t = listType[setType.Value];
            var listCtor = t.Item1.GetConstructor(Type.EmptyTypes);
            var list = Expression.New(listCtor);

            var variable = Expression.Variable(t.Item1);

            return (Expression.Block(t.Item1, new[] { variable }, (new Expression[] { Expression.Assign(variable, list) }).Concat(expressions.Select(n => Expression.Call(variable, t.Item2, n))).Concat(new[] { variable }).ToArray()), new DataType(setType.Value, true));
        }
        else if (term.Value.SyntaxKind == SyntaxKind.NumericLiteral)
        {
            _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.NumericLiteral);
            return (Expression.Constant(double.Parse(term.Value.Range.ApplyTo(input).ToString())), new DataType(DataTypeBase.Number));
        }
        else if (term.Value.SyntaxKind == SyntaxKind.StringLiteral)
        {
            _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.StringLiteral);
            return (Expression.Constant(term.Value.Range.ApplyTo(input)[1..^1].ToString()), new DataType(DataTypeBase.String));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static (Expression, DataType) ResolveInternalMethod(MethodInfo methodInfo, CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        var methodParamType = GetMethodParameterDataType(methodInfo);
        var methodReturnType = GetMethodReturnDataType(methodInfo);

        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.OpenParenthesis);

        var expression = ResolveTerm(compilationUnit, input);

        if (expression.Item2 != methodParamType)
        {
            throw new InvalidOperationException();
        }
        
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.CloseParenthesis);

        return (Expression.Call(compilationUnit.EvaluatorParameter, methodInfo, expression.Item1), methodReturnType);
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

    private static (Expression, DataType) ResolvePropertyIdentifier(string identifier, CompilationUnit compilationUnit)
    {
        var method = typeof(DataSetEvaluator).GetMethod(nameof(DataSetEvaluator.GetPropertyValue));
        
        var property = compilationUnit.DataSetDefinition.Properties.FirstOrDefault(n => n.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        if (property != null)
        {
            var returnType = TranslateDataType(property.DataPropertyType);
            return (Expression.Unbox(Expression.Call(compilationUnit.EvaluatorParameter, method, compilationUnit.RowExpression, Expression.Constant(property.Name)), returnType), property.DataPropertyType);
        }
        else
        {
            Console.WriteLine();
            throw new NotImplementedException();
        }
    }

    private static DataType GetMethodParameterDataType(MethodInfo methodInfo)
    {
        var parameter = methodInfo.GetParameters().First();
        return TranslateDataType(parameter.ParameterType);
    }

    private static DataType GetMethodReturnDataType(MethodInfo methodInfo)
    {
        return TranslateDataType(methodInfo.ReturnType);
    }


    private static DataType TranslateDataType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            var inner = TranslateDataType(type.GetGenericArguments()[0]);
            return new DataType(inner.Type, true);
        }
        else
        {
            if (type == typeof(double))
                return new DataType(DataTypeBase.Number);
            if (type == typeof(string))
                return new DataType(DataTypeBase.String);
            if (type == typeof(bool))
                return new DataType(DataTypeBase.Bool);

            throw new NotSupportedException();
        }
    }

    private static Type TranslateDataType(DataType dataType)
    {
        var baseType = dataType.Type switch
        {
            DataTypeBase.Number => typeof(double),
            DataTypeBase.String => typeof(string),
            _ => throw new NotImplementedException()
        };

        if (dataType.IsSet)
        {
            return typeof(IReadOnlyList<>).MakeGenericType(baseType);
        }

        return baseType;
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
