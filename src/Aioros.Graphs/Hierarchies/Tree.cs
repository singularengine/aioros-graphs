namespace Aioros.Graphs.Hierarchies;

/// <summary>
/// d3-hierarchy's <c>tree</c>: a tidy node-link diagram (Reingold–Tilford, with Buchheim, Jünger and Leipert's
/// linear-time algorithm), ported operation for operation.
/// </summary>
public static class Tree
{
    /// <summary>Lays out <paramref name="hierarchy"/> as d3's <c>tree()</c> with the given settings.</summary>
    /// <param name="hierarchy">The hierarchy, in its current child order.</param>
    /// <param name="options">The size and separation.</param>
    /// <returns>Each node's x (breadth) and y (depth).</returns>
    public static NodePoints Compute(Hierarchy hierarchy, NodeLayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(options);
        var walker = new Walker(hierarchy, options.Separation);
        var x = new double[hierarchy.Count];
        var y = new double[hierarchy.Count];
        using var post = new NodeOrder(hierarchy, NodeOrderKind.Post);
        foreach (var node in post.Nodes)
        {
            walker.FirstWalk(node);
        }

        walker.M[hierarchy.Count] = -walker.Z[hierarchy.Root];
        using var pre = new NodeOrder(hierarchy, NodeOrderKind.Pre);
        foreach (var node in pre.Nodes)
        {
            var parent = walker.Parent(node);
            x[node] = walker.Z[node] + walker.M[parent];
            walker.M[node] += walker.M[parent];
        }

        var depths = hierarchy.Depths;
        if (options.NodeSize)
        {
            foreach (var node in pre.Nodes)
            {
                x[node] *= options.Width;
                y[node] = depths[node] * options.Height;
            }

            return new NodePoints(x, y);
        }

        var left = hierarchy.Root;
        var right = left;
        var bottom = left;
        foreach (var node in pre.Nodes)
        {
            left = x[node] < x[left] ? node : left;
            right = x[node] > x[right] ? node : right;
            bottom = depths[node] > depths[bottom] ? node : bottom;
        }

        var s = left == right ? 1 : Separations.Of(hierarchy, options.Separation, left, right) / 2;
        var tx = s - x[left];
        var kx = options.Width / (x[right] + s + tx);
        var ky = options.Height / (depths[bottom] != 0 ? depths[bottom] : 1);
        foreach (var node in pre.Nodes)
        {
            x[node] = (x[node] + tx) * kx;
            y[node] = depths[node] * ky;
        }

        return new NodePoints(x, y);
    }

    private readonly struct Walker
    {
        private readonly Hierarchy hierarchy;
        private readonly NodeSeparation separation;
        private readonly int virtualRoot;
        private readonly double[] change;
        private readonly double[] shifts;
        private readonly int[] thread;
        private readonly int[] ancestor;
        private readonly int[] defaultAncestor;
        private readonly int[] number;

        public Walker(Hierarchy hierarchy, NodeSeparation separation)
        {
            this.hierarchy = hierarchy;
            this.separation = separation;
            var n = hierarchy.Count;
            virtualRoot = n;
            Z = new double[n + 1];
            M = new double[n + 1];
            change = new double[n + 1];
            shifts = new double[n + 1];
            thread = new int[n + 1];
            ancestor = new int[n + 1];
            defaultAncestor = new int[n + 1];
            number = new int[n + 1];
            thread.AsSpan().Fill(-1);
            defaultAncestor.AsSpan().Fill(-1);
            for (var i = 0; i <= n; i++)
            {
                ancestor[i] = i;
            }

            for (var node = 0; node < n; node++)
            {
                var children = hierarchy.Children(node);
                for (var i = 0; i < children.Length; i++)
                {
                    number[children[i]] = i;
                }
            }
        }

        public double[] Z { get; }

        public double[] M { get; }

        public int Parent(int node) => node == hierarchy.Root ? virtualRoot : hierarchy.Parents[node];

        public void FirstWalk(int v)
        {
            var parent = Parent(v);
            var first = Child(parent, 0);
            var w = number[v] != 0 ? Child(parent, number[v] - 1) : -1;
            var count = hierarchy.ChildCount(v);
            if (count > 0)
            {
                ExecuteShifts(v);
                var midpoint = (Z[Child(v, 0)] + Z[Child(v, count - 1)]) / 2;
                if (w >= 0)
                {
                    Z[v] = Z[w] + Separations.Of(hierarchy, separation, v, w);
                    M[v] = Z[v] - midpoint;
                }
                else
                {
                    Z[v] = midpoint;
                }
            }
            else if (w >= 0)
            {
                Z[v] = Z[w] + Separations.Of(hierarchy, separation, v, w);
            }

            defaultAncestor[parent] = Apportion(v, w, defaultAncestor[parent] >= 0 ? defaultAncestor[parent] : first);
        }

        private int Child(int node, int index) => node == virtualRoot ? hierarchy.Root : hierarchy.Children(node)[index];

        private int NextLeft(int v) => hierarchy.ChildCount(v) > 0 ? hierarchy.Children(v)[0] : thread[v];

        private int NextRight(int v) => hierarchy.ChildCount(v) is var count and > 0 ? hierarchy.Children(v)[count - 1] : thread[v];

        private void MoveSubtree(int wm, int wp, double shift)
        {
            var delta = shift / (number[wp] - number[wm]);
            change[wp] -= delta;
            shifts[wp] += shift;
            change[wm] += delta;
            Z[wp] += shift;
            M[wp] += shift;
        }

        private void ExecuteShifts(int v)
        {
            var shift = 0.0;
            var total = 0.0;
            var children = hierarchy.Children(v);
            for (var i = children.Length - 1; i >= 0; i--)
            {
                var w = children[i];
                Z[w] += shift;
                M[w] += shift;
                total += change[w];
                shift += shifts[w] + total;
            }
        }

        private int NextAncestor(int vim, int v, int fallback) => Parent(ancestor[vim]) == Parent(v) ? ancestor[vim] : fallback;

        private int Apportion(int v, int w, int fallback)
        {
            if (w < 0)
            {
                return fallback;
            }

            var vip = v;
            var vop = v;
            var vim = w;
            var vom = Child(Parent(vip), 0);
            var sip = M[vip];
            var sop = M[vop];
            var sim = M[vim];
            var som = M[vom];
            while (true)
            {
                vim = NextRight(vim);
                vip = NextLeft(vip);
                if (vim < 0 || vip < 0)
                {
                    break;
                }

                vom = NextLeft(vom);
                vop = NextRight(vop);
                ancestor[vop] = v;
                var shift = Z[vim] + sim - Z[vip] - sip + Separations.Of(hierarchy, separation, vim, vip);
                if (shift > 0)
                {
                    MoveSubtree(NextAncestor(vim, v, fallback), v, shift);
                    sip += shift;
                    sop += shift;
                }

                sim += M[vim];
                sip += M[vip];
                som += M[vom];
                sop += M[vop];
            }

            if (vim >= 0 && NextRight(vop) < 0)
            {
                thread[vop] = vim;
                M[vop] += sim - sop;
            }

            if (vip >= 0 && NextLeft(vom) < 0)
            {
                thread[vom] = vip;
                M[vom] += sip - som;
                fallback = v;
            }

            return fallback;
        }
    }
}
