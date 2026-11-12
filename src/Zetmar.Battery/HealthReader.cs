using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
namespace Zetmar.Battery;
public sealed record BatteryHealth(string Name,long? Design,long? Full,long? Cycles)
{
    public double? HealthPercent => Design is >0&&Full is >0?(double)Full.Value/Design.Value*100:null;
}
public static class HealthReader
{
    public static IReadOnlyList<BatteryHealth> Parse(string xml)
    {
        using var text=new StringReader(xml);
        using var reader=XmlReader.Create(text,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=16*1024*1024 });
        var document=XDocument.Load(reader);
        return document.Descendants().Where(e=>e.Name.LocalName=="Battery"&&e.Parent?.Name.LocalName=="Batteries").Select(e=>
        {
            string Get(string key)=>e.Elements().FirstOrDefault(p=>p.Name.LocalName==key)?.Value??"";
            long? Number(string key)=>long.TryParse(Get(key),NumberStyles.Integer,CultureInfo.InvariantCulture,out long v)&&v>=0?v:null;
            return new BatteryHealth(Get("Id"),Number("DesignCapacity"),Number("FullChargeCapacity"),Number("CycleCount"));
        }).ToArray();
    }
    public static async Task<IReadOnlyList<BatteryHealth>> ReadAsync(string folder)
    {
        Directory.CreateDirectory(folder);string report=Path.Combine(folder,"battery-health.xml");
        var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","powercfg.exe")) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        foreach(var arg in new[]{"/batteryreport","/xml","/duration","1","/output",report})start.ArgumentList.Add(arg);
        using var process=Process.Start(start)??throw new IOException("Could not start powercfg");
        var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
        catch(OperationCanceledException) { try { process.Kill(); } catch(InvalidOperationException){} throw new IOException("Battery report timed out"); }
        string error=await errors.ConfigureAwait(false);await output.ConfigureAwait(false);
        if(process.ExitCode!=0)throw new IOException("powercfg: "+error);
        if(new FileInfo(report).Length>16*1024*1024)throw new IOException("Battery report is too large");
        return Parse(await File.ReadAllTextAsync(report).ConfigureAwait(false));
    }
}
