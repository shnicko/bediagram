using BeDiagram.Models;
using BeDiagram.Services;

namespace BeDiagram.Mcp;

/// <summary>
/// The diagrams the MCP tools can reach: the app's storage folder, through the same <see cref="DiagramStore"/> the web
/// app uses, so a diagram Claude makes shows in the app's Open list and names work the same way
/// (<c>Projects/Audit/Clash check</c>). Nothing outside that folder can be read or written.
/// </summary>
public class DiagramWorkspace
{
    readonly DiagramStore store;

    public DiagramWorkspace(DiagramStore store)
    {
        this.store = store;
    }

    /// <summary>The storage folder on disk.</summary>
    public string Folder
    {
        get
        {
            return store.Folder;
        }
    }

    /// <summary>Every diagram, newest first.</summary>
    public List<(string Name, DateTime Modified)> List()
    {
        return new List<(string Name, DateTime Modified)>(store.List());
    }

    public bool Exists(string name)
    {
        return store.Exists(name);
    }

    /// <summary>Opens a diagram by name. An unknown name is a <see cref="ToolArgumentException"/>.</summary>
    public async Task<Diagram> OpenAsync(string name)
    {
        string safe = DiagramStore.SafeName(name);
        if (!store.Exists(safe))
        {
            throw new ToolArgumentException("There's no diagram called \"" + name + "\". Use list_diagrams to see the names.");
        }
        return await store.LoadAsync(safe);
    }

    /// <summary>Saves the diagram under its name and returns the file's path.</summary>
    public async Task<string> SaveAsync(Diagram d)
    {
        return await store.SaveAsync(d);
    }

    /// <summary>Writes an export next to the diagram (<paramref name="extension"/> such as ".svg") and returns its path.</summary>
    public async Task<string> SaveExportAsync(string name, string extension, byte[] content)
    {
        return await store.SaveBytesAsync(name, extension, content);
    }
}
