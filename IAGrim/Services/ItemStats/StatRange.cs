namespace IAGrim.Services.ItemStats;

/// <summary>
/// Represents a real rollable stat boundary.
/// </summary>
public sealed class StatRange
{
    public StatRange(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public double Minimum { get; }
    public double Maximum { get; }
}