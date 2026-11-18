using System.Text.Json;
using BeDiagram.Models;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Editing;

/// <summary>The JSON editor: the open diagram as file JSON, and replacing it with edited or pasted JSON.</summary>
public partial class DiagramEditor
{
    /// <summary>The open diagram exactly as it would be saved (<see cref="DiagramJson"/>).</summary>
    public string ToJson()
    {
        return DiagramJson.Write(Diagram);
    }

    /// <summary>
    /// Replaces the open diagram with one read from <paramref name="json"/>, as one undo step. Nothing changes when
    /// the text doesn't parse or doesn't pass <see cref="DiagramValidator"/>: the problems come back instead (an empty
    /// list means it was applied). The diagram keeps its name, since that is its file name, not part of the drawing.
    /// </summary>
    public List<string> ApplyJson(string json)
    {
        List<string> errors = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add("There is no JSON to apply.");
            return errors;
        }

        Diagram replacement;
        try
        {
            replacement = DiagramJson.Read(json);
        }
        catch (JsonException ex)
        {
            errors.Add(ParseError(ex));
            return errors;
        }
        catch (Exception ex)
        {
            // A converter or the old-comments shim can throw something other than JsonException on odd values.
            errors.Add("Could not read the JSON: " + ex.Message);
            return errors;
        }

        errors = DiagramValidator.Check(replacement);
        if (errors.Count > 0)
        {
            return errors;
        }

        // One undo step back to the diagram as it was.
        Checkpoint();
        replacement.Name = Diagram.Name;
        Diagram = replacement;
        // Hand-edited JSON may have missing or out-of-order step numbers; same as opening a file.
        Steps.Normalise();
        Selection.RemoveAll(id => FindBox(id) is null);
        if (SelectedEdgeId is not null && SelectedEdge is null)
        {
            SelectedEdgeId = null;
        }
        EditingId = null;
        Dirty = true;
        return errors;
    }

    /// <summary>"Line 12, position 5 (at $.nodes[3].w): …" from a parse error, counting lines and positions from 1.</summary>
    static string ParseError(JsonException ex)
    {
        string message = ex.Message;
        // System.Text.Json adds " Path: … | LineNumber: … | BytePositionInLine: …" to the message; that is shown
        // more plainly in front instead.
        int cut = message.IndexOf(" Path: ", StringComparison.Ordinal);
        if (cut < 0)
        {
            cut = message.IndexOf(" LineNumber: ", StringComparison.Ordinal);
        }
        if (cut > 0)
        {
            message = message.Substring(0, cut);
        }

        string where = "";
        if (ex.LineNumber is not null)
        {
            where = "Line " + (ex.LineNumber.Value + 1);
            if (ex.BytePositionInLine is not null)
            {
                where += ", position " + (ex.BytePositionInLine.Value + 1);
            }
        }
        if (!string.IsNullOrEmpty(ex.Path) && ex.Path != "$")
        {
            if (where.Length > 0)
            {
                where += " ";
            }
            where += "(at " + ex.Path + ")";
        }
        if (where.Length == 0)
        {
            return message;
        }
        return where + ": " + message;
    }
}
