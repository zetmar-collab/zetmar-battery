using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Automation;
using Forms = System.Windows.Forms;
namespace Zetmar.Battery;
public sealed class WidgetWindow : Window
{
    private readonly App app;
    private readonly Settings settings;
    private readonly TextBlock text = new();
    private readonly Border border = new();
    private readonly BatteryGauge battery = new();
    private IntPtr handle;
    private bool dragging;
    private bool hotkeysRegistered;
    private string? hotkeySignature;
    public string? HotkeyError { get; private set; }
    public WidgetWindow(App app, Settings settings)
    {
        this.app = app; this.settings = settings;
        Title = App.WindowTitle; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false; ShowActivated = false;
        SizeToContent = SizeToContent.WidthAndHeight; FontFamily = new FontFamily("Segoe UI");
        border.CornerRadius = new CornerRadius(10); border.Padding = new Thickness(14, 9, 14, 9); border.BorderThickness = new Thickness(1);
        text.FontWeight = FontWeights.SemiBold;
        text.VerticalAlignment = VerticalAlignment.Center;
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        battery.Margin = new Thickness(0, 0, 10, 0); battery.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(battery); row.Children.Add(text); border.Child = row; Content = border;
        SourceInitialized += (_, _) =>
        {
            handle = new WindowInteropHelper(this).Handle;
            var ex = Native.GetWindowLongPtr(handle, -20).ToInt64();
            Native.SetWindowLongPtr(handle, -20, new IntPtr(ex | 0x08000000 | 0x80));
            HwndSource.FromHwnd(handle).AddHook(WndProc);
            ConfigureHotkeys();
        };
        Loaded += (_, _) => Place(); SizeChanged += (_, _) => Place();
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) { app.ShowPanel(); return; }
            if (settings.Locked) return;
            dragging = true;
            try { DragMove(); } catch (InvalidOperationException) { }
            finally
            {
                dragging = false;
                if (Native.GetWindowRect(handle, out var r))
                {
                    var screen = Forms.Screen.FromHandle(handle); var area = SafeArea(screen);
                    var dpi = VisualTreeHelper.GetDpi(this); int margin=(int)(8*dpi.DpiScaleX);
                    var point = settings.SnapToEdges ? Zetmar.Core.WidgetPlacement.Snap(area,r.Right-r.Left,r.Bottom-r.Top,margin,r.Left,r.Top,(int)(16*dpi.DpiScaleX)) : new Zetmar.Core.WidgetPoint(r.Left,r.Top);
                    settings.Anchor="free"; settings.MonitorDevice=screen.DeviceName;
                    settings.X=point.X; settings.Y=point.Y; app.Changed();
                }
                Place();
            }
        };
        MouseRightButtonUp += (_, _) =>
        {
            var menu = new ContextMenu();
            var details = new MenuItem { Header = app.T("Szczegóły i ustawienia") }; details.Click += (_, _) => app.ShowPanel(); menu.Items.Add(details);
            var locked = new MenuItem { Header = app.T("Zablokuj położenie"), IsCheckable = true, IsChecked = settings.Locked }; locked.Click += (_, _) => { settings.Locked = locked.IsChecked; app.Changed(); }; menu.Items.Add(locked);
            var reset = new MenuItem { Header = app.T("Przywróć położenie") }; reset.Click += (_, _) => { settings.X = settings.Y = null; app.Changed(); }; menu.Items.Add(reset);
            var hide = new MenuItem { Header = app.T("Ukryj widżet") }; hide.Click += (_, _) => app.ToggleWidget(); menu.Items.Add(hide);
            menu.IsOpen = true;
        };
    }
    private IntPtr WndProc(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
    {
        if (msg == 0x0312) { if(w.ToInt32()==1)app.ShowPanel(); else if(w.ToInt32()==2)app.ToggleWidget(); handled=true; }
        if ((uint)msg == App.ShowMessage) { app.ShowPanel(); handled = true; }
        if (msg == 0x02E0 || msg == 0x007E || msg == 0x001A) Dispatcher.BeginInvoke(Place);
        return IntPtr.Zero;
    }
    public void Render()
    {
        text.Text = settings.WidgetView == "percent" && app.Current.State is not (Zetmar.Core.PowerState.NoBattery or Zetmar.Core.PowerState.Unavailable) ? app.Current.Percent?.ToString()+"%" : app.Summary;
        if (settings.WidgetView == "percent" && app.Current.Percent == null && app.Current.State is not (Zetmar.Core.PowerState.NoBattery or Zetmar.Core.PowerState.Unavailable)) text.Text="—";
        text.Visibility=settings.WidgetView=="battery"?Visibility.Collapsed:Visibility.Visible;
        battery.Margin=new Thickness(0,0,settings.WidgetView=="battery"?0:10,0); text.FontSize = settings.FontSize; text.Foreground = app.WidgetForeground;
        battery.Width = settings.FontSize * 2.1; battery.Height = settings.FontSize * 1.15;
        battery.Update(app.Current, app.WidgetForeground);
        AutomationProperties.SetName(battery, app.T("Bateria: ") + app.LongSummary);
        border.Background = app.WidgetBackground; border.BorderBrush = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : app.Dark ? new SolidColorBrush(Color.FromRgb(65, 76, 88)) : new SolidColorBrush(Color.FromRgb(190, 202, 212));
        ToolTip = app.LongSummary + "\n" + app.T("~ oznacza szacunek; zapis h:mm: godziny i minuty. Dwuklik: szczegóły. Przeciągnij, aby przenieść.");
        AutomationProperties.SetName(this, app.LongSummary); CheckVisibility();
    }
    public void Place()
    {
        if (handle == IntPtr.Zero || dragging || !IsLoaded) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        int width = (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX), height = (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY);
        var selected = Forms.Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==settings.MonitorDevice);
        bool custom=settings.X.HasValue&&settings.Y.HasValue;
        var screen=selected ?? (custom&&Forms.Screen.AllScreens.Any(s=>s.Bounds.Contains(settings.X!.Value,settings.Y!.Value)) ? Forms.Screen.FromPoint(new System.Drawing.Point(settings.X!.Value,settings.Y!.Value)) : Forms.Screen.PrimaryScreen!);
        var area=SafeArea(screen); int margin=(int)(8*dpi.DpiScaleX);
        var position=Zetmar.Core.WidgetPlacement.Place(area,width,height,margin,settings.Anchor,settings.X,settings.Y);
        int x=position.X,y=position.Y;
        Native.SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, 0x0015); // no size, z-order, or activation
    }
    private Zetmar.Core.ScreenArea SafeArea(Forms.Screen screen)
    {
        var w=screen.WorkingArea; var b=screen.Bounds;
        var area=new Zetmar.Core.ScreenArea(w.Left,w.Top,w.Right,w.Bottom);
        var monitor=new Zetmar.Core.ScreenArea(b.Left,b.Top,b.Right,b.Bottom);
        for(uint edge=0;edge<4;edge++)
        {
            var bar=new Native.AppBarData { Size=(uint)System.Runtime.InteropServices.Marshal.SizeOf<Native.AppBarData>(),Edge=edge,Rectangle=new Native.Rect { Left=b.Left,Top=b.Top,Right=b.Right,Bottom=b.Bottom } };
            var hwnd=Native.SHAppBarMessage(11,ref bar); // ABM_GETAUTOHIDEBAREX, per monitor
            if(hwnd==IntPtr.Zero)continue;
            int thickness=(int)(48*VisualTreeHelper.GetDpi(this).DpiScaleX);
            if(Native.GetWindowRect(hwnd,out var rect))
            {
                int measured=edge is 0 or 2?rect.Right-rect.Left:rect.Bottom-rect.Top;
                if(measured>0&&measured<(edge is 0 or 2?b.Width:b.Height)/3)thickness=measured;
            }
            area=Zetmar.Core.WidgetPlacement.AvoidBar(area,monitor,(int)edge,thickness);
        }
        return area;
    }
    public void ConfigureHotkeys()
    {
        if(handle==IntPtr.Zero)return;
        string signature=settings.Hotkeys+settings.HotkeyModifiers;
        if(hotkeySignature==signature&&HotkeyError==null)return;
        hotkeySignature=signature;
        Native.UnregisterHotKey(handle,1); Native.UnregisterHotKey(handle,2); hotkeysRegistered=false;HotkeyError=null;
        if(!settings.Hotkeys)return;
        uint modifiers=0x4000u | (settings.HotkeyModifiers=="ctrl-shift"?6u:settings.HotkeyModifiers=="alt-shift"?5u:3u);
        bool panel=Native.RegisterHotKey(handle,1,modifiers,0x42); // Ctrl+Alt+B, no repeat
        bool toggle=Native.RegisterHotKey(handle,2,modifiers,0x57); // Ctrl+Alt+W
        if(!panel||!toggle)
        {
            Native.UnregisterHotKey(handle,1); Native.UnregisterHotKey(handle,2);
            HotkeyError=app.T("Skrót jest zajęty przez inną aplikację.");
        }
        else hotkeysRegistered=true;
    }
    protected override void OnClosed(EventArgs e)
    {
        if(hotkeysRegistered) { Native.UnregisterHotKey(handle,1); Native.UnregisterHotKey(handle,2); }
        base.OnClosed(e);
    }
    public void CheckVisibility()
    {
        bool full = false;
        var fg = Native.GetForegroundWindow();
        if (fg != IntPtr.Zero && fg != handle && fg != Native.GetShellWindow() && fg != Native.GetDesktopWindow() && Native.GetWindowRect(fg, out var r))
        {
            var monitor = new Native.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<Native.MonitorInfo>() };
            if (Native.GetMonitorInfo(Native.MonitorFromWindow(fg, 2), ref monitor))
            {
                var m = monitor.Monitor;
                var own = Forms.Screen.FromHandle(handle).Bounds;
                full = own.Left == m.Left && own.Top == m.Top && r.Left <= m.Left && r.Top <= m.Top && r.Right >= m.Right && r.Bottom >= m.Bottom;
            }
        }
        Topmost = settings.Topmost && !full;
        Opacity = settings.WidgetVisible && !full ? 1 : 0;
        IsHitTestVisible = settings.WidgetVisible && !full;
    }
}
