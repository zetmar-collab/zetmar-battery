using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
namespace Zetmar.Battery;
public enum StartupKind { Off, Ready, MissingTarget, OtherLocation, Invalid, Unavailable }
public sealed record StartupState(StartupKind Kind,string? Target=null,string? Error=null);
public static class StartupRegistration
{
    public const string ShortcutName="ZETMAR Battery.lnk";
    private static void WithShortcut(string path,Action<dynamic> action)
    {
        object? shell=null,link=null;
        try
        {
            shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")??throw new IOException("Windows shortcut service unavailable"));
            link=((dynamic)shell!).CreateShortcut(path);
            action(link!);
        }
        catch(COMException e){throw new IOException("Cannot access the startup shortcut.",e);}
        finally
        {
            if(link!=null&&Marshal.IsComObject(link))Marshal.FinalReleaseComObject(link);
            if(shell!=null&&Marshal.IsComObject(shell))Marshal.FinalReleaseComObject(shell);
        }
    }
    public static void WriteShortcut(string folder,string executable)
    {
        CreateCommand(executable);
        if(!File.Exists(executable))throw new IOException("Application executable is missing");
        Directory.CreateDirectory(folder);
        string temp=Path.Combine(folder,"ZETMAR-Battery-"+Guid.NewGuid().ToString("N")+".tmp.lnk");
        try
        {
            WithShortcut(temp,link=>
            {
                link.TargetPath=Path.GetFullPath(executable);
                link.Arguments="--startup";
                link.WorkingDirectory=Path.GetDirectoryName(Path.GetFullPath(executable));
                link.IconLocation=executable+",0";
                link.Description="ZETMAR Battery";
                link.Save();
            });
            if(InspectShortcut(temp,executable).Kind!=StartupKind.Ready)throw new IOException("Startup shortcut verification failed");
            File.Move(temp,Path.Combine(folder,ShortcutName),true);
        }
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
    public static StartupState InspectShortcut(string path,string executable)
    {
        if(!File.Exists(path))return new(StartupKind.Off);
        try
        {
            StartupState result=new(StartupKind.Invalid);
            WithShortcut(path,link=>
            {
                string target=link.TargetPath,arguments=link.Arguments;
                result=arguments.Trim()=="--startup"?Inspect("\""+target+"\" --startup",executable):new(StartupKind.Invalid,target);
            });
            return result;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {return new(StartupKind.Unavailable,Error:e.Message);}
    }
    public static string CreateCommand(string executable)
    {
        if(!Path.IsPathFullyQualified(executable)||executable.IndexOfAny(new[]{'"','\r','\n'})>=0)
            throw new ArgumentException("The startup executable path must be absolute.",nameof(executable));
        string full=Path.GetFullPath(executable);
        if(!string.Equals(Path.GetExtension(full),".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("The startup target must be an EXE.",nameof(executable));
        string command="\""+full+"\" --startup";
        if(command.Length>260)throw new IOException("The startup command exceeds the Windows Run limit. Move the app to a shorter path.");
        return command;
    }
    public static StartupState Inspect(string? command,string executable,Func<string,bool>? exists=null)
    {
        if(string.IsNullOrWhiteSpace(command))return new(StartupKind.Off);
        if(command.Length>260)return new(StartupKind.Invalid);
        var match=Regex.Match(command.Trim(),"^(?:\"(?<path>[^\"]+)\"|(?<path>[^\\s\"]+))(?:\\s+--startup)?$",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
        if(!match.Success)return new(StartupKind.Invalid);
        string target=Environment.ExpandEnvironmentVariables(match.Groups["path"].Value);
        if(!Path.IsPathFullyQualified(target)||!string.Equals(Path.GetExtension(target),".exe",StringComparison.OrdinalIgnoreCase))return new(StartupKind.Invalid);
        try
        {
            target=Path.GetFullPath(target);
            if(!(exists??File.Exists)(target))return new(StartupKind.MissingTarget,target);
            return new(string.Equals(target,Path.GetFullPath(executable),StringComparison.OrdinalIgnoreCase)?StartupKind.Ready:StartupKind.OtherLocation,target);
        }
        catch(Exception e) when(e is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {return new(StartupKind.Unavailable,target,e.Message);}
    }
}
