namespace MacroYoutubeReport.UI.WinForms.Views;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Top MenuStrip
    private MenuStrip menuStripMain;
    private ToolStripMenuItem tsmiMenu;
    private ToolStripMenuItem tsmiOpenLogFolder;
    private ToolStripMenuItem tsmiExportHistory;
    private ToolStripMenuItem tsmiExit;
    private ToolStripMenuItem tsmiTools;
    private ToolStripMenuItem tsmiOpenVpn;
    private ToolStripMenuItem tsmiClearCache;
    private ToolStripMenuItem tsmiLangVi;
    private ToolStripMenuItem tsmiLangKr;
    private ToolStripMenuItem tsmiLangEn;
    private ToolStripMenuItem tsmiHelp;

    // Left Panel & Groups
    private Panel pnlLeft;
    private GroupBox gbPlatformAndAuth;
    private RadioButton rdoAndroid;
    private RadioButton rdoChrome;
    private TextBox txtMailFile;
    private Button btnSelectMailFile;
    private TextBox txtProxyFile;
    private Button btnSelectProxyFile;

    private GroupBox gbTargetInput;
    private Label lblTargetType;
    private ComboBox cmbTargetType;
    private Label lblKeyword;
    private TextBox txtKeyword;
    private Label lblChannel;
    private TextBox txtChannel;
    private Label lblTitle;
    private TextBox txtTitle;
    private Label lblUrl;
    private TextBox txtUrl;
    private Button btnAddTarget;
    private Button btnImportTargets;
    private Button btnRemoveTarget;

    private GroupBox gbReportSettings;
    private Label lblScenario;
    private ComboBox cmbScenario;
    private Label lblMainReason;
    private ComboBox cmbMainReason;
    private Label lblWatchDuration;
    private NumericUpDown numMinWatch;
    private Label lblTo;
    private NumericUpDown numMaxWatch;
    private Label lblCooldown;
    private NumericUpDown numCooldown;
    private Label lblConcurrency;
    private NumericUpDown numConcurrency;
    private CheckBox chkAutoSkipAd;
    private CheckBox chkNaturalEngagement;
    private CheckBox chkRandomWatch;
    private CheckBox chkRotateReasons;
    private CheckBox chkAutoAllocate;

    private Panel pnlLeftBottomButtons;
    private Button btnConnectDevices;
    private Button btnStart;
    private Button btnStop;
    private Button btnExitApp;

    // Right Area Container
    private Panel pnlRight;

    // Right Top Action Bar
    private Panel pnlTopActions;
    private Button btnActionHome;
    private Button btnActionCloseAll;
    private Button btnActionFullScreen;
    private Button btnActionCheckAccount;
    private Button btnActionCheckYoutube;
    private Button btnActionReallocate;
    private Button btnActionResetCache;
    private Button btnActionScanTcp;
    private Button btnActionExportCsv;
    private Button btnActionClearCompleted;

    // Right Upper: Targets / Tasks Grid
    private Panel pnlTasksContainer;
    private DataGridView dgvTasks;

    // Right Middle: Filter Bar
    private Panel pnlMiddleFilter;
    private ComboBox cmbFilterType;
    private Button btnSelectAllTasks;
    private Label lblTaskCountRatio;
    private RadioButton rdoFilterAll;
    private RadioButton rdoFilterDone;
    private RadioButton rdoFilterRunning;
    private RadioButton rdoFilterFailed;
    private Label lblStatusSummary;

    // Right Lower: SplitContainer (Devices Grid + Live Console Log)
    private SplitContainer splitLower;
    private DataGridView dgvDevices;
    private Panel pnlLogContainer;
    private Label lblLogHeader;
    private RichTextBox rtbLiveLog;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.menuStripMain = new MenuStrip();
        this.tsmiMenu = new ToolStripMenuItem();
        this.tsmiOpenLogFolder = new ToolStripMenuItem();
        this.tsmiExportHistory = new ToolStripMenuItem();
        this.tsmiExit = new ToolStripMenuItem();
        this.tsmiTools = new ToolStripMenuItem();
        this.tsmiOpenVpn = new ToolStripMenuItem();
        this.tsmiClearCache = new ToolStripMenuItem();
        this.tsmiLangVi = new ToolStripMenuItem();
        this.tsmiLangKr = new ToolStripMenuItem();
        this.tsmiLangEn = new ToolStripMenuItem();
        this.tsmiHelp = new ToolStripMenuItem();

        this.pnlLeft = new Panel();
        this.gbPlatformAndAuth = new GroupBox();
        this.rdoAndroid = new RadioButton();
        this.rdoChrome = new RadioButton();
        this.txtMailFile = new TextBox();
        this.btnSelectMailFile = new Button();
        this.txtProxyFile = new TextBox();
        this.btnSelectProxyFile = new Button();

        this.gbTargetInput = new GroupBox();
        this.lblTargetType = new Label();
        this.cmbTargetType = new ComboBox();
        this.lblKeyword = new Label();
        this.txtKeyword = new TextBox();
        this.lblChannel = new Label();
        this.txtChannel = new TextBox();
        this.lblTitle = new Label();
        this.txtTitle = new TextBox();
        this.lblUrl = new Label();
        this.txtUrl = new TextBox();
        this.btnAddTarget = new Button();
        this.btnImportTargets = new Button();
        this.btnRemoveTarget = new Button();

        this.gbReportSettings = new GroupBox();
        this.lblScenario = new Label();
        this.cmbScenario = new ComboBox();
        this.lblMainReason = new Label();
        this.cmbMainReason = new ComboBox();
        this.lblWatchDuration = new Label();
        this.numMinWatch = new NumericUpDown();
        this.lblTo = new Label();
        this.numMaxWatch = new NumericUpDown();
        this.lblCooldown = new Label();
        this.numCooldown = new NumericUpDown();
        this.lblConcurrency = new Label();
        this.numConcurrency = new NumericUpDown();
        this.chkAutoSkipAd = new CheckBox();
        this.chkNaturalEngagement = new CheckBox();
        this.chkRandomWatch = new CheckBox();
        this.chkRotateReasons = new CheckBox();
        this.chkAutoAllocate = new CheckBox();

        this.pnlLeftBottomButtons = new Panel();
        this.btnConnectDevices = new Button();
        this.btnStart = new Button();
        this.btnStop = new Button();
        this.btnExitApp = new Button();

        this.pnlRight = new Panel();
        this.pnlTopActions = new Panel();
        this.btnActionHome = new Button();
        this.btnActionCloseAll = new Button();
        this.btnActionFullScreen = new Button();
        this.btnActionCheckAccount = new Button();
        this.btnActionCheckYoutube = new Button();
        this.btnActionReallocate = new Button();
        this.btnActionResetCache = new Button();
        this.btnActionScanTcp = new Button();
        this.btnActionExportCsv = new Button();
        this.btnActionClearCompleted = new Button();

        this.pnlTasksContainer = new Panel();
        this.dgvTasks = new DataGridView();

        this.pnlMiddleFilter = new Panel();
        this.cmbFilterType = new ComboBox();
        this.btnSelectAllTasks = new Button();
        this.lblTaskCountRatio = new Label();
        this.rdoFilterAll = new RadioButton();
        this.rdoFilterDone = new RadioButton();
        this.rdoFilterRunning = new RadioButton();
        this.rdoFilterFailed = new RadioButton();
        this.lblStatusSummary = new Label();

        this.splitLower = new SplitContainer();
        this.dgvDevices = new DataGridView();
        this.pnlLogContainer = new Panel();
        this.lblLogHeader = new Label();
        this.rtbLiveLog = new RichTextBox();

        // 1. MenuStrip
        this.menuStripMain.Items.AddRange(new ToolStripItem[] {
            this.tsmiMenu, this.tsmiTools, this.tsmiLangVi, this.tsmiLangKr, this.tsmiLangEn, this.tsmiHelp
        });
        this.tsmiMenu.Text = "Menu";
        this.tsmiMenu.DropDownItems.AddRange(new ToolStripItem[] {
            this.tsmiOpenLogFolder, this.tsmiExportHistory, new ToolStripSeparator(), this.tsmiExit
        });
        this.tsmiOpenLogFolder.Text = "Mở thư mục Logs";
        this.tsmiExportHistory.Text = "Xuất báo cáo CSV";
        this.tsmiExit.Text = "Thoát";

        this.tsmiTools.Text = "Khác";
        this.tsmiTools.DropDownItems.AddRange(new ToolStripItem[] {
            this.tsmiOpenVpn, this.tsmiClearCache
        });
        this.tsmiOpenVpn.Text = "Cài đặt OpenVPN / Proxy";
        this.tsmiClearCache.Text = "Xóa Cache YouTube";

        this.tsmiLangVi.Text = "VI";
        this.tsmiLangKr.Text = "KR";
        this.tsmiLangEn.Text = "EN";
        this.tsmiHelp.Text = "Help";

        // 2. Left Panel
        this.pnlLeft.Dock = DockStyle.Left;
        this.pnlLeft.Width = 330;
        this.pnlLeft.BackColor = Color.FromArgb(240, 240, 240);
        this.pnlLeft.Padding = new Padding(6);
        this.pnlLeft.AutoScroll = true;

        // Group 1: Auth & Platform
        this.gbPlatformAndAuth.Text = "Đăng nhập & Nền tảng";
        this.gbPlatformAndAuth.Location = new Point(6, 6);
        this.gbPlatformAndAuth.Size = new Size(316, 125);
        this.gbPlatformAndAuth.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        this.rdoAndroid.Text = "Android (ADB)";
        this.rdoAndroid.Location = new Point(10, 20);
        this.rdoAndroid.Size = new Size(130, 22);
        this.rdoAndroid.Checked = true;
        this.rdoAndroid.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        this.rdoChrome.Text = "Chrome (Playwright)";
        this.rdoChrome.Location = new Point(145, 20);
        this.rdoChrome.Size = new Size(160, 22);
        this.rdoChrome.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        var lblMail = new Label { Text = "File tài khoản Gmail:", Location = new Point(10, 48), AutoSize = true };
        this.txtMailFile.Location = new Point(10, 66);
        this.txtMailFile.Size = new Size(230, 23);
        this.txtMailFile.PlaceholderText = "Chọn file account.txt...";
        this.btnSelectMailFile.Text = "Chọn";
        this.btnSelectMailFile.Location = new Point(245, 65);
        this.btnSelectMailFile.Size = new Size(60, 25);

        var lblProxy = new Label { Text = "File Proxy / IP:", Location = new Point(10, 94), AutoSize = true };
        this.txtProxyFile.Location = new Point(10, 93);
        this.txtProxyFile.Size = new Size(230, 23);
        this.txtProxyFile.PlaceholderText = "Chọn file proxy.txt...";
        this.btnSelectProxyFile.Text = "Chọn";
        this.btnSelectProxyFile.Location = new Point(245, 92);
        this.btnSelectProxyFile.Size = new Size(60, 25);

        this.gbPlatformAndAuth.Controls.AddRange(new Control[] {
            this.rdoAndroid, this.rdoChrome, lblMail, this.txtMailFile, this.btnSelectMailFile,
            this.txtProxyFile, this.btnSelectProxyFile
        });

        // Group 2: Target Input
        this.gbTargetInput.Text = "Cài đặt Mục tiêu";
        this.gbTargetInput.Location = new Point(6, 136);
        this.gbTargetInput.Size = new Size(316, 215);

        this.lblTargetType.Text = "Loại mục tiêu:";
        this.lblTargetType.Location = new Point(10, 22);
        this.lblTargetType.AutoSize = true;
        this.cmbTargetType.Location = new Point(95, 19);
        this.cmbTargetType.Size = new Size(210, 23);
        this.cmbTargetType.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbTargetType.Items.AddRange(new object[] { "Video", "Channel (Kênh)", "Shorts", "Comment (Bình luận)" });
        this.cmbTargetType.SelectedIndex = 0;

        this.lblKeyword.Text = "Từ khóa:";
        this.lblKeyword.Location = new Point(10, 52);
        this.lblKeyword.AutoSize = true;
        this.txtKeyword.Location = new Point(95, 49);
        this.txtKeyword.Size = new Size(210, 23);

        this.lblChannel.Text = "Tên kênh:";
        this.lblChannel.Location = new Point(10, 82);
        this.lblChannel.AutoSize = true;
        this.txtChannel.Location = new Point(95, 79);
        this.txtChannel.Size = new Size(210, 23);

        this.lblTitle.Text = "Tên tiêu đề:";
        this.lblTitle.Location = new Point(10, 112);
        this.lblTitle.AutoSize = true;
        this.txtTitle.Location = new Point(95, 109);
        this.txtTitle.Size = new Size(210, 23);

        this.lblUrl.Text = "URL:";
        this.lblUrl.Location = new Point(10, 142);
        this.lblUrl.AutoSize = true;
        this.txtUrl.Location = new Point(95, 139);
        this.txtUrl.Size = new Size(210, 23);

        this.btnAddTarget.Text = "➕ Thêm";
        this.btnAddTarget.Location = new Point(10, 172);
        this.btnAddTarget.Size = new Size(95, 30);
        this.btnAddTarget.BackColor = Color.FromArgb(230, 240, 255);

        this.btnImportTargets.Text = "📂 Nhập file";
        this.btnImportTargets.Location = new Point(112, 172);
        this.btnImportTargets.Size = new Size(95, 30);

        this.btnRemoveTarget.Text = "❌ Xóa";
        this.btnRemoveTarget.Location = new Point(214, 172);
        this.btnRemoveTarget.Size = new Size(91, 30);

        this.gbTargetInput.Controls.AddRange(new Control[] {
            this.lblTargetType, this.cmbTargetType, this.lblKeyword, this.txtKeyword,
            this.lblChannel, this.txtChannel, this.lblTitle, this.txtTitle,
            this.lblUrl, this.txtUrl, this.btnAddTarget, this.btnImportTargets, this.btnRemoveTarget
        });

        // Group 3: Report Settings & Anti-Bot
        this.gbReportSettings.Text = "Bộ lọc & Tiêu chí Báo cáo";
        this.gbReportSettings.Location = new Point(6, 356);
        this.gbReportSettings.Size = new Size(316, 265);

        this.lblScenario.Text = "Kịch bản:";
        this.lblScenario.Location = new Point(10, 22);
        this.lblScenario.AutoSize = true;
        this.cmbScenario.Location = new Point(95, 19);
        this.cmbScenario.Size = new Size(210, 23);
        this.cmbScenario.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbScenario.Items.AddRange(new object[] {
            "1. Tìm kiếm, xem và báo cáo",
            "2. Mở trực tiếp link URL",
            "3. Báo cáo Kênh người dùng",
            "4. Báo cáo video ngắn Shorts",
            "5. Báo cáo bình luận vi phạm"
        });
        this.cmbScenario.SelectedIndex = 0;

        this.lblMainReason.Text = "Lý do chính:";
        this.lblMainReason.Location = new Point(10, 52);
        this.lblMainReason.AutoSize = true;
        this.cmbMainReason.Location = new Point(95, 49);
        this.cmbMainReason.Size = new Size(210, 23);
        this.cmbMainReason.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbMainReason.Items.AddRange(new object[] {
            "Spam hoặc gây hiểu lầm",
            "Hành vi có hại hoặc nguy hiểm",
            "Thông tin sai lệch",
            "Kích động thù địch hoặc lăng mạ",
            "Quấy rối hoặc bắt nạt",
            "Nội dung bạo lực hoặc phản cảm",
            "Nội dung khiêu dâm"
        });
        this.cmbMainReason.SelectedIndex = 0;

        this.lblWatchDuration.Text = "Xem (giây):";
        this.lblWatchDuration.Location = new Point(10, 82);
        this.lblWatchDuration.AutoSize = true;
        this.numMinWatch.Location = new Point(95, 79);
        this.numMinWatch.Size = new Size(55, 23);
        this.numMinWatch.Minimum = 5;
        this.numMinWatch.Maximum = 600;
        this.numMinWatch.Value = 35;

        this.lblTo.Text = "đến";
        this.lblTo.Location = new Point(155, 82);
        this.lblTo.AutoSize = true;
        this.numMaxWatch.Location = new Point(185, 79);
        this.numMaxWatch.Size = new Size(55, 23);
        this.numMaxWatch.Minimum = 10;
        this.numMaxWatch.Maximum = 900;
        this.numMaxWatch.Value = 90;

        this.lblCooldown.Text = "Nghỉ (s):";
        this.lblCooldown.Location = new Point(10, 112);
        this.lblCooldown.AutoSize = true;
        this.numCooldown.Location = new Point(95, 109);
        this.numCooldown.Size = new Size(55, 23);
        this.numCooldown.Minimum = 0;
        this.numCooldown.Maximum = 1800;
        this.numCooldown.Value = 120;

        this.lblConcurrency.Text = "Số luồng:";
        this.lblConcurrency.Location = new Point(160, 112);
        this.lblConcurrency.AutoSize = true;
        this.numConcurrency.Location = new Point(220, 109);
        this.numConcurrency.Size = new Size(55, 23);
        this.numConcurrency.Minimum = 1;
        this.numConcurrency.Maximum = 32;
        this.numConcurrency.Value = 3;

        this.chkAutoSkipAd.Text = "Bỏ qua quảng cáo (Skip Ad)";
        this.chkAutoSkipAd.Location = new Point(10, 140);
        this.chkAutoSkipAd.Size = new Size(200, 20);
        this.chkAutoSkipAd.Checked = true;

        this.chkNaturalEngagement.Text = "Mô phỏng người thật (cuộn đọc comment)";
        this.chkNaturalEngagement.Location = new Point(10, 164);
        this.chkNaturalEngagement.Size = new Size(290, 20);
        this.chkNaturalEngagement.Checked = true;

        this.chkRandomWatch.Text = "Xem ngẫu nhiên (Random watch)";
        this.chkRandomWatch.Location = new Point(10, 188);
        this.chkRandomWatch.Size = new Size(220, 20);
        this.chkRandomWatch.Checked = true;

        this.chkRotateReasons.Text = "Tự động xoay vòng lý do vi phạm";
        this.chkRotateReasons.Location = new Point(10, 212);
        this.chkRotateReasons.Size = new Size(230, 20);
        this.chkRotateReasons.Checked = true;

        this.chkAutoAllocate.Text = "Tự động phân bổ tài khoản & Proxy";
        this.chkAutoAllocate.Location = new Point(10, 236);
        this.chkAutoAllocate.Size = new Size(230, 20);
        this.chkAutoAllocate.Checked = true;

        this.gbReportSettings.Controls.AddRange(new Control[] {
            this.lblScenario, this.cmbScenario, this.lblMainReason, this.cmbMainReason,
            this.lblWatchDuration, this.numMinWatch, this.lblTo, this.numMaxWatch,
            this.lblCooldown, this.numCooldown, this.lblConcurrency, this.numConcurrency,
            this.chkAutoSkipAd, this.chkNaturalEngagement, this.chkRandomWatch,
            this.chkRotateReasons, this.chkAutoAllocate
        });

        // Bottom Controls
        this.pnlLeftBottomButtons.Location = new Point(6, 626);
        this.pnlLeftBottomButtons.Size = new Size(316, 45);

        this.btnConnectDevices.Text = "Quét thiết bị";
        this.btnConnectDevices.Location = new Point(2, 6);
        this.btnConnectDevices.Size = new Size(82, 34);

        this.btnStart.Text = "Bắt đầu";
        this.btnStart.Location = new Point(88, 6);
        this.btnStart.Size = new Size(78, 34);
        this.btnStart.BackColor = Color.FromArgb(235, 235, 235);
        this.btnStart.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        this.btnStop.Text = "Dừng";
        this.btnStop.Location = new Point(170, 6);
        this.btnStop.Size = new Size(70, 34);
        this.btnStop.Enabled = false;

        this.btnExitApp.Text = "Thoát";
        this.btnExitApp.Location = new Point(244, 6);
        this.btnExitApp.Size = new Size(70, 34);

        this.pnlLeftBottomButtons.Controls.AddRange(new Control[] {
            this.btnConnectDevices, this.btnStart, this.btnStop, this.btnExitApp
        });

        this.pnlLeft.Controls.AddRange(new Control[] {
            this.gbPlatformAndAuth, this.gbTargetInput, this.gbReportSettings, this.pnlLeftBottomButtons
        });

        // 3. Right Area
        this.pnlRight.Dock = DockStyle.Fill;
        this.pnlRight.BackColor = Color.White;

        // Top Action Bar
        this.pnlTopActions.Dock = DockStyle.Top;
        this.pnlTopActions.Height = 72;
        this.pnlTopActions.BackColor = Color.FromArgb(245, 245, 245);
        this.pnlTopActions.BorderStyle = BorderStyle.FixedSingle;
        this.pnlTopActions.Padding = new Padding(4);

        this.btnActionHome.Text = "Home";
        this.btnActionHome.Location = new Point(6, 6);
        this.btnActionHome.Size = new Size(75, 28);

        this.btnActionCloseAll.Text = "Đóng tất cả";
        this.btnActionCloseAll.Location = new Point(85, 6);
        this.btnActionCloseAll.Size = new Size(95, 28);

        this.btnActionFullScreen.Text = "Toàn màn hình";
        this.btnActionFullScreen.Location = new Point(184, 6);
        this.btnActionFullScreen.Size = new Size(110, 28);

        this.btnActionCheckAccount.Text = "Kiểm tra tài khoản";
        this.btnActionCheckAccount.Location = new Point(298, 6);
        this.btnActionCheckAccount.Size = new Size(130, 28);

        this.btnActionCheckYoutube.Text = "Kiểm tra trên YouTube";
        this.btnActionCheckYoutube.Location = new Point(432, 6);
        this.btnActionCheckYoutube.Size = new Size(150, 28);

        this.btnActionReallocate.Text = "Phân bổ lại tài khoản";
        this.btnActionReallocate.Location = new Point(586, 6);
        this.btnActionReallocate.Size = new Size(140, 28);

        this.btnActionResetCache.Text = "Xóa cache YouTube";
        this.btnActionResetCache.Location = new Point(6, 38);
        this.btnActionResetCache.Size = new Size(135, 28);

        this.btnActionScanTcp.Text = "Quét gom TCP";
        this.btnActionScanTcp.Location = new Point(145, 38);
        this.btnActionScanTcp.Size = new Size(110, 28);

        this.btnActionExportCsv.Text = "Xuất báo cáo CSV";
        this.btnActionExportCsv.Location = new Point(259, 38);
        this.btnActionExportCsv.Size = new Size(125, 28);

        this.btnActionClearCompleted.Text = "Dọn tác vụ xong";
        this.btnActionClearCompleted.Location = new Point(388, 38);
        this.btnActionClearCompleted.Size = new Size(120, 28);

        this.pnlTopActions.Controls.AddRange(new Control[] {
            this.btnActionHome, this.btnActionCloseAll, this.btnActionFullScreen,
            this.btnActionCheckAccount, this.btnActionCheckYoutube, this.btnActionReallocate,
            this.btnActionResetCache, this.btnActionScanTcp, this.btnActionExportCsv, this.btnActionClearCompleted
        });

        // Upper Section: Tasks Grid
        this.pnlTasksContainer.Dock = DockStyle.Top;
        this.pnlTasksContainer.Height = 220;

        this.dgvTasks.Dock = DockStyle.Fill;
        this.dgvTasks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvTasks.AllowUserToAddRows = false;
        this.dgvTasks.ReadOnly = true;
        this.dgvTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvTasks.BackgroundColor = Color.White;
        this.dgvTasks.RowHeadersVisible = false;

        this.dgvTasks.Columns.Add("COL_NUM", "STT");
        this.dgvTasks.Columns["COL_NUM"].Width = 45;
        this.dgvTasks.Columns.Add("COL_TYPE", "Loại");
        this.dgvTasks.Columns["COL_TYPE"].Width = 70;
        this.dgvTasks.Columns.Add("COL_KEYWORD", "Từ khóa");
        this.dgvTasks.Columns.Add("COL_CHANNEL", "Tên kênh");
        this.dgvTasks.Columns.Add("COL_TITLE", "Tên tiêu đề");
        this.dgvTasks.Columns.Add("COL_URL", "URL YouTube");
        this.dgvTasks.Columns.Add("COL_INTERVAL", "Khoảng chạy");
        this.dgvTasks.Columns["COL_INTERVAL"].Width = 90;
        this.dgvTasks.Columns.Add("COL_PROGRESS", "Đã chạy");
        this.dgvTasks.Columns["COL_PROGRESS"].Width = 75;
        this.dgvTasks.Columns.Add("COL_STATUS", "Trạng thái");
        this.dgvTasks.Columns["COL_STATUS"].Width = 90;

        this.pnlTasksContainer.Controls.Add(this.dgvTasks);

        // Middle: Filter Bar
        this.pnlMiddleFilter.Dock = DockStyle.Top;
        this.pnlMiddleFilter.Height = 36;
        this.pnlMiddleFilter.BackColor = Color.FromArgb(245, 245, 245);
        this.pnlMiddleFilter.BorderStyle = BorderStyle.FixedSingle;
        this.pnlMiddleFilter.Padding = new Padding(4);

        this.cmbFilterType.Location = new Point(6, 5);
        this.cmbFilterType.Size = new Size(80, 23);
        this.cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbFilterType.Items.AddRange(new object[] { "Tất cả", "Video", "Kênh", "Shorts" });
        this.cmbFilterType.SelectedIndex = 0;

        this.btnSelectAllTasks.Text = "Chọn";
        this.btnSelectAllTasks.Location = new Point(92, 4);
        this.btnSelectAllTasks.Size = new Size(55, 25);

        this.lblTaskCountRatio.Text = "0/0";
        this.lblTaskCountRatio.Location = new Point(152, 8);
        this.lblTaskCountRatio.AutoSize = true;

        this.rdoFilterAll.Text = "Tất cả";
        this.rdoFilterAll.Location = new Point(230, 6);
        this.rdoFilterAll.Size = new Size(65, 22);
        this.rdoFilterAll.Checked = true;

        this.rdoFilterDone.Text = "Hoàn thành";
        this.rdoFilterDone.Location = new Point(300, 6);
        this.rdoFilterDone.Size = new Size(95, 22);

        this.rdoFilterRunning.Text = "Đang chạy";
        this.rdoFilterRunning.Location = new Point(400, 6);
        this.rdoFilterRunning.Size = new Size(90, 22);

        this.rdoFilterFailed.Text = "Thất bại";
        this.rdoFilterFailed.Location = new Point(495, 6);
        this.rdoFilterFailed.Size = new Size(75, 22);

        this.lblStatusSummary.Text = "0/0/0 - 0/0";
        this.lblStatusSummary.Location = new Point(680, 8);
        this.lblStatusSummary.AutoSize = true;
        this.lblStatusSummary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        this.pnlMiddleFilter.Controls.AddRange(new Control[] {
            this.cmbFilterType, this.btnSelectAllTasks, this.lblTaskCountRatio,
            this.rdoFilterAll, this.rdoFilterDone, this.rdoFilterRunning, this.rdoFilterFailed,
            this.lblStatusSummary
        });

        // Lower Section: SplitContainer (Devices Grid + Live Console Log)
        this.splitLower.Dock = DockStyle.Fill;
        this.splitLower.Orientation = Orientation.Vertical;
        this.splitLower.SplitterDistance = 640;

        // Lower Left: Devices Grid
        this.dgvDevices.Dock = DockStyle.Fill;
        this.dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvDevices.AllowUserToAddRows = false;
        this.dgvDevices.ReadOnly = true;
        this.dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvDevices.BackgroundColor = Color.White;
        this.dgvDevices.RowHeadersVisible = false;

        this.dgvDevices.Columns.Add("DEV_SEL", "Chọn");
        this.dgvDevices.Columns["DEV_SEL"].Width = 45;
        this.dgvDevices.Columns.Add("DEV_SERIAL", "Serial thiết bị / IP");
        this.dgvDevices.Columns.Add("DEV_MODEL", "Thiết bị");
        this.dgvDevices.Columns.Add("DEV_MAIL", "Tài khoản");
        this.dgvDevices.Columns.Add("DEV_PROXY", "Proxy / IP");
        this.dgvDevices.Columns.Add("DEV_TARGET", "Mục tiêu đang chạy");
        this.dgvDevices.Columns.Add("DEV_STATUS", "Trạng thái");

        this.splitLower.Panel1.Controls.Add(this.dgvDevices);

        // Lower Right: Live Log
        this.pnlLogContainer.Dock = DockStyle.Fill;
        this.lblLogHeader.Text = "Nhật Ký Thực Thi Thời Gian Thực (Live Log)";
        this.lblLogHeader.Dock = DockStyle.Top;
        this.lblLogHeader.Height = 24;
        this.lblLogHeader.BackColor = Color.FromArgb(235, 235, 235);
        this.lblLogHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        this.lblLogHeader.TextAlign = ContentAlignment.MiddleLeft;
        this.lblLogHeader.Padding = new Padding(6, 0, 0, 0);

        this.rtbLiveLog.Dock = DockStyle.Fill;
        this.rtbLiveLog.ReadOnly = true;
        this.rtbLiveLog.BackColor = Color.FromArgb(28, 28, 28);
        this.rtbLiveLog.ForeColor = Color.Gainsboro;
        this.rtbLiveLog.Font = new Font("Consolas", 9F);

        this.pnlLogContainer.Controls.Add(this.rtbLiveLog);
        this.pnlLogContainer.Controls.Add(this.lblLogHeader);
        this.splitLower.Panel2.Controls.Add(this.pnlLogContainer);

        this.pnlRight.Controls.Add(this.splitLower);
        this.pnlRight.Controls.Add(this.pnlMiddleFilter);
        this.pnlRight.Controls.Add(this.pnlTasksContainer);
        this.pnlRight.Controls.Add(this.pnlTopActions);

        // Form settings
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1366, 740);
        this.Controls.Add(this.pnlRight);
        this.Controls.Add(this.pnlLeft);
        this.Controls.Add(this.menuStripMain);
        this.MainMenuStrip = this.menuStripMain;
        this.Text = "YouTube Auto Report - 1.0.0 - AutoYouTube Report Macro";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1180, 650);
    }
}
