using HtmlNado.Utilities;

namespace HtmlNado;

public class HtmlTextBlockReader
{
    private readonly Queue<HtmlTextBlock> _queue = new();
    private bool _eof;

    public HtmlTextBlockReader(HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Node = node;
        CurrentNode = node;
    }

    public HtmlNode Node { get; }
    public virtual HtmlNode CurrentNode { get; protected set; }
    public virtual HtmlTextBlock? Value { get; protected set; }

    public virtual bool Read()
    {
        if (_queue.Count > 0)
        {
            Value = _queue.Dequeue();
            return true;
        }

        if (_eof)
            return false;

        DoRead();

        if (_queue.Count > 0)
        {
            Value = _queue.Dequeue();
            return true;
        }

        return false;
    }

    protected virtual void DoRead()
    {
        var current = CurrentNode;
        if (current == null || current.ChildNodes == null)
        {
            _eof = true;
            return;
        }

        HtmlTextBlock block;
        var bt = IsHeadingOrTitleTag(current.Name);
        if (bt.HasValue)
        {
            block = new HtmlTextBlock(current, bt.Value);
            _queue.Enqueue(block);
            return;
        }
    }

    private static HtmlTextBlockType? IsHeadingOrTitleTag(string? name)
    {
        if (name == null)
            return null;

        if (name.EqualsOrdinalIgnoreCase("title"))
            return HtmlTextBlockType.Title;

        if (name.Length != 2)
            return null;

        if (name[0] != 'h' && name[0] != 'H')
            return null;

        if (char.IsDigit(name[1]))
            return HtmlTextBlockType.Heading;

        return null;
    }
}
