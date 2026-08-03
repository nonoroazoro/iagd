namespace GrimDawnItemStats;

/// <summary>
/// Produces deterministic lower or upper roll boundaries.
/// </summary>
public sealed class BoundRandom : IRollSource
{
    private readonly bool _upper;

    public BoundRandom(bool upper)
    {
        _upper = upper;
    }

    public int NextRange(int maximumInclusive)
    {
        return _upper ? maximumInclusive : 0;
    }

    public double NextUnit()
    {
        double state = _upper ? 2147483646.0 : 1.0;
        return state * System.Math.Pow(2.0, -31);
    }

    public void Consume()
    {
    }
}