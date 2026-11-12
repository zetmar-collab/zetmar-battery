using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Zetmar.Battery;
public sealed class DetailsWindow : Window
{
    private readonly App app;
    private readonly Settings settings;
    private TextBlock summary = new(), state = new(), source = new(), session = new();
    private StackPanel body = new();
    private readonly List<(CheckBox Check, Func<bool> Read)> checks = new();
    private readonly Dictionary<string, string> drafts = new();
    private readonly HashSet<string> errors = new();
    private readonly Dictionary<string, TextBox> colorInputs = new();
    private ScrollViewer? scroll;
    private readonly Dictionary<int,bool> expanded=new();
    private readonly Dictionary<string,Control> focusControls=new();
    private int sectionIndex,controlIndex;
    private bool healthReading;
    private string healthText="";
    private TextBlock healthOutput=new();
    private Button healthButton=new(), retrySaveButton=new();
    private TextBlock saveStatus = new(), progress = new(), hotkeyStatus = new(), startupStatus = new();
    private TextBlock remaining=new(),previewText=new(),previewContrast=new();
    private BatteryGauge heroBattery=new(),previewBattery=new();
    private Border preview=new(),errorBanner=new();
    private readonly Dictionary<int,ScrollViewer> pages=new();
    private readonly Dictionary<int,double> pageOffsets=new();
    private ListBox navigation=new();
    private int selectedPage;
    private ColumnDefinition sidebar=new();
    public DetailsWindow(App app, Settings settings)
    {
        this.app = app; this.settings = settings;
        Title = "ZETMAR Battery";
        Width=Math.Min(900,Math.Max(580,SystemParameters.WorkArea.Width-32));
        Height=Math.Min(820,Math.Max(300,SystemParameters.WorkArea.Height-40));
        MinWidth=Math.Min(640,Width);MinHeight=Math.Min(400,Height);
        UseLayoutRounding=true;SnapsToDevicePixels=true;
        KeyboardNavigation.SetTabNavigation(this,KeyboardNavigationMode.Cycle);
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/battery.ico"));
        SizeChanged+=(_,_)=>sidebar.Width=new GridLength(184);
        SourceInitialized+=(_,_)=>UiTheme.ApplyTitleBar(this,app.Dark);
        Rebuild();
    }
    private TextBlock Text(string value, double size = 14) => new() { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12), Foreground = size<=13?UiTheme.Brush(this,"SecondaryTextBrush"):app.Ink, LineHeight=size*1.45 };
    private void Check(string label, Func<bool> read, Action<bool> changed, bool reset = false)
    {
        var check = new CheckBox { Content = label, IsChecked = read(), Foreground = app.Ink, Margin = new Thickness(0, 0, 0, 4) };
        check.Tag="control-"+controlIndex++;focusControls[(string)check.Tag]=check;
        AutomationProperties.SetName(check, label);
        checks.Add((check, read));
        check.Click += (_, _) => { changed(check.IsChecked == true); app.Changed(reset); check.IsChecked = read(); };
        body.Children.Add(check);
    }
    private void Select(string label, string[] labels, int index, Action<int> changed)
    {
        var combo = new ComboBox { ItemsSource = labels, SelectedIndex = index, Margin = new Thickness(0, 0, 0, 14), MinHeight = 40 };
        combo.Tag="control-"+controlIndex++;focusControls[(string)combo.Tag]=combo;
        var caption = new Label { Content = label, Foreground = app.Ink, Target = combo, Padding = new Thickness(0, 0, 0, 5) };
        AutomationProperties.SetName(combo, label); body.Children.Add(caption); body.Children.Add(combo);
        combo.SelectionChanged += (_, _) => { if (combo.SelectedIndex < 0) return; combo.IsDropDownOpen=false; changed(combo.SelectedIndex); app.Changed(); Rebuild(); };
    }
    private sealed class DialogOwner(IntPtr handle) : System.Windows.Forms.IWin32Window { public IntPtr Handle => handle; }
    private void ColorSetting(string key, string label, Func<string?> read, Action<string?> write, Brush fallback)
    {
        var input = new TextBox { Text = drafts.TryGetValue(key, out var draft) ? draft : read() ?? "", Width = 95, MaxLength = 7, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        input.Tag = key;
        AutomationProperties.SetAutomationId(input, key);
        colorInputs[key] = input; focusControls[key]=input;
        var validation = Text("", 12); validation.Visibility = Visibility.Collapsed;
        AutomationProperties.SetLiveSetting(validation, System.Windows.Automation.AutomationLiveSetting.Assertive);
        string hint = app.T("Kolor #RRGGBB; puste pole przywraca kolor automatyczny.");
        string error = app.T("Wpisz kolor w formacie #RRGGBB, np. #2457A8.");
        void Feedback(bool invalid)
        {
            input.ToolTip = invalid ? error : hint;
            AutomationProperties.SetHelpText(input, invalid ? error : hint);
            validation.Text = invalid ? "! " + error : "";
            validation.Visibility = invalid ? Visibility.Visible : Visibility.Collapsed;
            if (invalid) input.BorderBrush = Brushes.Red; else input.ClearValue(Control.BorderBrushProperty);
        }
        Feedback(errors.Contains(key));
        input.TextChanged += (_, _) => drafts[key] = input.Text;
        Border? colorSwatch=null;
        void Accept(string? value)
        {
            input.Text = value ?? ""; drafts.Remove(key); errors.Remove(key); Feedback(false);
            write(value); app.Changed(); if(colorSwatch!=null)colorSwatch.Background=WidgetColors.Brush(read())??fallback;
        }
        AutomationProperties.SetName(input, label + " HEX");
        var caption = new Label { Content = label, Foreground = app.Ink, Target = input, Padding = new Thickness(0, 0, 0, 5) };
        body.Children.Add(caption);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        var swatch=new Border { Width=30,Height=30,CornerRadius=new CornerRadius(6),Margin=new Thickness(0,0,8,0),BorderThickness=new Thickness(1),BorderBrush=UiTheme.Brush(this,"OutlineBrush"),Background=WidgetColors.Brush(read())??fallback };
        colorSwatch=swatch;row.Children.Add(swatch);row.Children.Add(input);
        var choose = new Button { Content = app.T("Wybierz…"), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 8, 0) };
        AutomationProperties.SetName(choose, label + app.T(" — wybierz kolor"));
        choose.Click += (_, _) =>
        {
            using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true, SolidColorOnly = true };
            if ((WidgetColors.Brush(read()) ?? fallback) is SolidColorBrush brush) dialog.Color = System.Drawing.Color.FromArgb(brush.Color.R, brush.Color.G, brush.Color.B);
            if (dialog.ShowDialog(new DialogOwner(new System.Windows.Interop.WindowInteropHelper(this).Handle)) != System.Windows.Forms.DialogResult.OK) return;
            input.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
            Accept(input.Text);
        };
        row.Children.Add(choose);
        void Apply()
        {
            var value = WidgetColors.Normalize(input.Text);
            if (!string.IsNullOrWhiteSpace(input.Text) && value == null) { errors.Add(key); Feedback(true); return; }
            Accept(value);
        }
        var apply = new Button { Content = app.T("Zastosuj"), Padding = new Thickness(10, 6, 10, 6) };
        AutomationProperties.SetName(apply, label + app.T(" — zastosuj"));
        apply.Click += (_, _) => Apply();
        input.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Apply(); e.Handled = true; } };
        row.Children.Add(apply); body.Children.Add(row); body.Children.Add(validation);
    }
    private void Section(string title, Action build)
    {
        var parent=body; var content=new StackPanel { Margin=new Thickness(0,16,0,0) };
        var section=new Expander { Header=title,Content=content,Foreground=app.Ink,Margin=new Thickness(0,0,0,16) };
        System.Windows.Documents.TextElement.SetFontWeight(content,FontWeights.Normal);
        int id=sectionIndex++;section.IsExpanded=expanded.GetValueOrDefault(id);
        section.Expanded+=(_,_)=>expanded[id]=true;section.Collapsed+=(_,_)=>expanded[id]=false;
        parent.Children.Add(new Border { Background=UiTheme.Brush(this,"CardBrush"),BorderBrush=UiTheme.Brush(this,"CardBorderBrush"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(18),Margin=new Thickness(0,0,0,16),Child=section });body=content;build();body=parent;
    }
    private void Button(string text, Action action)
    {
        var button=new Button { Content=text,Padding=new Thickness(10,7,10,7),Margin=new Thickness(0,0,0,10) };
        button.Click+=(_,_)=>
        {
            try { action(); }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or System.Security.SecurityException)
            { MessageBox.Show(this,app.T("Nie udało się wykonać operacji: ")+e.Message,"ZETMAR Battery",MessageBoxButton.OK,MessageBoxImage.Warning); }
        };
        body.Children.Add(button);
    }
    private void AddProfiles()
    {
        var presets=new[] {
            new AppearanceProfile(app.T("Systemowy"),15,false,null,null,"full"),
            new AppearanceProfile(app.T("Ocean"),15,false,"#173A5E","#FFFFFF","full"),
            new AppearanceProfile(app.T("Bursztyn"),18,true,"#252525","#FFD166","full") };
        var profiles=presets.Concat(settings.Profiles).ToArray();
        Select(app.T("Wczytaj profil wyglądu"),new[]{app.T("Wybierz profil…")}.Concat(profiles.Select(p=>p.Name)).ToArray(),0,i=>
        {
            if(i==0)return;var p=profiles[i-1];settings.FontSize=p.FontSize;settings.Compact=p.Compact;
            settings.WidgetBackground=p.Background;settings.WidgetForeground=p.Foreground;settings.WidgetView=p.View;drafts.Clear();errors.Clear();
        });
        var name=new TextBox { MaxLength=40,Margin=new Thickness(0,0,0,8) };
        body.Children.Add(new Label { Content=app.T("Nazwa nowego profilu"),Foreground=app.Ink,Target=name });
        AutomationProperties.SetName(name,app.T("Nazwa nowego profilu"));body.Children.Add(name);
        Button(app.T("Zapisz bieżący wygląd"),()=>
        {
            string value=name.Text.Trim();
            if(value.Length==0){name.Focus();return;}
            var existing=settings.Profiles.FindIndex(p=>string.Equals(p.Name,value,StringComparison.OrdinalIgnoreCase));
            var profile=new AppearanceProfile(value,settings.FontSize,settings.Compact,settings.WidgetBackground,settings.WidgetForeground,settings.WidgetView);
            if(existing>=0)settings.Profiles[existing]=profile;
            else if(settings.Profiles.Count<10)settings.Profiles.Add(profile);
            else { MessageBox.Show(this,app.T("Można zapisać maksymalnie 10 profili. Użyj nazwy istniejącego profilu, aby go zastąpić."));return; }
            app.Changed();Rebuild();
        });
    }
    private void AddPosition()
    {
        var monitors=System.Windows.Forms.Screen.AllScreens;
        var devices=new string?[]{null}.Concat(monitors.Select(s=>(string?)s.DeviceName)).ToArray();
        var labels=new[]{app.T("Automatyczny")}.Concat(monitors.Select(s=>$"{s.DeviceName} · {s.Bounds.Width} × {s.Bounds.Height}" )).ToArray();
        Select(app.T("Monitor widżetu"),labels,Math.Max(0,Array.IndexOf(devices,settings.MonitorDevice)),i=>{settings.MonitorDevice=devices[i];settings.X=settings.Y=null;});
        var anchors=new[]{"free","bottom-right","bottom-left","top-right","top-left"};
        Select(app.T("Zakotwiczenie"),new[]{app.T("Swobodnie / przeciąganie"),app.T("Prawy dolny róg"),app.T("Lewy dolny róg"),app.T("Prawy górny róg"),app.T("Lewy górny róg")},Array.IndexOf(anchors,settings.Anchor),i=>settings.Anchor=anchors[i]);
        Check(app.T("Przyciągaj do krawędzi"),()=>settings.SnapToEdges,v=>settings.SnapToEdges=v);
    }
    private void AddConfigTools()
    {
        Button(app.T("Eksportuj ustawienia…"),()=>
        {
            var dialog=new Microsoft.Win32.SaveFileDialog { Filter="JSON|*.json",FileName="ZetmarBattery-settings.json" };
            if(dialog.ShowDialog(this)==true)File.WriteAllText(dialog.FileName,settings.ToJson());
        });
        void Replace(Settings incoming)
        {
            if(!settings.Save()){app.Changed();return;}
            settings.CopyFrom(incoming);app.Changed(true);Rebuild(true);
        }
        Button(app.T("Importuj ustawienia…"),()=>
        {
            var dialog=new Microsoft.Win32.OpenFileDialog { Filter="JSON|*.json",CheckFileExists=true };
            if(dialog.ShowDialog(this)==true)Replace(Settings.FromJson(Settings.ReadConfig(dialog.FileName)));
        });
        Button(app.T("Przywróć poprzedni zapis"),()=>Replace(Settings.FromJson(Settings.ReadConfig(Path.Combine(Settings.Folder,"settings.json.bak")))));
        Button(app.T("Przywróć ustawienia domyślne"),()=>Replace(new Settings()));
        body.Children.Add(Text(app.T("Import i reset zmieniają ustawienia aplikacji. Autostart w Windows ustawiaj osobnym przełącznikiem."),12));
    }
    private void Card(string title,Action build)
    {
        var parent=body;var content=new StackPanel();
        var card=new Border { Child=content,Padding=new Thickness(20),CornerRadius=new CornerRadius(12),Background=UiTheme.Brush(this,"CardBrush"),BorderBrush=UiTheme.Brush(this,"CardBorderBrush"),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,16) };
        parent.Children.Add(card);body=content;
        if(title.Length>0){var heading=Text(title,16);heading.FontWeight=FontWeights.SemiBold;body.Children.Add(heading);}
        build();body=parent;
    }
    private TextBlock Metric(string label,Panel target)
    {
        var stack=new StackPanel { Margin=new Thickness(0,0,14,0) };
        var caption=Text(label,12);caption.Margin=new Thickness(0,0,0,5);stack.Children.Add(caption);
        var value=Text("—",16);value.Margin=new Thickness(0);value.FontWeight=FontWeights.SemiBold;stack.Children.Add(value);target.Children.Add(stack);return value;
    }
    private void AddOverview()
    {
        Card("",()=>
        {
            state=Text("",13);state.FontWeight=FontWeights.SemiBold;state.Foreground=app.Accent;
            body.Children.Add(state);
            var hero=new Grid { Margin=new Thickness(0,0,0,20) };
            hero.ColumnDefinitions.Add(new ColumnDefinition());hero.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
            var values=new StackPanel();summary=Text("—",58);summary.FontWeight=FontWeights.SemiBold;summary.Margin=new Thickness(0);values.Children.Add(summary);
            var note=Text(app.T("Poziom naładowania"),13);note.Margin=new Thickness(0);values.Children.Add(note);hero.Children.Add(values);
            heroBattery.Width=112;heroBattery.Height=60;heroBattery.Margin=new Thickness(16,0,0,0);Grid.SetColumn(heroBattery,1);hero.Children.Add(heroBattery);body.Children.Add(hero);
            body.Children.Add(new Border { Height=1,Background=UiTheme.Brush(this,"CardBorderBrush"),Margin=new Thickness(0,0,0,18) });
            var metrics=new System.Windows.Controls.Primitives.UniformGrid { Columns=2 };
            remaining=Metric(app.T("Pozostały czas"),metrics);session=Metric(app.T("Sesja na baterii"),metrics);body.Children.Add(metrics);
            var sourceRow=new StackPanel { Margin=new Thickness(0,18,0,0) };source=Text("",12);source.Margin=new Thickness(0);sourceRow.Children.Add(source);body.Children.Add(sourceRow);
            progress=Text("",12);progress.Margin=new Thickness(0,8,0,0);body.Children.Add(progress);
        });
        Card(app.T("Szybki dostęp"),()=>
        {
            Check(app.T("Pokaż widżet"),()=>settings.WidgetVisible,v=>settings.WidgetVisible=v);
            var buttons=new System.Windows.Controls.Primitives.UniformGrid { Columns=2,Margin=new Thickness(0,12,0,0) };
            var appearance=new Button { Content=app.T("Zmień wygląd"),Style=(Style)FindResource("PrimaryButton"),Margin=new Thickness(0,0,8,0) };
            appearance.Click+=(_,_)=>Navigate(1);buttons.Children.Add(appearance);
            var data=new Button { Content=app.T("Historia i kondycja") };data.Click+=(_,_)=>Navigate(3);buttons.Children.Add(data);body.Children.Add(buttons);
        });
        body.Children.Add(Text(app.T("Dane odświeżają się automatycznie co około 30 sekund. Symbol ~ oznacza czas przybliżony."),13));
    }
    private void AddWidget()
    {
        Card(app.T("Podgląd widżetu"),()=>
        {
            var inner=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center };
            previewBattery.Width=32;previewBattery.Height=18;previewBattery.Margin=new Thickness(0,0,10,0);previewBattery.VerticalAlignment=VerticalAlignment.Center;inner.Children.Add(previewBattery);
            previewText=Text("",15);previewText.Margin=new Thickness(0);previewText.VerticalAlignment=VerticalAlignment.Center;inner.Children.Add(previewText);
            preview=new Border { Child=inner,CornerRadius=new CornerRadius(10),Padding=new Thickness(16,12,16,12),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,12,0,16),BorderThickness=new Thickness(1),BorderBrush=UiTheme.Brush(this,"OutlineBrush") };
            var viewport=new Viewbox { Child=preview,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,MaxHeight=96 };AutomationProperties.SetAutomationId(viewport,"WidgetPreview");body.Children.Add(viewport);previewContrast=Text("",12);previewContrast.Margin=new Thickness(0);body.Children.Add(previewContrast);
        });
        Card(app.T("Kolory i zawartość"),()=>
        {
            ColorSetting(nameof(Settings.WidgetBackground),app.T("Kolor tła widżetu"),()=>settings.WidgetBackground,v=>settings.WidgetBackground=v,app.WidgetBackground);
            ColorSetting(nameof(Settings.WidgetForeground),app.T("Kolor tekstu i baterii"),()=>settings.WidgetForeground,v=>settings.WidgetForeground=v,app.WidgetForeground);
            Button(app.T("Przywróć kolory automatyczne"),()=>{settings.WidgetBackground=settings.WidgetForeground=null;app.Changed();Rebuild(true);});
            body.Children.Add(Text(app.T("Wybierz kolor z palety lub wpisz HEX i zastosuj. Bez własnego koloru tekstu kontrast dopasuje się automatycznie."),13));
            var sizes=new[]{12d,15d,18d,22d,settings.FontSize}.Distinct().OrderBy(v=>v).ToArray();
            Select(app.T("Rozmiar tekstu"),sizes.Select(v=>v.ToString("0.#")).ToArray(),Array.IndexOf(sizes,settings.FontSize),i=>settings.FontSize=sizes[i]);
            Select(app.T("Elementy widżetu"),new[]{app.T("Bateria, procent i czas"),app.T("Bateria i procent"),app.T("Tylko bateria")},Array.IndexOf(new[]{"full","percent","battery"},settings.WidgetView),i=>settings.WidgetView=new[]{"full","percent","battery"}[i]);
            Check(app.T("Skrócony czas (godziny:minuty)"),()=>settings.Compact,v=>settings.Compact=v);
        });
        Card(app.T("Widoczność i zachowanie"),()=>
        {
            Check(app.T("Pokaż widżet"),()=>settings.WidgetVisible,v=>settings.WidgetVisible=v);
            Check(app.T("Zawsze na wierzchu"),()=>settings.Topmost,v=>settings.Topmost=v);
            Check(app.T("Zablokuj położenie"),()=>settings.Locked,v=>settings.Locked=v);
        });
        Section(app.T("Profile wyglądu"),AddProfiles);
        Section(app.T("Położenie i monitor"),()=>{AddPosition();Button(app.T("Przywróć położenie widżetu"),()=>{settings.X=settings.Y=null;settings.Anchor="bottom-right";app.Changed();});});
    }
    private void AddAlerts()
    {
        Card(app.T("Niski poziom baterii"),()=>
        {
            body.Children.Add(Text(app.T("Przypomnienie o podłączeniu zasilacza, zanim zabraknie energii."),13));
            Check(app.T("Alarm niskiego poziomu baterii"),()=>settings.Alarm,v=>settings.Alarm=v);
            var levels=new[]{5,10,15,20,25,30,settings.Threshold}.Distinct().OrderBy(v=>v).ToArray();
            Select(app.T("Próg alarmu"),levels.Select(v=>v+"%").ToArray(),Array.IndexOf(levels,settings.Threshold),i=>settings.Threshold=levels[i]);
        });
        Card(app.T("Poziom docelowy ładowania"),()=>
        {
            body.Children.Add(Text(app.T("Powiadomienie po osiągnięciu wybranego poziomu. Aplikacja nie zatrzymuje ładowania."),13));
            Check(app.T("Alarm osiągnięcia poziomu ładowania"),()=>settings.ChargeAlarm,v=>settings.ChargeAlarm=v);
            var targets=new[]{80,90,100,settings.ChargeThreshold}.Distinct().OrderBy(v=>v).ToArray();
            Select(app.T("Poziom ładowania dla alarmu"),targets.Select(v=>v+"%").ToArray(),Array.IndexOf(targets,settings.ChargeThreshold),i=>settings.ChargeThreshold=targets[i]);
        });
        body.Children.Add(Text(app.T("Windows może wyciszać powiadomienia w trybie Nie przeszkadzać."),13));
    }
    private void AddSettings()
    {
        Card(app.T("Preferencje aplikacji"),()=>
        {
            Select(app.T("Język"),new[]{app.T("Systemowy"),"Polski","English"},Array.IndexOf(new[]{"system","pl","en"},settings.Language),i=>settings.Language=new[]{"system","pl","en"}[i]);
            Select(app.T("Motyw"),new[]{app.T("Systemowy"),app.T("Jasny"),app.T("Ciemny")},Array.IndexOf(new[]{"system","light","dark"},settings.Theme),i=>settings.Theme=new[]{"system","light","dark"}[i]);
            Check(app.T("Uruchamiaj po zalogowaniu"),()=>Settings.Autostart,v=>
            {
                try{Settings.Autostart=v;}catch(Exception e){Settings.Log(e.ToString());MessageBox.Show(this,app.T("Nie udało się zmienić autostartu."));}
            });
            startupStatus=Text("",13);body.Children.Add(startupStatus);
            Button(app.T("Napraw autostart"),()=>{Settings.Autostart=true;app.Changed();UpdateValues();});
            Button(app.T("Autostart w ustawieniach Windows…"),()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:startupapps"){UseShellExecute=true}));
            body.Children.Add(Text(app.T("Program uruchomi się po zalogowaniu. Windows może opóźnić start lub wyłączyć go w swoich ustawieniach."),13));
        });
        Section(app.T("Skróty klawiaturowe"),()=>
        {
            Check(app.T("Włącz skróty globalne"),()=>settings.Hotkeys,v=>settings.Hotkeys=v);
            Select(app.T("Kombinacja klawiszy"),new[]{"Ctrl+Alt","Ctrl+Shift","Alt+Shift"},Array.IndexOf(new[]{"ctrl-alt","ctrl-shift","alt-shift"},settings.HotkeyModifiers),i=>settings.HotkeyModifiers=new[]{"ctrl-alt","ctrl-shift","alt-shift"}[i]);
            body.Children.Add(Text(app.T("+B: panel · +W: pokaż/ukryj widżet"),13));hotkeyStatus=Text("");body.Children.Add(hotkeyStatus);
        });
        Section(app.T("Kopia i przenoszenie ustawień"),AddConfigTools);
        Card(app.T("Warto wiedzieć"),()=>
        {
            body.Children.Add(Text(app.T("Ukryta ikona? Ustawienia Windows → Personalizacja → Pasek zadań → Inne ikony zasobnika systemowego. Możesz też przeciągnąć ikonę z menu ^ obok zegara."),13));
            body.Children.Add(Text(app.T("Widżet jest osobnym oknem nad paskiem zadań. Przeciągnij go, aby zmienić położenie. Dwuklik otwiera ten panel."),13));
        });
    }
    private void Navigate(int page)
    {
        selectedPage=page;navigation.SelectedIndex=page;
        foreach(var item in pages)item.Value.Visibility=item.Key==page?Visibility.Visible:Visibility.Collapsed;
        scroll=pages.GetValueOrDefault(page);
    }
    private FrameworkElement NavIcon(ListBoxItem item,string geometry)
    {
        var path=new System.Windows.Shapes.Path { Data=Geometry.Parse(geometry),StrokeThickness=1.6,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round };
        path.SetBinding(System.Windows.Shapes.Path.StrokeProperty,new System.Windows.Data.Binding("Foreground"){Source=item});
        var canvas=new Canvas { Width=24,Height=24 };canvas.Children.Add(path);
        return new Viewbox { Width=18,Height=18,Child=canvas,Margin=new Thickness(0,0,8,0),IsHitTestVisible=false };
    }
    public void Rebuild(bool discardEdits=false)
    {
        if(discardEdits){drafts.Clear();errors.Clear();}
        foreach(var page in pages)pageOffsets[page.Key]=page.Value.VerticalOffset;
        string? focused=(Keyboard.FocusedElement as FrameworkElement)?.Tag as string;
        checks.Clear();colorInputs.Clear();focusControls.Clear();pages.Clear();sectionIndex=controlIndex=0;
        heroBattery=new BatteryGauge();previewBattery=new BatteryGauge();
        UiTheme.Apply(this,app);Background=app.Surface;Foreground=app.Ink;
        var root=new Grid();root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });root.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });root.RowDefinitions.Add(new RowDefinition());Content=root;
        var header=new Grid { Margin=new Thickness(24,16,24,16) };header.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var brandBattery=new BatteryGauge { Width=24,Height=14 };brandBattery.Update(new(Zetmar.Core.PowerState.Battery,75),app.Accent);
        header.Children.Add(new Border { Child=brandBattery,CornerRadius=new CornerRadius(10),Padding=new Thickness(10),Background=UiTheme.Brush(this,"AccentSoftBrush") });
        var brand=Text("ZETMAR Battery",18);brand.FontWeight=FontWeights.SemiBold;brand.VerticalAlignment=VerticalAlignment.Center;brand.Margin=new Thickness(12,0,0,0);Grid.SetColumn(brand,1);header.Children.Add(brand);
        var version=Text("v1.3.1 · Windows 11",12);version.VerticalAlignment=VerticalAlignment.Center;version.Margin=new Thickness(12,0,0,0);Grid.SetColumn(version,2);header.Children.Add(version);
        var headerBorder=new Border { Child=header,Background=UiTheme.Brush(this,"CardBrush"),BorderBrush=UiTheme.Brush(this,"CardBorderBrush"),BorderThickness=new Thickness(0,0,0,1) };root.Children.Add(headerBorder);
        var errorRow=new DockPanel();saveStatus=Text("",13);retrySaveButton=new Button { Content=app.T("Ponów zapis ustawień"),Margin=new Thickness(12,0,0,0) };retrySaveButton.Click+=(_,_)=>app.Changed();DockPanel.SetDock(retrySaveButton,Dock.Right);errorRow.Children.Add(retrySaveButton);errorRow.Children.Add(saveStatus);
        errorBanner=new Border { Child=errorRow,Padding=new Thickness(16),Margin=new Thickness(20,12,20,0),Background=UiTheme.Brush(this,"AccentSoftBrush"),CornerRadius=new CornerRadius(10) };Grid.SetRow(errorBanner,1);root.Children.Add(errorBanner);
        var layout=new Grid();sidebar=new ColumnDefinition { Width=new GridLength(184) };layout.ColumnDefinitions.Add(sidebar);layout.ColumnDefinitions.Add(new ColumnDefinition());Grid.SetRow(layout,2);root.Children.Add(layout);
        var side=new DockPanel { Margin=new Thickness(8,20,8,16) };
        var footer=new StackPanel();footer.Children.Add(Text(app.T("Zamknięcie okna nie kończy pracy aplikacji."),12));
        var exit=new Button { Content=app.T("Zakończ aplikację"),FontSize=12,Padding=new Thickness(8,8,8,8) };KeyboardNavigation.SetTabIndex(exit,1000);exit.Click+=(_,_)=>app.Shutdown();footer.Children.Add(exit);DockPanel.SetDock(footer,Dock.Bottom);side.Children.Add(footer);
        navigation=new ListBox { VerticalAlignment=VerticalAlignment.Top };AutomationProperties.SetName(navigation,app.T("Nawigacja"));side.Children.Add(navigation);
        layout.Children.Add(new Border { Child=side,Background=UiTheme.Brush(this,"CardBrush"),BorderBrush=UiTheme.Brush(this,"CardBorderBrush"),BorderThickness=new Thickness(0,0,1,0) });
        var content=new Grid();Grid.SetColumn(content,1);layout.Children.Add(content);
        string[] names={app.T("Przegląd"),app.T("Widżet"),app.T("Powiadomienia"),app.T("Dane baterii"),app.T("Ustawienia")};
        string[] descriptions={app.T("Najważniejsze informacje w jednym miejscu."),app.T("Dopasuj wygląd i położenie do swojego pulpitu."),app.T("Przypomnienia wtedy, gdy są potrzebne."),app.T("Poznaj zużycie energii i kondycję baterii."),app.T("Język, motyw i preferencje uruchamiania.")};
        string[] icons={"M3,5 H19 V19 H3 Z M20,9 H22 V15 H20","M3,3 H10 V10 H3 Z M14,3 H21 V10 H14 Z M3,14 H10 V21 H3 Z M14,14 H21 V21 H14 Z","M5,17 H19 L17,14 V9 A5,5 0 0 0 7,9 V14 Z M10,20 H14","M3,3 V21 H22 M7,16 L12,10 16,13 21,5","M12,4 A8,8 0 1 0 12,20 A8,8 0 1 0 12,4 M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9"};
        Action[] builders={AddOverview,AddWidget,AddAlerts,AddData,AddSettings};
        for(int i=0;i<names.Length;i++)
        {
            var item=new ListBoxItem();AutomationProperties.SetName(item,names[i]);AutomationProperties.SetAutomationId(item,"Nav"+i);
            var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(26) });row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(NavIcon(item,icons[i]));var label=new TextBlock { Text=names[i],FontSize=13,VerticalAlignment=VerticalAlignment.Center,TextWrapping=TextWrapping.Wrap };Grid.SetColumn(label,1);row.Children.Add(label);item.HorizontalContentAlignment=HorizontalAlignment.Stretch;item.Content=row;navigation.Items.Add(item);
            body=new StackPanel { Margin=new Thickness(24,22,24,24) };var heading=Text(names[i],26);heading.FontWeight=FontWeights.SemiBold;heading.Margin=new Thickness(0,0,0,4);body.Children.Add(heading);body.Children.Add(Text(descriptions[i],13));
            builders[i]();
            var pane=new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed };pages[i]=pane;content.Children.Add(pane);
        }
        navigation.SelectionChanged+=(_,_)=>{if(navigation.SelectedIndex>=0)Navigate(navigation.SelectedIndex);};Navigate(selectedPage);
        UpdateValues();
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>
        {
            foreach(var page in pages)page.Value.ScrollToVerticalOffset(pageOffsets.GetValueOrDefault(page.Key));
            if(IsActive&&focused!=null&&focusControls.TryGetValue(focused,out var field)){field.Focus();field.BringIntoView();}
        }));
    }
    private void AddData()
    {
        Card(app.T("Szacowanie czasu"),()=>
        {
            Check(app.T("Szacuj czas, gdy Windows go nie podaje"),()=>settings.Estimate,v=>settings.Estimate=v,reset:true);
            body.Children.Add(Text(app.T("Własny szacunek wymaga 10 minut obserwacji i spadku 2 punktów procentowych. Zawsze pierwszeństwo ma wynik Windows."),13));
        });
        Section(app.T("Historia i kondycja baterii"),()=>
        {
            Check(app.T("Zapisuj lokalną historię (7 dni)"),()=>settings.HistoryEnabled,v=>settings.HistoryEnabled=v);
            body.Children.Add(Text(app.T("Domyślnie wyłączona. Zapis co około 5 minut; wyłączenie zatrzymuje zbieranie. Dane pozostają wyłącznie na tym komputerze."),12));
            Button(app.T("Pokaż wykres historii"),()=>app.ShowHistory());
            var output=Text(healthText);healthOutput=output;
            var read=new Button { Content=app.T("Odczytaj kondycję baterii"),Padding=new Thickness(10,7,10,7),Margin=new Thickness(0,0,0,10) };
            healthButton=read;read.IsEnabled=!healthReading;
            read.Click+=async(_,_)=>
            {
                healthReading=true;healthButton.IsEnabled=false;healthText=app.T("Odczytywanie raportu Windows…");healthOutput.Text=healthText;
                try
                {
                    var values=await HealthReader.ReadAsync(Settings.Folder);
                    healthText=values.Count==0?app.T("Brak danych o kondycji baterii."):string.Join("\n\n",values.Select(v=>
                        (string.IsNullOrWhiteSpace(v.Name)?app.T("Bateria"):v.Name)+"\n"+
                        app.T("Pojemność projektowa: ")+(v.Design?.ToString()??"—")+" mWh\n"+
                        app.T("Przy pełnym naładowaniu: ")+(v.Full?.ToString()??"—")+" mWh\n"+
                        app.T("Kondycja: ")+(v.HealthPercent?.ToString("0.0")??"—")+"% · "+app.T("Cykle: ")+(v.Cycles?.ToString()??"—")));
                }
                catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.Xml.XmlException or System.ComponentModel.Win32Exception)
                { healthText=app.T("Dane niedostępne: ")+e.Message; }
                finally{healthReading=false;healthButton.IsEnabled=true;healthOutput.Text=healthText;}
            };
            body.Children.Add(read);body.Children.Add(output);
            body.Children.Add(Text(app.T("Odczyt na żądanie z raportu Windows; dostępność zależy od sterownika. Kondycja oznacza stosunek pojemności przy pełnym naładowaniu do projektowej, a nie bieżący procent."),12));
        });
    }
    public void UpdateValues()
    {
        foreach (var item in checks) item.Check.IsChecked = item.Read();
        saveStatus.Text = app.SaveError == null ? "" : app.T("! Zmiana działa, ale nie została zapisana. Użyj „Ponów zapis ustawień”.");
        saveStatus.Visibility = app.SaveError == null ? Visibility.Collapsed : Visibility.Visible;
        retrySaveButton.Visibility=saveStatus.Visibility;
        hotkeyStatus.Text=app.HotkeyError??"";
        var startup=Settings.StartupStatus;
        startupStatus.Text=app.T(startup.Kind switch
        {
            StartupKind.Ready=>"Autostart wskazuje bieżący plik aplikacji.",
            StartupKind.Off=>"Autostart jest wyłączony.",
            StartupKind.MissingTarget=>"! Plik zapisany w autostarcie nie istnieje. Kliknij Napraw autostart.",
            StartupKind.OtherLocation=>"! Autostart wskazuje inną lokalizację. Kliknij Napraw autostart.",
            StartupKind.Invalid=>"! Wpis autostartu jest nieprawidłowy. Kliknij Napraw autostart.",
            _=>"! Nie można odczytać konfiguracji autostartu."
        });
        startupStatus.ToolTip=startup.Target;
        progress.Text = settings.Estimate ? app.EstimationProgress : "";
        progress.Visibility = settings.Estimate ? Visibility.Visible : Visibility.Collapsed;
        errorBanner.Visibility=app.SaveError==null?Visibility.Collapsed:Visibility.Visible;
        summary.Text=app.Current.Percent is int percent?percent+"%":"—";
        heroBattery.Update(app.Current,app.Accent);
        remaining.Text=app.RemainingTime;
        preview.Background=app.WidgetBackground;previewText.Foreground=app.WidgetForeground;
        previewText.Text=settings.WidgetView=="percent"&&app.Current.State is not (Zetmar.Core.PowerState.NoBattery or Zetmar.Core.PowerState.Unavailable)?app.Current.Percent?.ToString()+"%":app.Summary;
        if(settings.WidgetView=="percent"&&app.Current.Percent==null&&app.Current.State is not (Zetmar.Core.PowerState.NoBattery or Zetmar.Core.PowerState.Unavailable))previewText.Text="—";
        previewText.Visibility=settings.WidgetView=="battery"?Visibility.Collapsed:Visibility.Visible;
        previewText.FontSize=settings.FontSize;previewBattery.Width=settings.FontSize*2.1;previewBattery.Height=settings.FontSize*1.15;previewBattery.Margin=new Thickness(0,0,settings.WidgetView=="battery"?0:10,0);previewBattery.Update(app.Current,app.WidgetForeground);
        double contrast=UiTheme.Contrast(app.WidgetBackground,app.WidgetForeground);
        previewContrast.Text=contrast<4.5?app.T("! Mały kontrast. Wybierz jaśniejszy lub ciemniejszy tekst."):app.T("Podgląd pokazuje wybrane kolory i bieżący stan baterii.");
        state.Text = Zetmar.Core.BatteryText.State(app.Current.State, app.Polish);
        source.Text = app.T("Źródło czasu: ") + app.EstimateSource;
        session.Text = app.Session;
    }
}

