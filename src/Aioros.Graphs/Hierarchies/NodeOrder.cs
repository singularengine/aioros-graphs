using System.Buffers;

namespace Aioros.Graphs.Hierarchies;

internal enum NodeOrderKind
{
    Pre,
    Post,
}

internal readonly struct NodeOrder : IDisposable
{
    private readonly int[] buffer;
    private readonly int count;

    public NodeOrder(Hierarchy hierarchy, NodeOrderKind kind)
    {
        count = hierarchy.Count;
        buffer = ArrayPool<int>.Shared.Rent(count);
        if (kind == NodeOrderKind.Pre)
        {
            hierarchy.WritePreOrder(buffer.AsSpan(0, count));
        }
        else
        {
            hierarchy.WritePostOrder(buffer.AsSpan(0, count));
        }
    }

    public ReadOnlySpan<int> Nodes => buffer.AsSpan(0, count);

    public void Dispose() => ArrayPool<int>.Shared.Return(buffer);
}
