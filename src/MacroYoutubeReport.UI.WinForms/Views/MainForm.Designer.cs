namespace MacroYoutubeReport.UI.WinForms.Views;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Top MenuStrip
    private MenuStrip menuStripMain;
    private ToolStripMenuItem tsmiMenu;
    private ToolStripMenuItem tsmiOpenLogFolder;
    private ToolStripMenuItem tsmiExportHistory;
    private ToolStripMenuItem tsmiImportAccounts;
    private ToolStripMenuItem tsmiImportProxies;
    private ToolStripMenuItem tsmiExit;
    private ToolStripMenuItem tsmiTools;
    private ToolStripMenuItem tsmiClearCache;
    private ToolStripMenuItem tsmiRestartAdb;
    private ToolStripMenuItem tsmiAdbCmd;
    private ToolStripMenuItem tsmiLangVi;
    private ToolStripMenuItem tsmiLangKr;
    private ToolStripMenuItem tsmiLangEn;
    private ToolStripMenuItem tsmiHelp;
    private ToolStripMenuItem tsmiUserGuide;
    private ToolStripMenuItem tsmiAbout;

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
    private Button btnMoveUp;
    private Button btnMoveDown;
    private Button btnCheckAll;
    private RadioButton rdoFilterAll;
    private RadioButton rdoFilterDone;
    private RadioButton rdoFilterRunning;
    private RadioButton rdoFilterFailed;
    private Button btnFind;
    private Button btnRemoveDup;
    private Button btnDuplicate;
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
        this.tsmiImportAccounts = new ToolStripMenuItem();
        this.tsmiImportProxies = new ToolStripMenuItem();
        this.tsmiExit = new ToolStripMenuItem();
        this.tsmiTools = new ToolStripMenuItem();
        this.tsmiClearCache = new ToolStripMenuItem();
        this.tsmiRestartAdb = new ToolStripMenuItem();
        this.tsmiAdbCmd = new ToolStripMenuItem();
        this.tsmiLangVi = new ToolStripMenuItem();
        this.tsmiLangKr = new ToolStripMenuItem();
        this.tsmiLangEn = new ToolStripMenuItem();
        this.tsmiHelp = new ToolStripMenuItem();
        this.tsmiUserGuide = new ToolStripMenuItem();
        this.tsmiAbout = new ToolStripMenuItem();

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
        this.btnMoveUp = new Button();
        this.btnMoveDown = new Button();
        this.btnCheckAll = new Button();
        this.rdoFilterAll = new RadioButton();
        this.rdoFilterDone = new RadioButton();
        this.rdoFilterRunning = new RadioButton();
        this.rdoFilterFailed = new RadioButton();
        this.btnFind = new Button();
        this.btnRemoveDup = new Button();
        this.btnDuplicate = new Button();
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
        this.menuStripMain.Dock = DockStyle.Top;
        this.menuStripMain.BackColor = Color.FromArgb(245, 245, 245);

        this.tsmiMenu.Text = "Menu";
        this.tsmiMenu.DropDownItems.AddRange(new ToolStripItem[] {
            this.tsmiOpenLogFolder, this.tsmiExportHistory, new ToolStripSeparator(),
            this.tsmiImportAccounts, this.tsmiImportProxies, new ToolStripSeparator(),
            this.tsmiExit
        });
        this.tsmiOpenLogFolder.Text = "Mở thư mục Logs";
        this.tsmiExportHistory.Text = "Xuất báo cáo CSV";
        this.tsmiImportAccounts.Text = "Nhập tài khoản Gmail...";
        this.tsmiImportProxies.Text = "Nhập danh sách Proxy...";
        this.tsmiExit.Text = "Thoát";

        this.tsmiTools.Text = "Khác";
        this.tsmiTools.DropDownItems.AddRange(new ToolStripItem[] {
            this.tsmiClearCache, this.tsmiRestartAdb, this.tsmiAdbCmd
        });
        this.tsmiClearCache.Text = "Xóa Cache ứng dụng YouTube";
        this.tsmiRestartAdb.Text = "Khởi động lại ADB Server";
        this.tsmiAdbCmd.Text = "Lệnh ADB Console...";

        this.tsmiLangVi.Text = "VI";
        this.tsmiLangKr.Text = "KR";
        this.tsmiLangEn.Text = "EN";

        this.tsmiHelp.Text = "Help";
        this.tsmiHelp.DropDownItems.AddRange(new ToolStripItem[] {
            this.tsmiUserGuide, this.tsmiAbout
        });
        this.tsmiUserGuide.Text = "Hướng dẫn sử dụng";
        this.tsmiAbout.Text = "Thông tin phiên bản";

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
        this.rdoAndroid.Location = new Point(10, 22);
        this.rdoAndroid.Size = new Size(130, 20);
        this.rdoAndroid.Checked = true;

        this.rdoChrome.Text = "Chrome (Playwright)";
        this.rdoChrome.Location = new Point(145, 22);
        this.rdoChrome.Size = new Size(160, 20);

        this.txtMailFile.Location = new Point(10, 52);
        this.txtMailFile.Size = new Size(225, 23);
        this.txtMailFile.PlaceholderText = "Chọn file account.txt...";

        this.btnSelectMailFile.Text = "Chọn";
        this.btnSelectMailFile.Location = new Point(240, 51);
        this.btnSelectMailFile.Size = new Size(65, 25);

        this.txtProxyFile.Location = new Point(10, 84);
        this.txtProxyFile.Size = new Size(225, 23);
        this.txtProxyFile.PlaceholderText = "Chọn file proxy.txt...";

        this.btnSelectProxyFile.Text = "Chọn";
        this.btnSelectProxyFile.Location = new Point(240, 83);
        this.btnSelectProxyFile.Size = new Size(65, 25);

        this.gbPlatformAndAuth.Controls.AddRange(new Control[] {
            this.rdoAndroid, this.rdoChrome, this.txtMailFile, this.btnSelectMailFile, this.txtProxyFile, this.btnSelectProxyFile
        });

        // Group 2: Target Input
        this.gbTargetInput.Text = "Cài đặt Mục tiêu";
        this.gbTargetInput.Location = new Point(6, 137);
        this.gbTargetInput.Size = new Size(316, 215);

        this.lblTargetType.Text = "Loại:";
        this.lblTargetType.Location = new Point(10, 25);
        this.lblTargetType.AutoSize = true;

        this.cmbTargetType.Location = new Point(80, 22);
        this.cmbTargetType.Size = new Size(225, 23);
        this.cmbTargetType.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbTargetType.Items.AddRange(new object[] { "Video", "Kênh (Channel)", "Shorts" });
        this.cmbTargetType.SelectedIndex = 0;

        this.lblKeyword.Text = "Từ khóa:";
        this.lblKeyword.Location = new Point(10, 55);
        this.lblKeyword.AutoSize = true;
        this.txtKeyword.Location = new Point(80, 52);
        this.txtKeyword.Size = new Size(225, 23);

        this.lblChannel.Text = "Tên kênh:";
        this.lblChannel.Location = new Point(10, 85);
        this.lblChannel.AutoSize = true;
        this.txtChannel.Location = new Point(80, 82);
        this.txtChannel.Size = new Size(225, 23);

        this.lblTitle.Text = "Tiêu đề:";
        this.lblTitle.Location = new Point(10, 115);
        this.lblTitle.AutoSize = true;
        this.txtTitle.Location = new Point(80, 112);
        this.txtTitle.Size = new Size(225, 23);

        this.lblUrl.Text = "URL:";
        this.lblUrl.Location = new Point(10, 145);
        this.lblUrl.AutoSize = true;
        this.txtUrl.Location = new Point(80, 142);
        this.txtUrl.Size = new Size(225, 23);

        this.btnAddTarget.Text = "+ Thêm";
        this.btnAddTarget.Location = new Point(10, 175);
        this.btnAddTarget.Size = new Size(88, 30);

        this.btnImportTargets.Text = "Nhập file";
        this.btnImportTargets.Location = new Point(105, 175);
        this.btnImportTargets.Size = new Size(100, 30);

        this.btnRemoveTarget.Text = "Xóa";
        this.btnRemoveTarget.Location = new Point(212, 175);
        this.btnRemoveTarget.Size = new Size(93, 30);

        this.gbTargetInput.Controls.AddRange(new Control[] {
            this.lblTargetType, this.cmbTargetType, this.lblKeyword, this.txtKeyword,
            this.lblChannel, this.txtChannel, this.lblTitle, this.txtTitle, this.lblUrl, this.txtUrl,
            this.btnAddTarget, this.btnImportTargets, this.btnRemoveTarget
        });

        // Group 3: Report & Anti-bot Settings
        this.gbReportSettings.Text = "Bộ lọc Tiêu chí Báo cáo";
        this.gbReportSettings.Location = new Point(6, 358);
        this.gbReportSettings.Size = new Size(316, 270);

        this.lblScenario.Text = "Kịch bản:";
        this.lblScenario.Location = new Point(10, 25);
        this.lblScenario.AutoSize = true;
        this.cmbScenario.Location = new Point(80, 22);
        this.cmbScenario.Size = new Size(225, 23);
        this.cmbScenario.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbScenario.Items.AddRange(new object[] {
            "1. Tìm kiếm, xem và báo cáo",
            "2. Truy cập URL trực tiếp và báo cáo",
            "3. Báo cáo kênh vi phạm bản quyền/spam",
            "4. Báo cáo video Shorts",
            "5. Báo cáo bình luận độc hại"
        });
        this.cmbScenario.SelectedIndex = 0;

        this.lblMainReason.Text = "Lý do:";
        this.lblMainReason.Location = new Point(10, 53);
        this.lblMainReason.AutoSize = true;
        this.cmbMainReason.Location = new Point(80, 50);
        this.cmbMainReason.Size = new Size(225, 23);
        this.cmbMainReason.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbMainReason.Items.AddRange(new object[] {
            "Spam hoặc gây hiểu lầm",
            "Hành vi có hại hoặc nguy hiểm",
            "Thông tin sai lệch",
            "Nội dung kích động thù địch",
            "Quấy rối hoặc bắt nạt",
            "Bạo lực hoặc phản cảm",
            "Nội dung khiêu dâm",
            "Lạm dụng trẻ em",
            "Khủng bố",
            "Vi phạm bản quyền của tôi"
        });
        this.cmbMainReason.SelectedIndex = 0;

        this.lblWatchDuration.Text = "Xem (giây):";
        this.lblWatchDuration.Location = new Point(10, 82);
        this.lblWatchDuration.AutoSize = true;
        this.numMinWatch.Location = new Point(80, 79);
        this.numMinWatch.Size = new Size(55, 23);
        this.numMinWatch.Minimum = 5;
        this.numMinWatch.Maximum = 600;
        this.numMinWatch.Value = 35;

        this.lblTo.Text = "đến";
        this.lblTo.Location = new Point(140, 82);
        this.lblTo.AutoSize = true;
        this.numMaxWatch.Location = new Point(170, 79);
        this.numMaxWatch.Size = new Size(55, 23);
        this.numMaxWatch.Minimum = 10;
        this.numMaxWatch.Maximum = 900;
        this.numMaxWatch.Value = 90;

        this.lblCooldown.Text = "Nghỉ (s):";
        this.lblCooldown.Location = new Point(10, 112);
        this.lblCooldown.AutoSize = true;
        this.numCooldown.Location = new Point(80, 109);
        this.numCooldown.Size = new Size(55, 23);
        this.numCooldown.Minimum = 0;
        this.numCooldown.Maximum = 1800;
        this.numCooldown.Value = 120;

        this.lblConcurrency.Text = "Luồng:";
        this.lblConcurrency.Location = new Point(145, 112);
        this.lblConcurrency.AutoSize = true;
        this.numConcurrency.Location = new Point(190, 109);
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

        this.chkAutoAllocate.Text = "Tự động phân bổ tài khoản Proxy";
        this.chkAutoAllocate.Location = new Point(10, 236);
        this.chkAutoAllocate.Size = new Size(230, 20);
        this.chkAutoAllocate.Checked = true;

        this.gbReportSettings.Controls.AddRange(new Control[] {
            this.lblScenario, this.cmbScenario, this.lblMainReason, this.cmbMainReason,
            this.lblWatchDuration, this.numMinWatch, this.lblTo, this.numMaxWatch,
            this.lblCooldown, this.numCooldown, this.lblConcurrency, this.numConcurrency,
            this.chkAutoSkipAd, this.chkNaturalEngagement, this.chkRandomWatch, this.chkRotateReasons, this.chkAutoAllocate
        });

        // Left Bottom Action Buttons
        this.pnlLeftBottomButtons.Location = new Point(6, 634);
        this.pnlLeftBottomButtons.Size = new Size(316, 50);

        this.btnConnectDevices.Text = "Quét thiết bị";
        this.btnConnectDevices.Location = new Point(0, 8);
        this.btnConnectDevices.Size = new Size(80, 36);

        this.btnStart.Text = "Bắt đầu";
        this.btnStart.Location = new Point(84, 8);
        this.btnStart.Size = new Size(76, 36);
        this.btnStart.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        this.btnStop.Text = "Dừng";
        this.btnStop.Location = new Point(164, 8);
        this.btnStop.Size = new Size(70, 36);
        this.btnStop.Enabled = false;

        this.btnExitApp.Text = "Thoát";
        this.btnExitApp.Location = new Point(238, 8);
        this.btnExitApp.Size = new Size(78, 36);

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
        this.pnlTasksContainer.Height = 230;

        this.dgvTasks.Dock = DockStyle.Fill;
        this.dgvTasks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvTasks.AllowUserToAddRows = false;
        this.dgvTasks.ReadOnly = false;
        this.dgvTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvTasks.BackgroundColor = Color.White;
        this.dgvTasks.RowHeadersVisible = false;

        // Action button columns in Tasks Grid
        var colActionStart = new DataGridViewButtonColumn
        {
            Name = "COL_ACTION_START",
            HeaderText = "Bắt đầu",
            Text = "Bắt đầu",
            UseColumnTextForButtonValue = true,
            Width = 65,
            ReadOnly = true
        };
        var colActionRestart = new DataGridViewButtonColumn
        {
            Name = "COL_ACTION_RESTART",
            HeaderText = "Khởi động lại",
            Text = "Khởi động lại",
            UseColumnTextForButtonValue = true,
            Width = 90,
            ReadOnly = true
        };
        var colActionStop = new DataGridViewButtonColumn
        {
            Name = "COL_ACTION_STOP",
            HeaderText = "Dừng",
            Text = "Dừng",
            UseColumnTextForButtonValue = true,
            Width = 55,
            ReadOnly = true
        };
        var colActionDel = new DataGridViewButtonColumn
        {
            Name = "COL_ACTION_DEL",
            HeaderText = "Xóa",
            Text = "Xóa",
            UseColumnTextForButtonValue = true,
            Width = 55,
            ReadOnly = true
        };

        this.dgvTasks.Columns.Add(colActionStart);
        this.dgvTasks.Columns.Add(colActionRestart);
        this.dgvTasks.Columns.Add(colActionStop);
        this.dgvTasks.Columns.Add(colActionDel);

        this.dgvTasks.Columns.Add("COL_DEV_COUNT", "Số thiết bị");
        this.dgvTasks.Columns["COL_DEV_COUNT"].Width = 70;
        this.dgvTasks.Columns["COL_DEV_COUNT"].ReadOnly = true;

        this.dgvTasks.Columns.Add("COL_KEYWORD", "Từ khóa");
        this.dgvTasks.Columns["COL_KEYWORD"].ReadOnly = true;

        this.dgvTasks.Columns.Add("COL_CHANNEL", "Tên kênh");
        this.dgvTasks.Columns["COL_CHANNEL"].ReadOnly = true;

        this.dgvTasks.Columns.Add("COL_TITLE", "Tên tiêu đề");
        this.dgvTasks.Columns["COL_TITLE"].ReadOnly = true;

        this.dgvTasks.Columns.Add("COL_URL", "URL YouTube");
        this.dgvTasks.Columns["COL_URL"].ReadOnly = true;

        this.dgvTasks.Columns.Add("COL_INTERVAL", "Khoảng chạy");
        this.dgvTasks.Columns["COL_INTERVAL"].Width = 85;
        this.dgvTasks.Columns["COL_INTERVAL"].ReadOnly = true;

        var colRandStart = new DataGridViewCheckBoxColumn
        {
            Name = "COL_RANDOM_START",
            HeaderText = "Bắt đầu ngẫu nhiên",
            Width = 70,
            TrueValue = true,
            FalseValue = false,
            ReadOnly = false
        };
        this.dgvTasks.Columns.Add(colRandStart);

        var colDirect = new DataGridViewCheckBoxColumn
        {
            Name = "COL_DIRECT",
            HeaderText = "Trực tiếp",
            Width = 60,
            TrueValue = true,
            FalseValue = false,
            ReadOnly = false
        };
        this.dgvTasks.Columns.Add(colDirect);

        this.dgvTasks.Columns.Add("COL_PROGRESS", "Đã chạy");
        this.dgvTasks.Columns["COL_PROGRESS"].Width = 70;
        this.dgvTasks.Columns["COL_PROGRESS"].ReadOnly = true;

        this.dgvTasks.Columns.Add("COL_STATUS", "Trạng thái");
        this.dgvTasks.Columns["COL_STATUS"].Width = 90;
        this.dgvTasks.Columns["COL_STATUS"].ReadOnly = true;

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

        this.btnMoveUp.Text = "▲";
        this.btnMoveUp.Location = new Point(185, 4);
        this.btnMoveUp.Size = new Size(28, 25);

        this.btnMoveDown.Text = "▼";
        this.btnMoveDown.Location = new Point(216, 4);
        this.btnMoveDown.Size = new Size(28, 25);

        this.btnCheckAll.Text = "✓";
        this.btnCheckAll.Location = new Point(247, 4);
        this.btnCheckAll.Size = new Size(28, 25);

        this.rdoFilterAll.Text = "Tất cả";
        this.rdoFilterAll.Location = new Point(285, 6);
        this.rdoFilterAll.Size = new Size(65, 22);
        this.rdoFilterAll.Checked = true;

        this.rdoFilterDone.Text = "Hoàn thành";
        this.rdoFilterDone.Location = new Point(352, 6);
        this.rdoFilterDone.Size = new Size(92, 22);

        this.rdoFilterRunning.Text = "Đang chạy";
        this.rdoFilterRunning.Location = new Point(446, 6);
        this.rdoFilterRunning.Size = new Size(85, 22);

        this.rdoFilterFailed.Text = "Thất bại";
        this.rdoFilterFailed.Location = new Point(533, 6);
        this.rdoFilterFailed.Size = new Size(72, 22);

        this.btnFind.Text = "Tìm";
        this.btnFind.Location = new Point(610, 4);
        this.btnFind.Size = new Size(45, 25);

        this.btnRemoveDup.Text = "Trùng";
        this.btnRemoveDup.Location = new Point(658, 4);
        this.btnRemoveDup.Size = new Size(52, 25);

        this.btnDuplicate.Text = "Sao chép";
        this.btnDuplicate.Location = new Point(713, 4);
        this.btnDuplicate.Size = new Size(68, 25);

        this.lblStatusSummary.Text = "0/0/0 - 0/0";
        this.lblStatusSummary.Location = new Point(790, 8);
        this.lblStatusSummary.AutoSize = true;
        this.lblStatusSummary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        this.pnlMiddleFilter.Controls.AddRange(new Control[] {
            this.cmbFilterType, this.btnSelectAllTasks, this.lblTaskCountRatio,
            this.btnMoveUp, this.btnMoveDown, this.btnCheckAll,
            this.rdoFilterAll, this.rdoFilterDone, this.rdoFilterRunning, this.rdoFilterFailed,
            this.btnFind, this.btnRemoveDup, this.btnDuplicate,
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
        this.dgvDevices.ReadOnly = false;
        this.dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvDevices.BackgroundColor = Color.White;
        this.dgvDevices.RowHeadersVisible = false;

        // CheckBox column for device selection
        var colDevSel = new DataGridViewCheckBoxColumn
        {
            Name = "DEV_SEL",
            HeaderText = "Chọn",
            Width = 48,
            TrueValue = true,
            FalseValue = false,
            ReadOnly = false
        };
        this.dgvDevices.Columns.Add(colDevSel);

        this.dgvDevices.Columns.Add("DEV_NUM", "STT");
        this.dgvDevices.Columns["DEV_NUM"].Width = 40;
        this.dgvDevices.Columns["DEV_NUM"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_IP", "IP / Proxy");
        this.dgvDevices.Columns["DEV_IP"].Width = 120;
        this.dgvDevices.Columns["DEV_IP"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_SERIAL", "Serial thiết bị");
        this.dgvDevices.Columns["DEV_SERIAL"].Width = 125;
        this.dgvDevices.Columns["DEV_SERIAL"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_MODEL", "Thiết bị");
        this.dgvDevices.Columns["DEV_MODEL"].Width = 85;
        this.dgvDevices.Columns["DEV_MODEL"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_MAIL", "Tài khoản");
        this.dgvDevices.Columns["DEV_MAIL"].Width = 110;
        this.dgvDevices.Columns["DEV_MAIL"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_PASS", "Mật khẩu");
        this.dgvDevices.Columns["DEV_PASS"].Width = 80;
        this.dgvDevices.Columns["DEV_PASS"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_TARGET", "Mục tiêu đang chạy");
        this.dgvDevices.Columns["DEV_TARGET"].ReadOnly = true;

        this.dgvDevices.Columns.Add("DEV_STATUS", "Trạng thái");
        this.dgvDevices.Columns["DEV_STATUS"].Width = 85;
        this.dgvDevices.Columns["DEV_STATUS"].ReadOnly = true;

        this.splitLower.Panel1.Controls.Add(this.dgvDevices);

        // Lower Right: Live Log (White background matching UI)
        this.pnlLogContainer.Dock = DockStyle.Fill;
        this.pnlLogContainer.BackColor = Color.White;

        this.lblLogHeader.Text = "Nhật Ký Thực Thi Thời Gian Thực (Live Log)";
        this.lblLogHeader.Dock = DockStyle.Top;
        this.lblLogHeader.Height = 26;
        this.lblLogHeader.BackColor = Color.FromArgb(245, 245, 245);
        this.lblLogHeader.ForeColor = Color.FromArgb(40, 40, 40);
        this.lblLogHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        this.lblLogHeader.TextAlign = ContentAlignment.MiddleLeft;
        this.lblLogHeader.Padding = new Padding(6, 0, 0, 0);

        this.rtbLiveLog.Dock = DockStyle.Fill;
        this.rtbLiveLog.ReadOnly = true;
        this.rtbLiveLog.BackColor = Color.White;
        this.rtbLiveLog.ForeColor = Color.FromArgb(33, 37, 41);
        this.rtbLiveLog.Font = new Font("Consolas", 9F);
        this.rtbLiveLog.BorderStyle = BorderStyle.None;

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

        // Ensure menu is brought to front
        this.menuStripMain.BringToFront();
    }
}
