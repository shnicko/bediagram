using System.Text.Json.Nodes;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;
using BeDiagram.Services;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>new_diagram</c>: saves an empty diagram with a page, flow direction and header to start from.</summary>
public class NewDiagramTool : IMcpTool
{
    readonly DiagramWorkspace workspace;

    public NewDiagramTool(DiagramWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public string Name
    {
        get
        {
            return "new_diagram";
        }
    }

    public string Description
    {
        get
        {
            return "Create an empty diagram to build with add_shape and connect. Sets the page size and orientation, "
                + "the flow direction and the page title / description. Fails if the name is already used.";
        }
    }

    public bool ReadOnly
    {
        get
        {
            return false;
        }
    }

    public JsonObject InputSchema()
    {
        List<string> sizes = new List<string>();
        sizes.Add(PageSetup.None);
        foreach ((string key, string _, double _, double _) in PageSetup.Sizes)
        {
            sizes.Add(key);
        }
        ToolSchema schema = new ToolSchema();
        schema.Text("diagram", "Name for the new diagram, folders separated by '/'.", true);
        schema.Choice("page", "Page size, or none for an open canvas (default A3).", sizes, false);
        schema.Choice("orientation", "Page orientation (default landscape).", new List<string> { "landscape", "portrait" }, false);
        schema.Choice("flow", "Which way new steps go (default down).", new List<string> { "down", "right" }, false);
        schema.Text("title", "Page title shown at the top.", false);
        schema.Text("description", "Page description under the title.", false);
        return schema.Build();
    }

    public async Task<McpToolResult> CallAsync(ToolArguments args)
    {
        string name = DiagramStore.SafeName(args.Text("diagram"));
        if (workspace.Exists(name))
        {
            return McpToolResult.Error("\"" + name + "\" already exists. Choose another name, or edit that one.");
        }

        Diagram d = new Diagram();
        d.Name = name;
        string? page = args.OptionalText("page");
        if (page is null)
        {
            page = "A3";
        }
        d.Page.Size = page;
        string? orientation = args.OptionalText("orientation");
        if (orientation is not null)
        {
            d.Page.Orientation = orientation;
        }
        FlowDirection? flow = EnumNames.Parse<FlowDirection>(args.OptionalText("flow"));
        if (flow is not null)
        {
            d.Flow = flow.Value;
        }
        d.Title = args.OptionalText("title");
        d.Description = args.OptionalText("description");

        string path = await workspace.SaveAsync(d);
        return McpToolResult.Ok("Created \"" + d.Name + "\" (" + (d.Page.Enabled ? d.Page.Describe() : "open canvas") + ", flow "
            + EnumNames.Of(d.Flow) + ") at " + path + ".");
    }
}
