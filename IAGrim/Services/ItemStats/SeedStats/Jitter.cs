namespace GrimDawnItemStats;

/// <summary>
/// Applies Grim Dawn item stat jitter.
/// </summary>
public static class Jitter
{
    /// <summary>
    /// Applies integer-uniform character stat jitter.
    /// </summary>
    public static double Char(double value, double jitterPercent, IRollSource rng)
    {
        if (value == 0.0 || jitterPercent == 0.0)
            return value;

        int spread = (int)(value * jitterPercent * 0.01);
        if (spread == 0)
            spread = 1;

        int roll = rng.NextRange(2 * spread);
        double rolled = roll - spread + value;
        if (Math.Abs(rolled) < 1.0)
            return value;
        return rolled;
    }

    /// <summary>
    /// Applies skill stat jitter while preserving its draw behavior.
    /// </summary>
    public static double Skill(double value, double jitterPercent, IRollSource rng)
    {
        if (value == 0.0)
            return value;

        int spread = (int)(value * jitterPercent * 0.01);
        if (spread == 0)
        {
            rng.Consume();
            return value;
        }

        int roll = rng.NextRange(2 * spread);
        double rolled = roll - spread + value;
        if (Math.Abs(rolled) < 1.0)
            return value;
        return rolled;
    }

    /// <summary>
    /// Applies the offensive scale using the game's float32 calculation.
    /// </summary>
    public static double ApplyScale(double jittered, double scalePercent)
    {
        float numerator = (float)((float)jittered * (float)(100.0 + scalePercent));
        return (int)(numerator / 100.0f);
    }

    /// <summary>
    /// Applies multiplicative conversion jitter.
    /// </summary>
    public static double Conversion(double value, double jitterPercent, IRollSource rng)
    {
        if (jitterPercent <= 0.0)
            return value;

        double jitter = jitterPercent * 0.01;
        double unit = rng.NextUnit();
        float factor = (float)(unit * (2.0 * jitter) + (1.0 - jitter));
        double rolled = value * factor;
        if (rolled < 0.0)
            return 0.0;
        if (rolled > 100.0)
            return 100.0;
        return rolled;
    }
}