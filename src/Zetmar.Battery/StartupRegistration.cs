using System;
using System.IO;
using System.Text.RegularExpressions;
namespace Zetmar.Battery;
public enum StartupKind { Off, Ready, MissingTarget, OtherLocation, Invalid, Unavailable }
public sealed record StartupState(StartupKind Kind,string? Target=null,string? Error=null);
public static class StartupRegistration
{
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
