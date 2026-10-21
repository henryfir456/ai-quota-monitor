namespace AiQuotaMonitor.App.Services;

public enum ThemeMode
{
    Dark,
    Light
}

public sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 1;

    public int RefreshIntervalSeconds { get; init; } = 300;

    public ThemeMode Theme { get; init; } = ThemeMode.Dark;

    public bool ShowUsedPercent { get; init; }

    public bool LowAlertsEnabled { get; init; }

    public int AlertThresholdPercent { get; init; } = 20;

    public bool StartWithWindows { get; init; }

    public bool FloatingAlwaysOnTop { get; init; } = true;

    public double? FloatingLeft { get; init; }

    public double? FloatingTop { get; init; }

    public AppSettings Normalize() => this with
    {
        SchemaVersion = 1,
        RefreshIntervalSeconds = Math.Clamp(RefreshIntervalSeconds, 60, 1800),
        Theme = Enum.IsDefined(Theme) ? Theme : ThemeMode.Dark,
        AlertThresholdPercent = AlertThresholdPercent is 10 or 20 ? AlertThresholdPercent : 20,
        FloatingLeft = double.IsFinite(FloatingLeft ?? 0) && FloatingLeft is >= -10000 and <= 10000 ? FloatingLeft : null,
        FloatingTop = double.IsFinite(FloatingTop ?? 0) && FloatingTop is >= -10000 and <= 10000 ? FloatingTop : null
    };
}
