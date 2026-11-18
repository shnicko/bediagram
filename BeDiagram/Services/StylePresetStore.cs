using System.Text.Json;
using BeDiagram.Models.Presets;

namespace BeDiagram.Services;

/// <summary>
/// The saved styles, shared by every diagram: one file, <c>styles.json</c>, in the diagram storage folder (it doesn't
/// end in <see cref="DiagramStore.Extension"/>, so it never shows up as a diagram). Loaded once and written straight
/// back after every change. One instance serves every open tab, so the methods take a lock.
/// </summary>
public class StylePresetStore
{
    public const string FileName = "styles.json";

    static readonly JsonSerializerOptions Json = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    readonly object gate = new object();
    readonly List<StylePreset> presets;

    /// <summary>Where the styles are kept.</summary>
    public string FilePath { get; }

    public StylePresetStore(DiagramStore diagrams)
    {
        FilePath = Path.Combine(diagrams.Folder, FileName);
        presets = Load(FilePath);
    }

    /// <summary>The file's styles, or none when there is no file yet or it can't be read (a broken file is left as it
    /// is until the next save, so nothing is lost by just opening the app).</summary>
    static List<StylePreset> Load(string path)
    {
        List<StylePreset> result = new List<StylePreset>();
        if (!File.Exists(path))
        {
            return result;
        }
        try
        {
            List<StylePreset>? read = JsonSerializer.Deserialize<List<StylePreset>>(File.ReadAllText(path), Json);
            if (read is not null)
            {
                foreach (StylePreset p in read)
                {
                    // Skip entries with no style in them (hand-edited or from a newer version).
                    if (p.Shape is not null || p.Group is not null || p.TextBox is not null || p.Connector is not null)
                    {
                        result.Add(p);
                    }
                }
            }
        }
        catch (JsonException)
        {
            return result;
        }
        catch (IOException)
        {
            return result;
        }
        return result;
    }

    void Write()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(presets, Json));
    }

    /// <summary>The shape styles, in name order.</summary>
    public List<StylePreset> Shapes()
    {
        return Matching(p => p.Shape is not null);
    }

    /// <summary>The group styles, in name order.</summary>
    public List<StylePreset> Groups()
    {
        return Matching(p => p.Group is not null);
    }

    /// <summary>The text box styles, in name order.</summary>
    public List<StylePreset> TextBoxes()
    {
        return Matching(p => p.TextBox is not null);
    }

    /// <summary>The connector styles, in name order.</summary>
    public List<StylePreset> Connectors()
    {
        return Matching(p => p.Connector is not null);
    }

    List<StylePreset> Matching(Func<StylePreset, bool> test)
    {
        List<StylePreset> result = new List<StylePreset>();
        lock (gate)
        {
            foreach (StylePreset p in presets)
            {
                if (test(p))
                {
                    result.Add(p);
                }
            }
        }
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return result;
    }

    /// <summary>The style with this id, or null.</summary>
    public StylePreset? Find(string? id)
    {
        if (id is null)
        {
            return null;
        }
        lock (gate)
        {
            foreach (StylePreset p in presets)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
        }
        return null;
    }

    /// <summary>The style of the same kind as <paramref name="like"/> called <paramref name="name"/> (ignoring case), or null.</summary>
    public StylePreset? Named(string name, StylePreset like)
    {
        string wanted = name.Trim();
        lock (gate)
        {
            foreach (StylePreset p in presets)
            {
                if (p.SameKind(like) && string.Equals(p.Name, wanted, StringComparison.CurrentCultureIgnoreCase))
                {
                    return p;
                }
            }
        }
        return null;
    }

    /// <summary>Saves <paramref name="preset"/> under its name. A style of the same kind with the same name is
    /// replaced (it keeps its id).</summary>
    public void Save(StylePreset preset)
    {
        preset.Name = preset.Name.Trim();
        lock (gate)
        {
            StylePreset? old = null;
            foreach (StylePreset p in presets)
            {
                if (p.SameKind(preset) && string.Equals(p.Name, preset.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    old = p;
                }
            }
            if (old is not null)
            {
                preset.Id = old.Id;
                presets[presets.IndexOf(old)] = preset;
            }
            else
            {
                presets.Add(preset);
            }
            Write();
        }
    }

    /// <summary>Deletes the style with this id (nothing happens when there is none).</summary>
    public void Delete(string id)
    {
        lock (gate)
        {
            int at = presets.FindIndex(p => p.Id == id);
            if (at < 0)
            {
                return;
            }
            presets.RemoveAt(at);
            Write();
        }
    }
}
