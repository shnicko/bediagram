using System.Text.Json;
using System.Text.RegularExpressions;
using BeDiagram.Models;

namespace BeDiagram.Services;

/// <summary>
/// Reads and writes diagrams in a local folder (configurable via "Storage:Folder") and its subfolders. A diagram's name
/// is its path below that folder without the extension, folders separated by '/' (<c>Projects/Audit/Clash check</c>);
/// a diagram at the top has no folder part. Folder paths use the same form, "" being the top.
/// </summary>
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
        string? configured = config["Storage:Folder"];
        Folder = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BeDiagram")
            : Environment.ExpandEnvironmentVariables(configured);
        Directory.CreateDirectory(Folder);
    }

    static readonly EnumerationOptions Here = new() { IgnoreInaccessible = true };
    static readonly EnumerationOptions Below = new() { IgnoreInaccessible = true, RecurseSubdirectories = true };

    /// <summary>Cleans one part of a name or folder path: letters, digits, '_', '-' and spaces only.</summary>
    static string CleanPart(string part) => Regex.Replace(part, @"[^\w\- ]+", "").Trim();

    /// <summary>Turns a user-entered name into a safe diagram name: each folder and the file name cleaned (no odd
    /// characters, so no "..", drive letters or rooted paths), a backslash read as '/'. An empty file name becomes "Untitled".</summary>
    public static string SafeName(string name)
    {
        string[] raw = (name ?? "").Split('/', '\\');
        string leaf = CleanPart(raw[^1]);
        return Join(SafeFolder(string.Join('/', raw[..^1])), leaf.Length == 0 ? "Untitled" : leaf);
    }

    /// <summary>Turns a folder path into a safe one ("" = the top): each part cleaned, empty parts dropped.</summary>
    public static string SafeFolder(string? folder) =>
        string.Join('/', (folder ?? "").Split('/', '\\').Select(CleanPart).Where(p => p.Length > 0));

    /// <summary>A folder path and a name (or subfolder) in it.</summary>
    public static string Join(string folder, string name) => folder.Length == 0 ? name : folder + "/" + name;

    /// <summary>The folder part of a diagram name ("" when it is at the top).</summary>
    public static string FolderOf(string name)
    {
        int i = name.LastIndexOf('/');
        return i < 0 ? "" : name[..i];
    }

    /// <summary>The last part of a diagram name or folder path.</summary>
    public static string LeafOf(string name) => name[(name.LastIndexOf('/') + 1)..];

    string PathFor(string name, string ext) => Path.Combine(Folder, SafeName(name) + ext);

    string FullFolder(string folder)
    {
        string f = SafeFolder(folder);
        return f.Length == 0 ? Folder : Path.Combine(Folder, f);
    }

    string Relative(string path) => Path.GetRelativePath(Folder, path).Replace('\\', '/');

    /// <summary>The diagram name of a *.diagram.json path inside the storage folder.</summary>
    string NameOf(string path) => Relative(path)[..^Extension.Length];

    /// <summary>A folder path that <see cref="SafeFolder"/> leaves as it is. Folders with other characters (dots and
    /// the like) can't be reached by name, so they are left out of the lists.</summary>
    static bool Usable(string folder) => SafeFolder(folder) == folder;

    /// <summary>Every diagram in the storage folder and its subfolders, newest first (the link picker's list).</summary>
    public IReadOnlyList<(string Name, DateTime Modified)> List() =>
        Directory.EnumerateFiles(Folder, "*" + Extension, Below)
            .Select(f => (Name: NameOf(f), Modified: File.GetLastWriteTime(f)))
            .Where(f => Usable(FolderOf(f.Name)))
            .OrderByDescending(f => f.Modified)
            .ToList();

    /// <summary>The diagrams directly in one folder, newest first.</summary>
    public IReadOnlyList<(string Name, DateTime Modified)> Files(string folder) =>
        FolderExists(folder)
            ? Directory.EnumerateFiles(FullFolder(folder), "*" + Extension, Here)
                .Select(f => (Name: NameOf(f), Modified: File.GetLastWriteTime(f)))
                .OrderByDescending(f => f.Modified)
                .ToList()
            : Array.Empty<(string Name, DateTime Modified)>();

    /// <summary>The subfolders directly in one folder, by name (hidden and unusable folders left out).</summary>
    public IReadOnlyList<string> Folders(string folder) =>
        FolderExists(folder)
            ? Directory.EnumerateDirectories(FullFolder(folder), "*", Here)
                .Select(Relative)
                .Where(Usable)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : Array.Empty<string>();

    /// <summary>Every usable folder below the storage folder, by path (not including the top, "").</summary>
    public IReadOnlyList<string> AllFolders() =>
        Directory.EnumerateDirectories(Folder, "*", Below)
            .Select(Relative)
            .Where(Usable)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool FolderExists(string folder) => Directory.Exists(FullFolder(folder));

    /// <summary>Makes a subfolder (cleaned like a name) and returns its path. Throws when the name has nothing usable.</summary>
    public string CreateFolder(string parent, string name)
    {
        string leaf = CleanPart(name ?? "");
        if (leaf.Length == 0) throw new ArgumentException("Use letters, digits, spaces, '-' or '_' in a folder name.");
        string path = Join(SafeFolder(parent), leaf);
        Directory.CreateDirectory(FullFolder(path));
        return path;
    }

    /// <summary>The folder holds nothing at all (no diagrams, exports, subfolders or other files).</summary>
    public bool IsEmpty(string folder) =>
        FolderExists(folder) && !Directory.EnumerateFileSystemEntries(FullFolder(folder)).Any();

    /// <summary>Deletes an empty subfolder. The top folder and folders with anything in them are left alone.</summary>
    public bool DeleteFolder(string folder)
    {
        if (SafeFolder(folder).Length == 0 || !IsEmpty(folder)) return false;
        Directory.Delete(FullFolder(folder));
        return true;
    }

    public bool Exists(string name) => File.Exists(PathFor(name, Extension));

    public async Task<Diagram> LoadAsync(string name)
    {
        await using FileStream s = File.OpenRead(PathFor(name, Extension));
        Diagram d = await JsonSerializer.DeserializeAsync<Diagram>(s, Json) ?? new Diagram();
        d.Name = SafeName(name);
        return d;
    }

    public async Task<string> SaveAsync(Diagram d)
    {
        d.Name = SafeName(d.Name);
        d.Modified = DateTime.UtcNow;
        string path = PathFor(d.Name, Extension);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string tmp = path + ".tmp";
        await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(d, Json));
        File.Move(tmp, path, overwrite: true);
        return path;
    }

    public async Task<string> SaveBytesAsync(string name, string ext, byte[] content)
    {
        string path = PathFor(name, ext);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content);
        return path;
    }

    public void Delete(string name)
    {
        string p = PathFor(name, Extension);
        if (File.Exists(p)) File.Delete(p);
    }
}
