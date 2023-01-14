namespace FormulaTest;

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
}