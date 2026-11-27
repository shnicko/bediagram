using BeDiagram.Models;
using BeDiagram.Models.Serialization;
using BeDiagram.Services;

namespace BeDiagram.Cli;

/// <summary>Reads and writes one diagram file by path, and finds the diagram files in a list of files and folders.</summary>
public static class DiagramFile
{
    /// <summary>Reads a diagram. Its name is the file name without <c>.diagram.json</c>.</summary>
    public static Diagram Read(string path)
    {
        Diagram d = DiagramJson.Read(File.ReadAllText(path));
        d.Name = NameOf(path);
        return d;
    }

    /// <summary>Writes a diagram back, the same way the app saves (a temporary file, then moved over the old one).</summary>
    public static void Write(string path, Diagram d)
    {
        d.Modified = DateTime.UtcNow;
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, DiagramJson.Write(d));
        File.Move(tmp, path, true);
    }

    /// <summary>The file name without its folder and without <c>.diagram.json</c>.</summary>
    public static string NameOf(string path)
    {
        string file = Path.GetFileName(path);
        if (file.EndsWith(DiagramStore.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return file.Substring(0, file.Length - DiagramStore.Extension.Length);
        }
        return Path.GetFileNameWithoutExtension(file);
    }

    /// <summary>
    /// The diagram files named by <paramref name="paths"/>: a file as it is, a folder as every <c>*.diagram.json</c>
    /// in it and its subfolders (sorted by path). Paths that don't exist go into <paramref name="missing"/>.
    /// </summary>
    public static List<string> Expand(List<string> paths, List<string> missing)
    {
        List<string> result = new List<string>();
        foreach (string p in paths)
        {
            if (File.Exists(p))
            {
                result.Add(p);
            }
            else if (Directory.Exists(p))
            {
                List<string> found = new List<string>(Directory.GetFiles(p, "*" + DiagramStore.Extension, SearchOption.AllDirectories));
                found.Sort(StringComparer.OrdinalIgnoreCase);
                result.AddRange(found);
            }
            else
            {
                missing.Add(p);
            }
        }
        return result;
    }
}
