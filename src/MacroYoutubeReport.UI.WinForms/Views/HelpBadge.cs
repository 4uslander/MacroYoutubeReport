using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MacroYoutubeReport.UI.WinForms.Views;

/// <summary>
/// Huy hiệu trợ giúp nhỏ gọn hình tròn (Help Badge) với màu sắc nhã nhặn,
/// hòa hợp với màu nền hệ thống, đóng vai trò như điểm gợi ý thông tin nhẹ nhàng.
/// </summary>
public sealed class HelpBadge : Control
{
    private bool _isHovered;

    public HelpBadge()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);

        this.Size = new Size(18, 18);
        this.Cursor = Cursors.Hand;
        this.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        this.BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        this.Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);

        Color fillColor = _isHovered ? Color.FromArgb(226, 232, 240) : Color.FromArgb(241, 245, 249);
        Color borderColor = _isHovered ? Color.FromArgb(148, 163, 184) : Color.FromArgb(203, 213, 225);
        Color textColor = _isHovered ? Color.FromArgb(30, 41, 59) : Color.FromArgb(100, 116, 139);

        using (var brush = new SolidBrush(fillColor))
        {
            g.FillEllipse(brush, rect);
        }

        using (var pen = new Pen(borderColor, 1f))
        {
            g.DrawEllipse(pen, rect);
        }

        TextRenderer.DrawText(
            g,
            "?",
            this.Font,
            new Rectangle(0, 0, Width, Height),
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
