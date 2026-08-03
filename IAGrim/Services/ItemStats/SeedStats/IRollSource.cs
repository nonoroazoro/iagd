namespace GrimDawnItemStats;

/// <summary>
/// Supplies random values to the item stat roll engine.
/// </summary>
public interface IRollSource
{
    int NextRange(int maximumInclusive);
    double NextUnit();
    void Consume();
}