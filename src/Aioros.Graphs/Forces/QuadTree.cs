namespace Aioros.Graphs.Forces;

internal interface IQuadVisitor
{
    bool Visit(QuadTree tree, int quad, double x0, double y0, double x1, double y1);
}

internal interface IQuadAccumulator
{
    void Accumulate(QuadTree tree, int quad);
}

internal sealed class QuadTree
{
    private int[] children = new int[64];
    private int[] data = new int[16];
    private int[] next = new int[16];
    private double[] value = new double[16];
    private double[] centerX = new double[16];
    private double[] centerY = new double[16];
    private int[] order = new int[16];
    private QuadFrame[] frames = new QuadFrame[16];
    private double[] pointX = [];
    private double[] pointY = [];
    private int count;
    private int root;
    private double x0;
    private double y0;
    private double x1;
    private double y1;

    public Span<double> Value => value;

    public Span<double> CenterX => centerX;

    public Span<double> CenterY => centerY;

    public bool IsInternal(int quad) => data[quad] < 0;

    public int Data(int quad) => data[quad];

    public int Next(int quad) => next[quad];

    public int Child(int quad, int i) => children[(quad << 2) | i];

    public void Build(double[] x, double[] y, int n)
    {
        count = 0;
        root = -1;
        x0 = y0 = x1 = y1 = double.NaN;
        pointX = x;
        pointY = y;
        Reserve(4 * n + 16);
        var (minX, minY, maxX, maxY) = Extent(x, y, n);
        if (minX > maxX || minY > maxY)
        {
            return;
        }

        Cover(minX, minY);
        Cover(maxX, maxY);
        for (var i = 0; i < n; i++)
        {
            Add(x[i], y[i], i);
        }
    }

    public void Visit<TVisitor>(ref TVisitor visitor)
        where TVisitor : struct, IQuadVisitor
    {
        if (root < 0)
        {
            return;
        }

        var top = 0;
        frames[top++] = new QuadFrame(root, x0, y0, x1, y1);
        while (top > 0)
        {
            var q = frames[--top];
            if (visitor.Visit(this, q.Quad, q.X0, q.Y0, q.X1, q.Y1) || data[q.Quad] >= 0)
            {
                continue;
            }

            var xm = (q.X0 + q.X1) / 2;
            var ym = (q.Y0 + q.Y1) / 2;
            var c = q.Quad << 2;
            EnsureFrames(top + 4);
            if (children[c + 3] >= 0)
            {
                frames[top++] = new QuadFrame(children[c + 3], xm, ym, q.X1, q.Y1);
            }

            if (children[c + 2] >= 0)
            {
                frames[top++] = new QuadFrame(children[c + 2], q.X0, ym, xm, q.Y1);
            }

            if (children[c + 1] >= 0)
            {
                frames[top++] = new QuadFrame(children[c + 1], xm, q.Y0, q.X1, ym);
            }

            if (children[c] >= 0)
            {
                frames[top++] = new QuadFrame(children[c], q.X0, q.Y0, xm, ym);
            }
        }
    }

    public void VisitAfter<TAccumulator>(ref TAccumulator accumulator)
        where TAccumulator : struct, IQuadAccumulator
    {
        if (root < 0)
        {
            return;
        }

        var stack = order.AsSpan(0, count);
        var after = order.AsSpan(count, count);
        var top = 0;
        var done = 0;
        stack[top++] = root;
        while (top > 0)
        {
            var quad = stack[--top];
            if (data[quad] < 0)
            {
                top = PushChildren(stack, top, quad);
            }

            after[done++] = quad;
        }

        while (done > 0)
        {
            accumulator.Accumulate(this, after[--done]);
        }
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) Extent(double[] x, double[] y, int n)
    {
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        for (var i = 0; i < n; i++)
        {
            var px = x[i];
            var py = y[i];
            if (!double.IsNaN(px) && !double.IsNaN(py))
            {
                minX = Lower(px, minX);
                maxX = Higher(px, maxX);
                minY = Lower(py, minY);
                maxY = Higher(py, maxY);
            }
        }

        return (minX, minY, maxX, maxY);
    }

    private static double Lower(double value, double current) => value < current ? value : current;

    private static double Higher(double value, double current) => value > current ? value : current;

    private int PushChildren(Span<int> stack, int top, int quad)
    {
        for (var i = 0; i < 4; i++)
        {
            var child = children[(quad << 2) | i];
            if (child >= 0)
            {
                stack[top++] = child;
            }
        }

        return top;
    }

    private void Cover(double x, double y)
    {
        if (double.IsNaN(x0))
        {
            x0 = Math.Floor(x);
            x1 = x0 + 1;
            y0 = Math.Floor(y);
            y1 = y0 + 1;
            return;
        }

        var z = x1 - x0;
        z = z == 0 || double.IsNaN(z) ? 1 : z;
        var node = root;
        var wrap = root >= 0 && data[root] < 0;
        while (x0 > x || x >= x1 || y0 > y || y >= y1)
        {
            var i = ((y < y0 ? 1 : 0) << 1) | (x < x0 ? 1 : 0);
            if (wrap)
            {
                var parent = NewInternal();
                children[(parent << 2) | i] = node;
                node = parent;
            }

            z *= 2;
            Grow(i, z);
        }

        if (wrap)
        {
            root = node;
        }
    }

    private void Grow(int i, double z)
    {
        switch (i)
        {
            case 0: x1 = x0 + z; y1 = y0 + z; break;
            case 1: x0 = x1 - z; y1 = y0 + z; break;
            case 2: x1 = x0 + z; y0 = y1 - z; break;
            default: x0 = x1 - z; y0 = y1 - z; break;
        }
    }

    private void Add(double x, double y, int d)
    {
        if (double.IsNaN(x) || double.IsNaN(y))
        {
            return;
        }

        var leaf = NewLeaf(d);
        if (root < 0)
        {
            root = leaf;
            return;
        }

        var cell = new Cell(x0, y0, x1, y1);
        var parent = -1;
        var i = 0;
        var node = root;
        while (data[node] < 0)
        {
            parent = node;
            i = cell.Halve(x, y);
            node = children[(parent << 2) | i];
            if (node < 0)
            {
                children[(parent << 2) | i] = leaf;
                return;
            }
        }

        var xp = pointX[data[node]];
        var yp = pointY[data[node]];
        if (x == xp && y == yp)
        {
            next[leaf] = node;
            Link(parent, i, leaf);
            return;
        }

        Split(ref cell, parent, i, node, leaf, x, y);
    }

    private void Split(ref Cell cell, int parent, int i, int node, int leaf, double x, double y)
    {
        var xp = pointX[data[node]];
        var yp = pointY[data[node]];
        int j;
        do
        {
            var split = NewInternal();
            Link(parent, i, split);
            parent = split;
            i = cell.Halve(x, y);
            j = ((yp >= cell.Ym ? 1 : 0) << 1) | (xp >= cell.Xm ? 1 : 0);
        }
        while (i == j);

        children[(parent << 2) | j] = node;
        children[(parent << 2) | i] = leaf;
    }

    private void Link(int parent, int i, int quad)
    {
        if (parent >= 0)
        {
            children[(parent << 2) | i] = quad;
        }
        else
        {
            root = quad;
        }
    }

    private int NewLeaf(int d)
    {
        var quad = NewQuad();
        data[quad] = d;
        return quad;
    }

    private int NewInternal()
    {
        var quad = NewQuad();
        data[quad] = -1;
        children.AsSpan(quad << 2, 4).Fill(-1);
        return quad;
    }

    private int NewQuad()
    {
        if (count == data.Length)
        {
            Reserve(count * 2);
        }

        next[count] = -1;
        return count++;
    }

    private void Reserve(int size)
    {
        if (size <= data.Length)
        {
            return;
        }

        Array.Resize(ref data, size);
        Array.Resize(ref next, size);
        Array.Resize(ref value, size);
        Array.Resize(ref centerX, size);
        Array.Resize(ref centerY, size);
        Array.Resize(ref children, size * 4);
        order = new int[size * 2];
        frames = frames.Length >= 256 ? frames : new QuadFrame[256];
    }

    private void EnsureFrames(int size)
    {
        if (size > frames.Length)
        {
            Array.Resize(ref frames, Math.Max(size, frames.Length * 2));
        }
    }

    private struct Cell(double x0, double y0, double x1, double y1)
    {
        private double x0 = x0;
        private double y0 = y0;
        private double x1 = x1;
        private double y1 = y1;

        public double Xm { get; private set; }

        public double Ym { get; private set; }

        public int Halve(double x, double y)
        {
            Xm = (x0 + x1) / 2;
            Ym = (y0 + y1) / 2;
            var right = x >= Xm;
            var bottom = y >= Ym;
            if (right)
            {
                x0 = Xm;
            }
            else
            {
                x1 = Xm;
            }

            if (bottom)
            {
                y0 = Ym;
            }
            else
            {
                y1 = Ym;
            }

            return ((bottom ? 1 : 0) << 1) | (right ? 1 : 0);
        }
    }

    private readonly record struct QuadFrame(int Quad, double X0, double Y0, double X1, double Y1);
}
