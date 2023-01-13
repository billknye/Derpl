
namespace ConsoleApp6;
public readonly record struct TextSpan(int Start, int Length)
{
    public ReadOnlySpan<char> ApplyTo(ReadOnlySpan<char> input)
    {
        return input.Slice(Start, Length);
    }
}