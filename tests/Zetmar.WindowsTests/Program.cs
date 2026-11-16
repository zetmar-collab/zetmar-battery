using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using Zetmar.Battery;
using Zetmar.Core;
internal sealed class FakeSource : IBatterySource { public int Calls; public BatteryReading Value = new(PowerState.Battery,10); public BatteryReading Read() { Calls++; return Value; } }
internal sealed class FakeClock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
internal static class Program
{
    private static int passed;
    static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
    static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        foreach(var child in LogicalTreeHelper.GetChildren(root)) if(child is DependencyObject d) { if(d is T t) yield return t; foreach(var sub in Find<T>(d)) yield return sub; }
    }
    [STAThread] static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(),"ZetmarWindowsTests",Guid.NewGuid().ToString("N"));
        Settings.DataDirectory = folder; Directory.CreateDirectory(folder);
        var settings = new Settings { Language="en",Alarm=true,Theme="light" };
        Check("Initial atomic save",settings.Save()); settings.Theme="dark"; Check("Save creates backup",settings.Save() && File.Exists(Path.Combine(folder,"settings.json.bak")));
        File.WriteAllText(Path.Combine(folder,"settings.json"),"{corrupt"); Check("Recover backup",Settings.Load().Theme=="light");
        Check("Repair corrupted primary",settings.Save());
        string before = File.ReadAllText(Path.Combine(folder,"settings.json"));
        using(var locked = new FileStream(Path.Combine(folder,"settings.json"),FileMode.Open,FileAccess.Read,FileShare.None)) { Check("Write failure reported",!settings.Save() && settings.SaveError!=null); }
        Check("Write failure preserves primary",File.ReadAllText(Path.Combine(folder,"settings.json"))==before);
        Check("Retry clears write failure",settings.Save() && settings.SaveError==null);
        var normalized=Settings.FromJson("{\"FontSize\":100,\"Threshold\":-1,\"WidgetBackground\":\"#abcdef\",\"WidgetForeground\":\"bad\"}");
        Check("Validate imported values",normalized.FontSize==24 && normalized.Threshold==1 && normalized.WidgetBackground=="#ABCDEF" && normalized.WidgetForeground==null);
        bool rejected=false;try{Settings.FromJson("{\"SchemaVersion\":999}");}catch(System.Text.Json.JsonException){rejected=true;}Check("Reject future schema",rejected);
        var source=new FakeSource(); var clock=new FakeClock(); var app=new App(source,clock,settings);
        app.Refresh(); string session=app.Session; source.Value=new(PowerState.Unavailable);clock.Now=clock.Now.AddMinutes(5);app.Refresh();source.Value=new(PowerState.Battery,10);app.Refresh(); Check("Injected clock and API recovery preserve session",app.Session.StartsWith("5"));
        int calls=source.Calls;app.Changed();Check("Appearance does not read battery",source.Calls==calls);
        var panel=new DetailsWindow(app,settings);
        Check("Panel synchronizes lock",Find<CheckBox>(panel).First(c=>(string)c.Content=="Lock position").IsChecked==false);
        settings.Locked=true;panel.UpdateValues();Check("External lock update reflected",Find<CheckBox>(panel).First(c=>(string)c.Content=="Lock position").IsChecked==true);
        var input=Find<TextBox>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="WidgetBackground");input.Text="#123456";panel.Rebuild();input=Find<TextBox>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="WidgetBackground");Check("Rebuild retains color draft",input.Text=="#123456");
        input.Text="#ZZZZZZ";
        var apply=Find<Button>(panel).First(c=>AutomationProperties.GetName(c)=="Widget background color — apply");apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check("Invalid HEX has accessible error",AutomationProperties.GetHelpText(input).StartsWith("Enter"));
        input.Text="#2457a8";apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check("Corrected HEX clears error",AutomationProperties.GetHelpText(input).StartsWith("Color") && settings.WidgetBackground=="#2457A8");
        var copy=Settings.FromJson(settings.ToJson());copy.Anchor="top-left";copy.WidgetView="battery";settings.CopyFrom(copy);Check("Import copies view and anchor",settings.Anchor=="top-left"&&settings.WidgetView=="battery");
        var health=HealthReader.Parse("<BatteryReport xmlns='urn:test'><Batteries><Battery><Id>Test</Id><DesignCapacity>70000</DesignCapacity><FullChargeCapacity>64070</FullChargeCapacity><CycleCount>170</CycleCount></Battery></Batteries></BatteryReport>");
        Check("Health XML ignores namespace",health.Count==1&&health[0].Cycles==170&&health[0].HealthPercent>91&&health[0].HealthPercent<92);
        Check("Health handles missing capacities",HealthReader.Parse("<Report><Batteries><Battery><Id>Missing</Id></Battery></Batteries></Report>")[0].HealthPercent==null);
        bool blocked=false;try{HealthReader.Parse("<!DOCTYPE x [<!ENTITY ext SYSTEM 'file:///never-read'>]><x>&ext;</x>");}catch(System.Xml.XmlException){blocked=true;}Check("XML external entities prohibited",blocked);
        var historyStore=new HistoryStore(folder,clock.Now);historyStore.Data.Add(new(PowerState.Battery,80),clock.Now);historyStore.Flush(clock.Now,true).GetAwaiter().GetResult();
        Check("History persists in isolated folder",new HistoryStore(folder,clock.Now).Data.Points.Count==1);
        foreach(int percent in new[]{0,1,50,100})
        {
            var gauge=new BatteryGauge { Width=88,Height=48 };gauge.Update(new(PowerState.Battery,percent),System.Windows.Media.Brushes.Black);gauge.Measure(new Size(88,48));gauge.Arrange(new Rect(0,0,88,48));
            var image=new System.Windows.Media.Imaging.RenderTargetBitmap(88,48,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(gauge);
            var pixels=new byte[88*48*4];image.CopyPixels(pixels,88*4,0);int visible=pixels.Where((_,i)=>i%4==3).Count(a=>a>0);
            Check("Battery renders "+percent+"%",visible>200);
        }
        Find<ComboBox>(panel).First(c=>AutomationProperties.GetName(c)=="Load appearance profile").SelectedIndex=2;
        Check("Ocean profile applies from UI",settings.WidgetBackground=="#173A5E"&&settings.WidgetForeground=="#FFFFFF");
        Find<ComboBox>(panel).First(c=>AutomationProperties.GetName(c)=="Anchor").SelectedIndex=4;
        Check("Anchor selector updates settings",settings.Anchor=="top-left");
        Find<ComboBox>(panel).First(c=>AutomationProperties.GetName(c)=="Widget content").SelectedIndex=2;
        Check("Battery-only selector updates settings",settings.WidgetView=="battery");
        var profiles=Find<Expander>(panel).First(c=>(string)c.Header=="Appearance profiles");profiles.IsExpanded=true;panel.Rebuild();
        Check("Expanded section survives rebuild",Find<Expander>(panel).First(c=>(string)c.Header=="Appearance profiles").IsExpanded);
        var name=Find<TextBox>(panel).First(c=>AutomationProperties.GetName(c)=="New profile name");name.Text="Personal";
        Find<Button>(panel).First(c=>(string)c.Content=="Save current appearance").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check("Custom appearance profile persists",Settings.Load().Profiles.Any(p=>p.Name=="Personal"&&p.View=="battery"));
        var navItems=Find<ListBoxItem>(panel).Where(c=>AutomationProperties.GetAutomationId(c).StartsWith("Nav")).ToArray();
        Check("Five accessible navigation sections",navItems.Length==5);
        navItems.First(c=>AutomationProperties.GetAutomationId(c)=="Nav1").IsSelected=true;panel.Rebuild();
        Check("Active navigation survives rebuild",Find<ListBoxItem>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="Nav1").IsSelected);
        var draftField=Find<TextBox>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="WidgetBackground");draftField.Text="#102030";
        Find<ListBoxItem>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="Nav4").IsSelected=true;
        Find<ListBoxItem>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="Nav1").IsSelected=true;
        Check("Navigation retains uncommitted color",draftField.Text=="#102030");
        foreach(var theme in new[]{"light","dark"})
        {
            settings.Theme=theme;panel.Rebuild();
            Check(theme+" primary text contrast",UiTheme.Contrast(UiTheme.Brush(panel,"CardBrush"),UiTheme.Brush(panel,"TextBrush"))>=4.5);
            Check(theme+" secondary text contrast",UiTheme.Contrast(UiTheme.Brush(panel,"CardBrush"),UiTheme.Brush(panel,"SecondaryTextBrush"))>=4.5);
            Check(theme+" primary action contrast",UiTheme.Contrast(UiTheme.Brush(panel,"AccentBrush"),UiTheme.Brush(panel,"OnAccentBrush"))>=4.5);
            Check(theme+" input boundary contrast",UiTheme.Contrast(UiTheme.Brush(panel,"ControlBrush"),UiTheme.Brush(panel,"OutlineBrush"))>=3);
        }
        var fieldFocus=Find<TextBox>(panel).First(c=>AutomationProperties.GetAutomationId(c)=="WidgetBackground");
        Check("Keyboard focus style present",fieldFocus.FocusVisualStyle!=null);
        var root=(FrameworkElement)panel.Content;root.Measure(new Size(624,562));root.Arrange(new Rect(0,0,624,562));root.UpdateLayout();
        var colorRow=Find<StackPanel>(panel).First(c=>c.Orientation==Orientation.Horizontal&&c.Children.OfType<TextBox>().Any(t=>AutomationProperties.GetAutomationId(t)=="WidgetBackground"));
        Check("Color editor fits compact viewport",colorRow.DesiredSize.Width<=colorRow.ActualWidth+1);
        settings.FontSize=24;settings.Threshold=17;settings.ChargeThreshold=85;panel.Rebuild();
        var numeric=Find<ComboBox>(panel).ToArray();
        Check("Imported numeric preferences remain visible",numeric.First(c=>AutomationProperties.GetName(c)=="Text size").SelectedItem?.ToString()=="24"&&numeric.First(c=>AutomationProperties.GetName(c)=="Alert threshold").SelectedItem?.ToString()=="17%"&&numeric.First(c=>AutomationProperties.GetName(c)=="Charge alert target").SelectedItem?.ToString()=="85%");
        string expected=@"C:\Apps\ZETMAR Battery.exe";
        Check("Startup disabled when absent",StartupRegistration.Inspect(null,expected,_=>true).Kind==StartupKind.Off);
        Check("Startup command quotes spaces",StartupRegistration.CreateCommand(expected)=="\""+expected+"\" --startup");
        Check("Startup accepts current legacy command",StartupRegistration.Inspect("\""+expected+"\"",expected,_=>true).Kind==StartupKind.Ready);
        Check("Startup verifies target existence",StartupRegistration.Inspect("\""+expected+"\"",expected,_=>false).Kind==StartupKind.MissingTarget);
        Check("Startup detects moved application",StartupRegistration.Inspect("\"C:\\Old\\ZETMAR Battery.exe\"",expected,_=>true).Kind==StartupKind.OtherLocation);
        Check("Startup rejects malformed commands",StartupRegistration.Inspect("cmd /c something",expected,_=>true).Kind==StartupKind.Invalid);
        Check("Startup repair button is present",Find<Button>(panel).Any(c=>(string)c.Content=="Repair startup"));
        Settings.Autostart=true;Check("Startup can enable in isolated profile",Settings.Autostart);
        Check("Startup creates a real isolated shortcut",File.Exists(Settings.StartupShortcut)&&Settings.StartupShortcut.StartsWith(folder,StringComparison.OrdinalIgnoreCase));
        Check("Shortcut detects another executable location",StartupRegistration.InspectShortcut(Settings.StartupShortcut,expected).Kind==StartupKind.OtherLocation);
        string startupCopy=Path.Combine(folder,"Temporary battery.exe");File.Copy(Environment.ProcessPath!,startupCopy);
        StartupRegistration.WriteShortcut(Settings.StartupFolder,startupCopy);File.Delete(startupCopy);
        Check("Shortcut detects deleted target",Settings.StartupStatus.Kind==StartupKind.MissingTarget);
        Settings.Autostart=true;Check("Repair replaces broken shortcut",Settings.Autostart);
        bool missingRejected=false;try{StartupRegistration.WriteShortcut(Settings.StartupFolder,startupCopy);}catch(IOException){missingRejected=true;}
        Check("Failed replacement preserves valid shortcut",missingRejected&&Settings.Autostart);
        string unrelated=Path.Combine(Settings.StartupFolder,"Other application.lnk");File.WriteAllText(unrelated,"unrelated");
        Settings.Autostart=false;Check("Startup can disable in isolated profile",!Settings.Autostart);
        Check("Disable removes only owned shortcut",!File.Exists(Settings.StartupShortcut)&&File.Exists(unrelated));
        panel.Close();app.Shutdown();Console.WriteLine($"{passed} Windows tests passed. Isolated data: {folder}");
    }
}
