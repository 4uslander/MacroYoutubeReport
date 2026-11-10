using System.ComponentModel;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Localization;
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

        // Platform & Connect
        this.btnConnectDevices.Click += async (_, _) =>
        {
            if (ScanDevicesRequested != null) await ScanDevicesRequested.Invoke();
        };

        // Start & Stop
        this.btnStart.Click += async (_, _) =>
        {
            if (StartBatchRequested != null) await StartBatchRequested.Invoke();
        };

        this.btnStop.Click += (_, _) =>
        {
            StopBatchRequested?.Invoke();
        };

        this.btnExitApp.Click += (_, _) =>
        {
            this.Close();
        };

        // File selectors
        this.btnSelectMailFile.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtMailFile.Text = ofd.FileName;
                AppendLog($"Đã chọn file tài khoản Gmail: {Path.GetFileName(ofd.FileName)}", "INFO");
            }
        };

        this.btnSelectProxyFile.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtProxyFile.Text = ofd.FileName;
                AppendLog($"Đã chọn file Proxy: {Path.GetFileName(ofd.FileName)}", "INFO");
            }
        };

        // Target operations
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

        this.btnImportTargets.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var lines = File.ReadAllLines(ofd.FileName);
                int count = 0;
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
                    count++;
                }
                AppendLog($"Đã nhập {count} mục tiêu từ file {Path.GetFileName(ofd.FileName)}", "INFO");
            }
        };

        // Top Quick Actions
        this.btnActionHome.Click += async (_, _) =>
        {
            if (RefreshHistoryRequested != null) await RefreshHistoryRequested.Invoke();
            AppendLog("Đã làm mới dữ liệu màn hình chính.", "INFO");
        };

        this.btnActionCloseAll.Click += (_, _) =>
        {
            StopBatchRequested?.Invoke();
            AppendLog("Đã phát lệnh đóng tất cả tác vụ đang chạy.", "WARN");
        };

        this.btnActionFullScreen.Click += (_, _) =>
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        };

        this.btnActionCheckAccount.Click += (_, _) =>
        {
            AppendLog("Đang kiểm tra trạng thái tài khoản...", "INFO");
        };

        this.btnActionCheckYoutube.Click += (_, _) =>
        {
            AppendLog("Kiểm tra ứng dụng YouTube trên các thiết bị...", "INFO");
        };

        this.btnActionReallocate.Click += (_, _) =>
        {
            AppendLog("Đang phân bổ lại tài khoản cho các thiết bị...", "INFO");
        };

        this.btnActionResetCache.Click += (_, _) =>
        {
            AppendLog("Đã xóa bộ nhớ đệm tạm thời.", "INFO");
        };

        this.btnActionScanTcp.Click += async (_, _) =>
        {
            AppendLog("Đang quét gom TCP các thiết bị ADB...", "INFO");
            if (ScanDevicesRequested != null) await ScanDevicesRequested.Invoke();
        };

        this.btnActionExportCsv.Click += (_, _) =>
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

        this.btnActionClearCompleted.Click += (_, _) =>
        {
            lock (_targets)
            {
                _targets.RemoveAll(t => t.ReportsCompleted >= t.MaxReportsRequired);
            }
            RefreshTargetsGrid();
            AppendLog("Đã dọn dẹp các tác vụ hoàn thành khỏi danh sách.", "INFO");
        };

        // Filter Actions
        this.btnSelectAllTasks.Click += (_, _) =>
        {
            foreach (DataGridViewRow row in dgvTasks.Rows)
            {
                row.Selected = true;
            }
        };

        this.rdoFilterAll.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.rdoFilterDone.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.rdoFilterRunning.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.rdoFilterFailed.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.cmbFilterType.SelectedIndexChanged += (_, _) => ApplyTaskFilter();

        // Menu strip
        this.tsmiExportHistory.Click += (_, _) => btnActionExportCsv.PerformClick();
        this.tsmiExit.Click += (_, _) => this.Close();
        this.tsmiLangVi.Click += (_, _) => UiLanguageManager.CurrentLanguage = AppLanguage.Vietnamese;
        this.tsmiLangKr.Click += (_, _) => UiLanguageManager.CurrentLanguage = AppLanguage.Korean;
        this.tsmiLangEn.Click += (_, _) => UiLanguageManager.CurrentLanguage = AppLanguage.English;
    }

    private void ApplyTaskFilter()
    {
        string typeFilter = cmbFilterType.SelectedItem?.ToString() ?? "Tất cả";
        foreach (DataGridViewRow row in dgvTasks.Rows)
        {
            if (row.IsNewRow) continue;
            bool visible = true;

            string rowType = row.Cells["COL_TYPE"].Value?.ToString() ?? string.Empty;
            string rowStatus = row.Cells["COL_STATUS"].Value?.ToString() ?? string.Empty;

            if (typeFilter != "Tất cả" && !rowType.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
            {
                visible = false;
            }

            if (rdoFilterDone.Checked && rowStatus != "Hoàn thành") visible = false;
            if (rdoFilterRunning.Checked && rowStatus != "Đang chạy") visible = false;
            if (rdoFilterFailed.Checked && rowStatus != "Thất bại") visible = false;

            row.Visible = visible;
        }
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
        if (dgvTasks.CurrentRow != null && dgvTasks.CurrentRow.Index >= 0)
        {
            int index = dgvTasks.CurrentRow.Index;
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

        dgvTasks.Rows.Clear();
        lock (_targets)
        {
            int stt = 1;
            foreach (var t in _targets)
            {
                string interval = $"{numMinWatch.Value}-{numMaxWatch.Value}s";
                string progress = $"{t.ReportsCompleted}/{t.MaxReportsRequired}";
                string status = t.ReportsCompleted >= t.MaxReportsRequired ? "Hoàn thành" : "Chờ xử lý";
                dgvTasks.Rows.Add(stt++, t.TargetType.ToString(), t.Keyword, t.ChannelName ?? string.Empty, t.VideoTitle ?? string.Empty, t.TargetUrl ?? string.Empty, interval, progress, status);
            }
            lblTaskCountRatio.Text = $"0/{_targets.Count}";
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

        var selectedReason = cmbMainReason.SelectedIndex switch
        {
            0 => ReportReasonCategory.SpamOrMisleading,
            1 => ReportReasonCategory.HarmfulDangerousActs,
            2 => ReportReasonCategory.Misinformation,
            3 => ReportReasonCategory.HatefulOrAbusive,
            4 => ReportReasonCategory.HarassmentOrBullying,
            5 => ReportReasonCategory.ViolentOrRepulsive,
            6 => ReportReasonCategory.SexualContent,
            7 => ReportReasonCategory.ChildAbuse,
            8 => ReportReasonCategory.PromotesTerrorism,
            9 => ReportReasonCategory.InfringesMyRights,
            _ => ReportReasonCategory.SpamOrMisleading
        };

        if (chkRotateReasons.Checked)
        {
            config.ReasonPool = new List<ReportReasonConfig>
            {
                new(ReportReasonCategory.SpamOrMisleading),
                new(ReportReasonCategory.HarmfulDangerousActs),
                new(ReportReasonCategory.Misinformation),
                new(ReportReasonCategory.HatefulOrAbusive),
                new(ReportReasonCategory.HarassmentOrBullying),
                new(ReportReasonCategory.ViolentOrRepulsive)
            };
        }
        else
        {
            config.ReasonPool = new List<ReportReasonConfig> { new(selectedReason) };
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

        dgvDevices.Rows.Clear();
        foreach (var dev in devices)
        {
            dgvDevices.Rows.Add(false, dev.Serial, dev.Model, "Chưa gán", "Direct", "Đang rảnh", dev.Status);
        }
    }

    public void SetChromeProfiles(IReadOnlyList<string> profiles)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetChromeProfiles(profiles));
            return;
        }
    }

    public void SetAccounts(IReadOnlyList<AccountProfile> accounts)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetAccounts(accounts));
            return;
        }
    }

    public void SetProxies(IReadOnlyList<ProxyConfig> proxies)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetProxies(proxies));
            return;
        }
    }

    public void SetHistoryRecords(IReadOnlyList<ReportTaskRecord> records)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetHistoryRecords(records));
            return;
        }

        lock (_targets)
        {
            foreach (var record in records)
            {
                var matched = _targets.FirstOrDefault(t => t.Matches(record.TargetNameOrUrl, record.TargetNameOrUrl, record.TargetNameOrUrl));
                if (matched != null && record.Status == ExecutionStatus.Succeeded)
                {
                    matched.ReportsCompleted = Math.Max(matched.ReportsCompleted, 1);
                }
            }
        }
        RefreshTargetsGrid();
    }

    public void UpdateQueueStatus(int pendingCount, int runningCount, int completedCount, int failedCount)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateQueueStatus(pendingCount, runningCount, completedCount, failedCount));
            return;
        }

        lblStatusSummary.Text = $"Chờ: {pendingCount} | Chạy: {runningCount} | Xong: {completedCount} | Lỗi: {failedCount}";
        int total = _targets.Count;
        lblTaskCountRatio.Text = $"{completedCount}/{total}";
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
            "SUCCESS" => Color.FromArgb(100, 255, 100),
            "WARN" => Color.FromArgb(255, 215, 0),
            "ERROR" => Color.FromArgb(255, 100, 100),
            _ => Color.FromArgb(220, 220, 220)
        };

        rtbLiveLog.SelectionStart = rtbLiveLog.TextLength;
        rtbLiveLog.SelectionLength = 0;
        rtbLiveLog.SelectionColor = Color.FromArgb(140, 140, 140);
        rtbLiveLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");

        rtbLiveLog.SelectionColor = color;
        rtbLiveLog.AppendText($"[{level}] {message}\r\n");
        rtbLiveLog.ScrollToCaret();
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
