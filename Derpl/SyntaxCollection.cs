using System.Collections;


namespace Derpl;

public class SyntaxCollection : IEnumerable<SyntaxNode>
{
    private readonly IReadOnlyList<SyntaxNode> nodes;
    private int index;

    private Stack<int> indexStack;

    public SyntaxCollection(IReadOnlyList<SyntaxNode> nodes)
    {
        this.nodes = nodes;
        this.index = 0;

        indexStack = new Stack<int>();
    }

    public IEnumerator<SyntaxNode> GetEnumerator() => nodes.Where(n => n.SyntaxKind != SyntaxKind.Whitespace).GetEnumerator();

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
    public SyntaxNode? Peek()
    {
        var nextIndex = index;
        while (true)
        {
            if (nextIndex >= nodes.Count)
            {
                return null;
            }

            var next = nodes[nextIndex];

            if (next.SyntaxKind == SyntaxKind.Whitespace)
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
    public SyntaxNode? Take()
    {
        while (index < nodes.Count)
        {
            var node = nodes[index];

            // skip whitespace
            if (node.SyntaxKind == SyntaxKind.Whitespace)
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
    public SyntaxNode TakeExpect(SyntaxKind kind)
    {
        while (index < nodes.Count)
        {
            var node = nodes[index];

            // skip whitespace
            if (node.SyntaxKind == SyntaxKind.Whitespace)
            {
                index++;
                continue;
            }

            if (node.SyntaxKind == kind)
            {
                index++;
                return node;
            }
            else
            {
                throw new InvalidOperationException($"Expected {kind}, Got: {node.SyntaxKind}");
            }
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Take any elements of the kinds provided (or whitespace).
    /// </summary>
    /// <param name="kindsToTake"></param>
    /// <returns></returns>
    public IEnumerable<SyntaxNode> TakeWhile(params SyntaxKind[] kindsToTake)
    {
        while (index < nodes.Count)
        {
            var node = nodes[index];

            // skip whitespace
            if (node.SyntaxKind == SyntaxKind.Whitespace)
            {
                index++;
                continue;
            }

            if (kindsToTake.Contains(node.SyntaxKind))
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
