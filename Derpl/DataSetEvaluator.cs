using System.Collections;
using System.Linq.Expressions;

namespace Derpl;

public class DataSetEvaluator
{
    public object? GetPropertyValue(DataRow row, string propertyName)
    {
        row.Values.TryGetValue(propertyName, out var value);
        return value;
    }

    public bool EvaluateOrderedAscending(IReadOnlyList<double> numbers)
    {
        double? previous = null;
        foreach (var value in numbers)
        {
            if (previous != null && value < previous.Value)
            {
                return false;
            }

            previous = value;
            continue;
        }

        return true;
    }

    public DateTimeOffset EvaluateNow()
    {
        return DateTimeOffset.Now;
    }

    public double EvaluateDiff(DateTimeOffset left, DateTimeOffset right)
    {
        return (left - right).TotalDays;
    }

    public double EvaluateDiff(double left, double right)
    {
        return left - right;
    }

    public double EvaluateSum(IReadOnlyList<double> numbers)
    {
        return numbers.Sum();
    }

    public double EvaluateAvg(IReadOnlyList<double> numbers)
    {
        return numbers.Average();
    }

    public string EvaluateConcat(IReadOnlyList<string> words)
    {
        return string.Concat(words);
    }

    public bool EvaluateAny(IReadOnlyList<bool> values)
    {
        return values.Any(n => n);
    }

    public bool EvaluateAll(IReadOnlyList<bool> values)
    {
        return values.All(n => n);
    }

    public bool EvaluateNone(IReadOnlyList<bool> values)
    {
        return values.All(n => !n);
    }

    public LambdaExpressionReturn<IReadOnlyList<double>> EvaluateFilter(
        LambdaExpressionParameter<IReadOnlyList<double>> values,
        LambdaExpressionArguments<double, bool> lambda)
    {
        var body = ExpressionSetToSetBuilder<double, double>(
            values.Variable,
            lambda.Variable,
            n => Expression.IfThen(lambda.Iterator, n.AddMethod(n.element)));

        return new LambdaExpressionReturn<IReadOnlyList<double>>(body);
    }

    private static Expression ExpressionSetToSetBuilder<TElementInputType, TElementOutputType>(
    Expression input,
    ParameterExpression lambdaVariable,
    Func<(Func<Expression, Expression> AddMethod, ParameterExpression element), Expression> builder)
    {
        // declare list to receive elements
        var listVar = Expression.Variable(typeof(List<TElementOutputType>), "EvaluateFilterList");
        var addMethod = typeof(List<TElementOutputType>).GetMethod("Add");
        var listCtor = typeof(List<TElementOutputType>).GetConstructor(Type.EmptyTypes);

        // boiler plate
        var elementType = typeof(TElementInputType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
        var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

        var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
        var getEnumeratorCall = Expression.Call(input, enumerableType.GetMethod("GetEnumerator"));
        var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

        // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
        var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

        var breakLabel = Expression.Label("LoopBreak");

        // mostly loop boiler plate
        var loop = Expression.Block(new[] { listVar, enumeratorVar },
            Expression.Assign(listVar, Expression.New(listCtor)),
            enumeratorAssign,
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.Equal(moveNextCall, Expression.Constant(true)),
                    Expression.Block(new[] { lambdaVariable },
                        Expression.Assign(lambdaVariable, Expression.Property(enumeratorVar, "Current")),

                        builder.Invoke((n => Expression.Call(listVar, addMethod, n), lambdaVariable))

                    // call experssion and add to list if true
                    // Expression.IfThen(lambda.Iterator, Expression.Call(listVar, addMethod, lambda.Variable))
                    ),
                    Expression.Break(breakLabel)
                ),
            breakLabel),

            // what to return from this expression.
            listVar
        ); ;

        return loop;
    }
}

// TODO rename these...
public record LambdaExpressionArguments<TArg0, TReturn>(ParameterExpression Variable, Expression Iterator);

public record LambdaExpressionReturn<TReturn>(Expression Expression) : LambdaExpressionReturn(Expression);
public record LambdaExpressionReturn(Expression Expression);

public record LambdaExpressionParameter<T>(Expression Variable);