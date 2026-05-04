using HtmlNado.Utilities;

namespace HtmlNado;

[DebuggerDisplay("{Name}")]
public class HtmlElement : HtmlNode
{
    private bool? _empty;
    private bool? _dontCloseIfEmpty;
    private bool? _alwaysClose;
    private bool? _noChild;
    private HtmlNodeType _nodeType;

    protected internal HtmlElement(string prefix, string localName, string? namespaceURI, HtmlDocument? ownerDocument)
        : base(prefix, localName, namespaceURI, ownerDocument)
    {
        _nodeType = IsDocumentType ? HtmlNodeType.DocumentType : HtmlNodeType.Element;
#if DEBUG_HTML_ID
        SetAttribute(DebugIdAttributeName, _debugId.ToString());
        _debugId++;
#endif
    }

#if DEBUG_HTML_ID
    public const string DebugIdAttributeName = "__id";
    private static int _debugId;

    public int DebugId
    {
        get
        {
            return GetAttributeValue(DebugIdAttributeName, -1);
        }
    }
#endif

    public virtual bool IsDocumentType => Name.EqualsOrdinalIgnoreCase("!doctype");

    public virtual char CloseChar
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(CloseChar)));
            }
        }
    } = '/';

    public virtual bool IsProcessingInstruction
    {
        get => field;
        set
        {
            if (value != field)
            {
                if (value)
                {
                    _nodeType = HtmlNodeType.ProcessingInstruction;
                }
                else if (IsDocumentType)
                {
                    _nodeType = HtmlNodeType.DocumentType;
                }
                else
                {
                    _nodeType = HtmlNodeType.Element;
                }
                ClearCaches();
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsProcessingInstruction)));
            }
        }
    }

    public override string? InnerHtml
    {
        get => base.InnerHtml;
        set
        {
            if (!string.Equals(value, base.InnerHtml, StringComparison.Ordinal))
            {
                RemoveAll();
                if (value != null)
                {
                    var doc = OwnerDocument != null ? OwnerDocument.CreateDocument() : new HtmlDocument();
                    doc.LoadHtml(value);
                    if (doc.HasChildNodes)
                    {
                        foreach (var node in doc.ChildNodes)
                        {
                            ChildNodes.AddNoCheck(node);
                        }
                    }
                }
                ClearCaches();
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(InnerHtml)));
            }
        }
    }

    public virtual bool IsClosed
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                ClearCaches();
            }
        }
    } = true;

    public virtual bool IsEmpty
    {
        get
        {
            if (NoChild)
                return true;

            if (HasChildNodes)
                return false;

            if (IsProcessingInstruction)
                return true;

            if (_empty.HasValue)
                return _empty.Value;

            return true;
        }
        set
        {
            if (value != _empty)
            {
                ClearCaches();
                _empty = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEmpty)));
            }
        }
    }

    public override HtmlNodeType NodeType => _nodeType;

    internal HtmlElement? GetParentToClose(int indent, string? name)
    {
        // NOTE: this avoids possible stack overflow errors for "super malformed" documents
        if (indent > _maxRecursion)
            return null;

        if (name.EqualsOrdinalIgnoreCase(Name))
            return this;

        if (ParentNode is HtmlElement element)
            return element.GetParentToClose(indent + 1, name);

        return null;
    }

    public virtual bool NoChild
    {
        get
        {
            if (_noChild.HasValue)
                return _noChild.Value;

            var owner = OwnerDocument;
            if (owner == null)
                return false;

            return owner.Options.GetElementWriteOptions(Name).HasFlag(HtmlElementWriteOptions.NoChild);
        }
        set => _noChild = value;
    }

    public virtual bool AlwaysClose
    {
        get
        {
            if (_alwaysClose.HasValue)
                return _alwaysClose.Value;

            if (OwnerDocument == null)
                return false;

            return OwnerDocument.Options.GetElementWriteOptions(Name).HasFlag(HtmlElementWriteOptions.AlwaysClose);
        }
        set => _alwaysClose = value;
    }

    public virtual bool DontCloseIfEmpty
    {
        get
        {
            if (_dontCloseIfEmpty.HasValue)
                return _dontCloseIfEmpty.Value;

            if (OwnerDocument == null)
                return false;

            return OwnerDocument.Options.GetElementWriteOptions(Name).HasFlag(HtmlElementWriteOptions.DontCloseIfEmpty);
        }
        set => _dontCloseIfEmpty = value;
    }

    public override void WriteTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write('<');
        if (IsProcessingInstruction)
        {
            writer.Write('?');
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

        if (HasAttributes)
        {
            foreach (var attribute in Attributes)
            {
                writer.Write(' ');
                attribute.WriteTo(writer);
            }
        }

        var alwaysClose = AlwaysClose;
        var dontCloseIfEmpty = DontCloseIfEmpty;
        if ((OwnerDocument != null && OwnerDocument.IsXhtml) || alwaysClose)
        {
            dontCloseIfEmpty = false;
        }

        if (IsEmpty && !alwaysClose)
        {
            if (IsProcessingInstruction)
            {
                writer.Write(" ?>");
            }
            else if (Name?.StartsWith('!') == true)
            {
                // suc as !DOCTYPE
                writer.Write('>');
            }
            else if (dontCloseIfEmpty)
            {
                writer.Write('>');
            }
            else
            {
                writer.Write(' ');
                writer.Write(CloseChar);
                writer.Write('>');
            }
        }
        else
        {
            writer.Write('>');

            if ((!HasChildNodes || NoChild) && dontCloseIfEmpty)
                return;

            WriteContentTo(writer);

            if (IsClosed || alwaysClose || (OwnerDocument != null && OwnerDocument.IsXhtml))
            {
                writer.Write("</");
                writer.Write(Name);
                writer.Write('>');
            }
        }
    }

    public override void WriteContentTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!NoChild)
        {
            if (HasChildNodes)
            {
                foreach (var node in ChildNodes)
                {
                    node.WriteTo(writer);
                }
            }
        }
    }

    public override void WriteTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (IsDocumentType)
        {
            var owner = OwnerDocument;
            owner?.WriteDocType(writer);
            return;
        }

        if (LocalName == null)
            return;

        writer.WriteStartElement(GetValidXmlName(Prefix), GetValidXmlName(LocalName), NamespaceURI);
        if (HasAttributes)
        {
            foreach (var attribute in Attributes)
            {
                if (string.Equals(attribute.Prefix, XmlnsPrefix, StringComparison.Ordinal) ||
                    string.Equals(attribute.Name, XmlnsPrefix, StringComparison.Ordinal))
                    continue;

                attribute.WriteTo(writer);
            }
        }

        if (IsEmpty)
        {
            writer.WriteEndElement();
        }
        else
        {
            WriteContentTo(writer);
            writer.WriteFullEndElement();
        }
    }

    public override void WriteContentTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Name.EqualsOrdinalIgnoreCase("!doctype"))
            return;

        if (!NoChild)
        {
            if (HasChildNodes)
            {
                foreach (var node in ChildNodes)
                {
                    node.WriteTo(writer);
                }
            }
        }
    }

    public override void CopyTo(HtmlNode target, HtmlCloneOptions copyOptions)
    {
        base.CopyTo(target, copyOptions);
        var element = (HtmlElement)target;
        element.IsClosed = IsClosed;
        element._empty = _empty;
        element.IsProcessingInstruction = IsProcessingInstruction;
        element._alwaysClose = _alwaysClose;
        element.CloseChar = CloseChar;
        element._dontCloseIfEmpty = _dontCloseIfEmpty;
        element._nodeType = _nodeType;
    }
}
