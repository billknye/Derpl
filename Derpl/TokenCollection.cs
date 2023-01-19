using System.Collections;


namespace Derpl;

public class TokenCollection : IEnumerable<TokenNode>
{
    private readonly IReadOnlyList<TokenNode> nodes;
    private int index;

    private Stack<int> indexStack;

    public TokenCollection(IReadOnlyList<TokenNode> nodes)
    {
        this.nodes = nodes;
        this.index = 0;

        indexStack = new Stack<int>();
    }

    public IEnumerator<TokenNode> GetEnumerator() => nodes.Where(n => n.TokenKind != TokenKind.Whitespace).GetEnumerator();

    public void PushState()
    {
        indexStack.Push(index);
    }

    public void PopState()
    {
        index = indexStack.Pop();
    }

    /// <summary>
    /// Returns the current node without consumeing it (taking).
    /// </summary>
    /// <returns></returns>
    public TokenNode? Peek()
    {
        var nextIndex = index;
        while (true)
        {
            if (nextIndex >= nodes.Count)
            {
                return null;
            }

            var next = nodes[nextIndex];

            if (next.TokenKind == TokenKind.Whitespace)
            {
                nextIndex++;
            }
            else
            {
                return next;
            }
        }
    }

    /// <summary>
    /// Takes the next node.
    /// </summary>
    /// <returns></returns>
    public TokenNode? Take()
    {
        while (index < nodes.Count)
        {
            var node = nodes[index];

            // skip whitespace
            if (node.TokenKind == TokenKind.Whitespace)
            {
                index++;
                continue;
            }

            return node;
        }

        return null;
    }

    /// <summary>
    /// Take an element of the expected kind, or throw.
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public TokenNode TakeExpect(TokenKind kind)
    {
        while (index < nodes.Count)
        {
            var node = nodes[index];

            // skip whitespace
            if (node.TokenKind == TokenKind.Whitespace)
            {
                index++;
                continue;
            }

            if (node.TokenKind == kind)
            {
                index++;
                return node;
            }
            else
            {
                throw new InvalidOperationException($"Expected {kind}, Got: {node.TokenKind}");
            }
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Take any elements of the kinds provided (or whitespace).
    /// </summary>
    /// <param name="kindsToTake"></param>
    /// <returns></returns>
    public IEnumerable<TokenNode> TakeWhile(params TokenKind[] kindsToTake)
    {
        while (index < nodes.Count)
        {
            var node = nodes[index];

            // skip whitespace
            if (node.TokenKind == TokenKind.Whitespace)
            {
                index++;
                continue;
            }

            if (kindsToTake.Contains(node.TokenKind))
            {
                yield return node;
                index++;
            }
            else
            {
                yield break;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
