namespace HtmlNado;

[DebuggerDisplay("'{Value}'")]
public class HtmlComment : HtmlNode
{
    protected internal HtmlComment(HtmlDocument ownerDocument)
        : base(string.Empty, "#comment", string.Empty, ownerDocument)
    {
    }

    public override HtmlNodeType NodeType => HtmlNodeType.Comment;

    public override string? Name
    {
        get => base.Name;
        set
        {
            // do nothing
        }
    }

    public override string? InnerText
    {
        get => Value;
        set
        {
            if (!string.Equals(value, Value, StringComparison.Ordinal))
            {
                Value = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(InnerText)));
            }
        }
    }

    public override string? Value
    {
        get => field;
        set
        {
            if (!string.Equals(value, field, StringComparison.Ordinal))
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }

    public override void WriteTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write("<!--");
        writer.Write(Value);
        writer.Write("-->");
    }

    public override void WriteContentTo(TextWriter writer)
    {
    }

    public override void WriteTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteComment(Value);
    }

    public override void WriteContentTo(XmlWriter writer)
    {
    }

    public override void CopyTo(HtmlNode target, HtmlCloneOptions copyOptions)
    {
        base.CopyTo(target, copyOptions);
        var comment = (HtmlComment)target;
        comment.Value = Value;
    }
}
