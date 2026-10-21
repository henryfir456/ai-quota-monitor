using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AiQuotaMonitor.Core;
using AiQuotaMonitor.App.Services;

namespace AiQuotaMonitor.App.ViewModels;

public sealed class ProviderCardViewModel : INotifyPropertyChanged
{
    private static readonly TimeZoneInfo TaipeiTimeZone = FindTaipeiTimeZone();
    private readonly Dictionary<WindowId, QuotaWindowViewModel> _windowsById;
    private string _sourceText;
    private string _statusText = "資料待確認";
    private string _lastUpdatedText = "最後成功：N/A";
    private string _diagnosticText = "原因：尚未收到額度來源結果。建議：等待首次唯讀刷新完成。";

    public ProviderCardViewModel(ProviderId providerId)
    {
        ProviderId = providerId;
        Name = providerId switch
        {
            ProviderId.Claude => "Claude Code",
            ProviderId.Codex => "Codex",
            _ => providerId.ToString()
        };
        var now = DateTimeOffset.UtcNow;
        var errorCode = providerId == ProviderId.Claude ? "statusline_not_configured" : "provider_not_checked";
        Windows = new ObservableCollection<QuotaWindowViewModel>
        {
            new(new QuotaObservation(providerId, WindowId.FiveHour, null, null, now, SourceName(providerId), QuotaStatus.Unknown, errorCode)),
            new(new QuotaObservation(providerId, WindowId.Weekly, null, null, now, SourceName(providerId), QuotaStatus.Unknown, errorCode))
        };
        _windowsById = Windows.ToDictionary(window => window.WindowId);
        _sourceText = $"資料來源：{SourceName(providerId)}";
        DiagnosticText = string.Join(Environment.NewLine, Windows
            .Select(window => $"{window.Label}：{window.DiagnosticText}"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProviderId ProviderId { get; }

    public string Name { get; }

    public bool IsClaude => ProviderId == ProviderId.Claude;

    public ObservableCollection<QuotaWindowViewModel> Windows { get; }

    public string SourceText
    {
        get => _sourceText;
        private set => SetField(ref _sourceText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string LastSuccessfulUpdateText
    {
        get => _lastUpdatedText;
        private set => SetField(ref _lastUpdatedText, value);
    }

    public string DiagnosticText
    {
        get => _diagnosticText;
        private set => SetField(ref _diagnosticText, value);
    }

    public void Apply(ProviderSnapshot snapshot, bool showUsedPercent)
    {
        if (snapshot.ProviderId != ProviderId)
        {
            return;
        }

        foreach (var observation in snapshot.Windows)
        {
            if (_windowsById.TryGetValue(observation.WindowId, out var window))
            {
                window.Update(observation);
                window.ShowUsedPercent = showUsedPercent;
            }
        }

        SourceText = $"資料來源：{SourceName(ProviderId)}";
        var lastUpdated = Windows.Select(window => window.LastSuccessfulUpdateAtUtc)
            .Where(updated => updated is not null)
            .Max();
        LastSuccessfulUpdateText = lastUpdated is DateTimeOffset observed
            ? $"最後成功：{TimeZoneInfo.ConvertTime(observed, TaipeiTimeZone):yyyy/MM/dd HH:mm:ss} Asia/Taipei"
            : "最後成功：N/A";
        DiagnosticText = string.Join(Environment.NewLine, Windows
            .Select(window => $"{window.Label}：{window.DiagnosticText}")
            .Distinct(StringComparer.Ordinal));
        var freshWindows = snapshot.Windows.Count(window =>
            window.Status == QuotaStatus.Fresh && window.RemainingPercent is not null);
        StatusText = freshWindows == _windowsById.Count
            ? "資料最新"
            : freshWindows > 0
                ? "部分額度可用"
                : Windows.Select(window => window.StatusText).Distinct(StringComparer.Ordinal).Take(2).Count() == 1
                    ? Windows[0].StatusText
                    : "部分來源異常";
    }

    private static string SourceName(ProviderId providerId) => providerId switch
    {
        ProviderId.Claude => "Claude Code 官方 statusline JSON（本機命令）",
        ProviderId.Codex => "Codex 官方 App Server account/rateLimits/read",
        _ => "未知來源"
    };

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

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
