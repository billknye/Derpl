using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace FormulaTest;

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

    /// <summary>
    /// Compiles the given syntax input, syntax tree and data set definition into an invokable func the evaluate the compiled expression.
    /// </summary>
    /// <param name="input">The raw input.</param>
    /// <param name="syntax">The parsed syntax tree from the <see cref="SyntaxVisitor"/>.</param>
    /// <param name="dataSet">A <see cref="DataSetDefinition"/> instance to bind property lookups from.</param>
    /// <returns></returns>
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

        // always box the expression into object.
        var boxing = Expression.TypeAs(expression.Expression, typeof(object));
        var a = Expression.Lambda<Func<DataSetEvaluator, DataRow, object>>(boxing, compilationUnit.EvaluatorParameter, compilationUnit.RowExpression).Compile();

        return a;
    }

    /// <summary>
    /// Resolves a single expression term.
    /// </summary>
    /// <param name="compilationUnit">The working compliation unit to build the expression from.</param>
    /// <param name="input">The raw input, used for identifier resolution.</param>
    /// <returns>A <see cref="TypedExpression"/> instance of the expression to evalute and the type that results.</returns>
    private static TypedExpression ResolveTerm(CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        var term = compilationUnit.Syntax.Peek();
        if (term == null)
        {
            throw new InvalidOperationException();
        }

        return term.Value.SyntaxKind switch
        {
            SyntaxKind.Identifier => ResolveIdentifier(compilationUnit, input),
            SyntaxKind.OpenSquareBracket => ResolveSetDeclaration(compilationUnit, input),
            SyntaxKind.NumericLiteral => ResolveNumericLiteral(compilationUnit, input, term),
            SyntaxKind.StringLiteral => ResolveStringLiteral(compilationUnit, input, term),
            SyntaxKind.TrueLiteral => ResolveTrueLiteral(compilationUnit),
            SyntaxKind.FalseLiteral => ResolveFalseLiteral(compilationUnit),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Resolves and identifier to an expression.
    /// </summary>
    /// <param name="compilationUnit">Thhe working compilation unit to build the expression from.</param>
    /// <param name="input">The raw input, used for identifier resolution.</param>
    /// <returns>A <see cref="TypedExpression"/> instance of the expression to evlauate and the type that results.</returns>
    private static TypedExpression ResolveIdentifier(CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        var identifierSet = compilationUnit.Syntax.TakeWhile(SyntaxKind.Identifier, SyntaxKind.DotOperator);
        var identifier = FlattenIdentifier(input, identifierSet);

        var internalMethod = ResolveInternalIdentifier(identifier);
        if (internalMethod.Length > 0)
        {
            return ResolveInternalMethod(internalMethod, compilationUnit, input);
        }
        else
        {
            return ResolvePropertyIdentifier(identifier, compilationUnit);
        }
    }

    /// <summary>
    /// Creates a false literal expression.
    /// </summary>
    /// <param name="compilationUnit"></param>
    /// <returns></returns>
    private static TypedExpression ResolveFalseLiteral(CompilationUnit compilationUnit)
    {
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.FalseLiteral);
        return new TypedExpression(Expression.Constant(false), new DataType(DataTypeBase.Bool));
    }

    /// <summary>
    /// Creates a true literal expression.
    /// </summary>
    /// <param name="compilationUnit"></param>
    /// <returns></returns>
    private static TypedExpression ResolveTrueLiteral(CompilationUnit compilationUnit)
    {
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.TrueLiteral);
        return new TypedExpression(Expression.Constant(true), new DataType(DataTypeBase.Bool));
    }

    /// <summary>
    /// Creates a string literal expression.
    /// </summary>
    /// <param name="compilationUnit"></param>
    /// <param name="input"></param>
    /// <param name="term"></param>
    /// <returns></returns>
    private static TypedExpression ResolveStringLiteral(CompilationUnit compilationUnit, ReadOnlySpan<char> input, SyntaxNode? term)
    {
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.StringLiteral);
        return new TypedExpression(Expression.Constant(term.Value.Range.ApplyTo(input)[1..^1].ToString()), new DataType(DataTypeBase.String));
    }

    /// <summary>
    /// Creates a numeric literal expression.
    /// </summary>
    /// <param name="compilationUnit"></param>
    /// <param name="input"></param>
    /// <param name="term"></param>
    /// <returns></returns>
    private static TypedExpression ResolveNumericLiteral(CompilationUnit compilationUnit, ReadOnlySpan<char> input, SyntaxNode? term)
    {
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.NumericLiteral);
        return new TypedExpression(Expression.Constant(double.Parse(term.Value.Range.ApplyTo(input).ToString())), new DataType(DataTypeBase.Number));
    }

    /// <summary>
    /// Creates a typed set expression.
    /// </summary>
    /// <remarks>
    /// The underlying type is always a <see cref="List{T}"/>, typed as a <see cref="IReadOnlyList{T}"/>.
    /// </remarks>
    /// <param name="compilationUnit"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    private static TypedExpression ResolveSetDeclaration(CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        // read terms in the set []
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.OpenSquareBracket);
        var expressions = new List<Expression>();
        DataTypeBase? setType = null;
        while (true)
        {
            var element = ResolveTerm(compilationUnit, input);

            // Use the first element to set the expected type.
            if (setType == null)
            {
                setType = element.Type.Type;
            }

            // Throw if we are trying to set sets.
            if (element.Type.IsSet)
            {
                throw new NotSupportedException();
            }

            expressions.Add(element.Expression);

            // Check we have a comma, indicating another element or a closing square bracket indicating the end of the set.
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

            throw new InvalidOperationException("Unexpected syntax kind while parsing set.");
        }

        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.CloseSquareBracket);

        var t = listType[setType.Value];
        var listCtor = t.Item1.GetConstructor(Type.EmptyTypes);
        var listInstance = Expression.New(listCtor);

        // Create a variable reference for the list to add items to.
        var variable = Expression.Variable(t.Item1);

        var blockExpression = Expression
            .Block(
            t.Item1, // The resulting type of the block.
            new[] { variable }, // the variables scoped to the block.
            (new Expression[] { Expression.Assign(variable, listInstance) }) // Assign the list ctor expression to the variable.
            .Concat(expressions.Select(n => Expression.Call(variable, t.Item2, n))) // Call .Add on the variable instance with the values.
            .Concat(new[] { variable }) // the last block expression is the return value, in this case the variable.
            .ToArray());

        return new TypedExpression(blockExpression, new DataType(setType.Value, true));
    }

    /// <summary>
    /// Attempts to bind to a built-in method call.
    /// </summary>
    /// <param name="methodInfos"></param>
    /// <param name="compilationUnit"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    private static TypedExpression ResolveInternalMethod(MethodInfo[] methodInfos, CompilationUnit compilationUnit, ReadOnlySpan<char> input)
    {
        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.OpenParenthesis);

        var arguments = new List<TypedExpression>();
        
        if (compilationUnit.Syntax.Peek()?.SyntaxKind != SyntaxKind.CloseParenthesis)
        {
            arguments.Add(ResolveTerm(compilationUnit, input));

            while (compilationUnit.Syntax.Peek()?.SyntaxKind == SyntaxKind.Comma)
            {
                _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.Comma);
                arguments.Add(ResolveTerm(compilationUnit, input));
            }
        }

        _ = compilationUnit.Syntax.TakeExpect(SyntaxKind.CloseParenthesis);

        // Method overload resolution
        foreach (var methodInfo in methodInfos)
        {
            var methodParamTypes = GetMethodParameterDataTypes(methodInfo);
            var methodReturnType = GetMethodReturnDataType(methodInfo);

            if (methodParamTypes.Length != arguments.Count)
                continue;

            var match = true;
            for (int i = 0; i < methodParamTypes.Length; i++)
            {
                if (methodParamTypes[i].Type != arguments[i].Type)
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return new TypedExpression(Expression.Call(compilationUnit.EvaluatorParameter, methodInfo, arguments.Select(n => n.Expression).ToArray()), methodReturnType);
            }
        }

        throw new InvalidOperationException("Could not find method that matches the signature.");
    }

    /// <summary>
    /// Resolves an identifier into a set of possible <see cref="MethodInfo"/>s.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    private static MethodInfo[] ResolveInternalIdentifier(string identifier)
    {
        var targetMethodName = $"Evaluate{identifier}";

        return typeof(DataSetEvaluator)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(n => n.Name.Equals(targetMethodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Resolves an identifier into a property from the data set.
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="compilationUnit"></param>
    /// <returns></returns>
    private static TypedExpression ResolvePropertyIdentifier(string identifier, CompilationUnit compilationUnit)
    {
        var method = typeof(DataSetEvaluator).GetMethod(nameof(DataSetEvaluator.GetPropertyValue));
        
        var property = compilationUnit.DataSetDefinition.Properties.FirstOrDefault(n => n.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        if (property != null)
        {
            var returnType = property.DataPropertyType.ToNetType();
            // TODO handle reference types.
            return new TypedExpression(Expression.Unbox(Expression.Call(compilationUnit.EvaluatorParameter, method, compilationUnit.RowExpression, Expression.Constant(property.Name)), returnType), property.DataPropertyType);
        }
        else
        {
            throw new InvalidOperationException($"Unresolved property: {identifier}");
        }
    }

    /// <summary>
    /// Gets the set of method paramters for a given <see cref="MethodInfo"/>.
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    private static MethodParameter[] GetMethodParameterDataTypes(MethodInfo methodInfo)
    {
        return methodInfo
            .GetParameters()
            .Select(p => new MethodParameter(p.Name!, DataType.FromNetType(p.ParameterType)))
            .ToArray();
    }

    /// <summary>
    /// Gets the return type for the given <see cref="MethodInfo"/>.
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    private static DataType GetMethodReturnDataType(MethodInfo methodInfo)
    {
        return DataType.FromNetType(methodInfo.ReturnType);
    }
    
    /// <summary>
    /// Turns the identifier nodes into a simple <see cref="string"/> from the raw input.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="identifierNodes"></param>
    /// <returns></returns>
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
