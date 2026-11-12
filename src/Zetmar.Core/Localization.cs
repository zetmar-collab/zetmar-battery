using System.Text.Json;
namespace Zetmar.Core;
public static class Localization
{
    private static readonly Dictionary<string,string> Polish=Load("pl"),English=Load("en");
    private static Dictionary<string,string> Load(string language)
    {
        using var stream=typeof(Localization).Assembly.GetManifestResourceStream($"Zetmar.Core.Resources.{language}.json")??throw new InvalidOperationException("Missing language resource: "+language);
        return JsonSerializer.Deserialize<Dictionary<string,string>>(stream)??throw new InvalidOperationException("Invalid language resource");
    }
    public static string Get(string key,bool polish) => (polish?Polish:English).GetValueOrDefault(key,key);
}
