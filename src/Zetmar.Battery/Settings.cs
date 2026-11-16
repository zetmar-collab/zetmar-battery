using System.Linq;
using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
namespace Zetmar.Battery;
public sealed record AppearanceProfile(string Name, double FontSize, bool Compact, string? Background, string? Foreground, string View);
public sealed class Settings
{
    public string Language { get; set; } = "system";
    public string Theme { get; set; } = "system";
    public string? WidgetBackground { get; set; }
    public string? WidgetForeground { get; set; }
    public bool WidgetVisible { get; set; } = true;
    public bool Locked { get; set; }
    public string? MonitorDevice { get; set; }
    public string Anchor { get; set; } = "free";
    public bool SnapToEdges { get; set; } = true;
    public string WidgetView { get; set; } = "full";
    public System.Collections.Generic.List<AppearanceProfile> Profiles { get; set; } = new();
    public bool ChargeAlarm { get; set; }
    public int ChargeThreshold { get; set; } = 80;
    public bool Hotkeys { get; set; }
    public string HotkeyModifiers { get; set; } = "ctrl-alt";
    public bool HistoryEnabled { get; set; }
    public bool Topmost { get; set; } = true;
    public bool Compact { get; set; }
    public bool Estimate { get; set; }
    public bool Alarm { get; set; }
    public int Threshold { get; set; } = 20;
    public double FontSize { get; set; } = 15;
    // Physical coordinates survive monitor DPI changes; null means automatic placement.
    public int? X { get; set; }
    public int? Y { get; set; }
    public static string? DataDirectory { get; set; }
    public static string Folder => DataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZetmarBattery");
    internal static string InstanceSuffix => DataDirectory == null ? "" : "." + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(Folder).ToUpperInvariant())))[..16];
    public const int CurrentSchemaVersion = 2;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    [System.Text.Json.Serialization.JsonIgnore] public string? SaveError { get; private set; }
    public static Settings FromJson(string json)
    {
        var s = JsonSerializer.Deserialize<Settings>(json) ?? throw new JsonException("Empty configuration");
        if (s.SchemaVersion > CurrentSchemaVersion || s.SchemaVersion < 1) throw new JsonException("Unsupported configuration version");
        s.SchemaVersion = CurrentSchemaVersion;
        s.FontSize = double.IsFinite(s.FontSize) ? Math.Clamp(s.FontSize, 12, 24) : 15;
        s.Threshold = Math.Clamp(s.Threshold, 1, 50);
        if (s.Language is not ("pl" or "en" or "system")) s.Language = "system";
        if (s.Theme is not ("light" or "dark" or "system")) s.Theme = "system";
        s.WidgetBackground = WidgetColors.Normalize(s.WidgetBackground);
        s.WidgetForeground = WidgetColors.Normalize(s.WidgetForeground);
        if (s.Anchor is not ("free" or "bottom-right" or "bottom-left" or "top-right" or "top-left")) s.Anchor = "free";
        if (s.WidgetView is not ("full" or "percent" or "battery")) s.WidgetView = "full";
        if (s.HotkeyModifiers is not ("ctrl-alt" or "ctrl-shift" or "alt-shift")) s.HotkeyModifiers = "ctrl-alt";
        s.ChargeThreshold = Math.Clamp(s.ChargeThreshold, 50, 100);
        s.Profiles = (s.Profiles ?? new()).Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name)).Take(10)
            .Select(p => p with { Name=p.Name.Trim()[..Math.Min(p.Name.Trim().Length,40)], FontSize=double.IsFinite(p.FontSize)?Math.Clamp(p.FontSize,12,24):15,
                Background=WidgetColors.Normalize(p.Background),Foreground=WidgetColors.Normalize(p.Foreground),View=p.View is "percent" or "battery"?p.View:"full" }).ToList();
        return s;
    }
    public static Settings Load(string? folder = null)
    {
        foreach (var name in new[] { "settings.json", "settings.json.bak" })
        {
            try { return FromJson(ReadConfig(Path.Combine(folder ?? Folder, name))); }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        }
        return new();
    }
    public void CopyFrom(Settings other)
    {
        foreach (var property in typeof(Settings).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            if (property.SetMethod?.IsPublic == true) property.SetValue(this,property.GetValue(other));
    }
    public static string ReadConfig(string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024) throw new IOException("Configuration is too large");
        return File.ReadAllText(path);
    }
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    public bool Save() => SaveTo(Folder);
    public bool SaveTo(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "settings.json"), temp = file + ".tmp";
            File.WriteAllText(temp, ToJson());
            bool valid = false;
            if (File.Exists(file))
            {
                try { FromJson(ReadConfig(file)); valid = true; } catch (JsonException) { }
            }
            if (valid) File.Replace(temp, file, file + ".bak");
            else File.Move(temp, file, true);
            SaveError = null; return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        { SaveError = e.Message; Log(e.ToString()); return false; }
    }
    public static void Log(string message)
    {
        try { Directory.CreateDirectory(Folder); var p = Path.Combine(Folder, "app.log"); if (File.Exists(p) && new FileInfo(p).Length > 262144) File.Move(p, p + ".old", true); File.AppendAllText(p, $"{DateTimeOffset.Now:O} {message}\n"); } catch { }
    }
    public static string StartupFolder => DataDirectory!=null ? Path.Combine(Folder,"startup") : Environment.GetFolderPath(Environment.SpecialFolder.Startup);
    public static string StartupShortcut => Path.Combine(StartupFolder,StartupRegistration.ShortcutName);
    public static StartupState StartupStatus
    {
        get
        {
            try
            {
                var shortcut=StartupRegistration.InspectShortcut(StartupShortcut,Environment.ProcessPath??"");
                if(shortcut.Kind!=StartupKind.Off||DataDirectory!=null)return shortcut;
                using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                string? command=key?.GetValue("ZetmarBattery") as string;
                return StartupRegistration.Inspect(command,Environment.ProcessPath??"");
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {return new(StartupKind.Unavailable,Error:e.Message);}
        }
    }
    public static bool Autostart
    {
        get=>StartupStatus.Kind==StartupKind.Ready;
        set
        {
            if(value)
            {
                string path=Environment.ProcessPath??throw new IOException("Application path unavailable");
                StartupRegistration.WriteShortcut(StartupFolder,path);
            }
            else File.Delete(StartupShortcut);
            if(DataDirectory!=null)return;
            using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true);
            key?.DeleteValue("ZetmarBattery",false);
        }
    }
}
