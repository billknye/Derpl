namespace ConsoleApp6;

public enum DataTypeBase
{
    Unknown = 0,
    String = 1,
    Number = 2,
    Date = 3,
    Bool = 4
}

public record DataType(DataTypeBase Type, bool IsSet = false);