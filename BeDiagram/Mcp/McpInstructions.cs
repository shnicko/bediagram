namespace BeDiagram.Mcp;

/// <summary>
/// The <c>instructions</c> sent with the <c>initialize</c> reply. The client adds them to the model's context, so this
/// is where the model learns what BeDiagram is and how to use the tools well together.
/// </summary>
public static class McpInstructions
{
    public const string Text =
        "BeDiagram is Nick's local flowchart and workflow tool. Diagrams are JSON files (schema bediagram/1) in one "
        + "storage folder; the same files open in the BeDiagram web app.\n"
        + "\n"
        + "Shapes are typed, and the type is meaning, not just look: start / end, process, decision, check (a rule "
        + "check with ruleId, ifcEntity, property, operator, value properties), action, data (IFC or other data), "
        + "status, circle, outcome (pass, fail, rework, conditional, hold badges), list and ipo panels with rows, and "
        + "class / interface / enumeration / entity data models. Every shape except outcomes has a step number "
        + "(1, 2, 3 …; parallel steps share one). Top-level groups are swim lanes.\n"
        + "\n"
        + "Working with the tools:\n"
        + "- list_diagrams for names, describe_diagram for the ids before editing.\n"
        + "- Small changes: add_shape (with after=<id> to place and join it), connect, update_shape. Each saves.\n"
        + "- A whole new diagram or many changes: save_diagram_json. Read an existing file with get_diagram_json "
        + "first to see the format; leave out fields you don't need and they take their defaults.\n"
        + "- Afterwards run check_diagram, then tidy_connectors if it reports crossings or routes to improve. "
        + "auto_layout moves every shape, so only use it on new or messy diagrams or when asked.\n"
        + "- If the diagram is open in the app, Nick reloads it there to see the changes; the app warns before "
        + "saving over a file that changed.";
}
