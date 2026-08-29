namespace Twm.Core.Geometry;

/// <summary>
/// An integer rectangle in virtual-screen coordinates, addressed by its
/// top-left corner (<see cref="X" />, <see cref="Y" />) plus size.
/// </summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// The x-coordinate just past the right edge (exclusive).
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// The y-coordinate just past the bottom edge (exclusive).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>The integer midpoint of the rectangle.</summary>
    public Point Center => new(X + (Width / 2), Y + (Height / 2));

    /// <summary>
    /// Whether the point lies within the half-open bounds
    /// [<see cref="X" />, <see cref="Right" />) x [<see cref="Y" />,
    /// <see cref="Bottom" />).
    /// </summary>
    public bool Contains(Point point)
    {
        return point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;
    }

    /// <summary>
    /// Splits the rectangle into <paramref name="count" /> equal slices along
    /// <paramref name="direction" />. Any leftover pixels from integer division
    /// go to the last slice, so the slices always tile the rectangle exactly.
    /// </summary>
    public Rect[] SplitEvenly(TilingDirection direction, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        int total = direction == TilingDirection.Horizontal ? Width : Height;
        int each = total / count;

        int[] sizes = new int[count];
        int used = 0;
        for (int i = 0; i < count - 1; i++)
        {
            sizes[i] = each;
            used += each;
        }

        sizes[count - 1] = total - used;
        return SlicesFromSizes(direction, sizes);
    }

    /// <summary>
    /// Splits the rectange along <paramref name="direction" /> in proportion to
    /// <paramref name="weights" />. Each slice is floored to whoe pixels and
    /// the rounding remainder goes to the last slice, so the slices tile the
    /// rectangle exactly.
    /// </summary>
    public Rect[] Split(TilingDirection direction, IReadOnlyList<double> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0)
        {
            throw new ArgumentException("At least one weight is required.", nameof(weights));
        }

        double maxWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            double weight = weights[i];
            if (weight < 0 || double.IsNaN(weight) || double.IsInfinity(weight))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weights),
                    weight,
                    "Weights must be finite and non-negative."
                );
            }

            maxWeight = Math.Max(maxWeight, weight);
        }

        if (maxWeight <= 0)
        {
            throw new ArgumentException("Weights must sum to a positive value.", nameof(weights));
        }

        double sum = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            sum += weights[i] / maxWeight;
        }

        int total = direction == TilingDirection.Horizontal ? Width : Height;
        int[] sizes = new int[weights.Count];
        int used = 0;
        for (int i = 0; i < weights.Count - 1; i++)
        {
            int size = (int)(total * (weights[i] / maxWeight / sum));
            sizes[i] = size;
            used += size;
        }

        sizes[weights.Count - 1] = total - used;
        return SlicesFromSizes(direction, sizes);
    }

    private Rect[] SlicesFromSizes(TilingDirection direction, int[] sizes)
    {
        Rect[] slices = new Rect[sizes.Length];
        if (direction == TilingDirection.Horizontal)
        {
            int x = X;
            for (int i = 0; i < sizes.Length; i++)
            {
                slices[i] = new Rect(x, Y, sizes[i], Height);
                x += sizes[i];
            }
        }
        else
        {
            int y = Y;
            for (int i = 0; i < sizes.Length; i++)
            {
                slices[i] = new Rect(X, y, Width, sizes[i]);
                y += sizes[i];
            }
        }

        return slices;
    }
}
