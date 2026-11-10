using AdvancedSharpAdbClient.Models;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;

namespace MacroYoutubeReport.Driver.Android.Adb;

/// <summary>
/// Đại diện cho một phần tử giao diện được trích xuất từ cây XML của UIAutomator.
/// </summary>
public sealed record AdbUiElement
{
    public string ClassName { get; init; } = string.Empty;
    public string Package { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string ContentDesc { get; init; } = string.Empty;
    public string ResourceId { get; init; } = string.Empty;
    public Rectangle Bounds { get; init; }
    public Point CenterPoint => new(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);
    public bool IsClickable { get; init; }
    public bool IsSelected { get; init; }
    public bool IsChecked { get; init; }

    /// <summary>
    /// Điểm click có thêm jitter ngẫu nhiên để tránh bot detection.
    /// </summary>
    public Point GetJitterCenter(int maxJitterPx = 8)
    {
        var rnd = Random.Shared;
        int jx = rnd.Next(-maxJitterPx, maxJitterPx + 1);
        int jy = rnd.Next(-maxJitterPx, maxJitterPx + 1);
        return new Point(CenterPoint.X + jx, CenterPoint.Y + jy);
    }
}

/// <summary>
/// Bóc tách giao diện Android qua uiautomator dump và tìm kiếm phần tử bằng XPath.
/// </summary>
public sealed class AdbUiXmlDumper
{
    private static readonly Lazy<AdbUiXmlDumper> _instance = new(() => new AdbUiXmlDumper());
    public static AdbUiXmlDumper Instance => _instance.Value;

    public async Task<XmlDocument?> DumpUiXmlDocumentAsync(DeviceData device, CancellationToken ct = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            string output = await AdbClientGeneral.Instance.ExecuteShellAsync(
                device, 
                "uiautomator dump /sdcard/window_dump.xml && cat /sdcard/window_dump.xml", 
                linkedCts.Token).ConfigureAwait(false);

            string xmlPayload = ExtractXml(output);

            if (string.IsNullOrWhiteSpace(xmlPayload))
            {
                if (output.Contains("could not get idle state", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await AdbClientGeneral.Instance.SendKeyEventAsync(device, 127, ct).ConfigureAwait(false); // KEYCODE_MEDIA_PAUSE
                        await Task.Delay(200, ct).ConfigureAwait(false);
                        output = await AdbClientGeneral.Instance.ExecuteShellAsync(
                            device, 
                            "uiautomator dump /sdcard/window_dump.xml && cat /sdcard/window_dump.xml", 
                            linkedCts.Token).ConfigureAwait(false);
                        xmlPayload = ExtractXml(output);
                    }
                    finally
                    {
                        await AdbClientGeneral.Instance.SendKeyEventAsync(device, 126, ct).ConfigureAwait(false); // KEYCODE_MEDIA_PLAY
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(xmlPayload))
            {
                output = await AdbClientGeneral.Instance.ExecuteShellAsync(
                    device, 
                    "cat /sdcard/window_dump.xml", 
                    linkedCts.Token).ConfigureAwait(false);
                xmlPayload = ExtractXml(output);
            }

            if (string.IsNullOrWhiteSpace(xmlPayload))
            {
                return null;
            }

            var doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(xmlPayload);
            return doc;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdbUiElement?> FindElementAsync(DeviceData device, string xpath, CancellationToken ct = default)
    {
        var doc = await DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return null;

        var node = doc.SelectSingleNode(xpath);
        return node == null ? null : ParseElement(node);
    }

    public async Task<IReadOnlyList<AdbUiElement>> FindElementsAsync(DeviceData device, string xpath, CancellationToken ct = default)
    {
        var doc = await DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return Array.Empty<AdbUiElement>();

        var nodes = doc.SelectNodes(xpath);
        if (nodes == null || nodes.Count == 0) return Array.Empty<AdbUiElement>();

        var list = new List<AdbUiElement>(nodes.Count);
        foreach (XmlNode node in nodes)
        {
            var elem = ParseElement(node);
            if (elem != null) list.Add(elem);
        }

        return list;
    }

    public async Task<bool> TapXPathAsync(DeviceData device, string xpath, CancellationToken ct = default, bool withJitter = true)
    {
        var elem = await FindElementAsync(device, xpath, ct).ConfigureAwait(false);
        if (elem == null) return false;

        var pt = withJitter ? elem.GetJitterCenter() : elem.CenterPoint;
        return await AdbClientGeneral.Instance.TapAsync(device, pt.X, pt.Y, ct).ConfigureAwait(false);
    }

    public async Task<bool> TapElementAsync(DeviceData device, AdbUiElement element, CancellationToken ct = default, bool withJitter = true)
    {
        ArgumentNullException.ThrowIfNull(element);
        var pt = withJitter ? element.GetJitterCenter() : element.CenterPoint;
        return await AdbClientGeneral.Instance.TapAsync(device, pt.X, pt.Y, ct).ConfigureAwait(false);
    }

    private static string ExtractXml(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput)) return string.Empty;

        int startIndex = rawOutput.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0) startIndex = rawOutput.IndexOf("<hierarchy", StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0) return string.Empty;

        int endIndex = rawOutput.LastIndexOf("</hierarchy>", StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0) return string.Empty;

        return rawOutput.Substring(startIndex, (endIndex + 12) - startIndex).Trim();
    }

    public static AdbUiElement? ParseElement(XmlNode node)
    {
        if (node.Attributes == null) return null;

        string boundsStr = node.Attributes["bounds"]?.Value ?? string.Empty;
        Rectangle bounds = ParseBounds(boundsStr);

        return new AdbUiElement
        {
            ClassName = node.Attributes["class"]?.Value ?? string.Empty,
            Package = node.Attributes["package"]?.Value ?? string.Empty,
            Text = node.Attributes["text"]?.Value ?? string.Empty,
            ContentDesc = node.Attributes["content-desc"]?.Value ?? string.Empty,
            ResourceId = node.Attributes["resource-id"]?.Value ?? string.Empty,
            Bounds = bounds,
            IsClickable = string.Equals(node.Attributes["clickable"]?.Value, "true", StringComparison.OrdinalIgnoreCase),
            IsSelected = string.Equals(node.Attributes["selected"]?.Value, "true", StringComparison.OrdinalIgnoreCase),
            IsChecked = string.Equals(node.Attributes["checked"]?.Value, "true", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static Rectangle ParseBounds(string boundsStr)
    {
        var match = Regex.Match(boundsStr, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
        if (match.Success)
        {
            int x1 = int.Parse(match.Groups[1].Value);
            int y1 = int.Parse(match.Groups[2].Value);
            int x2 = int.Parse(match.Groups[3].Value);
            int y2 = int.Parse(match.Groups[4].Value);
            return new Rectangle(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));
        }

        return Rectangle.Empty;
    }
}
