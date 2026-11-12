using System.Collections.Generic;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Win32;
using Zetmar.Core;
namespace Zetmar.Battery;
public sealed class App : Application
{
    private Mutex? mutex;
    private bool ownsInstance;
    private readonly Settings settings;
    private readonly IBatterySource source;
    private readonly TimeProvider clock;
    private readonly BatterySession batterySession = new();
    private TimeSpan? ownEstimate;
    private bool saveWarningShown;
    private readonly HistoryStore history;
    private HistoryWindow? historyWindow;
    private readonly DischargeEstimator estimator = new();
    private TrayService? tray;
    private WidgetWindow widget = null!;
    private DetailsWindow? panel;
    private DispatcherTimer? timer, visibilityTimer;
    public App(IBatterySource? source = null, TimeProvider? clock = null, Settings? settings = null)
    {
        this.source = source ?? new WindowsBatterySource();
        this.clock = clock ?? TimeProvider.System;
        this.settings = settings ?? Settings.Load();
        history = new HistoryStore(Settings.Folder,this.clock.GetUtcNow());
    }
    internal static string WindowTitle => "ZETMAR Battery • Widget" + Settings.InstanceSuffix;
    internal static readonly uint ShowMessage = Native.RegisterWindowMessage("ZetmarBattery.ShowPanel.v1");
    public bool Polish => settings.Language == "pl" || (settings.Language == "system" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pl");
    public string T(string key) => Zetmar.Core.Localization.Get(key,Polish);
    public bool Dark
    {
        get
        {
            if (SystemParameters.HighContrast) return SystemColors.WindowColor.R < 128;
            if (settings.Theme != "system") return settings.Theme == "dark";
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return k?.GetValue("AppsUseLightTheme") is int n && n == 0;
        }
    }
    internal bool TrayDark
    {
        get { if (SystemParameters.HighContrast) return Dark; using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); return k?.GetValue("SystemUsesLightTheme") is int n && n == 0; }
    }
    public Brush Surface => SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Dark ? "#141B24" : "#F3F6F8"));
    public Brush Ink => SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Dark ? "#F0F6FA" : "#152B37"));
    public Brush Accent => SystemParameters.HighContrast ? SystemColors.HighlightBrush : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Dark ? "#80E5C5" : "#006B52"));
    public Brush WidgetBackground => SystemParameters.HighContrast ? SystemColors.WindowBrush : WidgetColors.Brush(settings.WidgetBackground) ?? Surface;
    public Brush WidgetForeground => SystemParameters.HighContrast ? SystemColors.WindowTextBrush : WidgetColors.Brush(settings.WidgetForeground)
        ?? (settings.WidgetBackground != null && WidgetBackground is SolidColorBrush b ? WidgetColors.Contrasting(b.Color) : Ink);
    public new BatteryReading Current { get; private set; } = new(PowerState.Unavailable);
    public string Summary { get; private set; } = "";
    public string LongSummary { get; private set; } = "";
    public string RemainingTime { get; private set; } = "—";
    public string EstimateSource { get; private set; } = "";
    public string Session => batterySession.Started is {} start ? BatteryText.Duration(clock.GetUtcNow() - start, Polish).TrimStart('~') : "—";
    public string EstimationProgress => string.Format(T("Obserwacja: {0:0}/10 min · spadek: {1}/2 p.p."), estimator.Observed.TotalMinutes, estimator.PercentageDrop);
    public string? SaveError => settings.SaveError;
    public string? HotkeyError => widget?.HotkeyError;
    [STAThread] public static void Main(string[] args)
    {
        int index = Array.IndexOf(args, "--data-dir");
        if (index >= 0)
        {
            if (index + 1 >= args.Length || !System.IO.Path.IsPathFullyQualified(args[index + 1])) return;
            Settings.DataDirectory = System.IO.Path.GetFullPath(args[index + 1]);
        }
        new App().Run();
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        mutex = new Mutex(true, @"Local\ZetmarBattery.v1" + Settings.InstanceSuffix, out bool first);
        if (!first)
        {
            if(Array.IndexOf(e.Args,"--startup")>=0){Shutdown();return;}
            IntPtr h=IntPtr.Zero;
            for(int attempt=0;attempt<40&&h==IntPtr.Zero;attempt++){h=Native.FindWindow(null,WindowTitle);if(h==IntPtr.Zero)Thread.Sleep(50);}
            if(h!=IntPtr.Zero)Native.PostMessage(h,ShowMessage,IntPtr.Zero,IntPtr.Zero);
            Shutdown();return;
        }
        ownsInstance=true;
        Settings.Log("Start: "+(Array.IndexOf(e.Args,"--startup")>=0?"startup":"manual")+"; executable="+Environment.ProcessPath);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, a) => { Settings.Log(a.Exception.ToString()); };
        widget = new WidgetWindow(this, settings); MainWindow = widget;
        tray = new TrayService(ShowPanel, ToggleWidget, () => Shutdown());
        tray.Menu(Polish, settings.WidgetVisible);
        widget.Show();
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.UserPreferenceChanged += PreferenceChanged;
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        timer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, (_, _) => Refresh(), Dispatcher); timer.Start();
        visibilityTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => widget.CheckVisibility(), Dispatcher); visibilityTimer.Start();
        Refresh();
        if (Array.IndexOf(e.Args, "--details") >= 0) ShowPanel();
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e) => Dispatcher.BeginInvoke(() => { if (e.Mode is PowerModes.Resume or PowerModes.Suspend) { estimator.Reset(); ownEstimate = null; batterySession.Resume(); } Refresh(); });
    private void PreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Dispatcher.BeginInvoke(() => { Render(); panel?.Rebuild(); });
    private void DisplayChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() => widget.Place());
    public void Refresh()
    {
        Current = source.Read();
        if(history.Data.Prune(clock.GetUtcNow())&&history.HasStoredFile) _=history.Flush(clock.GetUtcNow());
        if(settings.HistoryEnabled&&history.Data.Add(Current,clock.GetUtcNow())) _=history.Flush(clock.GetUtcNow());
        ownEstimate = estimator.Update(Current, clock.GetUtcNow());
        Render(); EvaluateAlerts();
    }
    private void EvaluateAlerts()
    {
        var alert = batterySession.Update(Current, clock.GetUtcNow(), settings.Alarm, settings.Threshold, settings.ChargeAlarm, settings.ChargeThreshold);
        if (alert == BatteryAlert.Low) tray?.Alert(T("Niski poziom baterii"), LongSummary);
        if (alert == BatteryAlert.ChargeTarget) tray?.Alert(T("Osiągnięto poziom ładowania"), LongSummary);
    }
    public void Render()
    {
        TimeSpan? remaining = Current.Seconds is uint sec ? TimeSpan.FromSeconds(sec) : settings.Estimate ? ownEstimate : null;
        RemainingTime = Current.State == PowerState.Battery ? remaining is {} duration ? BatteryText.Duration(duration,Polish) : T("Czas niedostępny") : BatteryText.State(Current.State,Polish);
        EstimateSource = remaining == null ? "—" : Current.Seconds != null ? T("Szacunek Windows") : T("Szacunek aplikacji");
        Summary = BatteryText.Summary(Current, remaining, settings.Estimate, Polish, settings.Compact);
        LongSummary = BatteryText.Summary(Current, remaining, settings.Estimate, Polish, false);
        widget?.Render();
        tray?.Update(Current.Percent?.ToString() ?? "—", T("Bateria: ") + LongSummary, TrayDark);
        panel?.UpdateValues();
        historyWindow?.RefreshTheme();
    }
    public void Changed(bool reset = false)
    {
        bool saved = settings.Save();
        if (!saved && !saveWarningShown) tray?.Alert(T("Nie zapisano ustawień"), T("Zmiany działają do zamknięcia aplikacji. Otwórz panel i ponów zapis."));
        saveWarningShown = !saved;
        if (reset) { estimator.Reset(); ownEstimate = null; }
        tray?.Menu(Polish, settings.WidgetVisible); widget?.ConfigureHotkeys(); Render(); EvaluateAlerts(); widget?.Place();
    }
    public void ToggleWidget() { settings.WidgetVisible = !settings.WidgetVisible; Changed(); panel?.Rebuild(); }
    public void ShowHistory()
    {
        if(historyWindow==null){historyWindow=new HistoryWindow(this,history);historyWindow.Closed+=(_,_)=>historyWindow=null;}
        historyWindow.Show();historyWindow.Activate();
    }
    public void ShowPanel()
    {
        if (panel == null) { panel = new DetailsWindow(this, settings); panel.Closed += (_, _) => panel = null; }
        panel.Show(); if (panel.WindowState == WindowState.Minimized) panel.WindowState = WindowState.Normal; panel.Activate();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        timer?.Stop(); visibilityTimer?.Stop();
        SystemEvents.PowerModeChanged -= PowerChanged; SystemEvents.UserPreferenceChanged -= PreferenceChanged; SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        if(ownsInstance&&(settings.HistoryEnabled||history.HasStoredFile))history.Flush(clock.GetUtcNow(),true).GetAwaiter().GetResult();
        tray?.Dispose(); mutex?.Dispose(); base.OnExit(e);
    }
}


