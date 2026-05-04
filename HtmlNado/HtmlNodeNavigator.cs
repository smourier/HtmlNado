//#define HTML_XPATH_TRACE
namespace HtmlNado;

public class HtmlNodeNavigator : XPathNavigator
{
    private readonly NameTable _nameTable = new();
    private readonly HtmlNode? _rootNode;

    [Conditional("HTML_XPATH_TRACE")]
    private static void Trace(object? value, [CallerMemberName] string? methodName = null)
    {
#if HTML_XPATH_TRACE
        if (!EnableTrace)
            return;

        System.Diagnostics.Trace.Write(methodName + ":" + value);
#endif
    }

#if HTML_XPATH_TRACE
    internal static bool EnableTrace { get; set; }
#endif

    public HtmlNodeNavigator(HtmlDocument? document, HtmlNode currentNode, HtmlNodeNavigatorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(currentNode);

        Document = document;
        CurrentNode = currentNode;
        BaseNode = currentNode;
        Options = options ?? new HtmlNodeNavigatorOptions();
        if (Options.HasFlag(HtmlNodeNavigatorOptions.RootNode))
        {
            _rootNode = CurrentNode;
        }
    }

    protected HtmlNodeNavigator(HtmlNodeNavigator other)
    {
        ArgumentNullException.ThrowIfNull(other);

        CurrentNode = other.CurrentNode;
        BaseNode = other.BaseNode;
        Document = other.Document;
        Options = other.Options;
        _rootNode = other._rootNode;
    }

    public virtual HtmlNode CurrentNode
    {
        get => field;
        protected set
        {
            if (field != value)
            {
                Trace("old: " + field + " new: " + value);
                field = value;
            }
        }
    }

    public HtmlNodeNavigatorOptions Options { get; }
    public HtmlDocument? Document { get; }
    public HtmlNode BaseNode { get; }
    public override object? UnderlyingObject => CurrentNode;
    public override string BaseURI => GetOrAdd(string.Empty);
    public override string OuterXml
    {
        get
        {
            var current = CurrentNode;
            if (current == null)
                return string.Empty;

            return current.OuterXml;
        }
        set => base.OuterXml = value ?? string.Empty;
    }

    public override bool IsEmptyElement
    {
        get
        {
            var element = CurrentNode as HtmlElement;
            Trace("=" + (element != null && element.IsEmpty));
            return element != null && element.IsEmpty;
        }
    }

    public override string LocalName
    {
        get
        {
            var name = CurrentNode?.LocalName;
            Trace("=" + name);
            if (name != null)
            {
                if (Options.HasFlag(HtmlNodeNavigatorOptions.UppercasedNames))
                    return name.ToUpper(CultureInfo.CurrentCulture);

                if (Options.HasFlag(HtmlNodeNavigatorOptions.LowercasedNames))
                    return name.ToLower(CultureInfo.CurrentCulture);
            }

            return name ?? string.Empty;
        }
    }

    public override string Name
    {
        get
        {
            var name = CurrentNode?.Name;
            Trace("=" + name);
            if (name != null)
            {
                if (Options.HasFlag(HtmlNodeNavigatorOptions.UppercasedNames))
                    return name.ToUpper(CultureInfo.CurrentCulture);

                if (Options.HasFlag(HtmlNodeNavigatorOptions.LowercasedNames))
                    return name.ToLower(CultureInfo.CurrentCulture);
            }

            return name ?? string.Empty;
        }
    }

    public override XmlNameTable NameTable => _nameTable;

    public override string NamespaceURI
    {
        get
        {
            var ns = CurrentNode?.NamespaceURI;
            if (Document != null && ns != null && Document.Options.EmptyNamespacesForXPath.Contains(ns))
                return string.Empty;

            if (Options.HasFlag(HtmlNodeNavigatorOptions.UppercasedNamespaceURIs))
                return ns?.ToUpper(CultureInfo.CurrentCulture) ?? string.Empty;

            if (Options.HasFlag(HtmlNodeNavigatorOptions.LowercasedNamespaceURIs))
                return ns?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty;

            Trace("=" + ns);
            return ns ?? string.Empty;
        }
    }

    public override XPathNodeType NodeType
    {
        get
        {
            if (CurrentNode == null)
                return XPathNodeType.Text;

            var nt = CurrentNode.NodeType switch
            {
                HtmlNodeType.Attribute => XPathNodeType.Attribute,
                HtmlNodeType.Comment => XPathNodeType.Comment,
                HtmlNodeType.Document => XPathNodeType.Root,
                HtmlNodeType.DocumentType or HtmlNodeType.Element => XPathNodeType.Element,//case HtmlNodeType.EndElement:
                HtmlNodeType.ProcessingInstruction => XPathNodeType.ProcessingInstruction,
                //case HtmlNodeType.None:
                //case CssNodeType.Text:
                //case HtmlNodeType.Text:
                _ => XPathNodeType.Text,
            };
            Trace("=" + nt);
            return nt;
        }
    }

    public override string Prefix
    {
        get
        {
            var prefix = CurrentNode?.Prefix;
            Trace("=" + prefix);
            if (Options.HasFlag(HtmlNodeNavigatorOptions.UppercasedPrefixes))
                return prefix?.ToUpper(CultureInfo.CurrentCulture) ?? string.Empty;

            if (Options.HasFlag(HtmlNodeNavigatorOptions.LowercasedPrefixes))
                return prefix?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty;

            return prefix ?? string.Empty;
        }
    }

    public override string Value
    {
        get
        {
            Trace("=" + CurrentNode?.Value);
            if (CurrentNode is HtmlElement element)
            {
                if (Options.HasFlag(HtmlNodeNavigatorOptions.UppercasedValues))
                    return element.InnerText?.ToUpper(CultureInfo.CurrentCulture) ?? string.Empty;

                if (Options.HasFlag(HtmlNodeNavigatorOptions.LowercasedValues))
                    return element.InnerText?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty;

                return element.InnerText ?? string.Empty;
            }

            var value = CurrentNode?.Value;
            if (value != null)
            {
                if (Options.HasFlag(HtmlNodeNavigatorOptions.UppercasedValues))
                    return value.ToUpper(CultureInfo.CurrentCulture);

                if (Options.HasFlag(HtmlNodeNavigatorOptions.LowercasedValues))
                    return value.ToLower(CultureInfo.CurrentCulture);
            }

            return value ?? string.Empty;
        }
    }

    public override XPathNavigator Clone() => new HtmlNodeNavigator(this);
    public override bool IsSamePosition(XPathNavigator other)
    {
        if (other is not HtmlNodeNavigator nav)
            return false;

        if (Document != null)
            return Document == nav.Document && CurrentNode == nav.CurrentNode;

        return BaseNode == nav.BaseNode && CurrentNode == nav.CurrentNode;
    }

    public override bool MoveTo(XPathNavigator other)
    {
        var nav = other as HtmlNodeNavigator;
        Trace("nav:" + nav);
        if (nav == null || (Document != null && nav.Document != Document) || (BaseNode != nav.BaseNode))
            return false;

        CurrentNode = nav.CurrentNode;
        return true;
    }

    public override bool MoveToFirstAttribute()
    {
        var element = CurrentNode as HtmlElement;
        Trace("element:" + element);
        if (element == null)
            return false;

        Trace("element.HasAttributes:" + element.HasAttributes);
        if (!element.HasAttributes)
            return false;

        CurrentNode = element.Attributes[0];
        return true;
    }

    public override bool MoveToFirstChild()
    {
        Trace("ChildNodes.HasChildNodes:" + CurrentNode?.HasChildNodes);
        if (CurrentNode == null || !CurrentNode.HasChildNodes)
            return false;

        CurrentNode = CurrentNode.ChildNodes[0];
        return true;
    }

    public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
    {
        if (CurrentNode is not HtmlElement element)
            return false;

        HtmlAttribute? att = null;
        HtmlAttributeList? attributes = null;
        switch (scope)
        {
            case XPathNamespaceScope.Local:
                if (element.HasAttributes)
                {
                    att = MoveToFirstNamespaceLocal(element.Attributes);
                }
                if (att == null)
                    return false;

                CurrentNode = att;
                break;

            case XPathNamespaceScope.ExcludeXml:
                if (element.HasAttributes)
                {
                    attributes = element.Attributes;
                    att = MoveToFirstNamespaceGlobal(_rootNode, ref attributes);
                }
                if (att == null)
                    return false;

                while (string.Equals(att.LocalName, HtmlNode.XmlPrefix, StringComparison.Ordinal))
                {
                    att = MoveToNextNamespaceGlobal(_rootNode, ref attributes, att);
                    if (att == null)
                        return false;
                }
                CurrentNode = att;
                break;

            default:
                //case XPathNamespaceScope.All:
                if (element.HasAttributes)
                {
                    attributes = element.Attributes;
                    att = MoveToFirstNamespaceGlobal(_rootNode, ref attributes);
                }
                if (att == null)
                {
                    if (Document == null)
                        return false;

                    CurrentNode = Document.NamespaceXml;
                }
                else
                {
                    CurrentNode = att;
                }
                break;
        }
        return true;
    }

    public override bool MoveToNextNamespace(XPathNamespaceScope scope)
    {
        if (CurrentNode is not HtmlAttribute attribute || !attribute.IsNamespace)
            return false;

        HtmlAttribute? att;
        var attributes = attribute.ParentNode?.HasAttributes == true ? attribute.ParentNode.Attributes : null;
        switch (scope)
        {
            case XPathNamespaceScope.Local:
                att = MoveToNextNamespaceLocal(attribute);
                if (att == null)
                    return false;

                CurrentNode = att;
                break;

            case XPathNamespaceScope.ExcludeXml:
                att = attribute;
                do
                {
                    att = MoveToNextNamespaceGlobal(_rootNode, ref attributes, att);
                    if (att == null)
                        return false;
                }
                while (string.Equals(att.LocalName, HtmlNode.XmlPrefix, StringComparison.Ordinal));
                CurrentNode = att;
                break;

            default:
                //case XPathNamespaceScope.All:
                att = attribute;
                do
                {
                    att = MoveToNextNamespaceGlobal(_rootNode, ref attributes, att);
                    if (att == null)
                    {
                        if (Document == null)
                            return false;

                        CurrentNode = Document.NamespaceXml;
                        return true;
                    }
                }
                while (string.Equals(att.LocalName, HtmlNode.XmlPrefix, StringComparison.Ordinal));
                CurrentNode = att;
                break;
        }
        return true;
    }

    public override bool MoveToId(string id)
    {
        Trace("id:" + id);
        throw new NotImplementedException();
    }

    public override bool MoveToNext()
    {
        var node = CurrentNode?.NextSibling;
        Trace("node:" + node);
        if (node == null)
            return false;

        CurrentNode = node;
        return true;
    }

    public override bool MoveToNextAttribute()
    {
        var att = CurrentNode as HtmlAttribute;
        Trace("att:" + att);
        if (att == null)
            return false;

        var node = att.NextSibling;
        Trace("next att:" + att);
        if (node == null)
            return false;

        CurrentNode = node;
        return true;
    }

    public override bool MoveToParent()
    {
        Trace("ParentNode:" + CurrentNode?.ParentNode);
        if (CurrentNode?.ParentNode == null)
            return false;

        if (_rootNode != null && CurrentNode.ParentNode == _rootNode)
        {
            Trace("ParentNode reached root");
            return false;
        }

        CurrentNode = CurrentNode.ParentNode;
        return true;
    }

    public override bool MoveToPrevious()
    {
        var node = CurrentNode?.PreviousSibling;
        Trace("PreviousSibling:" + node);
        if (node == null)
            return false;

        CurrentNode = node;
        return true;
    }

    public override void MoveToRoot()
    {
        Trace(null);
        if (Document != null)
        {
            CurrentNode = Document;
        }
        else
        {
            CurrentNode = BaseNode;
        }
    }

    private string GetOrAdd(string array)
    {
        var s = _nameTable.Get(array);
        return s ?? _nameTable.Add(array);
    }

    private static HtmlAttribute? MoveToFirstNamespaceLocal(HtmlAttributeList? attributes)
    {
        if (attributes == null)
            return null;

        foreach (var att in attributes)
        {
            if (att.IsNamespace)
                return att;
        }
        return null;
    }

    private static HtmlAttribute? MoveToFirstNamespaceGlobal(HtmlNode? rootNode, ref HtmlAttributeList attributes)
    {
        var att = MoveToFirstNamespaceLocal(attributes);
        if (att != null)
            return att;

        if (rootNode != null && attributes != null && attributes.Parent == rootNode)
            return null;

        var element = attributes != null ? attributes.Parent.ParentNode as HtmlElement : null;
        while (element != null)
        {
            if (rootNode != null && element.Equals(rootNode))
                return null;

            if (element.HasAttributes)
            {
                attributes = element.Attributes;
                att = MoveToFirstNamespaceLocal(attributes);
            }
            if (att != null)
                return att;

            element = element.ParentNode as HtmlElement;
        }
        return null;
    }

    private static HtmlAttribute? MoveToNextNamespaceLocal(HtmlAttribute? att)
    {
        att = att?.NextSibling;
        while (att != null)
        {
            if (att.IsNamespace)
                return att;

            att = att.NextSibling;
        }
        return null;
    }

    private static HtmlAttribute? MoveToNextNamespaceGlobal(HtmlNode? rootNode, ref HtmlAttributeList? attributes, HtmlAttribute? att)
    {
        var next = MoveToNextNamespaceLocal(att);
        if (next != null)
            return next;

        if (rootNode != null && attributes != null && attributes.Parent == rootNode)
            return null;

        var element = attributes != null ? attributes.Parent.ParentNode as HtmlElement : null;
        while (element != null)
        {
            if (rootNode != null && element.Equals(rootNode))
                return null;

            if (element.HasAttributes)
            {
                attributes = element.Attributes;
                next = MoveToFirstNamespaceLocal(attributes);
                if (next != null)
                    return next;
            }
            element = element.ParentNode as HtmlElement;
        }
        return null;
    }
}
