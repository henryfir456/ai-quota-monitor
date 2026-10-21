using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.App.ViewModels;

public sealed class ProviderCardViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<WindowId, QuotaWindowViewModel> _windowsById;
    private string _sourceText = "來源：未取得";
    private string _statusText = "UNKNOWN";
    private string _lastUpdatedText = "最後更新：N/A";

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
        Windows = new ObservableCollection<QuotaWindowViewModel>
        {
            new(new QuotaObservation(providerId, WindowId.FiveHour, null, null, now, "未取得", QuotaStatus.Unknown)),
            new(new QuotaObservation(providerId, WindowId.Weekly, null, null, now, "未取得", QuotaStatus.Unknown))
        };
        _windowsById = Windows.ToDictionary(window => window.WindowId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProviderId ProviderId { get; }

    public string Name { get; }

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

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        private set => SetField(ref _lastUpdatedText, value);
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

        var first = snapshot.Windows.FirstOrDefault();
        SourceText = first is null ? "來源：未取得" : $"來源：{first.Source}";
        var lastUpdated = snapshot.Windows.MaxBy(window => window.ObservedAtUtc)?.ObservedAtUtc;
        LastUpdatedText = lastUpdated is DateTimeOffset observed
            ? $"最後更新：{observed.ToLocalTime():yyyy/MM/dd HH:mm:ss}"
            : "最後更新：N/A";
        StatusText = snapshot.Windows.Any(window => window.Status == QuotaStatus.Error)
            ? "資料失敗"
            : snapshot.Windows.Any(window => window.Status == QuotaStatus.Stale)
                ? "資料已過期"
            : snapshot.Windows.Any(window => window.Status == QuotaStatus.Fresh)
                ? "資料最新"
                : snapshot.Windows.Any(window => window.Status == QuotaStatus.Unsupported)
                    ? "未支援"
                    : "UNKNOWN";
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
