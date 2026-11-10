using System.Collections.Concurrent;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Bộ nhớ chia sẻ trạng thái xuyên suốt quá trình thực thi giữa các bước trong một kịch bản.
/// </summary>
public sealed class WorkflowContext
{
    private readonly ConcurrentDictionary<string, object?> _items = new(StringComparer.OrdinalIgnoreCase);

    public ReportTargetItem? CurrentTarget { get; set; }
    public ReportReasonConfig? CurrentReason { get; set; }
    public ReportScenarioConfig? CurrentScenario { get; set; }
    public DateTime SessionStartTime { get; } = DateTime.UtcNow;

    public void Set<T>(string key, T value) => _items[key] = value;

    public T? Get<T>(string key)
    {
        if (_items.TryGetValue(key, out var val) && val is T typedVal)
        {
            return typedVal;
        }
        return default;
    }

    public bool ContainsKey(string key) => _items.ContainsKey(key);
}
