namespace HtmlNado;

public class HtmlNodeDepthComparer : IComparer<HtmlNode>
{
    public virtual ListSortDirection Direction { get; set; }

    public virtual int Compare(HtmlNode? x, HtmlNode? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        if (ReferenceEquals(x, y))
            return 0;

        var comp = x.Depth.CompareTo(y.Depth);
        return Direction == ListSortDirection.Ascending ? comp : -comp;
    }
}
