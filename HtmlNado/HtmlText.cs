namespace HtmlNado;

[DebuggerDisplay("'{Value}'")]
public class HtmlText : HtmlNode
{
    protected internal HtmlText(HtmlDocument ownerDocument)
        : base(string.Empty, "#text", string.Empty, ownerDocument)
    {
    }

    public override HtmlNodeType NodeType => HtmlNodeType.Text;
    public virtual bool IsWhitespace => string.IsNullOrWhiteSpace(Value);

    public virtual bool IsCData
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsCData)));
            }
        }
    }

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

    public override string? InnerHtml
    {
        get => Value;
        set
        {
            if (!string.Equals(value, Value, StringComparison.Ordinal))
            {
                Value = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(InnerHtml)));
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

        if (IsCData)
        {
            writer.Write("<![CDATA[");
            writer.Write(Value);
            writer.Write("]]>");
        }
        else
        {
            writer.Write(Value);
        }
    }

    public override void WriteContentTo(TextWriter writer)
    {
    }

    public override void WriteTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (IsCData)
        {
            writer.WriteCData(Value);
        }
        else if (IsWhitespace && IsWhiteSpace(Value))
        {
            writer.WriteWhitespace(Value);
        }
        else
        {
            writer.WriteString(Value);
        }
    }

    private static bool IsWhiteSpace(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (!XmlConvert.IsWhitespaceChar(c))
                return false;
        }
        return true;
    }

    public override void WriteContentTo(XmlWriter writer)
    {
    }

    public override void CopyTo(HtmlNode target, HtmlCloneOptions copyOptions)
    {
        base.CopyTo(target, copyOptions);
        var text = (HtmlText)target;
        text.IsCData = IsCData;
        text.Value = Value;
    }
}
