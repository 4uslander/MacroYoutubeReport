using System.ComponentModel;
using System.Diagnostics;
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

        // 1. Menu Strip Operations
        this.tsmiOpenLogFolder.Click += (_, _) =>
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacroYoutubeReport", "logs");
                Directory.CreateDirectory(logDir);
                Process.Start(new ProcessStartInfo { FileName = logDir, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi mở thư mục Logs: {ex.Message}", "WARN");
            }
        };

        this.tsmiExportHistory.Click += (_, _) => btnActionExportCsv.PerformClick();
        this.tsmiImportAccounts.Click += (_, _) => btnSelectMailFile.PerformClick();
        this.tsmiImportProxies.Click += (_, _) => btnSelectProxyFile.PerformClick();
        this.tsmiExit.Click += (_, _) => this.Close();

        this.tsmiClearCache.Click += async (_, _) =>
        {
            AppendLog("Đang gửi lệnh dọn bộ nhớ cache ứng dụng YouTube trên các thiết bị...", "INFO");
            try
            {
                var devices = await AdbDeviceManager.Instance.ScanDevicesAsync();
                foreach (var dev in devices)
                {
                    await AdbClientGeneral.Instance.ExecuteShellAsync(dev.RawDevice, "pm clear com.google.android.youtube");
                    AppendLog($"Đã dọn cache YouTube trên thiết bị [{dev.Serial}]", "SUCCESS");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi dọn cache: {ex.Message}", "WARN");
            }
        };

        this.tsmiRestartAdb.Click += async (_, _) =>
        {
            AppendLog("Đang khởi động lại ADB Server...", "WARN");
            try
            {
                await Task.Run(() => AdbServerManager.Instance.EnsureServerRunning());
                AppendLog("ADB Server đã được khởi động lại thành công.", "SUCCESS");
                if (ScanDevicesRequested != null) await ScanDevicesRequested.Invoke();
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi khởi động lại ADB: {ex.Message}", "ERROR");
            }
        };

        this.tsmiAdbCmd.Click += (_, _) =>
        {
            AppendLog("ADB Console: Gõ lệnh shell trực tiếp thông qua cửa sổ điều khiển hoặc công cụ mở rộng.", "INFO");
            ShowNotification("Tính năng ADB Console: Bạn có thể kiểm tra danh sách thiết bị bằng nút 'Quét gom TCP' hoặc 'Quét thiết bị'.");
        };

        // Language items
        this.tsmiLangVi.Click += (_, _) => SwitchLanguage(AppLanguage.Vietnamese);
        this.tsmiLangKr.Click += (_, _) => SwitchLanguage(AppLanguage.Korean);
        this.tsmiLangEn.Click += (_, _) => SwitchLanguage(AppLanguage.English);

        // Help items
        this.tsmiUserGuide.Click += (_, _) =>
        {
            ShowNotification(
                "HƯỚNG DẪN SỬ DỤNG MACRO YOUTUBE REPORT:\n\n" +
                "1. Chọn nền tảng thực thi (Android ADB hoặc Chrome Playwright).\n" +
                "2. Thêm mục tiêu cần báo cáo (Video, Kênh, Shorts) hoặc bấm 'Nhập file' từ text.\n" +
                "3. Thiết lập tiêu chí: Lý do vi phạm, thời gian xem video giả lập người thật, thời gian nghỉ.\n" +
                "4. Trong danh sách thiết bị bên dưới, tích chọn các thiết bị sẽ tham gia chạy.\n" +
                "5. Nhấn 'Bắt đầu' để chạy toàn bộ hoặc bấm 'Bắt đầu' trên từng hàng tác vụ.\n" +
                "6. Theo dõi tiến trình thời gian thực trong bảng Nhật ký trực tiếp.",
                isError: false);
        };

        this.tsmiAbout.Click += (_, _) =>
        {
            ShowNotification("YouTube Auto Report Macro v1.0.0\nPhát triển chuyên dụng cho tự động hóa mô phỏng tương tác người thật.", isError: false);
        };

        // 2. Platform & Connect
        this.btnConnectDevices.Click += async (_, _) =>
        {
            if (ScanDevicesRequested != null) await ScanDevicesRequested.Invoke();
        };

        // 3. Start & Stop
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

        // 4. File selectors
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

        // 5. Target Input & Operations
        this.btnAddTarget.Click += (_, _) =>
        {
            var targetType = cmbTargetType.SelectedIndex switch
            {
                1 => ReportTargetType.Channel,
                2 => ReportTargetType.Shorts,
                3 => ReportTargetType.LiveStream,
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

        // 6. Action Column Clicks on Tasks Grid (Bắt đầu, Khởi động lại, Dừng, Xóa)
        this.dgvTasks.CellContentClick += async (s, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _targets.Count) return;

            string colName = dgvTasks.Columns[e.ColumnIndex].Name;
            var target = _targets[e.RowIndex];

            if (colName == "COL_ACTION_START")
            {
                AppendLog($"[Tác vụ #{e.RowIndex + 1}] Bắt đầu thực thi: {target.VideoTitle ?? target.Keyword}", "INFO");
                if (StartBatchRequested != null) await StartBatchRequested.Invoke();
            }
            else if (colName == "COL_ACTION_RESTART")
            {
                target.ReportsCompleted = 0;
                RefreshTargetsGrid();
                AppendLog($"[Tác vụ #{e.RowIndex + 1}] Đã khởi động lại mục tiêu: {target.VideoTitle ?? target.Keyword}", "INFO");
                if (StartBatchRequested != null) await StartBatchRequested.Invoke();
            }
            else if (colName == "COL_ACTION_STOP")
            {
                AppendLog($"[Tác vụ #{e.RowIndex + 1}] Dừng mục tiêu: {target.VideoTitle ?? target.Keyword}", "WARN");
                StopBatchRequested?.Invoke();
            }
            else if (colName == "COL_ACTION_DEL")
            {
                lock (_targets)
                {
                    _targets.RemoveAt(e.RowIndex);
                }
                RefreshTargetsGrid();
                AppendLog($"Đã xóa mục tiêu tại dòng #{e.RowIndex + 1}", "INFO");
            }
        };

        // 7. Click Checkbox on Devices Grid (Bật/tắt chọn thiết bị)
        this.dgvDevices.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && dgvDevices.Columns[e.ColumnIndex].Name == "DEV_SEL")
            {
                dgvDevices.CommitEdit(DataGridViewDataErrorContexts.Commit);
                UpdateSelectedDevicesRatio();
            }
        };

        // 8. Top Quick Actions
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
            AppendLog("Đang kiểm tra tính hợp lệ của tài khoản...", "INFO");
        };

        this.btnActionCheckYoutube.Click += (_, _) =>
        {
            AppendLog("Đang kiểm tra ứng dụng YouTube trên các thiết bị...", "INFO");
        };

        this.btnActionReallocate.Click += (_, _) =>
        {
            AppendLog("Đang phân bổ lại tài khoản và proxy cho danh sách thiết bị...", "INFO");
        };

        this.btnActionResetCache.Click += (_, _) =>
        {
            tsmiClearCache.PerformClick();
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

        // 9. Middle Filter & Task Reordering Actions
        this.btnSelectAllTasks.Click += (_, _) =>
        {
            // Toggle check all devices
            bool hasUnchecked = false;
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (!Convert.ToBoolean(row.Cells["DEV_SEL"].Value ?? false))
                {
                    hasUnchecked = true;
                    break;
                }
            }

            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                row.Cells["DEV_SEL"].Value = hasUnchecked;
            }
            UpdateSelectedDevicesRatio();
        };

        this.btnCheckAll.Click += (_, _) =>
        {
            foreach (DataGridViewRow row in dgvTasks.Rows)
            {
                row.Selected = true;
            }
        };

        this.btnMoveUp.Click += (_, _) => MoveSelectedTask(-1);
        this.btnMoveDown.Click += (_, _) => MoveSelectedTask(1);

        this.btnFind.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(txtKeyword.Text))
            {
                string kw = txtKeyword.Text.Trim();
                foreach (DataGridViewRow row in dgvTasks.Rows)
                {
                    string targetKw = row.Cells["COL_KEYWORD"].Value?.ToString() ?? "";
                    if (targetKw.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        dgvTasks.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
        };

        this.btnRemoveDup.Click += (_, _) =>
        {
            lock (_targets)
            {
                var distinct = _targets
                    .GroupBy(t => t.TargetUrl ?? t.Keyword)
                    .Select(g => g.First())
                    .ToList();
                int removed = _targets.Count - distinct.Count;
                _targets.Clear();
                _targets.AddRange(distinct);
                AppendLog($"Đã loại bỏ {removed} mục tiêu trùng lặp.", "INFO");
            }
            RefreshTargetsGrid();
        };

        this.btnDuplicate.Click += (_, _) =>
        {
            if (dgvTasks.CurrentRow != null && dgvTasks.CurrentRow.Index >= 0)
            {
                int idx = dgvTasks.CurrentRow.Index;
                if (idx < _targets.Count)
                {
                    var orig = _targets[idx];
                    var copy = new ReportTargetItem(orig.Keyword, orig.ChannelName, orig.VideoTitle)
                    {
                        TargetType = orig.TargetType,
                        TargetUrl = orig.TargetUrl,
                        MaxReportsRequired = orig.MaxReportsRequired
                    };
                    AddTargetItem(copy);
                    AppendLog("Đã nhân bản dòng tác vụ đã chọn.", "INFO");
                }
            }
        };

        this.rdoFilterAll.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.rdoFilterDone.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.rdoFilterRunning.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.rdoFilterFailed.CheckedChanged += (_, _) => ApplyTaskFilter();
        this.cmbFilterType.SelectedIndexChanged += (_, _) => ApplyTaskFilter();
    }

    private void SwitchLanguage(AppLanguage lang)
    {
        UiLanguageManager.CurrentLanguage = lang;
        tsmiLangVi.Checked = lang == AppLanguage.Vietnamese;
        tsmiLangKr.Checked = lang == AppLanguage.Korean;
        tsmiLangEn.Checked = lang == AppLanguage.English;

        ApplyLocalization(this, lang);

        // Localize Grids
        foreach (DataGridViewColumn col in dgvTasks.Columns)
        {
            if (col.Tag == null) col.Tag = col.HeaderText;
            col.HeaderText = UiLanguageManager.Translate((string)col.Tag, lang);
        }

        foreach (DataGridViewColumn col in dgvDevices.Columns)
        {
            if (col.Tag == null) col.Tag = col.HeaderText;
            col.HeaderText = UiLanguageManager.Translate((string)col.Tag, lang);
        }

        // Localize Menu
        LocalizeToolStrip(menuStripMain, lang);

        string langName = lang switch
        {
            AppLanguage.Korean => "한국어",
            AppLanguage.English => "English",
            _ => "Tiếng Việt"
        };
        AppendLog($"Đã chuyển ngôn ngữ sang: {langName}", "INFO");
    }

    private void ApplyLocalization(Control parent, AppLanguage lang)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is Button || c is Label || c is GroupBox || c is CheckBox || c is RadioButton)
            {
                if (c.Tag == null) c.Tag = c.Text ?? string.Empty;
                c.Text = UiLanguageManager.Translate(c.Tag?.ToString() ?? string.Empty, lang);
            }

            if (c.HasChildren)
            {
                ApplyLocalization(c, lang);
            }
        }
    }

    private void LocalizeToolStrip(ToolStrip strip, AppLanguage lang)
    {
        foreach (ToolStripItem item in strip.Items)
        {
            LocalizeToolStripItem(item, lang);
        }
    }

    private void LocalizeToolStripItem(ToolStripItem item, AppLanguage lang)
    {
        if (item is ToolStripMenuItem menuItem)
        {
            // Do not translate language toggles themselves (VI, KR, EN)
            if (menuItem == tsmiLangVi || menuItem == tsmiLangKr || menuItem == tsmiLangEn)
                return;
        }

        if (item.Tag == null) item.Tag = item.Text ?? string.Empty;
        item.Text = UiLanguageManager.Translate(item.Tag?.ToString() ?? string.Empty, lang);

        if (item is ToolStripDropDownItem dropDown)
        {
            foreach (ToolStripItem sub in dropDown.DropDownItems)
            {
                LocalizeToolStripItem(sub, lang);
            }
        }
    }

    private void MoveSelectedTask(int direction)
    {
        if (dgvTasks.CurrentRow == null) return;
        int idx = dgvTasks.CurrentRow.Index;
        int targetIdx = idx + direction;

        lock (_targets)
        {
            if (targetIdx >= 0 && targetIdx < _targets.Count)
            {
                var item = _targets[idx];
                _targets.RemoveAt(idx);
                _targets.Insert(targetIdx, item);
                RefreshTargetsGrid();
                dgvTasks.Rows[targetIdx].Selected = true;
            }
        }
    }

    private void UpdateSelectedDevicesRatio()
    {
        int selected = 0;
        int total = dgvDevices.Rows.Count;
        foreach (DataGridViewRow row in dgvDevices.Rows)
        {
            if (Convert.ToBoolean(row.Cells["DEV_SEL"].Value ?? false))
            {
                selected++;
            }
        }
        lblTaskCountRatio.Text = $"{selected}/{total}";
    }

    private void ApplyTaskFilter()
    {
        string typeFilter = cmbFilterType.SelectedItem?.ToString() ?? "Tất cả";
        foreach (DataGridViewRow row in dgvTasks.Rows)
        {
            if (row.IsNewRow) continue;
            bool visible = true;

            string rowStatus = row.Cells["COL_STATUS"].Value?.ToString() ?? string.Empty;

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
            int devCount = dgvDevices.Rows.Count > 0 ? dgvDevices.Rows.Count : 1;
            foreach (var t in _targets)
            {
                string interval = $"{numMinWatch.Value}-{numMaxWatch.Value}s";
                string progress = $"{t.ReportsCompleted}/{t.MaxReportsRequired}";
                string status = t.ReportsCompleted >= t.MaxReportsRequired ? "Hoàn thành" : "Chờ xử lý";
                bool isLive = t.TargetType == ReportTargetType.LiveStream;
                dgvTasks.Rows.Add(
                    "Bắt đầu",
                    "Khởi động lại",
                    "Dừng",
                    "Xóa",
                    devCount.ToString(),
                    t.Keyword,
                    t.ChannelName ?? string.Empty,
                    t.VideoTitle ?? string.Empty,
                    t.TargetUrl ?? string.Empty,
                    interval,
                    true,
                    isLive,
                    progress,
                    status);
            }
        }
    }

    public ReportScenarioConfig GetScenarioConfig()
    {
        if (cmbScenario.SelectedIndex == 5 || (cmbScenario.SelectedItem?.ToString()?.Contains("Livestream") == true))
        {
            var liveConfig = ReportScenarioConfig.CreateLiveStreamFinancialScamConfig();
            liveConfig.MinWatchDurationSeconds = (int)numMinWatch.Value;
            liveConfig.MaxWatchDurationSeconds = (int)numMaxWatch.Value;
            liveConfig.CooldownBetweenReportsSeconds = (int)numCooldown.Value;
            liveConfig.EnableNaturalEngagement = chkNaturalEngagement.Checked;
            liveConfig.RotateReasons = chkRotateReasons.Checked;
            return liveConfig;
        }

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
            0 => ReportReasonCategory.FinancialOrCryptoScam,
            1 => ReportReasonCategory.SpamOrMisleading,
            2 => ReportReasonCategory.HarmfulDangerousActs,
            3 => ReportReasonCategory.Misinformation,
            4 => ReportReasonCategory.HatefulOrAbusive,
            5 => ReportReasonCategory.HarassmentOrBullying,
            6 => ReportReasonCategory.ViolentOrRepulsive,
            7 => ReportReasonCategory.SexualContent,
            8 => ReportReasonCategory.ChildAbuse,
            9 => ReportReasonCategory.PromotesTerrorism,
            10 => ReportReasonCategory.InfringesMyRights,
            _ => ReportReasonCategory.FinancialOrCryptoScam
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
        int stt = 1;
        foreach (var dev in devices)
        {
            dgvDevices.Rows.Add(
                true, // Default selected (checked)
                stt++,
                "127.0.0.1",
                dev.Serial,
                dev.Model,
                "Chưa gán",
                "••••••",
                "Đang rảnh",
                dev.Status.ToString());
        }
        UpdateSelectedDevicesRatio();
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

        lblStatusSummary.Text = $"{pendingCount}/{runningCount}/{completedCount} - 0/{failedCount}";
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
            "SUCCESS" => Color.FromArgb(25, 135, 84), // Emerald green
            "WARN" => Color.FromArgb(217, 119, 6),    // Amber/Orange
            "ERROR" => Color.FromArgb(220, 53, 69),   // Crimson Red
            _ => Color.FromArgb(33, 37, 41)           // Dark charcoal
        };

        rtbLiveLog.SelectionStart = rtbLiveLog.TextLength;
        rtbLiveLog.SelectionLength = 0;
        rtbLiveLog.SelectionColor = Color.FromArgb(120, 120, 120);
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
