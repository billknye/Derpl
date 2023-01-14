namespace FormulaTest;

public record DataType(DataTypeBase Type, bool IsSet = false)
{
    public static DataType FromNetType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            var inner = FromNetType(type.GetGenericArguments()[0]);
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
            if (type == typeof(DateTimeOffset))
                return new DataType(DataTypeBase.Date);

            throw new NotSupportedException();
        }
    }

    public Type ToNetType()
    {
        var baseType = Type switch
        {
            DataTypeBase.Number => typeof(double),
            DataTypeBase.String => typeof(string),
            DataTypeBase.Date => typeof(DateTimeOffset),
            _ => throw new NotImplementedException()
        };

        if (IsSet)
        {
            return typeof(IReadOnlyList<>).MakeGenericType(baseType);
        }

        return baseType;
    }
}