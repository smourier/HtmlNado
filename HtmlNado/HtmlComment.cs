namespace HtmlNado;

[DebuggerDisplay("'{Value}'")]
public class HtmlComment : HtmlNode
{
    private string? _value;

    protected internal HtmlComment(HtmlDocument ownerDocument)
        : base(string.Empty, "#comment", string.Empty, ownerDocument)
    {
    }

    [Browsable(false)]
    public override HtmlAttributeList Attributes => base.Attributes;

    [Browsable(false)]
    public override HtmlNodeList ChildNodes => base.ChildNodes;

    public override HtmlNodeType NodeType => HtmlNodeType.Comment;

    public override string Name
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
        get => _value;
        set
        {
            if (!string.Equals(value, _value, StringComparison.Ordinal))
            {
                _value = value;
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
        comment._value = _value;
    }
}
