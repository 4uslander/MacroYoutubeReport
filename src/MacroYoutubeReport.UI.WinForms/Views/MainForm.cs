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
    private readonly ContextMenuStrip _cmsTaskActions = new();

    private int _minWatchSeconds = 35;
    private int _maxWatchSeconds = 90;
    private int _cooldownSeconds = 120;
    private int _concurrency = 3;
    private string _reasonFilePath = string.Empty;
    private bool _autoSkipAd = true;
    private bool _enableNaturalEngagement = true;
    private bool _randomWatch = true;
    private bool _rotateReasons = true;
    private bool _autoAllocate = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PlatformType SelectedPlatform => rdoAndroid.Checked ? PlatformType.Android : PlatformType.Chrome;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ConcurrencyLimit
    {
        get => _concurrency;
        set => _concurrency = Math.Clamp(value, 1, 32);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsRunning => btnStop.Enabled;

    public event Func<Task>? ViewLoaded;
    public event Func<Task>? ScanDevicesRequested;
    public event Func<Task>? StartBatchRequested;
    public event Func<ReportTargetItem, Task>? StartSingleTargetRequested;
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
        SwitchLanguage(AppLanguage.Korean);
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
            var serials = GetSelectedDeviceSerials();
            if (serials.Count == 0)
            {
                AppendLog("Không tìm thấy thiết bị nào để dọn cache.", "WARN");
                return;
            }

            AppendLog($"Đang gửi lệnh dọn bộ nhớ cache ứng dụng YouTube trên {serials.Count} thiết bị...", "INFO");
            var tasks = serials.Select(async serial =>
            {
                try
                {
                    var dev = await AdbDeviceManager.Instance.GetDeviceBySerialAsync(serial);
                    if (dev != null && dev.RawDevice != null)
                    {
                        await AdbClientGeneral.Instance.ExecuteShellAsync(dev.RawDevice, "pm clear com.google.android.youtube");
                        AppendLog($"Đã dọn cache YouTube trên thiết bị [{serial}]", "SUCCESS");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Thiết bị [{serial}] lỗi dọn cache: {ex.Message}", "WARN");
                }
            });

            await Task.WhenAll(tasks);
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
            SyncTargetFromInputFields();
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
        this.btnHelpMailFile.Click += (_, _) => ShowFileHelpDialog("account");

        this.btnSelectProxyFile.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtProxyFile.Text = ofd.FileName;
                AppendLog($"Đã chọn file Proxy: {Path.GetFileName(ofd.FileName)}", "INFO");
            }
        };
        this.btnHelpProxyFile.Click += (_, _) => ShowFileHelpDialog("proxy");
        this.btnHelpImportTargets.Click += (_, _) => ShowFileHelpDialog("target");

        string defReasonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "report_reasons.txt");
        if (File.Exists(defReasonPath))
        {
            _reasonFilePath = defReasonPath;
        }

        this.btnOpenSettings.Click += (_, _) =>
        {
            using var dlg = new ReportSettingsDialog(
                minWatch: _minWatchSeconds,
                maxWatch: _maxWatchSeconds,
                cooldown: _cooldownSeconds,
                concurrency: _concurrency,
                reasonFile: _reasonFilePath,
                skipAd: _autoSkipAd,
                naturalEngagement: _enableNaturalEngagement,
                randomWatch: _randomWatch,
                rotateReasons: _rotateReasons,
                autoAllocate: _autoAllocate);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _minWatchSeconds = dlg.MinWatch;
                _maxWatchSeconds = dlg.MaxWatch;
                _cooldownSeconds = dlg.Cooldown;
                _concurrency = dlg.Concurrency;
                _reasonFilePath = dlg.ReasonFile;
                _autoSkipAd = dlg.SkipAd;
                _enableNaturalEngagement = dlg.NaturalEngagement;
                _randomWatch = dlg.RandomWatch;
                _rotateReasons = dlg.RotateReasons;
                _autoAllocate = dlg.AutoAllocate;

                AppendLog($"Đã cập nhật cài đặt: Xem {_minWatchSeconds}-{_maxWatchSeconds}s | Nghỉ {_cooldownSeconds}s | {_concurrency} luồng.", "SUCCESS");
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
            // Giữ lại nội dung cài đặt mục tiêu trên giao diện theo yêu cầu người dùng
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

        // 6. Action Column Clicks on Tasks Grid
        this.dgvTasks.CellContentClick += async (s, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _targets.Count) return;

            string colName = dgvTasks.Columns[e.ColumnIndex].Name;
            var target = _targets[e.RowIndex];

            if (colName == "COL_ACTION_START")
            {
                if (StartSingleTargetRequested != null)
                {
                    await StartSingleTargetRequested.Invoke(target);
                }
                else if (StartBatchRequested != null)
                {
                    await StartBatchRequested.Invoke();
                }
            }
            else if (colName == "COL_ACTIONS")
            {
                ShowTaskActionsMenu(e.ColumnIndex, e.RowIndex);
            }
        };

        // Khi người dùng chọn dòng mục tiêu trên lưới, hiển thị lại thông tin lên form Cài đặt Mục tiêu
        this.dgvTasks.SelectionChanged += (_, _) =>
        {
            if (dgvTasks.CurrentRow != null && dgvTasks.CurrentRow.Index >= 0)
            {
                int idx = dgvTasks.CurrentRow.Index;
                lock (_targets)
                {
                    if (idx < _targets.Count)
                    {
                        var t = _targets[idx];
                        this.txtKeyword.Text = t.Keyword ?? string.Empty;
                        this.txtTitle.Text = t.VideoTitle ?? string.Empty;
                        this.txtChannel.Text = t.ChannelName ?? string.Empty;
                        this.txtUrl.Text = t.TargetUrl ?? string.Empty;
                        int typeIdx = (int)t.TargetType;
                        if (typeIdx >= 0 && typeIdx < this.cmbTargetType.Items.Count)
                        {
                            this.cmbTargetType.SelectedIndex = typeIdx;
                        }
                    }
                }
            }
        };

        // 7. Click Checkbox on Devices Grid (Bật/tắt chọn thiết bị)
        this.dgvDevices.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvDevices.IsCurrentCellDirty && dgvDevices.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvDevices.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        this.dgvDevices.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex >= 0 && dgvDevices.Columns[e.ColumnIndex].Name == "DEV_SEL")
            {
                UpdateSelectedDevicesRatio();
            }
        };

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
            var serials = GetSelectedDeviceSerials();
            if (serials.Count == 0)
            {
                AppendLog("Không tìm thấy thiết bị nào khả dụng để gửi lệnh Home.", "WARN");
                return;
            }

            AppendLog($"Đang gửi lệnh Home tới {serials.Count} thiết bị...", "INFO");

            int successCount = 0;
            var tasks = serials.Select(async serial =>
            {
                try
                {
                    var dev = await AdbDeviceManager.Instance.GetDeviceBySerialAsync(serial);
                    if (dev != null && dev.RawDevice != null)
                    {
                        bool ok = await AdbClientGeneral.Instance.SendHomeAsync(dev.RawDevice);
                        if (ok)
                        {
                            Interlocked.Increment(ref successCount);
                            AppendLog($"Thiết bị [{serial}]: Đã ra màn hình Home thành công.", "SUCCESS");
                        }
                        else
                        {
                            AppendLog($"Thiết bị [{serial}]: Không thể gửi phím Home.", "WARN");
                        }
                    }
                    else
                    {
                        AppendLog($"Thiết bị [{serial}]: Chưa sẵn sàng kết nối ADB.", "WARN");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Thiết bị [{serial}] lỗi Home: {ex.Message}", "ERROR");
                }
            });

            await Task.WhenAll(tasks);
            AppendLog($"Hoàn tất: Đã đưa {successCount}/{serials.Count} thiết bị về màn hình Home.", successCount > 0 ? "SUCCESS" : "WARN");
        };

        this.btnActionCloseAll.Click += async (_, _) =>
        {
            StopBatchRequested?.Invoke();

            var serials = GetSelectedDeviceSerials();
            if (serials.Count == 0)
            {
                AppendLog("Không tìm thấy thiết bị nào khả dụng để đóng ứng dụng.", "WARN");
                return;
            }

            AppendLog($"Đang đóng tất cả ứng dụng/tab trên {serials.Count} thiết bị...", "INFO");

            int successCount = 0;
            var tasks = serials.Select(async serial =>
            {
                try
                {
                    var dev = await AdbDeviceManager.Instance.GetDeviceBySerialAsync(serial);
                    if (dev != null && dev.RawDevice != null)
                    {
                        bool ok = await AdbClientGeneral.Instance.CloseAllRecentAppsAsync(dev.RawDevice);
                        if (ok)
                        {
                            Interlocked.Increment(ref successCount);
                            AppendLog($"Thiết bị [{serial}]: Đã đóng tất cả ứng dụng và tab thành công.", "SUCCESS");
                        }
                        else
                        {
                            AppendLog($"Thiết bị [{serial}]: Đóng ứng dụng gặp lỗi.", "WARN");
                        }
                    }
                    else
                    {
                        AppendLog($"Thiết bị [{serial}]: Chưa sẵn sàng kết nối ADB.", "WARN");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Thiết bị [{serial}] lỗi đóng ứng dụng: {ex.Message}", "ERROR");
                }
            });

            await Task.WhenAll(tasks);
            AppendLog($"Hoàn tất: Đã đóng tất cả ứng dụng trên {successCount}/{serials.Count} thiết bị.", successCount > 0 ? "SUCCESS" : "WARN");
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

        this.btnActionClearCompleted.Click += async (_, _) =>
        {
            lock (_targets)
            {
                _targets.RemoveAll(t => t.ReportsCompleted >= t.MaxReportsRequired);
            }
            RefreshTargetsGrid();
            if (RefreshHistoryRequested != null) await RefreshHistoryRequested.Invoke();
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

    private void ShowTaskActionsMenu(int colIndex, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _targets.Count) return;

        var target = _targets[rowIndex];
        var lang = UiLanguageManager.CurrentLanguage;

        _cmsTaskActions.Items.Clear();

        var itemRestart = new ToolStripMenuItem(
            UiLanguageManager.Translate("Khởi động lại", lang),
            null,
            async (_, _) =>
            {
                target.ReportsCompleted = 0;
                RefreshTargetsGrid();
                if (StartSingleTargetRequested != null)
                {
                    await StartSingleTargetRequested.Invoke(target);
                }
                else if (StartBatchRequested != null)
                {
                    await StartBatchRequested.Invoke();
                }
            });

        var itemStop = new ToolStripMenuItem(
            UiLanguageManager.Translate("Dừng", lang),
            null,
            (_, _) =>
            {
                AppendLog($"[Tác vụ #{rowIndex + 1}] Dừng mục tiêu: {target.VideoTitle ?? target.Keyword}", "WARN");
                StopBatchRequested?.Invoke();
            });

        var itemDel = new ToolStripMenuItem(
            UiLanguageManager.Translate("Xóa", lang),
            null,
            (_, _) =>
            {
                lock (_targets)
                {
                    if (rowIndex < _targets.Count)
                    {
                        _targets.RemoveAt(rowIndex);
                    }
                }
                RefreshTargetsGrid();
                AppendLog($"Đã xóa mục tiêu tại dòng #{rowIndex + 1}", "INFO");
            });

        _cmsTaskActions.Items.AddRange(new ToolStripItem[] { itemRestart, itemStop, new ToolStripSeparator(), itemDel });

        var rect = dgvTasks.GetCellDisplayRectangle(colIndex, rowIndex, true);
        _cmsTaskActions.Show(dgvTasks, new Point(rect.Left, rect.Bottom));
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
            if (col is DataGridViewButtonColumn btnCol)
            {
                if (btnCol.Name == "COL_ACTION_START")
                {
                    btnCol.Text = UiLanguageManager.Translate("Bắt đầu", lang);
                }
                else if (btnCol.Name == "COL_ACTIONS")
                {
                    btnCol.Text = UiLanguageManager.Translate("Thao tác ▾", lang);
                }
            }
        }

        foreach (DataGridViewColumn col in dgvDevices.Columns)
        {
            if (col.Tag == null) col.Tag = col.HeaderText;
            col.HeaderText = UiLanguageManager.Translate((string)col.Tag, lang);
        }

        // Localize Menu
        LocalizeToolStrip(menuStripMain, lang);

        LocalizeTargetTypeComboBox(lang);
        UpdateFileHelpTooltips();

        RefreshTargetsGrid();

        string langName = lang switch
        {
            AppLanguage.Korean => "한국어",
            AppLanguage.English => "English",
            _ => "Tiếng Việt"
        };
        AppendLog($"Đã chuyển ngôn ngữ sang: {langName}", "INFO");
    }

    private void LocalizeTargetTypeComboBox(AppLanguage lang)
    {
        int prevIdx = cmbTargetType.SelectedIndex;
        if (prevIdx < 0) prevIdx = 3; // Mặc định là LiveStream

        cmbTargetType.Items.Clear();
        if (lang == AppLanguage.Korean)
        {
            cmbTargetType.Items.AddRange(new object[] { "동영상 (Video)", "채널 (Channel)", "Shorts", "라이브 (LiveStream)" });
        }
        else if (lang == AppLanguage.English)
        {
            cmbTargetType.Items.AddRange(new object[] { "Video", "Channel", "Shorts", "LiveStream" });
        }
        else
        {
            cmbTargetType.Items.AddRange(new object[] { "Video", "Kênh (Channel)", "Shorts", "LiveStream (Trực tiếp)" });
        }

        cmbTargetType.SelectedIndex = Math.Clamp(prevIdx, 0, cmbTargetType.Items.Count - 1);
    }

    private void UpdateFileHelpTooltips()
    {
        var lang = UiLanguageManager.CurrentLanguage;
        var accHelp = UiLanguageManager.GetFileFormatHelp("account", lang);
        var proxyHelp = UiLanguageManager.GetFileFormatHelp("proxy", lang);
        var targetHelp = UiLanguageManager.GetFileFormatHelp("target", lang);

        toolTipHelp.SetToolTip(btnHelpMailFile, accHelp.Tooltip);
        toolTipHelp.SetToolTip(btnHelpProxyFile, proxyHelp.Tooltip);
        toolTipHelp.SetToolTip(btnHelpImportTargets, targetHelp.Tooltip);
    }

    private void ShowFileHelpDialog(string fileType)
    {
        var lang = UiLanguageManager.CurrentLanguage;
        var help = UiLanguageManager.GetFileFormatHelp(fileType, lang);
        MessageBox.Show(this, help.DialogContent, help.DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    /// <summary>
    /// Tự động đồng bộ thông tin mục tiêu từ các ô nhập liệu trên giao diện vào danh sách mục tiêu.
    /// Giúp người dùng chỉ cần nhập cài đặt và bấm trực tiếp nút 'Bắt đầu' mà không cần bấm thêm/xóa thủ công.
    /// </summary>
    private void SyncTargetFromInputFields()
    {
        string kw = txtKeyword.Text.Trim();
        string title = txtTitle.Text.Trim();
        string url = txtUrl.Text.Trim();
        string channel = txtChannel.Text.Trim();

        if (!string.IsNullOrWhiteSpace(kw) || !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(url))
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
                Keyword = kw,
                VideoTitle = string.IsNullOrWhiteSpace(title) ? null : title,
                ChannelName = string.IsNullOrWhiteSpace(channel) ? null : channel,
                TargetUrl = string.IsNullOrWhiteSpace(url) ? null : url,
                MaxReportsRequired = 1
            };

            lock (_targets)
            {
                // Nếu danh sách chưa có mục tiêu nào, hoặc chỉ có 1 mục tiêu đơn (đang cấu hình trực tiếp trên form)
                if (_targets.Count <= 1)
                {
                    _targets.Clear();
                    _targets.Add(item);
                }
            }
            RefreshTargetsGrid();
        }
    }

    public IReadOnlyList<ReportTargetItem> GetTargetItems()
    {
        SyncTargetFromInputFields();
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
            var lang = UiLanguageManager.CurrentLanguage;
            string btnStartText = UiLanguageManager.Translate("Bắt đầu", lang);
            string btnActionText = UiLanguageManager.Translate("Thao tác ▾", lang);
            string completedText = UiLanguageManager.Translate("Hoàn thành", lang);
            string pendingText = UiLanguageManager.Translate("Chờ xử lý", lang);

            foreach (var t in _targets)
            {
                string interval = $"{_minWatchSeconds}-{_maxWatchSeconds}s";
                string progress = $"{t.ReportsCompleted}/{t.MaxReportsRequired}";
                string status = t.ReportsCompleted >= t.MaxReportsRequired ? completedText : pendingText;
                bool isLive = t.TargetType == ReportTargetType.LiveStream;
                dgvTasks.Rows.Add(
                    btnStartText,
                    btnActionText,
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

    private SearchFilterConfig GetSearchFilterConfigFromUi()
    {
        return new SearchFilterConfig
        {
            Type = cmbSearchFilterType.SelectedIndex switch
            {
                1 => SearchTypeFilter.Video,
                2 => SearchTypeFilter.Channel,
                3 => SearchTypeFilter.Playlist,
                4 => SearchTypeFilter.Movie,
                _ => SearchTypeFilter.All
            },
            Duration = cmbSearchFilterDuration.SelectedIndex switch
            {
                1 => SearchDurationFilter.Short,
                2 => SearchDurationFilter.Medium,
                3 => SearchDurationFilter.Long,
                _ => SearchDurationFilter.All
            },
            UploadDate = cmbSearchFilterUploadDate.SelectedIndex switch
            {
                1 => SearchUploadDateFilter.Today,
                2 => SearchUploadDateFilter.LastHour,
                3 => SearchUploadDateFilter.ThisWeek,
                4 => SearchUploadDateFilter.ThisMonth,
                5 => SearchUploadDateFilter.ThisYear,
                _ => SearchUploadDateFilter.All
            },
            SortBy = cmbSearchFilterSortBy.SelectedIndex switch
            {
                1 => SearchSortByFilter.UploadDate,
                2 => SearchSortByFilter.ViewCount,
                3 => SearchSortByFilter.Rating,
                _ => SearchSortByFilter.Relevance
            },
            LiveOnly = chkSearchFilterLive.Checked,
            CommonsOnly = chkSearchFilterCommons.Checked,
            SubtitlesOnly = chkSearchFilterSubtitles.Checked,
            FourKOnly = chkSearchFilter4K.Checked,
            MaxScrollAttempts = 50
        };
    }

    public ReportScenarioConfig GetScenarioConfig()
    {
        string reasonFilePath = _reasonFilePath.Trim();
        string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "report_reasons.txt");
        string fileToLoad = File.Exists(reasonFilePath) ? reasonFilePath : (File.Exists(defaultPath) ? defaultPath : string.Empty);
        var searchFilter = GetSearchFilterConfigFromUi();

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

        ReportScenarioConfig config;
        if (cmbScenario.SelectedIndex == 5 || (cmbScenario.SelectedItem?.ToString()?.Contains("Livestream") == true))
        {
            config = ReportScenarioConfig.CreateLiveStreamFinancialScamConfig();
        }
        else
        {
            config = new ReportScenarioConfig
            {
                ScenarioName = cmbScenario.SelectedItem?.ToString() ?? "Kịch bản mặc định"
            };
        }

        config.MinWatchDurationSeconds = _minWatchSeconds;
        config.MaxWatchDurationSeconds = _maxWatchSeconds;
        config.CooldownBetweenReportsSeconds = _cooldownSeconds;
        config.EnableNaturalEngagement = _enableNaturalEngagement;
        config.RotateReasons = _rotateReasons;
        config.SearchFilter = searchFilter;

        if (!string.IsNullOrWhiteSpace(fileToLoad))
        {
            config.LoadReasonTemplatesFromFile(fileToLoad);
        }

        // QUAN TRỌNG: Thiết lập danh mục lý do báo cáo theo đúng lựa chọn tại ComboBox Tiêu chí báo cáo (cmbMainReason)
        // Nếu người dùng chọn lý do cụ thể (như Lừa đảo tài chính), hệ thống PHẢI dùng đúng lý do đó,
        // và xoay vòng nội dung giải trình chi tiết trong file cấu hình cho chính danh mục đó.
        // Chỉ khi người dùng chọn chế độ "Tất cả danh mục (Xoay vòng ngẫu nhiên)" thì mới xoay vòng nhiều danh mục khác nhau.
        if (cmbMainReason.SelectedIndex == 11)
        {
            config.ReasonPool = new List<ReportReasonConfig>
            {
                new(ReportReasonCategory.FinancialOrCryptoScam),
                new(ReportReasonCategory.SpamOrMisleading),
                new(ReportReasonCategory.Misinformation),
                new(ReportReasonCategory.HarmfulDangerousActs),
                new(ReportReasonCategory.HatefulOrAbusive),
                new(ReportReasonCategory.HarassmentOrBullying),
                new(ReportReasonCategory.ViolentOrRepulsive)
            };
            config.RotateReasons = true;
        }
        else
        {
            // Tôn trọng 100% lựa chọn tiêu chí của người dùng tại cmbMainReason
            config.ReasonPool = new List<ReportReasonConfig> { new(selectedReason) };
            config.RotateReasons = _rotateReasons;
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

        _minWatchSeconds = Math.Clamp(config.MinWatchDurationSeconds, 5, 1800);
        _maxWatchSeconds = Math.Clamp(config.MaxWatchDurationSeconds, 5, 1800);
        _cooldownSeconds = Math.Clamp(config.CooldownBetweenReportsSeconds, 0, 3600);
        _enableNaturalEngagement = config.EnableNaturalEngagement;
        _rotateReasons = config.RotateReasons;

        if (config.ReasonPool.Count > 0)
        {
            var cat = config.ReasonPool[0].Category;
            int reasonIdx = cat switch
            {
                ReportReasonCategory.FinancialOrCryptoScam => 0,
                ReportReasonCategory.SpamOrMisleading => 1,
                ReportReasonCategory.HarmfulDangerousActs => 2,
                ReportReasonCategory.Misinformation => 3,
                ReportReasonCategory.HatefulOrAbusive => 4,
                ReportReasonCategory.HarassmentOrBullying => 5,
                ReportReasonCategory.ViolentOrRepulsive => 6,
                ReportReasonCategory.SexualContent => 7,
                ReportReasonCategory.ChildAbuse => 8,
                ReportReasonCategory.PromotesTerrorism => 9,
                ReportReasonCategory.InfringesMyRights => 10,
                _ => 0
            };
            if (reasonIdx >= 0 && reasonIdx < cmbMainReason.Items.Count)
            {
                cmbMainReason.SelectedIndex = reasonIdx;
            }
        }

        if (config.SearchFilter != null)
        {
            cmbSearchFilterType.SelectedIndex = Math.Clamp((int)config.SearchFilter.Type, 0, cmbSearchFilterType.Items.Count - 1);
            cmbSearchFilterDuration.SelectedIndex = Math.Clamp((int)config.SearchFilter.Duration, 0, cmbSearchFilterDuration.Items.Count - 1);
            cmbSearchFilterUploadDate.SelectedIndex = Math.Clamp((int)config.SearchFilter.UploadDate, 0, cmbSearchFilterUploadDate.Items.Count - 1);
            cmbSearchFilterSortBy.SelectedIndex = Math.Clamp((int)config.SearchFilter.SortBy, 0, cmbSearchFilterSortBy.Items.Count - 1);
            chkSearchFilterLive.Checked = config.SearchFilter.LiveOnly;
            chkSearchFilterCommons.Checked = config.SearchFilter.CommonsOnly;
            chkSearchFilterSubtitles.Checked = config.SearchFilter.SubtitlesOnly;
            chkSearchFilter4K.Checked = config.SearchFilter.FourKOnly;
        }
    }

    public IReadOnlyList<string> GetSelectedDeviceSerials()
    {
        var list = new List<string>();
        foreach (DataGridViewRow row in dgvDevices.Rows)
        {
            bool isChecked = Convert.ToBoolean(row.Cells["DEV_SEL"].Value ?? false);
            if (isChecked)
            {
                string? serial = row.Cells["DEV_SERIAL"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(serial))
                {
                    list.Add(serial.Trim());
                }
            }
        }

        // Tự động fallback lấy tất cả thiết bị online nếu chưa chọn ô nào
        if (list.Count == 0 && dgvDevices.Rows.Count > 0)
        {
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                string? serial = row.Cells["DEV_SERIAL"].Value?.ToString();
                string? status = row.Cells["DEV_STATUS"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(serial) && (status == "Online" || string.IsNullOrWhiteSpace(status)))
                {
                    list.Add(serial.Trim());
                    row.Cells["DEV_SEL"].Value = true;
                }
            }
            UpdateSelectedDevicesRatio();
        }

        return list;
    }

    public void RefreshTasksDisplay()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshTasksDisplay);
            return;
        }
        RefreshTargetsGrid();
    }

    public void UpdateDeviceRunningStatus(string deviceSerial, string targetName, string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateDeviceRunningStatus(deviceSerial, targetName, status));
            return;
        }

        foreach (DataGridViewRow row in dgvDevices.Rows)
        {
            string? serial = row.Cells["DEV_SERIAL"].Value?.ToString();
            if (string.Equals(serial, deviceSerial, StringComparison.OrdinalIgnoreCase))
            {
                row.Cells["DEV_TARGET"].Value = targetName;
                row.Cells["DEV_STATUS"].Value = status;
                if (status == "Đang chạy")
                {
                    row.Cells["DEV_STATUS"].Style.ForeColor = Color.DarkOrange;
                }
                else if (status == "Hoàn thành")
                {
                    row.Cells["DEV_STATUS"].Style.ForeColor = Color.DarkGreen;
                }
                else if (status == "Thất bại")
                {
                    row.Cells["DEV_STATUS"].Style.ForeColor = Color.Red;
                }
                else
                {
                    row.Cells["DEV_STATUS"].Style.ForeColor = Color.Black;
                }
                break;
            }
        }
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
            string ip = dev.Serial.Contains(':') ? dev.Serial.Split(':')[0] : "Direct";
            dgvDevices.Rows.Add(
                true, // Mặc định tích chọn tất cả thiết bị
                stt++,
                ip,
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

        string translated = UiLanguageManager.TranslateLog(message, UiLanguageManager.CurrentLanguage);

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
        rtbLiveLog.AppendText($"[{level}] {translated}\r\n");
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
        btnOpenSettings.Enabled = !isRunning;
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
