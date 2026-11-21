using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Panels;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// Structural model nodes: UML classes, interfaces and enumerations, and database entities. A name compartment over
/// one or more compartments of single-line rows (<see cref="Node.Items"/>, each with its <see cref="ListItem.Section"/>),
/// drawn by a <see cref="CompartmentLayout"/>. Rows are structured (visibility, name, data type, multiplicity, keys)
/// rather than free text, so the audit tool can read the model later. Model nodes are not steps in a flow.
/// <see cref="Node.NameOnly"/> hides the rows for a conceptual view.
/// </summary>
public abstract class ModelKind : PanelKind
{
    readonly IPanelLayout layout;

    protected ModelKind()
    {
        layout = new CompartmentLayout(this);
    }

    public override bool IsStep
    {
        get
        {
            return false;
        }
    }

    /// <summary>The compartments under the name, top to bottom.</summary>
    public abstract IReadOnlyList<RowSection> Sections { get; }

    /// <summary>A compartment's heading in the side panel ("Attributes", "Columns").</summary>
    public abstract string SectionName(RowSection section);

    /// <summary>The structured fields a row in <paramref name="section"/> uses.</summary>
    public abstract ModelRowFields Fields(RowSection section);

    /// <summary>A new row for <paramref name="section"/>: the side panel's "+ Add" button.</summary>
    protected abstract ListItem NewRow(Node n, RowSection section);

    /// <summary>Can be marked abstract (<see cref="Node.Abstract"/>): classes only.</summary>
    public virtual bool CanBeAbstract
    {
        get
        {
            return false;
        }
    }

    /// <summary>The line over the name ("«interface»", "{abstract}"), or null for none.</summary>
    public virtual string? Stereotype(Node n)
    {
        return null;
    }

    /// <summary>Entities: a gutter on the left of each row for PK / FK.</summary>
    public virtual bool KeyGutter
    {
        get
        {
            return false;
        }
    }

    /// <summary>The row's text as drawn: UML style ("- id : int [1]", "+ save() : bool") unless a kind draws it differently.</summary>
    public virtual string RowText(ListItem item, RowSection section)
    {
        string name = item.Label.Replace("\r", "").Replace("\n", " ").Trim();
        ModelRowFields fields = Fields(section);
        if (section == RowSection.Operation)
        {
            if (name.Length == 0)
            {
                name = "operation";
            }
            if (!name.Contains('('))
            {
                name += "()";
            }
        }
        else if (name.Length == 0)
        {
            name = "attribute";
        }
        string text = name;
        if (fields.Visibility && item.Visibility is not null)
        {
            text = Visibilities.Symbol(item.Visibility) + " " + text;
        }
        if (fields.DataType && item.DataType is not null)
        {
            text += " : " + item.DataType.Trim();
        }
        if (fields.Multiplicity && item.Multiplicity is not null)
        {
            text += " [" + item.Multiplicity.Trim() + "]";
        }
        return text;
    }

    /// <summary>Text drawn right-aligned on the row (an entity column's data type), or null.</summary>
    public virtual string? RowType(ListItem item)
    {
        return null;
    }

    /// <summary>Text drawn in the key gutter ("PK", "FK", "PK FK"), or null.</summary>
    public virtual string? RowKey(ListItem item)
    {
        return null;
    }

    public override IPanelLayout LayoutOf(Node n)
    {
        return layout;
    }

    /// <summary>The section a row belongs to here: its own if this kind has it, otherwise the first one (a row from
    /// another kind, or with no section).</summary>
    public RowSection SectionOf(ListItem item)
    {
        if (item.Section is not null && Sections.Contains(item.Section.Value))
        {
            return item.Section.Value;
        }
        return Sections[0];
    }

    /// <summary>The rows in one compartment, in order.</summary>
    public List<ListItem> RowsIn(Node n, RowSection section)
    {
        List<ListItem> result = new List<ListItem>();
        foreach (ListItem item in n.Rows)
        {
            if (SectionOf(item) == section)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>A new row after the last row of its compartment (the first compartment when none is given or this
    /// kind doesn't have it), keeping the file in drawing order.</summary>
    public override ListItem AddRow(Node n, RowSection? section)
    {
        n.Items ??= new List<ListItem>();
        RowSection key = Sections[0];
        if (section is not null && Sections.Contains(section.Value))
        {
            key = section.Value;
        }
        ListItem item = NewRow(n, key);
        item.Section = key;
        int keyIndex = IndexOf(key);
        int at = 0;
        for (int i = 0; i < n.Items.Count; i++)
        {
            if (IndexOf(SectionOf(n.Items[i])) <= keyIndex)
            {
                at = i + 1;
            }
        }
        n.Items.Insert(at, item);
        return item;
    }

    public override List<ListItem> Peers(Node n, string itemId)
    {
        foreach (ListItem item in n.Rows)
        {
            if (item.Id == itemId)
            {
                return RowsIn(n, SectionOf(item));
            }
        }
        return new List<ListItem>();
    }

    /// <summary>Every row in one of this kind's compartments (the first when it had none, or one this kind lacks),
    /// sorted by compartment, keeping the order inside each.</summary>
    public override void TidyRows(Node n)
    {
        n.Items ??= NewItems();
        List<ListItem> sorted = new List<ListItem>();
        foreach (RowSection section in Sections)
        {
            foreach (ListItem item in n.Items)
            {
                if (SectionOf(item) == section)
                {
                    sorted.Add(item);
                }
            }
        }
        foreach (ListItem item in sorted)
        {
            item.Section = SectionOf(item);
        }
        n.Items = sorted;
    }

    /// <summary>A node retyped to this kind keeps its name-only view, and stays abstract only if this kind can be.</summary>
    public override void Adopt(Node n)
    {
        bool nameOnly = n.NameOnly;
        bool isAbstract = n.Abstract;
        base.Adopt(n);
        n.NameOnly = nameOnly;
        n.Abstract = isAbstract && CanBeAbstract;
    }

    int IndexOf(RowSection section)
    {
        for (int i = 0; i < Sections.Count; i++)
        {
            if (Sections[i] == section)
            {
                return i;
            }
        }
        return 0;
    }

    /// <summary>A starter row in <paramref name="section"/>.</summary>
    protected static ListItem Row(RowSection section, string label, string? type, Visibility? visibility)
    {
        ListItem item = new ListItem();
        item.Label = label;
        item.DataType = type;
        item.Visibility = visibility;
        item.Section = section;
        return item;
    }
}
