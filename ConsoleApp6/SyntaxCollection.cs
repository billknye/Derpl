using System.Collections;


namespace ConsoleApp6;

public class SyntaxCollection : IEnumerable<SyntaxNode>
{
    private readonly IReadOnlyList<SyntaxNode> nodes;
    private int index;

    public SyntaxCollection(IReadOnlyList<SyntaxNode> nodes)
    {
        this.nodes = nodes;
        this.index = 0;
    }

    public IEnumerator<SyntaxNode> GetEnumerator() => nodes.Where(n => n.SyntaxKind != SyntaxKind.Whitespace).GetEnumerator();

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
                throw new InvalidOperationException();
            }
        }

        throw new InvalidOperationException();
    }

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
