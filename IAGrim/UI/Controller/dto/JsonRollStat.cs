namespace IAGrim.UI.Controller.dto;

/// <summary>
/// Describes a translated stat with a real variable roll range.
/// </summary>
public sealed class JsonRollStat
{
    public string? Text { get; set; }
    public string? Minimum { get; set; }
    public string? Maximum { get; set; }
    public bool IsMaximum { get; set; }
}