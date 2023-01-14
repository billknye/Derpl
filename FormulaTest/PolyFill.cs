namespace FormulaTest;
#if !NET7_0_OR_GREATER

public static class PolyFill
{
    
    public static int IndexOfAnyExcept<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> exemptions) where T : IEquatable<T>
    {
        for (int x = 0; x < span.Length; x++)
        {
            bool found = false;
            for (int y = 0; y < exemptions.Length; y++)
            {
                if (span[x].Equals(exemptions[y]))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return x;
            }
        }

        return -1;
    }

    public static bool IsAsciiDigit(this char c)
    {
        return c >= '0' && c <= '9';
    }
}

#endif