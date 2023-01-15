namespace Derpl;

public class DataType
{
    private DataTypeBase baseType;
    private bool isSet;

    public DataTypeBase Type => IsFunction ? throw new InvalidOperationException() : baseType;

    public bool IsSet => IsFunction ? throw new InvalidOperationException() : isSet;

    public bool IsFunction { get; }

    public IReadOnlyList<DataType>? Parameters { get; }

    public DataType? ReturnType { get; }

    public static DataType CreateBasic(DataTypeBase type) => new DataType(type);

    public static DataType CreateSet(DataTypeBase type) => new DataType(type, true);

    public static DataType CreateSet(DataType type) => type.IsSet == true ? throw new InvalidOperationException("Type is already a set") : new DataType(type.Type, true);

    public static DataType CreateFunction(DataType returnType, params DataType[] parameters) => new DataType(returnType.Type, returnType.IsSet, parameters);

    private DataType(DataTypeBase type, bool isSet = false, IEnumerable<DataType> parameters = null)
    {
        if (parameters != null && parameters.Any())
        {
            IsFunction = true;
            this.Parameters = parameters.ToList();
            this.ReturnType = new DataType(type, isSet);
        }
        else
        {
            baseType = type;
            this.isSet = isSet;
        }
    }

    public static DataType FromNetType(Type type)
    {
        if (type == typeof(double))
            return new DataType(DataTypeBase.Number);
        if (type == typeof(string))
            return new DataType(DataTypeBase.String);
        if (type == typeof(bool))
            return new DataType(DataTypeBase.Bool);
        if (type == typeof(DateTimeOffset))
            return new DataType(DataTypeBase.Date);
        if (type.IsGenericType)
        {
            var gen = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();

            if (gen == typeof(LambdaExpressionArguments<,>))
            {
                return DataType.CreateFunction(FromNetType(args[1]), FromNetType(args[0]));
            }
            else if (gen == typeof(IReadOnlyList<>))
            {
                return DataType.CreateSet(FromNetType(args[0]));
            }
            else if (gen == typeof(LambdaExpressionReturn<>))
            {
                return DataType.FromNetType(args[0]);
            }
            else if (gen == typeof(LambdaExpressionParameter<>))
            {
                return DataType.FromNetType(args[0]);
            }
        }

        throw new NotSupportedException();

    }


    public Type ToNetType()
    {
        if (IsFunction)
        {
            return typeof(Func<,,,>).MakeGenericType(new[] { typeof(DataSetEvaluator), typeof(DataRow) }.Concat(Parameters.Select(n => n.ToNetType())).Concat(new[] { ReturnType.ToNetType() }).ToArray());
        }

        var baseType = Type switch
        {
            DataTypeBase.Number => typeof(double),
            DataTypeBase.String => typeof(string),
            DataTypeBase.Date => typeof(DateTimeOffset),
            DataTypeBase.Bool => typeof(bool),
            _ => throw new NotImplementedException()
        };

        if (IsSet)
        {
            return typeof(IReadOnlyList<>).MakeGenericType(baseType);
        }

        return baseType;
    }

    public Type ToLambdaNetType()
    {
        if (IsFunction)
        {
            return typeof(LambdaExpressionArguments<,>).MakeGenericType(Parameters.Select(n => n.ToNetType()).Concat(new[] { ReturnType.ToNetType() }).ToArray());
        }
        else
        {
            return typeof(LambdaExpressionParameter<>).MakeGenericType(ToNetType());
        }
    }

    public bool IsCompatableWith(DataType type)
    {
        if (IsFunction)
        {
            if (!type.IsFunction || !ReturnType.IsCompatableWith(type.ReturnType))
                return false;

            if (Parameters.Count != type.Parameters.Count)
                return false;

            for (int i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].IsCompatableWith(type.Parameters[i]))
                    return false;
            }

            return true;
        }

        return Type == type.Type
            && IsSet == type.IsSet;
    }
}