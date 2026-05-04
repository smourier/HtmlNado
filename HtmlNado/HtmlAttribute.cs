namespace HtmlNado;

public class HtmlAttribute : HtmlNode
{
    protected internal HtmlAttribute(string prefix, string localName, string? namespaceURI, HtmlDocument? ownerDocument)
        : base(prefix, localName, namespaceURI, ownerDocument)
    {
    }

    public HtmlElement? OwnerElement => (HtmlElement?)ParentNode;
    public virtual bool IsNamespace => NamespaceURI != null && NamespaceURI.Equals(XmlnsNamespaceURI, StringComparison.Ordinal);
    public override HtmlNodeType NodeType => HtmlNodeType.Attribute;
    public override string? NamespaceURI
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Prefix))
                return string.Empty;

            return base.NamespaceURI;
        }
        set => base.NamespaceURI = value;
    }

    public virtual bool EscapeQuoteChar
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(EscapeQuoteChar)));
            }
        }
    } = true;

    public virtual bool IsValueDefined
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsValueDefined)));
            }
        }
    }

    public virtual char QuoteChar
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(QuoteChar)));
            }
        }
    }

    public virtual char NameQuoteChar
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(NameQuoteChar)));
            }
        }
    }

    public override int ParentIndex
    {
        get
        {
            var parent = ParentNode;
            if (parent != null && parent.HasAttributes)
            {
                for (var i = 0; i < parent.Attributes.Count; i++)
                {
                    if (parent.Attributes[i] == this)
                        return i;
                }
            }
            return -1;
        }
    }

    public new HtmlAttribute? NextSibling
    {
        get
        {
            var parent = ParentNode;
            if (parent == null || !parent.HasAttributes)
                return null;

            var index = ParentIndex;
            if (index < 0 || (index + 1) >= parent.Attributes.Count)
                return null;

            return parent.Attributes[index + 1];
        }
    }

    public new HtmlAttribute? PreviousSibling
    {
        get
        {
            var parent = ParentNode;
            if (parent == null || !parent.HasAttributes)
                return null;

            var index = ParentIndex;
            if (index <= 0)
                return null;

            return parent.Attributes[index - 1];
        }
    }

    public override string? Value
    {
        get => InnerText;
        set
        {
            var id = string.Compare(Name, "id", StringComparison.OrdinalIgnoreCase) == 0;
            var old = id ? Value : null;
            IsValueDefined = value != null;
            InnerText = value;
            if (id)
            {
                var od = OwnerDocument;
                if (od != null)
                {
                    od.ClearId(old);
                    od.SetNodeById(ParentNode);
                }
            }
        }
    }

    public override void WriteTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (NameQuoteChar != '\0')
        {
            writer.Write(NameQuoteChar);
        }

        if (!string.IsNullOrEmpty(Prefix))
        {
            writer.Write(Prefix);
            writer.Write(':');
            writer.Write(LocalName);
        }
        else
        {
            writer.Write(Name);
        }

        if (NameQuoteChar != '\0')
        {
            writer.Write(NameQuoteChar);
        }

        if (Value != null && IsValueDefined)
        {
            writer.Write('=');

            var quoteChar = GetQuoteChar();
            if (quoteChar == '\0')
            {
                WriteContentToWhenUndefinedQuoteChar(writer);
                return;
            }

            var owner = OwnerDocument;
            if (owner != null && owner.IsXhtml)
            {
                if (quoteChar != '\'' && quoteChar != '"')
                {
                    quoteChar = '\"';
                }
            }

            if (quoteChar != '\0')
            {
                writer.Write(quoteChar);
            }

            WriteContentTo(writer);

            if (quoteChar != '\0')
            {
                writer.Write(quoteChar);
            }
        }
    }

    public virtual void WriteContentToWhenUndefinedQuoteChar(TextWriter writer)
    {
        var eqc = EscapeQuoteChar;
        var value = GetValue(ref eqc);
        if (string.IsNullOrWhiteSpace(value) || value.IndexOf('"') < 0)
        {
            writer.Write('"');
            writer.Write(value);
            writer.Write('"');
            return;
        }

        writer.Write('\'');
        writer.Write(eqc ? value.Replace("'", "&apos;") : value);
        writer.Write('\'');
    }

    public override void WriteContentTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var eqc = EscapeQuoteChar;
        var s = GetValue(ref eqc);
        if (s != null)
        {
            if (eqc)
            {
                if (QuoteChar == '"')
                {
                    s = s.Replace(QuoteChar.ToString(CultureInfo.InvariantCulture), "&quot;");
                }
                else if (QuoteChar == '\'')
                {
                    s = s.Replace(QuoteChar.ToString(CultureInfo.InvariantCulture), "&apos;");
                }
            }
            writer.Write(s);
        }
    }

    public override void WriteTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (string.Equals(Prefix, XmlnsPrefix, StringComparison.Ordinal) || string.Equals(Name, XmlnsPrefix, StringComparison.Ordinal))
            return;

        writer.WriteStartAttribute(GetValidXmlName(Prefix), GetValidXmlName(LocalName) ?? string.Empty, NamespaceURI);
        WriteContentTo(writer);
        writer.WriteEndAttribute();
    }

    public override void WriteContentTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        foreach (var node in ChildNodes)
        {
            node.WriteTo(writer);
        }
    }

    public override void CopyTo(HtmlNode target, HtmlCloneOptions copyOptions)
    {
        base.CopyTo(target, HtmlCloneOptions.None); // don't do deep copy
        var att = (HtmlAttribute)target;
        att.NameQuoteChar = NameQuoteChar;
        att.QuoteChar = QuoteChar;
        att.IsValueDefined = IsValueDefined;
        att.EscapeQuoteChar = EscapeQuoteChar;
    }

    protected virtual char GetQuoteChar() => QuoteChar;
    protected virtual string? GetValue(ref bool escapeQuoteChar)
    {
        using var sw = HtmlDocument.CreateStringWriter(OwnerDocument, CultureInfo.InvariantCulture);
        foreach (var node in ChildNodes)
        {
            node.WriteTo(sw);
        }
        return sw.ToString();
    }

    internal static string? UnescapeText(string? text, char quoteChar)
    {
        if (text == null)
            return null;

        if (quoteChar == '"')
            return text.Replace("&quot;", quoteChar.ToString(CultureInfo.InvariantCulture));

        return text.Replace("&apos;", quoteChar.ToString(CultureInfo.InvariantCulture));
    }
}
