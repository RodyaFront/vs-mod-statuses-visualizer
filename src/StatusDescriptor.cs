using Vintagestory.API.Common;

namespace PlayerStatusStrip;

public enum StatusAffectKind
{
    Neutral,
    Positive,
    Negative
}

public sealed class StatusDescriptor
{
    public StatusDescriptor(
        string stableId,
        AssetLocation icon,
        int sortOrder,
        string tooltipVtml,
        float? pulseMetric = null,
        StatusAffectKind affectKind = StatusAffectKind.Neutral)
    {
        StableId = stableId;
        Icon = icon;
        SortOrder = sortOrder;
        TooltipVtml = tooltipVtml;
        PulseMetric = pulseMetric;
        AffectKind = affectKind;
    }

    public string StableId { get; }

    public AssetLocation Icon { get; }

    public int SortOrder { get; }

    public string TooltipVtml { get; }

    public float? PulseMetric { get; }

    public StatusAffectKind AffectKind { get; }
}
