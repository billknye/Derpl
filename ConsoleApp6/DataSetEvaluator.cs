namespace ConsoleApp6;

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
            if (previous == null)
            {
                previous = value;
                continue;
            }
            else
            {
                if (value < previous.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public double EvaluateSum(IReadOnlyList<double> numbers)
    {
        return numbers.Sum();
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
}