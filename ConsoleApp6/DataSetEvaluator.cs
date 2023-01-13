namespace ConsoleApp6;

public class DataSetEvaluator
{
    public object? GetPropertyValue(DataRow row, string propertyName)
    {
        row.Values.TryGetValue(propertyName, out var value);
        return value;
    }

    public bool EvaluateOrderedAscending(IEnumerable<object> something)
    {
        // tODO don't assume double

        double? previous = null;
        foreach (var value in something)
        {
            if (value is double d)
            {
                if (previous == null)
                {
                    previous = d;
                    continue;
                }
                else
                {
                    if (d < previous.Value)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}