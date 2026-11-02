using System.Text.Json;
using System.Text.RegularExpressions;
using BeDiagram.Models;

namespace BeDiagram.Services;

/// <summary>Reads and writes diagrams in a local folder (configurable via "Storage:Folder").</summary>
public class DiagramStore
{
    public const string Extension = ".diagram.json";
    static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string Folder { get; }

    public DiagramStore(IConfiguration config)
    {
        var configured = config["Storage:Folder"];
        Folder = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BeDiagram")
            : Environment.ExpandEnvironmentVariables(configured);
        Directory.CreateDirectory(Folder);
    }

    /// <summary>Turns a user-entered name into a safe file name (no paths, no odd characters).</summary>
    public static string SafeName(string name)
    {
        var n = Regex.Replace(name ?? "", @"[^\w\- ]+", "").Trim();
        return string.IsNullOrEmpty(n) ? "Untitled" : n;
    }

    string PathFor(string name, string ext) => Path.Combine(Folder, SafeName(name) + ext);

    public IReadOnlyList<(string Name, DateTime Modified)> List() =>
        Directory.EnumerateFiles(Folder, "*" + Extension)
            .Select(f => (Name: Path.GetFileName(f)[..^Extension.Length], Modified: File.GetLastWriteTime(f)))
            .OrderByDescending(f => f.Modified)
            .ToList();

    public bool Exists(string name) => File.Exists(PathFor(name, Extension));

    public async Task<Diagram> LoadAsync(string name)
    {
        await using var s = File.OpenRead(PathFor(name, Extension));
        var d = await JsonSerializer.DeserializeAsync<Diagram>(s, Json) ?? new Diagram();
        d.Name = SafeName(name);
        return d;
    }

    public async Task<string> SaveAsync(Diagram d)
    {
        d.Name = SafeName(d.Name);
        d.Modified = DateTime.UtcNow;
        var path = PathFor(d.Name, Extension);
        var tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(d, Json));
        File.Move(tmp, path, overwrite: true);
        return path;
    }

    public async Task<string> SaveTextAsync(string name, string ext, string content)
    {
        var path = PathFor(name, ext);
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    public async Task<string> SaveBytesAsync(string name, string ext, byte[] content)
    {
        var path = PathFor(name, ext);
        await File.WriteAllBytesAsync(path, content);
        return path;
    }

    public void Delete(string name)
    {
        var p = PathFor(name, Extension);
        if (File.Exists(p)) File.Delete(p);
    }
}
