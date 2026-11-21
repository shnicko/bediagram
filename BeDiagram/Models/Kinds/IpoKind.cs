using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Panels;

namespace BeDiagram.Models.Kinds;

/// <summary>An input–process–output box: a title band over three sections of rows (<see cref="IpoSections"/>),
/// side by side or, with <see cref="Node.Stacked"/>, one under another.</summary>
public class IpoKind : PanelKind
{
    static readonly IPanelLayout Columns = new IpoColumnsLayout();
    static readonly IPanelLayout Stacked = new IpoStackedLayout();

    public override string? PaletteInnerPath
    {
        get
        {
            return "M0,8 H48 M16,8 V28 M32,8 V28 M3,13 H13 V18 H3 Z M19,13 H29 V18 H19 Z M35,13 H45 V18 H35 Z";
        }
    }

    public override IPanelLayout LayoutOf(Node n)
    {
        if (n.Stacked)
        {
            return Stacked;
        }
        return Columns;
    }

    /// <summary>The box is named for its process, so it starts as "Process" rather than the type's long name.</summary>
    public override string NewLabel(Node n)
    {
        return "Process";
    }

    public override string RetypeLabel(Node n)
    {
        return "Process";
    }

    /// <summary>One row per section to start with.</summary>
    public override List<ListItem> NewItems()
    {
        return new List<ListItem>
        {
            new ListItem { Label = "Inputs", Section = RowSection.Input },
            new ListItem { Label = "Work done", Section = RowSection.Process },
            new ListItem { Label = "Results", Section = RowSection.Output },
        };
    }

    /// <summary>A new row "Item N" after the last row of its section, keeping the file in reading order.</summary>
    public override ListItem AddRow(Node n, RowSection? section)
    {
        n.Items ??= new List<ListItem>();
        RowSection key = IpoSections.Of(section);
        ListItem item = new ListItem { Label = $"Item {n.RowsIn(key).Count + 1}", Section = key };
        int at = n.Items.FindLastIndex(i => IpoSections.IndexOf(i.Section) <= IpoSections.IndexOf(key)) + 1;
        n.Items.Insert(at, item);
        return item;
    }

    public override List<ListItem> Peers(Node n, string itemId)
    {
        if (n.Items is null)
        {
            return new List<ListItem>();
        }
        ListItem? row = null;
        foreach (ListItem i in n.Items)
        {
            if (i.Id == itemId)
            {
                row = i;
                break;
            }
        }
        RowSection section = IpoSections.Of(row?.Section);
        return n.RowsIn(section);
    }

    /// <summary>One block per section, under its (possibly renamed) title: label and description only.</summary>
    public override List<RowGroup> RowGroups(Node n)
    {
        List<RowGroup> result = new List<RowGroup>();
        foreach (RowSection s in IpoSections.All)
        {
            result.Add(new RowGroup(s, n.SectionTitle(s), new ModelRowFields(false, false, false, false), n.RowsIn(s)));
        }
        return result;
    }

    /// <summary>Every row in a section (input when it had none), sorted by section.</summary>
    public override void TidyRows(Node n)
    {
        n.Items ??= NewItems();
        foreach (ListItem i in n.Items)
        {
            i.Section = IpoSections.Of(i.Section);
        }
        n.Items = n.Items.OrderBy(i => IpoSections.IndexOf(i.Section)).ToList();
    }
}
