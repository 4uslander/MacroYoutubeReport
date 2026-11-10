namespace MacroYoutubeReport.UI.WinForms.Views;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Controls
    private Panel pnlHeader;
    private RadioButton rdoAndroid;
    private RadioButton rdoChrome;
    private NumericUpDown numConcurrency;
    private Label lblConcurrency;
    private Button btnStart;
    private Button btnStop;
    private Label lblStatusHeader;

    private TabControl tabMain;
    private TabPage tabTargets;
    private TabPage tabScenario;
    private TabPage tabLiveMonitor;
    private TabPage tabDevices;
    private TabPage tabHistory;

    // Tab Targets
    private DataGridView dgvTargets;
    private ComboBox cmbTargetType;
    private TextBox txtKeyword;
    private TextBox txtTitle;
    private TextBox txtChannel;
    private TextBox txtUrl;
    private Button btnAddTarget;
    private Button btnImportTargets;
    private Button btnRemoveTarget;
    private Button btnClearTargets;

    // Tab Scenario
    private ComboBox cmbScenario;
    private NumericUpDown numMinWatch;
    private NumericUpDown numMaxWatch;
    private NumericUpDown numCooldown;
    private CheckBox chkNaturalEngagement;
    private CheckBox chkAutoSkipAd;
    private CheckBox chkRotateReasons;
    private CheckedListBox clbReasons;
    private TextBox txtSpinTemplates;

    // Tab Live Monitor
    private SplitContainer splitMonitor;
    private RichTextBox rtbLog;
    private PictureBox picProof;
    private Label lblPending;
    private Label lblRunning;
    private Label lblSuccess;
    private Label lblFailed;

    // Tab Devices
    private SplitContainer splitDevices;
    private DataGridView dgvAndroidDevices;
    private Button btnRefreshDevices;
    private ListBox lstChromeProfiles;
    private TextBox txtNewProfile;
    private Button btnAddProfile;

    // Tab History
    private DataGridView dgvHistory;
    private Button btnRefreshHistory;
    private Button btnExportCsv;

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
        this.pnlHeader = new Panel();
        this.rdoAndroid = new RadioButton();
        this.rdoChrome = new RadioButton();
        this.lblConcurrency = new Label();
        this.numConcurrency = new NumericUpDown();
        this.btnStart = new Button();
        this.btnStop = new Button();
        this.lblStatusHeader = new Label();

        this.tabMain = new TabControl();
        this.tabTargets = new TabPage();
        this.tabScenario = new TabPage();
        this.tabLiveMonitor = new TabPage();
        this.tabDevices = new TabPage();
        this.tabHistory = new TabPage();

        // 1. Header Panel
        this.pnlHeader.Dock = DockStyle.Top;
        this.pnlHeader.Height = 65;
        this.pnlHeader.BackColor = Color.FromArgb(245, 247, 250);
        this.pnlHeader.Padding = new Padding(10);

        this.rdoAndroid.Text = "Android (ADB)";
        this.rdoAndroid.Location = new Point(15, 20);
        this.rdoAndroid.Size = new Size(130, 24);
        this.rdoAndroid.Checked = true;
        this.rdoAndroid.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        this.rdoChrome.Text = "Chrome (Playwright)";
        this.rdoChrome.Location = new Point(155, 20);
        this.rdoChrome.Size = new Size(170, 24);
        this.rdoChrome.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        this.lblConcurrency.Text = "Số luồng chạy:";
        this.lblConcurrency.Location = new Point(340, 22);
        this.lblConcurrency.AutoSize = true;

        this.numConcurrency.Location = new Point(440, 20);
        this.numConcurrency.Size = new Size(60, 25);
        this.numConcurrency.Minimum = 1;
        this.numConcurrency.Maximum = 32;
        this.numConcurrency.Value = 3;

        this.btnStart.Text = "▶ BẮT ĐẦU CHẠY";
        this.btnStart.Location = new Point(530, 12);
        this.btnStart.Size = new Size(160, 40);
        this.btnStart.BackColor = Color.FromArgb(40, 167, 69);
        this.btnStart.ForeColor = Color.White;
        this.btnStart.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        this.btnStart.FlatStyle = FlatStyle.Flat;

        this.btnStop.Text = "⏹ DỪNG LẠI";
        this.btnStop.Location = new Point(705, 12);
        this.btnStop.Size = new Size(130, 40);
        this.btnStop.BackColor = Color.FromArgb(220, 53, 69);
        this.btnStop.ForeColor = Color.White;
        this.btnStop.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        this.btnStop.FlatStyle = FlatStyle.Flat;
        this.btnStop.Enabled = false;

        this.lblStatusHeader.Text = "Trạng thái: Sẵn sàng";
        this.lblStatusHeader.Location = new Point(855, 22);
        this.lblStatusHeader.AutoSize = true;
        this.lblStatusHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
        this.lblStatusHeader.ForeColor = Color.DarkSlateGray;

        this.pnlHeader.Controls.AddRange(new Control[] {
            this.rdoAndroid, this.rdoChrome, this.lblConcurrency, this.numConcurrency,
            this.btnStart, this.btnStop, this.lblStatusHeader
        });

        // 2. Tab Control
        this.tabMain.Dock = DockStyle.Fill;
        this.tabMain.Font = new Font("Segoe UI", 9.5F);

        // Tab 1: Targets
        InitTargetsTab();

        // Tab 2: Scenario
        InitScenarioTab();

        // Tab 3: Live Monitor
        InitMonitorTab();

        // Tab 4: Devices
        InitDevicesTab();

        // Tab 5: History
        InitHistoryTab();

        this.tabMain.TabPages.AddRange(new TabPage[] {
            this.tabTargets, this.tabScenario, this.tabLiveMonitor, this.tabDevices, this.tabHistory
        });

        // Form settings
        this.AutoScaleDimensions = new SizeF(7F, 17F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1180, 720);
        this.Controls.Add(this.tabMain);
        this.Controls.Add(this.pnlHeader);
        this.Text = "Macro YouTube Report - Hệ Thống Báo Cáo Kênh/Video Tự Động";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1000, 600);
    }

    private void InitTargetsTab()
    {
        this.tabTargets.Text = "🎯 Danh Sách Mục Tiêu";
        this.tabTargets.Padding = new Padding(10);

        var pnlInput = new Panel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(5) };
        var pnlActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(5) };

        this.dgvTargets = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            BackgroundColor = Color.White
        };

        this.dgvTargets.Columns.Add("Type", "Loại");
        this.dgvTargets.Columns.Add("Keyword", "Từ khóa");
        this.dgvTargets.Columns.Add("Title", "Tiêu đề video");
        this.dgvTargets.Columns.Add("Channel", "Tên kênh");
        this.dgvTargets.Columns.Add("Url", "Đường dẫn URL");
        this.dgvTargets.Columns.Add("Completed", "Tiến độ");

        // Input controls
        var lblType = new Label { Text = "Loại:", Location = new Point(10, 15), AutoSize = true };
        this.cmbTargetType = new ComboBox { Location = new Point(55, 12), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        this.cmbTargetType.Items.AddRange(new object[] { "Video", "Channel", "Shorts", "Comment" });
        this.cmbTargetType.SelectedIndex = 0;

        var lblKw = new Label { Text = "Từ khóa:", Location = new Point(180, 15), AutoSize = true };
        this.txtKeyword = new TextBox { Location = new Point(245, 12), Size = new Size(180, 25), PlaceholderText = "Từ khóa tìm kiếm..." };

        var lblTitle = new Label { Text = "Tiêu đề:", Location = new Point(440, 15), AutoSize = true };
        this.txtTitle = new TextBox { Location = new Point(495, 12), Size = new Size(250, 25), PlaceholderText = "Tiêu đề video cần khớp..." };

        var lblCh = new Label { Text = "Kênh:", Location = new Point(10, 55), AutoSize = true };
        this.txtChannel = new TextBox { Location = new Point(55, 52), Size = new Size(180, 25), PlaceholderText = "Tên kênh YouTube..." };

        var lblUrl = new Label { Text = "URL link:", Location = new Point(255, 55), AutoSize = true };
        this.txtUrl = new TextBox { Location = new Point(320, 52), Size = new Size(425, 25), PlaceholderText = "https://youtube.com/watch?v=..." };

        this.btnAddTarget = new Button { Text = "➕ Thêm Mục Tiêu", Location = new Point(765, 25), Size = new Size(150, 45), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

        pnlInput.Controls.AddRange(new Control[] {
            lblType, this.cmbTargetType, lblKw, this.txtKeyword, lblTitle, this.txtTitle,
            lblCh, this.txtChannel, lblUrl, this.txtUrl, this.btnAddTarget
        });

        this.btnImportTargets = new Button { Text = "📂 Nhập từ Text/CSV", Size = new Size(160, 32) };
        this.btnRemoveTarget = new Button { Text = "❌ Xóa Đã Chọn", Size = new Size(130, 32) };
        this.btnClearTargets = new Button { Text = "🗑 Xóa Toàn Bộ", Size = new Size(130, 32) };
        pnlActions.Controls.AddRange(new Control[] { this.btnImportTargets, this.btnRemoveTarget, this.btnClearTargets });

        this.tabTargets.Controls.Add(this.dgvTargets);
        this.tabTargets.Controls.Add(pnlActions);
        this.tabTargets.Controls.Add(pnlInput);
    }

    private void InitScenarioTab()
    {
        this.tabScenario.Text = "⚙️ Kịch Bản & Chống Bot";
        this.tabScenario.Padding = new Padding(15);

        var lblScn = new Label { Text = "Kịch bản thực thi:", Location = new Point(15, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        this.cmbScenario = new ComboBox { Location = new Point(150, 17), Size = new Size(420, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        this.cmbScenario.Items.AddRange(new object[] {
            "1. Tìm kiếm từ khóa, xem tự nhiên và báo cáo video",
            "2. Mở trực tiếp link URL, xem và báo cáo video",
            "3. Tìm kiếm và báo cáo toàn bộ kênh (User Channel)",
            "4. Mở video ngắn Shorts, xem tự nhiên và báo cáo",
            "5. Tìm và báo cáo bình luận vi phạm trong video"
        });
        this.cmbScenario.SelectedIndex = 0;

        var lblWatch = new Label { Text = "Thời gian xem video trước khi report (giây):", Location = new Point(15, 65), AutoSize = true };
        this.numMinWatch = new NumericUpDown { Location = new Point(290, 62), Size = new Size(65, 25), Minimum = 10, Maximum = 600, Value = 35 };
        var lblTo = new Label { Text = "đến", Location = new Point(365, 65), AutoSize = true };
        this.numMaxWatch = new NumericUpDown { Location = new Point(400, 62), Size = new Size(65, 25), Minimum = 15, Maximum = 900, Value = 90 };

        var lblCooldown = new Label { Text = "Thời gian nghỉ giữa các lượt report (giây):", Location = new Point(15, 105), AutoSize = true };
        this.numCooldown = new NumericUpDown { Location = new Point(290, 102), Size = new Size(65, 25), Minimum = 0, Maximum = 3600, Value = 120 };

        this.chkNaturalEngagement = new CheckBox { Text = "Mô phỏng hành vi tự nhiên (cuộn xem bình luận, vi tương tác)", Location = new Point(18, 145), AutoSize = true, Checked = true };
        this.chkAutoSkipAd = new CheckBox { Text = "Tự động nhận diện và bỏ qua quảng cáo (Auto Skip Ad)", Location = new Point(18, 175), AutoSize = true, Checked = true };
        this.chkRotateReasons = new CheckBox { Text = "Tự động xoay vòng danh mục lý do báo cáo tránh lặp cờ bot", Location = new Point(18, 205), AutoSize = true, Checked = true };

        var lblReasons = new Label { Text = "Các danh mục lý do được phép áp dụng:", Location = new Point(15, 245), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        this.clbReasons = new CheckedListBox { Location = new Point(18, 275), Size = new Size(450, 160), CheckOnClick = true };
        this.clbReasons.Items.AddRange(new object[] {
            "Spam hoặc gây hiểu lầm (Spam or misleading)",
            "Hành vi có hại hoặc nguy hiểm (Harmful dangerous acts)",
            "Thông tin sai lệch (Misinformation)",
            "Kích động thù địch hoặc lăng mạ (Hateful or abusive)",
            "Quấy rối hoặc bắt nạt (Harassment or bullying)",
            "Nội dung bạo lực hoặc phản cảm (Violent or repulsive)",
            "Nội dung khiêu dâm (Sexual content)"
        });
        for (int i = 0; i < this.clbReasons.Items.Count; i++) this.clbReasons.SetItemChecked(i, true);

        var lblSpin = new Label { Text = "Mẫu nội dung giải trình bổ sung (Spin-text, mỗi dòng một mẫu):", Location = new Point(500, 65), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        this.txtSpinTemplates = new TextBox { Location = new Point(500, 95), Size = new Size(640, 340), Multiline = true, ScrollBars = ScrollBars.Vertical };
        this.txtSpinTemplates.Text = "Nội dung vi phạm nghiêm trọng tiêu chuẩn cộng đồng YouTube.\r\nVideo chứa thông tin sai sự thật và mang tính chất lừa dối người xem.\r\nHành vi phát tán spam và gây hiểu lầm trên diện rộng, đề nghị xử lý.\r\nKênh liên tục đăng tải các nội dung nguy hại, vi phạm chính sách của nền tảng.";

        this.tabScenario.Controls.AddRange(new Control[] {
            lblScn, this.cmbScenario, lblWatch, this.numMinWatch, lblTo, this.numMaxWatch,
            lblCooldown, this.numCooldown, this.chkNaturalEngagement, this.chkAutoSkipAd,
            this.chkRotateReasons, lblReasons, this.clbReasons, lblSpin, this.txtSpinTemplates
        });
    }

    private void InitMonitorTab()
    {
        this.tabLiveMonitor.Text = "📊 Giám Sát Trực Tiếp";
        this.tabLiveMonitor.Padding = new Padding(5);

        var pnlStatus = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(240, 244, 248), Padding = new Padding(10) };
        this.lblPending = new Label { Text = "⏳ Đang chờ: 0", Location = new Point(15, 12), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.SlateGray };
        this.lblRunning = new Label { Text = "⚡ Đang chạy: 0", Location = new Point(160, 12), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 123, 255) };
        this.lblSuccess = new Label { Text = "✅ Thành công: 0", Location = new Point(310, 12), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(40, 167, 69) };
        this.lblFailed = new Label { Text = "❌ Thất bại: 0", Location = new Point(470, 12), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(220, 53, 69) };

        pnlStatus.Controls.AddRange(new Control[] { this.lblPending, this.lblRunning, this.lblSuccess, this.lblFailed });

        this.splitMonitor = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 650
        };

        this.rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Consolas", 9.5F)
        };

        this.picProof = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(20, 20, 20)
        };

        var pnlProofHeader = new Label { Text = "Ảnh Bằng Chứng Hoàn Tất (Proof Screenshot):", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        var pnlProofContainer = new Panel { Dock = DockStyle.Fill };
        pnlProofContainer.Controls.Add(this.picProof);
        pnlProofContainer.Controls.Add(pnlProofHeader);

        this.splitMonitor.Panel1.Controls.Add(this.rtbLog);
        this.splitMonitor.Panel2.Controls.Add(pnlProofContainer);

        this.tabLiveMonitor.Controls.Add(this.splitMonitor);
        this.tabLiveMonitor.Controls.Add(pnlStatus);
    }

    private void InitDevicesTab()
    {
        this.tabDevices.Text = "📱 Thiết Bị & Profiles";
        this.tabDevices.Padding = new Padding(10);

        this.splitDevices = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 650
        };

        // Left: Android
        var pnlAdbTop = new Panel { Dock = DockStyle.Top, Height = 40 };
        var lblAdb = new Label { Text = "Danh sách thiết bị Android (ADB):", Location = new Point(5, 10), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        this.btnRefreshDevices = new Button { Text = "🔄 Quét lại thiết bị", Location = new Point(350, 5), Size = new Size(150, 30) };
        pnlAdbTop.Controls.AddRange(new Control[] { lblAdb, this.btnRefreshDevices });

        this.dgvAndroidDevices = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White
        };
        this.dgvAndroidDevices.Columns.Add("Serial", "Serial / IP");
        this.dgvAndroidDevices.Columns.Add("Model", "Thiết bị");
        this.dgvAndroidDevices.Columns.Add("Version", "Phiên bản");
        this.dgvAndroidDevices.Columns.Add("Status", "Trạng thái");

        this.splitDevices.Panel1.Controls.Add(this.dgvAndroidDevices);
        this.splitDevices.Panel1.Controls.Add(pnlAdbTop);

        // Right: Chrome Profiles
        var pnlChromeTop = new Panel { Dock = DockStyle.Top, Height = 80 };
        var lblChrome = new Label { Text = "Danh sách Chrome Profiles:", Location = new Point(5, 10), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        this.txtNewProfile = new TextBox { Location = new Point(5, 40), Size = new Size(200, 25), PlaceholderText = "Tên Profile mới..." };
        this.btnAddProfile = new Button { Text = "➕ Thêm Profile", Location = new Point(215, 38), Size = new Size(120, 28) };
        pnlChromeTop.Controls.AddRange(new Control[] { lblChrome, this.txtNewProfile, this.btnAddProfile });

        this.lstChromeProfiles = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };

        this.splitDevices.Panel2.Controls.Add(this.lstChromeProfiles);
        this.splitDevices.Panel2.Controls.Add(pnlChromeTop);

        this.tabDevices.Controls.Add(this.splitDevices);
    }

    private void InitHistoryTab()
    {
        this.tabHistory.Text = "📜 Lịch Sử & Thống Kê";
        this.tabHistory.Padding = new Padding(10);

        var pnlHistoryTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
        this.btnRefreshHistory = new Button { Text = "🔄 Làm mới dữ liệu", Size = new Size(150, 32) };
        this.btnExportCsv = new Button { Text = "📥 Xuất file CSV báo cáo", Size = new Size(180, 32) };
        pnlHistoryTop.Controls.AddRange(new Control[] { this.btnRefreshHistory, this.btnExportCsv });

        this.dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White
        };
        this.dgvHistory.Columns.Add("Id", "ID");
        this.dgvHistory.Columns.Add("Time", "Thời gian");
        this.dgvHistory.Columns.Add("Target", "Mục tiêu");
        this.dgvHistory.Columns.Add("Device", "Thiết bị");
        this.dgvHistory.Columns.Add("Platform", "Nền tảng");
        this.dgvHistory.Columns.Add("Category", "Lý do");
        this.dgvHistory.Columns.Add("Status", "Trạng thái");
        this.dgvHistory.Columns.Add("Duration", "Thời lượng (s)");

        this.tabHistory.Controls.Add(this.dgvHistory);
        this.tabHistory.Controls.Add(pnlHistoryTop);
    }
}
