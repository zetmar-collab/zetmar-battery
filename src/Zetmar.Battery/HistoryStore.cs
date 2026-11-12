using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Zetmar.Core;
namespace Zetmar.Battery;
public sealed class HistoryStore
{
    public BatteryHistory Data { get; } = new();
    public string? Error { get; private set; }
    public bool HasStoredFile => File.Exists(file);
    private DateTimeOffset lastFlush=DateTimeOffset.MinValue;
    private Task pending=Task.CompletedTask;
    private readonly string file;
    public HistoryStore(string folder,DateTimeOffset now)
    {
        file=Path.Combine(folder,"history.json");
        try { if(File.Exists(file)&&new FileInfo(file).Length<=8*1024*1024)Data.Load(JsonSerializer.Deserialize<HistoryPoint[]>(File.ReadAllText(file))??Array.Empty<HistoryPoint>(),now); }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException) { Error=e.Message; }
    }
    public Task Flush(DateTimeOffset now,bool force=false)
    {
        if(!force&&now-lastFlush<TimeSpan.FromMinutes(5))return pending;
        lastFlush=now;var snapshot=Data.Points.ToArray();
        pending=pending.ContinueWith(_=>
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file+".tmp",JsonSerializer.Serialize(snapshot));File.Move(file+".tmp",file,true);Error=null; }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException) { Error=e.Message;Settings.Log(e.ToString()); }
        },TaskScheduler.Default);
        return pending;
    }
}
