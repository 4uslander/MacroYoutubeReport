using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MacroYoutubeReport.Core.Localization;

namespace MacroYoutubeReport.UI.WinForms.Views;

/// <summary>
/// Hộp thoại modal cấu hình nâng cao cho tiến trình báo cáo:
/// thời gian xem, thời gian nghỉ, số luồng, tệp lý do và các hành vi tương tác.
/// </summary>
public sealed class ReportSettingsDialog : Form
{
    public int MinWatch => (int)numMinWatch.Value;
    public int MaxWatch => (int)numMaxWatch.Value;
    public int Cooldown => (int)numCooldown.Value;
    public int Concurrency => (int)numConcurrency.Value;
    public string ReasonFile => txtReasonFile.Text.Trim();
    public bool SkipAd => chkAutoSkipAd.Checked;
    public bool NaturalEngagement => chkNaturalEngagement.Checked;
    public bool RandomWatch => chkRandomWatch.Checked;
    public bool RotateReasons => chkRotateReasons.Checked;
    public bool AutoAllocate => chkAutoAllocate.Checked;
    public bool DebugScreenshots => chkDebugScreenshots.Checked;

    private readonly NumericUpDown numMinWatch;
    private readonly NumericUpDown numMaxWatch;
    private readonly NumericUpDown numCooldown;
    private readonly NumericUpDown numConcurrency;
    private readonly TextBox txtReasonFile;
    private readonly Button btnBrowseReason;
    private readonly HelpBadge btnHelpReason;
    private readonly CheckBox chkAutoSkipAd;
    private readonly CheckBox chkNaturalEngagement;
    private readonly CheckBox chkRandomWatch;
    private readonly CheckBox chkRotateReasons;
    private readonly CheckBox chkAutoAllocate;
    private readonly CheckBox chkDebugScreenshots;
    private readonly Button btnSave;
    private readonly Button btnCancel;

    public ReportSettingsDialog(
        int minWatch,
        int maxWatch,
        int cooldown,
        int concurrency,
        string reasonFile,
        bool skipAd,
        bool naturalEngagement,
        bool randomWatch,
        bool rotateReasons,
        bool autoAllocate,
        bool debugScreenshots = false)
    {
        var lang = UiLanguageManager.CurrentLanguage;

        this.Text = UiLanguageManager.Translate("Cài đặt nâng cao & Mô phỏng tương tác", lang);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = false;
        this.ClientSize = new Size(460, 455);
        this.Font = new Font("Segoe UI", 9F);
        this.BackColor = Color.FromArgb(248, 249, 250);

        // 1. GroupBox: Thời gian & Luồng chạy
        var gbTiming = new GroupBox
        {
            Text = UiLanguageManager.Translate("Thời gian & Luồng chạy", lang),
            Location = new Point(12, 10),
            Size = new Size(436, 100)
        };

        var lblWatch = new Label { Text = UiLanguageManager.Translate("Xem video (giây):", lang), Location = new Point(12, 28), AutoSize = true };
        numMinWatch = new NumericUpDown { Location = new Point(135, 25), Size = new Size(55, 23), Minimum = 5, Maximum = 1800, Value = Math.Clamp(minWatch, 5, 1800) };
        var lblTo = new Label { Text = UiLanguageManager.Translate("đến", lang), Location = new Point(198, 28), AutoSize = true };
        numMaxWatch = new NumericUpDown { Location = new Point(230, 25), Size = new Size(55, 23), Minimum = 5, Maximum = 1800, Value = Math.Clamp(maxWatch, 5, 1800) };

        var lblCooldown = new Label { Text = UiLanguageManager.Translate("Nghỉ giữa lượt (s):", lang), Location = new Point(12, 63), AutoSize = true };
        numCooldown = new NumericUpDown { Location = new Point(135, 60), Size = new Size(55, 23), Minimum = 0, Maximum = 3600, Value = Math.Clamp(cooldown, 0, 3600) };

        var lblConcurrency = new Label { Text = UiLanguageManager.Translate("Số luồng:", lang), Location = new Point(230, 63), AutoSize = true };
        numConcurrency = new NumericUpDown { Location = new Point(295, 60), Size = new Size(55, 23), Minimum = 1, Maximum = 32, Value = Math.Clamp(concurrency, 1, 32) };

        gbTiming.Controls.AddRange(new Control[] {
            lblWatch, numMinWatch, lblTo, numMaxWatch,
            lblCooldown, numCooldown, lblConcurrency, numConcurrency
        });

        // 2. GroupBox: Tệp lý do báo cáo
        var gbReasonFile = new GroupBox
        {
            Text = UiLanguageManager.Translate("Tệp nội dung lý do báo cáo", lang),
            Location = new Point(12, 116),
            Size = new Size(436, 70)
        };

        txtReasonFile = new TextBox
        {
            Location = new Point(12, 28),
            Size = new Size(325, 23),
            Text = reasonFile,
            PlaceholderText = "report_reasons.txt"
        };

        btnBrowseReason = new Button
        {
            Text = UiLanguageManager.Translate("Chọn", lang),
            Location = new Point(345, 26),
            Size = new Size(60, 27)
        };
        btnBrowseReason.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Chọn file nội dung lý do báo cáo"
            };
            if (File.Exists(txtReasonFile.Text))
            {
                ofd.InitialDirectory = Path.GetDirectoryName(txtReasonFile.Text);
            }
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                txtReasonFile.Text = ofd.FileName;
            }
        };

        btnHelpReason = new HelpBadge
        {
            Location = new Point(410, 30),
            Size = new Size(18, 18)
        };

        var reasonHelp = UiLanguageManager.GetFileFormatHelp("reason", lang);
        var toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 200, ReshowDelay = 200, IsBalloon = true };
        toolTip.SetToolTip(btnHelpReason, reasonHelp.Tooltip);
        btnHelpReason.Click += (_, _) =>
        {
            MessageBox.Show(this, reasonHelp.DialogContent, reasonHelp.DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        gbReasonFile.Controls.AddRange(new Control[] { txtReasonFile, btnBrowseReason, btnHelpReason });

        // 3. GroupBox: Tùy chọn mô phỏng người thật
        var gbEngagement = new GroupBox
        {
            Text = UiLanguageManager.Translate("Hành vi tương tác & Tự động hóa", lang),
            Location = new Point(12, 192),
            Size = new Size(436, 205)
        };

        chkAutoSkipAd = new CheckBox
        {
            Text = UiLanguageManager.Translate("Bỏ qua quảng cáo (Skip Ad)", lang),
            Location = new Point(15, 26),
            Size = new Size(390, 22),
            Checked = skipAd
        };

        chkNaturalEngagement = new CheckBox
        {
            Text = UiLanguageManager.Translate("Mô phỏng người thật (cuộn đọc comment)", lang),
            Location = new Point(15, 54),
            Size = new Size(390, 22),
            Checked = naturalEngagement
        };

        chkRandomWatch = new CheckBox
        {
            Text = UiLanguageManager.Translate("Xem ngẫu nhiên (Random watch)", lang),
            Location = new Point(15, 82),
            Size = new Size(390, 22),
            Checked = randomWatch
        };

        chkRotateReasons = new CheckBox
        {
            Text = UiLanguageManager.Translate("Tự động xoay vòng lý do vi phạm", lang),
            Location = new Point(15, 110),
            Size = new Size(390, 22),
            Checked = rotateReasons
        };

        chkAutoAllocate = new CheckBox
        {
            Text = UiLanguageManager.Translate("Tự động phân bổ tài khoản Proxy", lang),
            Location = new Point(15, 138),
            Size = new Size(390, 22),
            Checked = autoAllocate
        };

        chkDebugScreenshots = new CheckBox
        {
            Text = UiLanguageManager.Translate("Chụp ảnh toàn bộ các bước (Gỡ lỗi - tốn ổ đĩa)", lang),
            Location = new Point(15, 166),
            Size = new Size(390, 22),
            Checked = debugScreenshots
        };

        gbEngagement.Controls.AddRange(new Control[] {
            chkAutoSkipAd, chkNaturalEngagement, chkRandomWatch, chkRotateReasons, chkAutoAllocate, chkDebugScreenshots
        });

        // 4. Buttons: Lưu & Hủy
        btnSave = new Button
        {
            Text = UiLanguageManager.Translate("Lưu cài đặt", lang),
            DialogResult = DialogResult.OK,
            Location = new Point(242, 410),
            Size = new Size(100, 32),
            BackColor = Color.FromArgb(0, 122, 255),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;

        btnCancel = new Button
        {
            Text = UiLanguageManager.Translate("Hủy bỏ", lang),
            DialogResult = DialogResult.Cancel,
            Location = new Point(348, 410),
            Size = new Size(100, 32)
        };

        this.Controls.AddRange(new Control[] {
            gbTiming, gbReasonFile, gbEngagement, btnSave, btnCancel
        });

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }
}
