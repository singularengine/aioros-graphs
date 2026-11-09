using System.Buffers;

namespace Aioros.Graphs.Chords;

/// <summary>
/// The chord layout of d3-chord (<c>chord</c>, <c>chordDirected</c> and <c>chordTranspose</c>): the groups and chords
/// of a square matrix of flows, as angles around a circle.
/// </summary>
public sealed class ChordLayout
{
    private const double Tau = 2 * Math.PI;
    private const byte HasSource = 1;
    private const byte HasTarget = 2;

    private ChordLayout(Chord[] chords, ChordGroup[] groups)
    {
        Chords = chords;
        Groups = groups;
    }

    /// <summary>The chords in d3's order: by matrix position, or as <see cref="ChordOptions.SortChords"/> orders them.</summary>
    public Chord[] Chords { get; }

    /// <summary>The groups, one per matrix row, in row order.</summary>
    public ChordGroup[] Groups { get; }

    /// <summary>Lays out the <paramref name="n"/> × <paramref name="n"/> <paramref name="matrix"/>.</summary>
    /// <param name="matrix">The flows, row by row: entry <c>i * n + j</c> flows from group i to group j.</param>
    /// <param name="n">The number of groups.</param>
    /// <param name="options">The settings.</param>
    /// <returns>The groups and the non-empty chords.</returns>
    /// <exception cref="ArgumentException">The matrix does not hold <paramref name="n"/> × <paramref name="n"/> values.</exception>
    public static ChordLayout Compute(ReadOnlySpan<double> matrix, int n, ChordOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (n < 0 || matrix.Length != n * n)
        {
            throw new ArgumentException("The matrix must hold n × n values.", nameof(matrix));
        }

        var work = new Work(n);
        try
        {
            work.Load(matrix, options.Transpose);
            var groups = work.Lay(options);
            return new ChordLayout(work.Collect(options.SortChords), groups);
        }
        finally
        {
            work.Return();
        }
    }

    private readonly struct Work
    {
        private readonly int n;
        private readonly double[] m;
        private readonly double[] sums;
        private readonly int[] groupIndex;
        private readonly int[] sub;
        private readonly double[] keys;
        private readonly int[] order;
        private readonly Chord[] slots;
        private readonly byte[] state;

        public Work(int n)
        {
            this.n = n;
            var size = Math.Max(1, n * n);
            var width = Math.Max(1, 2 * n);
            m = ArrayPool<double>.Shared.Rent(size);
            sums = ArrayPool<double>.Shared.Rent(Math.Max(1, n));
            groupIndex = ArrayPool<int>.Shared.Rent(Math.Max(1, n));
            sub = ArrayPool<int>.Shared.Rent(width);
            keys = ArrayPool<double>.Shared.Rent(width);
            order = ArrayPool<int>.Shared.Rent(width);
            slots = ArrayPool<Chord>.Shared.Rent(size);
            state = ArrayPool<byte>.Shared.Rent(size);
            Array.Clear(state, 0, n * n);
            Array.Clear(slots, 0, n * n);
        }

        public void Load(ReadOnlySpan<double> matrix, bool transpose)
        {
            for (var i = 0; i < matrix.Length; i++)
            {
                m[i] = transpose ? matrix[(i % n) * n + i / n] : matrix[i];
            }
        }

        public ChordGroup[] Lay(ChordOptions options)
        {
            var directed = options.Directed ? 1.0 : 0.0;
            var k = 0.0;
            for (var i = 0; i < n; i++)
            {
                var x = 0.0;
                for (var j = 0; j < n; j++)
                {
                    x += m[i * n + j] + directed * m[j * n + i];
                }

                sums[i] = x;
                k += x;
            }

            var pad = Math.Max(0, options.PadAngle);
            k = Math.Max(0, Tau - pad * n) / k;
            var dx = Truthy(k) ? pad : Tau / n;
            var groups = new ChordGroup[n];
            var index = groupIndex.AsSpan(0, n);
            for (var i = 0; i < n; i++)
            {
                index[i] = i;
            }

            ChordOrder.Sort(index, sums, options.SortGroups);
            var angle = 0.0;
            foreach (var i in index)
            {
                var x0 = angle;
                angle = options.Directed ? Directed(i, k, angle, options.SortSubgroups) : Undirected(i, k, angle, options.SortSubgroups);
                groups[i] = new ChordGroup(i, x0, angle, sums[i]);
                angle += dx;
            }

            return groups;
        }

        public Chord[] Collect(ChordSort sort)
        {
            var size = n * n;
            var count = 0;
            for (var i = 0; i < size; i++)
            {
                count += state[i] != 0 ? 1 : 0;
            }

            var chords = new Chord[count];
            count = 0;
            for (var i = 0; i < size; i++)
            {
                if (state[i] != 0)
                {
                    chords[count++] = slots[i];
                }
            }

            return sort == ChordSort.None ? chords : Sorted(chords, sort);
        }

        public void Return()
        {
            ArrayPool<double>.Shared.Return(m);
            ArrayPool<double>.Shared.Return(sums);
            ArrayPool<int>.Shared.Return(groupIndex);
            ArrayPool<int>.Shared.Return(sub);
            ArrayPool<double>.Shared.Return(keys);
            ArrayPool<int>.Shared.Return(order);
            ArrayPool<Chord>.Shared.Return(slots);
            ArrayPool<byte>.Shared.Return(state);
        }

        private static Chord[] Sorted(Chord[] chords, ChordSort sort)
        {
            var values = ArrayPool<double>.Shared.Rent(Math.Max(1, chords.Length));
            var index = ArrayPool<int>.Shared.Rent(Math.Max(1, chords.Length));
            try
            {
                for (var i = 0; i < chords.Length; i++)
                {
                    values[i] = chords[i].Source.Value + chords[i].Target.Value;
                    index[i] = i;
                }

                ChordOrder.Sort(index.AsSpan(0, chords.Length), values, sort);
                var sorted = new Chord[chords.Length];
                for (var i = 0; i < sorted.Length; i++)
                {
                    sorted[i] = chords[index[i]];
                }

                return sorted;
            }
            finally
            {
                ArrayPool<double>.Shared.Return(values);
                ArrayPool<int>.Shared.Return(index);
            }
        }

        private double Directed(int i, double k, double x, ChordSort sort)
        {
            var count = 0;
            for (var j = -n; j < n; j++)
            {
                var value = j < 0 ? m[~j * n + i] : m[i * n + j];
                if (Truthy(value))
                {
                    sub[count] = j;
                    keys[count] = j < 0 ? -value : value;
                    count++;
                }
            }

            foreach (var p in Order(count, sort))
            {
                var j = sub[p];
                var slot = j < 0 ? ~j * n + i : i * n + j;
                var value = m[slot];
                var start = x;
                x += value * k;
                var end = new ChordGroup(i, start, x, value);
                slots[slot] = j < 0 ? slots[slot] with { Target = end } : slots[slot] with { Source = end };
                state[slot] |= j < 0 ? HasTarget : HasSource;
            }

            return x;
        }

        private double Undirected(int i, double k, double x, ChordSort sort)
        {
            var count = 0;
            for (var j = 0; j < n; j++)
            {
                if (Truthy(m[i * n + j]) || Truthy(m[j * n + i]))
                {
                    sub[count] = j;
                    keys[count] = m[i * n + j];
                    count++;
                }
            }

            foreach (var p in Order(count, sort))
            {
                var j = sub[p];
                var value = m[i * n + j];
                var start = x;
                x += value * k;
                Place(i, j, new ChordGroup(i, start, x, value));
            }

            return x;
        }

        private void Place(int i, int j, ChordGroup end)
        {
            var slot = i < j ? i * n + j : j * n + i;
            var chord = slots[slot];
            if (i < j)
            {
                chord = chord with { Source = end };
                state[slot] |= HasSource;
            }
            else
            {
                chord = i == j ? new Chord(end, end) : chord with { Target = end };
                state[slot] |= i == j ? (byte)(HasSource | HasTarget) : HasTarget;
            }

            if (state[slot] == (HasSource | HasTarget) && chord.Source.Value < chord.Target.Value)
            {
                chord = new Chord(chord.Target, chord.Source);
            }

            slots[slot] = chord;
        }

        private static bool Truthy(double value) => value != 0 && !double.IsNaN(value);

        private ReadOnlySpan<int> Order(int count, ChordSort sort)
        {
            var index = order.AsSpan(0, count);
            for (var p = 0; p < count; p++)
            {
                index[p] = p;
            }

            ChordOrder.Sort(index, keys, sort);
            return index;
        }
    }
}
