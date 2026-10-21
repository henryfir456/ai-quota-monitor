using System.ComponentModel;
using System.Runtime.CompilerServices;
using AiQuotaMonitor.Core;
using AiQuotaMonitor.App.Services;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

namespace AiQuotaMonitor.App.ViewModels;

public sealed class QuotaWindowViewModel : INotifyPropertyChanged
{
    private static readonly TimeZoneInfo TaipeiTimeZone = FindTaipeiTimeZone();
    private QuotaObservation _observation;
    private bool _showUsedPercent;
    private DateTimeOffset? _lastSuccessfulUpdateAtUtc;

    public QuotaWindowViewModel(QuotaObservation observation)
    {
        _observation = observation;
        if (observation.Status == QuotaStatus.Fresh && observation.RemainingPercent is not null)
        {
            _lastSuccessfulUpdateAtUtc = observation.ObservedAtUtc;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowId WindowId => _observation.WindowId;

    public string Label => _observation.WindowId switch
    {
        WindowId.FiveHour => "5 小時額度",
        WindowId.Weekly => "每週額度",
        _ => "其他額度"
    };

    public bool ShowUsedPercent
    {
        get => _showUsedPercent;
        set
        {
            if (_showUsedPercent == value)
            {
                return;
            }

            _showUsedPercent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PercentageText));
            OnPropertyChanged(nameof(ProgressValue));
        }
    }

    public string RemainingText => FormatPercent(_observation.RemainingPercent);

    public string PercentageText => ShowUsedPercent
        ? $"已用 {FormatPercent(_observation.UsedPercent)}"
        : $"剩餘 {RemainingText}";

    public double? ProgressValue => ShowUsedPercent ? _observation.UsedPercent : _observation.RemainingPercent;

    public MediaBrush ProgressBrush => _observation.Status == QuotaStatus.Fresh && _observation.RemainingPercent is double remaining
        ? remaining <= 10 ? MediaBrushes.IndianRed : remaining <= 20 ? MediaBrushes.DarkOrange : MediaBrushes.MediumSeaGreen
        : MediaBrushes.SlateGray;

    public string StatusText => ProviderDiagnosticFormatter.Describe(_observation).Status;

    public string DiagnosticText => ProviderDiagnosticFormatter.Describe(_observation).DiagnosticText;

    public string ResetText => _observation.ResetAtUtc is DateTimeOffset reset
        ? $"重置：{TimeZoneInfo.ConvertTime(reset, TaipeiTimeZone):yyyy/MM/dd HH:mm} Asia/Taipei"
        : "重置：N/A";

    public string CountdownText
    {
        get
        {
            if (_observation.ResetAtUtc is not DateTimeOffset reset)
            {
                return "倒數：N/A";
            }

            var remaining = reset - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return "倒數：重置時間已到";
            }

            var totalHours = (int)Math.Floor(remaining.TotalHours);
            return $"倒數：{totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public DateTimeOffset? LastSuccessfulUpdateAtUtc => _lastSuccessfulUpdateAtUtc;

    public string LastSuccessfulUpdateText => _lastSuccessfulUpdateAtUtc is DateTimeOffset updated
        ? $"最後成功：{TimeZoneInfo.ConvertTime(updated, TaipeiTimeZone):yyyy/MM/dd HH:mm:ss} Asia/Taipei"
        : "最後成功：N/A";

    public void Update(QuotaObservation observation)
    {
        _observation = observation;
        if (observation.Status == QuotaStatus.Fresh && observation.RemainingPercent is not null)
        {
            _lastSuccessfulUpdateAtUtc = observation.ObservedAtUtc;
        }

        OnPropertyChanged(string.Empty);
    }

    public void Tick() => OnPropertyChanged(nameof(CountdownText));

    private static string FormatPercent(double? value) => value is double percent ? $"{percent:0.#}%" : "N/A";

    private static TimeZoneInfo FindTaipeiTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
