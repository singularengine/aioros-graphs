namespace Aioros.Graphs.Hierarchies;

internal static class HierarchyBuilder
{
    private const int Ambiguous = -2;

    public static Hierarchy FromNested<T>(T root, Func<T, IReadOnlyList<T>?> children, out T[] data)
    {
        ArgumentNullException.ThrowIfNull(children);
        var items = new List<T>();
        var parents = new List<int>();
        var stack = new Stack<(T Item, int Parent)>();
        stack.Push((root, -1));
        while (stack.Count > 0)
        {
            var (item, parent) = stack.Pop();
            var index = items.Count;
            items.Add(item);
            parents.Add(parent);
            var list = children(item);
            for (var i = (list?.Count ?? 0) - 1; i >= 0; i--)
            {
                stack.Push((list![i], index));
            }
        }

        data = [.. items];
        return Build(0, [.. parents], null, data.Length);
    }

    public static Hierarchy FromParents(ReadOnlySpan<int> parents)
    {
        var copy = new int[parents.Length];
        var root = -1;
        for (var i = 0; i < parents.Length; i++)
        {
            var parent = parents[i];
            if (parent >= 0)
            {
                if (parent >= parents.Length)
                {
                    throw new ArgumentException("missing: " + parent, nameof(parents));
                }

                copy[i] = parent;
                continue;
            }

            root = Root(root, i, nameof(parents));
            copy[i] = -1;
        }

        return Build(NoRoot(root, nameof(parents)), copy, null, copy.Length);
    }

    public static Hierarchy Stratify(ReadOnlySpan<string?> ids, ReadOnlySpan<string?> parentIds)
    {
        if (ids.Length != parentIds.Length)
        {
            throw new ArgumentException("There must be one parent id per id.", nameof(parentIds));
        }

        var nodeIds = new string?[ids.Length];
        var (parents, root) = Link(ids, parentIds, nodeIds);
        return Build(root, parents, nodeIds, ids.Length);
    }

    public static Hierarchy StratifyPaths(ReadOnlySpan<string> paths)
    {
        var ids = new List<string>(paths.Length);
        foreach (var path in paths)
        {
            ids.Add(Normalize(path));
        }

        var parentIds = new List<string>(ids.Count);
        foreach (var id in ids)
        {
            parentIds.Add(ParentOf(id));
        }

        var seen = new HashSet<string>(ids, StringComparer.Ordinal) { "" };
        for (var k = 0; k < parentIds.Count; k++)
        {
            var id = parentIds[k];
            if (seen.Add(id))
            {
                ids.Add(id);
                parentIds.Add(ParentOf(id));
            }
        }

        var nodeIds = new string?[ids.Count];
        var (parents, root) = Link(ids.ToArray(), parentIds.ToArray(), nodeIds);
        return Trim(parents, root, nodeIds, paths.Length);
    }

    private static (int[] Parents, int Root) Link(ReadOnlySpan<string?> ids, ReadOnlySpan<string?> parentIds, string?[] nodeIds)
    {
        var byKey = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (!string.IsNullOrEmpty(id))
            {
                nodeIds[i] = id;
                byKey[id] = byKey.ContainsKey(id) ? Ambiguous : i;
            }
        }

        var parents = new int[ids.Length];
        var root = -1;
        for (var i = 0; i < ids.Length; i++)
        {
            var parentId = parentIds[i];
            if (string.IsNullOrEmpty(parentId))
            {
                root = Root(root, i, nameof(parentIds));
                parents[i] = -1;
                continue;
            }

            if (!byKey.TryGetValue(parentId, out var parent))
            {
                throw new ArgumentException("missing: " + parentId, nameof(parentIds));
            }

            parents[i] = parent != Ambiguous ? parent : throw new ArgumentException("ambiguous: " + parentId, nameof(parentIds));
        }

        return (parents, NoRoot(root, nameof(parentIds)));
    }

    private static int Root(int root, int node, string parameter) =>
        root < 0 ? node : throw new ArgumentException("multiple roots", parameter);

    private static int NoRoot(int root, string parameter) =>
        root >= 0 ? root : throw new ArgumentException("no root", parameter);

    private static Hierarchy Trim(int[] parents, int root, string?[] ids, int rows)
    {
        var n = parents.Length;
        var childCount = new int[n];
        var onlyChild = new int[n];
        for (var i = 0; i < n; i++)
        {
            if (parents[i] >= 0)
            {
                childCount[parents[i]]++;
                onlyChild[parents[i]] = i;
            }
        }

        var removed = new bool[n];
        while (root >= rows && childCount[root] == 1)
        {
            removed[root] = true;
            root = onlyChild[root];
        }

        var index = new int[n];
        var next = 0;
        for (var i = 0; i < n; i++)
        {
            index[i] = removed[i] ? -1 : next++;
        }

        var compactParents = new int[next];
        var compactIds = new string?[next];
        for (var i = 0; i < n; i++)
        {
            if (!removed[i])
            {
                compactParents[index[i]] = i == root ? -1 : index[parents[i]];
                compactIds[index[i]] = ids[i];
            }
        }

        return Build(index[root], compactParents, compactIds, rows);
    }

    private static Hierarchy Build(int root, int[] parents, string?[]? ids, int rows)
    {
        var n = parents.Length;
        var childStart = new int[n + 1];
        for (var i = 0; i < n; i++)
        {
            if (parents[i] >= 0)
            {
                childStart[parents[i] + 1]++;
            }
        }

        for (var i = 0; i < n; i++)
        {
            childStart[i + 1] += childStart[i];
        }

        var childList = new int[childStart[n]];
        var cursor = childStart[..n];
        for (var i = 0; i < n; i++)
        {
            if (parents[i] >= 0)
            {
                childList[cursor[parents[i]]++] = i;
            }
        }

        var depths = new int[n];
        var heights = new int[n];
        var order = new int[n];
        order[0] = root;
        var end = 1;
        for (var k = 0; k < end; k++)
        {
            var node = order[k];
            for (var c = childStart[node]; c < childStart[node + 1]; c++)
            {
                depths[childList[c]] = depths[node] + 1;
                order[end++] = childList[c];
            }
        }

        if (end < n)
        {
            throw new ArgumentException("cycle", nameof(parents));
        }

        for (var k = n - 1; k > 0; k--)
        {
            var node = order[k];
            heights[parents[node]] = Math.Max(heights[parents[node]], heights[node] + 1);
        }

        var values = new double[n];
        values.AsSpan().Fill(double.NaN);
        return new Hierarchy(root, parents, depths, heights, childStart, childList, values, ids, rows);
    }

    private static string Normalize(string path)
    {
        var i = path.Length;
        if (Slash(path, i - 1) && !Slash(path, i - 2))
        {
            path = path[..^1];
        }

        return path.Length > 0 && path[0] == '/' ? path : "/" + path;
    }

    private static string ParentOf(string path)
    {
        var i = path.Length;
        if (i < 2)
        {
            return "";
        }

        while (--i > 1)
        {
            if (Slash(path, i))
            {
                break;
            }
        }

        return path[..i];
    }

    private static bool Slash(string path, int i)
    {
        if (i < 0 || i >= path.Length || path[i] != '/')
        {
            return false;
        }

        var k = 0;
        while (i > 0 && path[--i] == '\\')
        {
            k++;
        }

        return (k & 1) == 0;
    }
}
