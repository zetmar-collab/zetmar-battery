using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
namespace Zetmar.Battery;
internal sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon icon = new();
    private string key = "";
    public TrayService(Action show, Action toggle, Action exit)
    {
        Show = show; Toggle = toggle; Exit = exit;
        icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) Show(); };
    }
    private Action Show, Toggle, Exit;
    public void Menu(bool pl, bool visible)
    {
        string L(string key)=>Zetmar.Core.Localization.Get(key,pl);
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(L(visible ? "Ukryj widżet" : "Pokaż widżet"), null, (_, _) => Toggle());
        menu.Items.Add(L("Szczegóły baterii"), null, (_, _) => Show());
        menu.Items.Add(L("Ustawienia"), null, (_, _) => Show());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(L("Zakończ"), null, (_, _) => Exit());
        var old = icon.ContextMenuStrip; icon.ContextMenuStrip = menu; old?.Dispose();
    }
    public void Update(string number, string tooltip, bool dark)
    {
        icon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;
        string next = number + dark;
        if (key != next)
        {
            using var bitmap = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent); g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var font = new Font("Segoe UI", number.Length == 3 ? 18 : 22, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(dark ? Color.White : Color.FromArgb(20, 26, 32));
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(number, font, brush, new RectangleF(-2, -1, 36, 34), format);
            IntPtr handle = bitmap.GetHicon();
            try { using var temp = Icon.FromHandle(handle); var old = icon.Icon; icon.Icon = (Icon)temp.Clone(); old?.Dispose(); }
            finally { Native.DestroyIcon(handle); }
            key = next;
        }
        icon.Visible = true;
    }
    public void Alert(string title, string text) => icon.ShowBalloonTip(8000, title, text, Forms.ToolTipIcon.Warning);
    // NotifyIcon internally handles the registered TaskbarCreated message after Explorer restarts.
    public void Dispose() { icon.Visible = false; var image = icon.Icon; var menu = icon.ContextMenuStrip; icon.Dispose(); image?.Dispose(); menu?.Dispose(); }
}
