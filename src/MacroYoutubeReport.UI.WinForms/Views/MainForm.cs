using System.ComponentModel;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Android.Adb;
using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Proxy;

namespace MacroYoutubeReport.UI.WinForms.Views;

public partial class MainForm : Form, IMainView
{
    private readonly List<ReportTargetItem> _targets = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PlatformType SelectedPlatform => rdoAndroid.Checked ? PlatformType.Android : PlatformType.Chrome;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ConcurrencyLimit
    {
        get => (int)numConcurrency.Value;
        set => numConcurrency.Value = Math.Clamp(value, numConcurrency.Minimum, numConcurrency.Maximum);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsRunning => btnStop.Enabled;

    public event Func<Task>? ViewLoaded;
    public event Func<Task>? ScanDevicesRequested;
    public event Func<Task>? StartBatchRequested;
    public event Action? StopBatchRequested;
    public event Func<Task>? RefreshHistoryRequested;
    public event Action<string>? ExportHistoryCsvRequested;
#pragma warning disable CS0067
    public event Func<Task>? AddAccountRequested;
    public event Func<Task>? AddProxyRequested;
#pragma warning restore CS0067

    public MainForm()
    {
        InitializeComponent();
        WireEvents();
    }

    private void WireEvents()
    {
        this.Load += async (_, _) =>
        {
            if (ViewLoaded != null) await ViewLoaded.Invoke();
        };

        this.btnStart.Click += async (_, _) =>
        {
            if (StartBatchRequested != null) await StartBatchRequested.Invoke();
        };

        this.btnStop.Click += (_, _) =>
        {
            StopBatchRequested?.Invoke();
        };

        this.btnRefreshDevices.Click += async (_, _) =>
        {
            if (ScanDevicesRequested != null) await ScanDevicesRequested.Invoke();
        };

        this.btnRefreshHistory.Click += async (_, _) =>
        {
            if (RefreshHistoryRequested != null) await RefreshHistoryRequested.Invoke();
        };

        this.btnExportCsv.Click += (_, _) =>
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Report_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ExportHistoryCsvRequested?.Invoke(sfd.FileName);
            }
        };

        this.btnAddTarget.Click += (_, _) =>
        {
            var targetType = cmbTargetType.SelectedIndex switch
            {
                1 => ReportTargetType.Channel,
                2 => ReportTargetType.Shorts,
                _ => ReportTargetType.Video
            };

            var item = new ReportTargetItem
            {
                TargetType = targetType,
                Keyword = txtKeyword.Text.Trim(),
                VideoTitle = string.IsNullOrWhiteSpace(txtTitle.Text) ? null : txtTitle.Text.Trim(),
                ChannelName = string.IsNullOrWhiteSpace(txtChannel.Text) ? null : txtChannel.Text.Trim(),
                TargetUrl = string.IsNullOrWhiteSpace(txtUrl.Text) ? null : txtUrl.Text.Trim(),
                MaxReportsRequired = 1
            };

            if (string.IsNullOrWhiteSpace(item.Keyword) && string.IsNullOrWhiteSpace(item.TargetUrl) && string.IsNullOrWhiteSpace(item.VideoTitle))
            {
                ShowNotification("Vui lòng nhập Từ khóa, Tiêu đề hoặc URL mục tiêu.", isError: true);
                return;
            }

            AddTargetItem(item);
            txtKeyword.Clear();
            txtTitle.Clear();
            txtChannel.Clear();
            txtUrl.Clear();
        };

        this.btnRemoveTarget.Click += (_, _) => RemoveSelectedTarget();
        this.btnClearTargets.Click += (_, _) => ClearAllTargets();

        this.btnImportTargets.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var lines = File.ReadAllLines(ofd.FileName);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    var item = new ReportTargetItem
                    {
                        Keyword = parts[0].Trim(),
                        VideoTitle = parts.Length > 1 ? parts[1].Trim() : null,
                        ChannelName = parts.Length > 2 ? parts[2].Trim() : null,
                        TargetUrl = parts.Length > 3 ? parts[3].Trim() : (parts[0].StartsWith("http") ? parts[0].Trim() : null)
                    };
                    AddTargetItem(item);
                }
            }
        };

        this.btnAddProfile.Click += (_, _) =>
        {
            string name = txtNewProfile.Text.Trim();
            if (!string.IsNullOrWhiteSpace(name) && !lstChromeProfiles.Items.Contains(name))
            {
                lstChromeProfiles.Items.Add(name);
                txtNewProfile.Clear();
            }
        };
    }

    public IReadOnlyList<ReportTargetItem> GetTargetItems()
    {
        lock (_targets)
        {
            return _targets.ToList();
        }
    }

    public void SetTargetItems(IReadOnlyList<ReportTargetItem> targets)
    {
        lock (_targets)
        {
            _targets.Clear();
            _targets.AddRange(targets);
        }
        RefreshTargetsGrid();
    }

    public void AddTargetItem(ReportTargetItem target)
    {
        lock (_targets)
        {
            _targets.Add(target);
        }
        RefreshTargetsGrid();
    }

    public void RemoveSelectedTarget()
    {
        if (dgvTargets.CurrentRow != null && dgvTargets.CurrentRow.Index >= 0)
        {
            int index = dgvTargets.CurrentRow.Index;
            lock (_targets)
            {
                if (index < _targets.Count)
                {
                    _targets.RemoveAt(index);
                }
            }
            RefreshTargetsGrid();
        }
    }

    public void ClearAllTargets()
    {
        lock (_targets)
        {
            _targets.Clear();
        }
        RefreshTargetsGrid();
    }

    private void RefreshTargetsGrid()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshTargetsGrid);
            return;
        }

        dgvTargets.Rows.Clear();
        lock (_targets)
        {
            foreach (var t in _targets)
            {
                string progress = $"{t.ReportsCompleted}/{t.MaxReportsRequired}";
                dgvTargets.Rows.Add(t.TargetType.ToString(), t.Keyword, t.VideoTitle ?? string.Empty, t.ChannelName ?? string.Empty, t.TargetUrl ?? string.Empty, progress);
            }
        }
    }

    public ReportScenarioConfig GetScenarioConfig()
    {
        var config = new ReportScenarioConfig
        {
            ScenarioName = cmbScenario.SelectedItem?.ToString() ?? "Kịch bản mặc định",
            MinWatchDurationSeconds = (int)numMinWatch.Value,
            MaxWatchDurationSeconds = (int)numMaxWatch.Value,
            CooldownBetweenReportsSeconds = (int)numCooldown.Value,
            EnableNaturalEngagement = chkNaturalEngagement.Checked,
            RotateReasons = chkRotateReasons.Checked
        };

        var reasonPool = new List<ReportReasonConfig>();
        foreach (int idx in clbReasons.CheckedIndices)
        {
            var cat = idx switch
            {
                0 => ReportReasonCategory.SpamOrMisleading,
                1 => ReportReasonCategory.HarmfulDangerousActs,
                2 => ReportReasonCategory.Misinformation,
                3 => ReportReasonCategory.HatefulOrAbusive,
                4 => ReportReasonCategory.HarassmentOrBullying,
                5 => ReportReasonCategory.ViolentOrRepulsive,
                _ => ReportReasonCategory.SexualContent
            };
            reasonPool.Add(new ReportReasonConfig(cat));
        }

        if (reasonPool.Count > 0)
        {
            config.ReasonPool = reasonPool;
        }

        var templates = txtSpinTemplates.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        if (templates.Count > 0)
        {
            config.DetailTextTemplates = templates;
        }

        return config;
    }

    public void SetScenarioConfig(ReportScenarioConfig config)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetScenarioConfig(config));
            return;
        }

        numMinWatch.Value = Math.Clamp(config.MinWatchDurationSeconds, numMinWatch.Minimum, numMinWatch.Maximum);
        numMaxWatch.Value = Math.Clamp(config.MaxWatchDurationSeconds, numMaxWatch.Minimum, numMaxWatch.Maximum);
        numCooldown.Value = Math.Clamp(config.CooldownBetweenReportsSeconds, numCooldown.Minimum, numCooldown.Maximum);
        chkNaturalEngagement.Checked = config.EnableNaturalEngagement;
        chkRotateReasons.Checked = config.RotateReasons;
    }

    public void SetDevices(IReadOnlyList<ManagedAndroidDevice> devices)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetDevices(devices));
            return;
        }

        dgvAndroidDevices.Rows.Clear();
        foreach (var dev in devices)
        {
            dgvAndroidDevices.Rows.Add(dev.Serial, dev.Model, dev.AndroidVersion, dev.Status);
        }
    }

    public void SetChromeProfiles(IReadOnlyList<string> profiles)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetChromeProfiles(profiles));
            return;
        }

        lstChromeProfiles.Items.Clear();
        foreach (var p in profiles)
        {
            lstChromeProfiles.Items.Add(p);
        }
    }

    public void SetAccounts(IReadOnlyList<AccountProfile> accounts)
    {
        // Có thể mở rộng DataGridView tài khoản nếu cần
    }

    public void SetProxies(IReadOnlyList<ProxyConfig> proxies)
    {
        // Có thể mở rộng DataGridView proxy nếu cần
    }

    public void SetHistoryRecords(IReadOnlyList<ReportTaskRecord> records)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetHistoryRecords(records));
            return;
        }

        dgvHistory.Rows.Clear();
        foreach (var r in records)
        {
            dgvHistory.Rows.Add(r.Id, r.CreatedAt.ToLocalTime().ToString("HH:mm:ss"), r.TargetNameOrUrl, r.DeviceSerialOrProfile, r.Platform, r.Category, r.Status, r.DurationTotalSeconds.ToString("F0"));
        }
    }

    public void UpdateQueueStatus(int pendingCount, int runningCount, int completedCount, int failedCount)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateQueueStatus(pendingCount, runningCount, completedCount, failedCount));
            return;
        }

        lblPending.Text = $"⏳ Đang chờ: {pendingCount}";
        lblRunning.Text = $"⚡ Đang chạy: {runningCount}";
        lblSuccess.Text = $"✅ Thành công: {completedCount}";
        lblFailed.Text = $"❌ Thất bại: {failedCount}";
    }

    public void AppendLog(string message, string level = "INFO")
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message, level));
            return;
        }

        Color color = level switch
        {
            "SUCCESS" => Color.LightGreen,
            "WARN" => Color.Gold,
            "ERROR" => Color.FromArgb(255, 99, 71),
            _ => Color.WhiteSmoke
        };

        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor = Color.DarkGray;
        rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");

        rtbLog.SelectionColor = color;
        rtbLog.AppendText($"[{level}] {message}\r\n");
        rtbLog.ScrollToCaret();
    }

    public void DisplayProofScreenshot(string imagePath)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => DisplayProofScreenshot(imagePath));
            return;
        }

        try
        {
            if (File.Exists(imagePath))
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                picProof.Image?.Dispose();
                picProof.Image = Image.FromStream(stream);
            }
        }
        catch
        {
            // Bỏ qua lỗi mở file ảnh
        }
    }

    public void SetRunningState(bool isRunning)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetRunningState(isRunning));
            return;
        }

        btnStart.Enabled = !isRunning;
        btnStop.Enabled = isRunning;
        rdoAndroid.Enabled = !isRunning;
        rdoChrome.Enabled = !isRunning;
        numConcurrency.Enabled = !isRunning;

        lblStatusHeader.Text = isRunning ? "Trạng thái: Đang thực thi kịch bản..." : "Trạng thái: Đang chờ lệnh";
        lblStatusHeader.ForeColor = isRunning ? Color.FromArgb(0, 123, 255) : Color.DarkSlateGray;
    }

    public void ShowNotification(string message, bool isError = false)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowNotification(message, isError));
            return;
        }

        MessageBox.Show(message, isError ? "Lỗi" : "Thông báo", MessageBoxButtons.OK, isError ? MessageBoxIcon.Error : MessageBoxIcon.Information);
    }
}
