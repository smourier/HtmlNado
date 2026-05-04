using System.Diagnostics.CodeAnalysis;

namespace HtmlNado;

[TypeConverter(typeof(ExpandableObjectConverter))]
public abstract class HtmlNode : INotifyPropertyChanged, IXPathNavigable, IXmlNamespaceResolver
{
    // debugging
    internal const int _maxRecursion = 300;

    public const string XmlnsPrefix = "xmlns";
    public const string XmlnsNamespaceURI = "http://www.w3.org/2000/xmlns/";

    public const string XmlPrefix = "xml";
    public const string XmlNamespaceURI = "http://www.w3.org/XML/1998/namespace";

    public const string XhtmlPrefix = "xhtml";
    public const string XhtmlNamespaceURI = "http://www.w3.org/1999/xhtml";

    private readonly List<HtmlError> _errors = [];
    private HtmlNode? _parentNode;
    private HtmlDocument? _ownerDocument;
    private string _prefix;
    private string _localName;
    private object? _tag;

    // caches
    private string? _innerText;
    private string? _outerHtml;
    private string? _innerHtml;
    private string? _outerXml;
    private string? _innerXml;

    public event PropertyChangedEventHandler? PropertyChanged;

    static HtmlNode()
    {
        NamespaceManager = new XmlNamespaceManager(new NameTable());
        NamespaceManager.AddNamespace(XmlPrefix, XmlNamespaceURI);
        NamespaceManager.AddNamespace(XhtmlPrefix, XhtmlNamespaceURI);
    }

    protected HtmlNode(string prefix, string localName, string? namespaceURI, HtmlDocument? ownerDocument)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(localName);

        if (ownerDocument == null && this is not HtmlDocument)
            throw new ArgumentNullException(nameof(ownerDocument));

        _prefix = prefix;
        _localName = localName;
        DeclaredNamespaceURI = namespaceURI;
        OwnerDocument = ownerDocument;
        ChildNodes = new HtmlNodeList(this);
        Attributes = new HtmlAttributeList(this);
    }

    public static XmlNamespaceManager NamespaceManager { get; }

    public abstract HtmlNodeType NodeType { get; }
    public virtual bool HasAttributes => Attributes.Count > 0;
    public virtual bool HasChildNodes => ChildNodes.Count > 0;
    protected string? DeclaredNamespaceURI { get; private set; }

    public virtual int ParentIndex
    {
        get
        {
            var parent = ParentNode;
            if (parent != null && parent.HasChildNodes)
            {
                for (int i = 0; i < parent.ChildNodes.Count; i++)
                {
                    if (parent.ChildNodes[i] == this)
                        return i;
                }
            }
            return -1;
        }
    }

    public virtual IEnumerable<HtmlNode> NextSiblings
    {
        get
        {
            var parent = ParentNode;
            if (parent == null || !parent.HasChildNodes)
                yield break;

            var index = ParentIndex;
            if (index < 0 || (index + 1) >= parent.ChildNodes.Count)
                yield break;

            foreach (var node in parent.ChildNodes.Skip(index + 1))
            {
                yield return node;
            }
        }
    }

    public virtual HtmlNode? NextSibling
    {
        get
        {
            var parent = ParentNode;
            if (parent == null || !parent.HasChildNodes)
                return null;

            var index = ParentIndex;
            if (index < 0 || (index + 1) >= parent.ChildNodes.Count)
                return null;

            return parent.ChildNodes[index + 1];
        }
    }

    public virtual IEnumerable<HtmlNode> PreviousSiblings
    {
        get
        {
            var parent = ParentNode;
            if (parent == null || !parent.HasChildNodes)
                yield break;

            var index = ParentIndex;
            if (index <= 0)
                yield break;

            foreach (var node in parent.ChildNodes.Take(index))
            {
                yield return node;
            }
        }
    }

    public virtual HtmlNode? PreviousSibling
    {
        get
        {
            var parent = ParentNode;
            if (parent == null || !parent.HasChildNodes)
                return null;

            var index = ParentIndex;
            if (index <= 0)
                return null;

            return parent.ChildNodes[index - 1];
        }
    }

    public HtmlNodeList ChildNodes { get; }
    public HtmlAttributeList Attributes { get; }
    public string? Id { get => GetAttributeValue("id"); set => SetAttribute("id", value); }
    public virtual bool RaisePropertyChanged { get; set; }
    public virtual int StreamOrder { get; set; }

    public int Depth
    {
        get
        {
            var parent = ParentNode;
            return parent != null ? parent.Depth + 1 : 0;
        }
    }

    public string? Prefix
    {
        get => _prefix;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (string.Equals(_prefix, value, StringComparison.Ordinal))
                return;

            ClearCaches();
            _prefix = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Prefix)));
        }
    }

    public virtual string? LocalName
    {
        get => _localName;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            ClearCaches();
            _localName = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(LocalName)));
        }
    }

    public HtmlDocument? OwnerDocument
    {
        get
        {
            if (NodeType == HtmlNodeType.Document)
                return (HtmlDocument)this;

            return _ownerDocument;
        }
        private set => _ownerDocument = value;
    }

    public virtual string? NamespaceURI
    {
        get => GetNamespaceOfPrefix(Prefix);
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!string.Equals(DeclaredNamespaceURI, value, StringComparison.Ordinal))
            {
                DeclaredNamespaceURI = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(NamespaceURI)));
            }
        }
    }

    public virtual object? Tag
    {
        get => _tag;
        set
        {
            if (Equals(_tag, value))
                return;

            _tag = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Tag)));
        }
    }

    public virtual string? Value
    {
        get => null;
        set
        {
            //throw new InvalidOperationException();
        }
    }

    public virtual string? Name
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Prefix))
                return Prefix + ":" + LocalName;

            return LocalName;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            SetName(value);
        }
    }

    public HtmlNode? ParentNode
    {
        get => _parentNode;
        internal set
        {
            if (_parentNode != value)
            {
                if (value == null)
                {
                    // when detached, copy namespaces if it was computed
                    if (_parentNode != null)
                    {
                        foreach (var ns in _parentNode.GetAllNamespaces())
                        {
                            if (IsHtmlNs(ns.Value))
                                continue;

                            Attributes.Add(XmlnsPrefix, ns.Key, string.Empty, ns.Value);
                        }
                    }
                }
            }

            _parentNode = value;
            _ownerDocument = _parentNode?.OwnerDocument;
        }
    }

    public IEnumerable<HtmlError> Errors => _errors;
    public virtual string? OuterHtml
    {
        get
        {
            if (_outerHtml == null)
            {
                using var w = HtmlDocument.CreateStringWriter(OwnerDocument, CultureInfo.InvariantCulture);
                WriteTo(w);
                _outerHtml = w.ToString();
            }
            return _outerHtml;
        }
    }

    public virtual string? InnerText
    {
        get
        {
            if (_innerText == null)
            {
                var sb = new StringBuilder();
                AppendChildText(sb);
                _innerText = sb.ToString();
            }
            return _innerText;
        }
        set
        {
            if (!string.Equals(value, _innerText, StringComparison.Ordinal))
            {
                ClearCaches();
                var firstChild = FirstChild;
                if (firstChild != null && firstChild.NextSibling == null && firstChild.NodeType == HtmlNodeType.Text)
                {
                    firstChild.Value = value;
                }
                else
                {
                    if (OwnerDocument == null)
                        throw new ArgumentException(null, nameof(value));

                    RemoveAll();
                    var text = OwnerDocument.CreateText();
                    text.Value = value;
                    ChildNodes.Add(text);
                }
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(InnerText)));
            }
        }
    }

    public virtual string? InnerHtml
    {
        get
        {
            if (_innerHtml == null)
            {
                using var w = HtmlDocument.CreateStringWriter(OwnerDocument, CultureInfo.InvariantCulture);
                WriteContentTo(w);
                _innerHtml = w.ToString();
            }
            return _innerHtml;
        }
        set => throw new InvalidOperationException();
    }

    public virtual string OuterXml
    {
        get
        {
            if (_outerXml == null)
            {
                using var w = new StringWriter(CultureInfo.InvariantCulture);
                using (var writer = new XmlTextWriter(w))
                {
                    WriteTo(writer);
                }
                _outerXml = w.ToString();
            }
            return _outerXml;
        }
    }

    public virtual string InnerXml
    {
        get
        {
            if (_innerXml == null)
            {
                using var w = new StringWriter(CultureInfo.InvariantCulture);
                using (var writer = new XmlTextWriter(w))
                {
                    WriteContentTo(writer);
                }
                _innerXml = w.ToString();
            }
            return _innerXml;
        }
        set => throw new InvalidOperationException();
    }

    public HtmlNode? FirstChild
    {
        get
        {
            if (HasChildNodes)
                return ChildNodes[0];

            return null;
        }
    }

    public HtmlNode? LastChild
    {
        get
        {
            if (HasChildNodes)
                return ChildNodes[^1];

            return null;
        }
    }

    public override string ToString() => Name ?? string.Empty;

    [return: NotNullIfNotNull(nameof(name))]
    public static string? GetValidXmlName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return Utilities.Extensions.GetValidXmlName(name);
    }

    protected virtual internal void ClearCaches() => ClearCaches(0);

    private void ClearCaches(int index)
    {
        // deep recursion testing. incurred because of xslt in general
        if (index > _maxRecursion)
            throw new HtmlException("HTML0005: Maximum recursion depth (" + _maxRecursion + ") exceeded. This may be caused by a recursive XSLT.");

        _innerHtml = null;
        _innerText = null;
        _innerXml = null;
        _outerHtml = null;
        _outerXml = null;

        _parentNode?.ClearCaches(index + 1);
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!RaisePropertyChanged)
            return;

        PropertyChanged?.Invoke(this, e);
    }

    protected virtual void ParseName(string? name, out string? prefix, out string? localName)
    {
        if (name == null)
        {
            prefix = null;
            localName = null;
            return;
        }

        var pos = name.IndexOf(':');
        if (pos < 0)
        {
            prefix = string.Empty;
            localName = name;
            return;
        }

        prefix = name[..pos].Nullify();
        localName = name[(pos + 1)..].Nullify();
        if (prefix == null || localName == null)
        {
            prefix = string.Empty;
            localName = name;
        }
    }

    public virtual void ResetStreamOrder(int newOrder)
    {
        StreamOrder = newOrder;
        if (HasAttributes)
        {
            foreach (var att in Attributes)
            {
                att.ResetStreamOrder(newOrder);
            }
        }

        if (HasChildNodes)
        {
            foreach (var node in ChildNodes)
            {
                node.ResetStreamOrder(newOrder);
            }
        }
    }

    private void SetName(string name)
    {
        if (string.Equals(name, Name, StringComparison.Ordinal))
            return;

        ClearCaches();
        ParseName(name, out var prefix, out var localName);
        Prefix = prefix;
        LocalName = localName;
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Name)));
    }

    internal static bool IsHtmlNs(string? ns) => string.IsNullOrWhiteSpace(ns) || string.Equals(ns, XhtmlNamespaceURI, StringComparison.Ordinal);

    protected virtual internal void AddError(HtmlError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(error);
    }

    protected virtual internal void ClearErrors() => _errors.Clear();

    private void AppendChildText(StringBuilder builder)
    {
        if (HasChildNodes)
        {
            foreach (var node in ChildNodes)
            {
                if (node.NodeType == HtmlNodeType.Text)
                {
                    builder.Append(node.InnerText);
                }
                else
                {
                    node.AppendChildText(builder);
                }
            }
        }
    }

    public HtmlAttribute SetAttribute(string localName, string namespaceURI, string? value) => SetAttribute(string.Empty, localName, namespaceURI, value);
    public HtmlAttribute SetAttribute(string prefix, string localName, string namespaceURI, string? value)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceURI);

        var att = Attributes[localName, namespaceURI];
        if (att == null)
        {
            att = Attributes.Add(prefix, localName, namespaceURI, value);
        }
        else
        {
            att.Value = value;
        }
        return att;
    }

    public bool RemoveAttribute(string? name) => Attributes.Remove(name);
    public bool RemoveAttribute(string? localName, string? namespaceURI) => Attributes.Remove(localName, namespaceURI);
    public bool RemoveAttributeByPrefix(string? prefix, string? localName) => Attributes.RemoveByPrefix(prefix, localName);

    public HtmlAttribute SetAttribute(string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        var att = Attributes[name];
        if (att == null)
        {
            att = Attributes.Add(name, value);
        }
        else
        {
            att.Value = value;
        }
        return att;
    }

    public bool HasAttribute(string localName, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceURI);
        return Attributes[localName, namespaceURI] != null;
    }

    public bool HasNonNullNorWhitespaceAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return GetNullifiedAttributeValue(name) != null;
    }

    public bool HasAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Attributes[name] != null;
    }

    public string? GetAttributeValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var att = Attributes[name];
        if (att == null)
            return null;

        return att.Value;
    }

    public T? GetAttributeValue<T>(string name, T? defaultValue = default, IFormatProvider? provider = null) where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(name);

        var att = Attributes[name];
        if (att == null)
            return defaultValue;

        var value = att.Value;
        if (value == null)
            return defaultValue;

        if (typeof(T) == typeof(string) || typeof(T) == typeof(object))
            return (T)(object)value;

        if (T.TryParse(value, provider, out var result))
            return result;

        return defaultValue;
    }

    public T? GetAttributeValue<T>(string localName, string namespaceURI, T? defaultValue = default, IFormatProvider? provider = null) where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceURI);

        var att = Attributes[localName, namespaceURI];
        if (att == null)
            return defaultValue;

        var value = att.Value;
        if (value == null)
            return defaultValue;

        if (typeof(T) == typeof(string) || typeof(T) == typeof(object))
            return (T)(object)value;

        if (T.TryParse(value, provider, out var result))
            return result;

        return defaultValue;
    }

    public string? GetNullifiedAttributeValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Attributes[name]?.Value.Nullify();
    }

    public string? GetNullifiedAttributeValue(string localName, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceURI);
        return Attributes[localName, namespaceURI]?.Value.Nullify();
    }

    public virtual void AppendChild(HtmlNode newChild)
    {
        if (newChild is HtmlAttribute)
            throw new ArgumentException(null, nameof(newChild));

        ChildNodes.Add(newChild);
    }

    public virtual void InsertAfter(HtmlNode newChild, HtmlNode refChild)
    {
        ArgumentNullException.ThrowIfNull(newChild);

        if (newChild is HtmlAttribute)
            throw new ArgumentException(null, nameof(newChild));

        if (this == newChild || IsAncestor(newChild))
            throw new ArgumentException(null, nameof(newChild));

        if (newChild.NodeType == HtmlNodeType.Document)
            throw new ArgumentException(null, nameof(newChild));

        if (OwnerDocument == null)
            throw new InvalidOperationException();

        if (refChild == null)
        {
            PrependChild(newChild);
            return;
        }

        if (newChild == refChild)
            return;

        var index = -1;
        if (HasChildNodes)
        {
            for (var i = 0; i < ChildNodes.Count; i++)
            {
                var node = ChildNodes[i];
                if (node == refChild)
                {
                    index = i;
                    break;
                }
            }
        }

        if (index < 0)
            throw new ArgumentException(null, nameof(refChild));

        ChildNodes.Insert(index + 1, newChild);
    }

    public bool IsAncestor(HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        for (var n = ParentNode; (n != null) && (n != this); n = n.ParentNode)
        {
            if (n == node)
                return true;
        }
        return false;
    }

    public virtual void InsertBefore(HtmlNode newChild, HtmlNode refChild)
    {
        ArgumentNullException.ThrowIfNull(newChild);

        if (newChild is HtmlAttribute)
            throw new ArgumentException(null, nameof(newChild));

        if (this == newChild || IsAncestor(newChild))
            throw new ArgumentException(null, nameof(newChild));

        if (newChild.NodeType == HtmlNodeType.Document)
            throw new ArgumentException(null, nameof(newChild));

        if (refChild == null)
        {
            AppendChild(newChild);
            return;
        }

        if (newChild == refChild)
            return;

        var index = -1;
        if (HasChildNodes)
        {
            for (var i = 0; i < ChildNodes.Count; i++)
            {
                var node = ChildNodes[i];
                if (node == refChild)
                {
                    index = i;
                    break;
                }
            }
        }

        if (index < 0)
            throw new ArgumentException(null, nameof(refChild));

        ChildNodes.Insert(index, newChild);
    }

    public virtual void PrependChild(HtmlNode newChild)
    {
        if (newChild is HtmlAttribute)
            throw new ArgumentException(null, nameof(newChild));

        ChildNodes.Insert(0, newChild);
    }

    public virtual bool Remove(bool keepChildren = false)
    {
        var parent = ParentNode;
        if (parent != null)
            return parent.RemoveChild(this, keepChildren);

        return false;
    }

    public virtual void RemoveAll() => ChildNodes.RemoveAll();
    public virtual bool RemoveChild(HtmlNode oldChild, bool keepGrandChildren = false)
    {
        if (oldChild is HtmlAttribute att)
        {
            if (string.IsNullOrWhiteSpace(att.NamespaceURI))
                return RemoveAttribute(att.Name);

            return RemoveAttribute(att.LocalName, att.NamespaceURI);
        }

        var index = ChildNodes.IndexOf(oldChild);
        if (index < 0)
            return false;

        if (keepGrandChildren)
        {
            var oldIndex = oldChild.ParentIndex;
            foreach (var child in oldChild.ChildNodes)
            {
                child._parentNode = null;
                ChildNodes.Insert(oldIndex++, child);
            }
            oldChild.ChildNodes.RemoveAllNoCheck();
        }

        return ChildNodes.Remove(oldChild);
    }

    public virtual void ReplaceChild(HtmlNode newChild, HtmlNode oldChild)
    {
        if (newChild is HtmlAttribute)
            throw new ArgumentException(null, nameof(newChild));

        if (oldChild is HtmlAttribute)
            throw new ArgumentException(null, nameof(oldChild));

        ChildNodes.Replace(newChild, oldChild);
    }

    public virtual IXPathNavigable CreateNavigable(HtmlNodeNavigatorOptions options) => new Navigable(OwnerDocument, this, options);

    private sealed class Navigable(HtmlDocument? ownerDocument, HtmlNode node, HtmlNodeNavigatorOptions options) : IXPathNavigable
    {
        public XPathNavigator CreateNavigator() => new HtmlNodeNavigator(ownerDocument, node, options);
    }

    public XPathNavigator CreateNavigator() => CreateNavigator(HtmlNodeNavigatorOptions.None);
    public virtual XPathNavigator CreateNavigator(HtmlNodeNavigatorOptions options) => new HtmlNodeNavigator(OwnerDocument, this, options);

    public HtmlNode? SelectSingleNode(string? xpath, XmlNamespaceManager? nsmgr = null) => SelectSingleNode(xpath, nsmgr, HtmlNodeNavigatorOptions.None);
    public HtmlNode? SelectSingleNode(string? xpath, HtmlNodeNavigatorOptions? options) => SelectSingleNode(xpath, null, options);
    public HtmlNode? SelectSingleNode(string? xpath, XmlNamespaceManager? nsmgr, HtmlNodeNavigatorOptions? options) => SelectNodes(xpath, nsmgr, options).FirstOrDefault();

    public IEnumerable<HtmlNode> SelectNodes(string? xpath, XmlNamespaceManager? nsmgr = null) => SelectNodes(xpath, nsmgr, HtmlNodeNavigatorOptions.None);
    public IEnumerable<HtmlNode> SelectNodes(string? xpath, HtmlNodeNavigatorOptions? options) => SelectNodes(xpath, null, options);
    public virtual IEnumerable<HtmlNode> SelectNodes(string? xpath, XmlNamespaceManager? nsmgr, HtmlNodeNavigatorOptions? options)
    {
        var opts = options ?? HtmlNodeNavigatorOptions.None;
        if (opts.HasFlag(HtmlNodeNavigatorOptions.Dynamic))
            return DoSelectNodes(xpath, nsmgr, opts);

        var list = DoSelectNodes(xpath, nsmgr, opts).ToList();
        if (opts.HasFlag(HtmlNodeNavigatorOptions.DepthFirst))
        {
            list.Sort(new HtmlNodeDepthComparer { Direction = ListSortDirection.Descending });
        }
        return list;
    }

    protected virtual IEnumerable<HtmlNode> DoSelectNodes(string? xpath, XmlNamespaceManager? nsmgr, HtmlNodeNavigatorOptions options)
    {
        if (string.IsNullOrWhiteSpace(xpath))
            yield break;

        var navigator = CreateNavigator(options);
        if (navigator == null)
            yield break;

        var expr = navigator.Compile(xpath);
        if (nsmgr != null)
        {
            expr.SetContext(nsmgr);
        }

        var eval = navigator.Evaluate(expr);
        if (eval is XPathNodeIterator it)
        {
            while (it.MoveNext())
            {
                if (it.Current is HtmlNodeNavigator nav && nav.CurrentNode != null)
                    yield return nav.CurrentNode;
            }
        }
        else
        {
            yield return new HtmlXPathResult(OwnerDocument, eval);
        }
    }

    public virtual string? GetNamespaceOfPrefix(string? prefix)
    {
        if (prefix == null)
            return null;

        if (prefix.EqualsIgnoreCase(Prefix) && DeclaredNamespaceURI != null)
            return DeclaredNamespaceURI;

        foreach (var att in Attributes)
        {
            if (att.Prefix.EqualsIgnoreCase(XmlnsPrefix) && att.LocalName.EqualsIgnoreCase(prefix))
                return att.Value;
        }

        var parent = ParentNode;
        if (parent != null && parent != this)
            return parent.GetNamespaceOfPrefix(prefix);

        var owner = OwnerDocument;
        if (owner != null && owner != this)
            return owner.GetNamespaceOfPrefix(prefix);

        return null;
    }

    public virtual string? GetPrefixOfNamespace(string? namespaceURI)
    {
        if (namespaceURI.EqualsIgnoreCase(NamespaceURI))
            return Prefix;

        foreach (var att in Attributes)
        {
            if (att.Prefix.EqualsIgnoreCase(XmlnsPrefix) && att.Value.EqualsIgnoreCase(namespaceURI))
                return att.LocalName;
        }

        var parent = ParentNode;
        if (parent != null && parent != this)
            return parent.GetPrefixOfNamespace(namespaceURI);

        var owner = OwnerDocument;
        if (owner != null && owner != this)
            return owner.GetPrefixOfNamespace(namespaceURI);

        return null;
    }

    public virtual HtmlNode? GetParent(Func<HtmlNode, bool> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (ParentNode == null)
            return null;

        if (func(ParentNode))
            return ParentNode;

        return ParentNode.GetParent(func);
    }

    public IDictionary<string, string?> GetAllNamespaces()
    {
        var namespaces = new Dictionary<string, string?>();
        GetNamespaceAttributes(namespaces);
        return namespaces;
    }

    protected virtual void GetNamespaceAttributes(IDictionary<string, string?> namespaces)
    {
        ArgumentNullException.ThrowIfNull(namespaces);

        foreach (var att in Attributes)
        {
            if (att.Prefix.EqualsIgnoreCase(XmlnsPrefix))
            {
                if (att.LocalName != null)
                {
                    namespaces[att.LocalName] = att.Value;
                }
            }
        }

        ParentNode?.GetNamespaceAttributes(namespaces);
    }

    public abstract void WriteTo(TextWriter writer);
    public abstract void WriteContentTo(TextWriter writer);
    public abstract void WriteTo(XmlWriter writer);
    public abstract void WriteContentTo(XmlWriter writer);

    public HtmlNode Clone(HtmlCloneOptions options = HtmlCloneOptions.All)
    {
        var clone = CreateNew();
        CopyTo(clone, options);
        return clone;
    }

    public virtual void CopyTo(HtmlNode target, HtmlCloneOptions options)
    {
        ArgumentNullException.ThrowIfNull(target);

        target.Value = Value;

        if ((options & HtmlCloneOptions.StreamOrder) == HtmlCloneOptions.StreamOrder)
        {
            target.StreamOrder = StreamOrder;
        }

        if (!IsHtmlNs(DeclaredNamespaceURI))
        {
            target.DeclaredNamespaceURI = DeclaredNamespaceURI;
        }
        else
        {
            var ns = NamespaceURI;
            if (!IsHtmlNs(ns))
            {
                target.DeclaredNamespaceURI = ns;
            }
        }

        if ((options & HtmlCloneOptions.Attributes) == HtmlCloneOptions.Attributes)
        {
            foreach (var att in Attributes)
            {
#if DEBUG_HTML_ID
                if (att.Name == HtmlElement.DebugIdAttributeName)
                    continue;
#endif
                var cloneAtt = (HtmlAttribute)att.Clone(options);

                if ((options & HtmlCloneOptions.OverwriteAttributes) == HtmlCloneOptions.OverwriteAttributes)
                {
                    target.Attributes[cloneAtt.Name] = cloneAtt;
                }
                else
                {
                    target.Attributes.Add(cloneAtt);
                }
            }
        }

        if ((options & HtmlCloneOptions.Deep) == HtmlCloneOptions.Deep)
        {
            foreach (var node in ChildNodes)
            {
                var cloneNode = node.Clone(options);
                target.AppendChild(cloneNode);
            }
        }

        if ((options & HtmlCloneOptions.Tag) == HtmlCloneOptions.Tag)
        {
            target._tag = _tag;
        }
    }

    // NOTE: the doc must have been loaded as an HtmlXPathDocument for this to be valid
    public virtual string? XPathExpression => null;

    [Conditional("DEBUG")]
    internal virtual void CheckParenting()
    {
        if (HasAttributes)
        {
            foreach (var att in Attributes)
            {
                if (att.ParentNode != this)
                    throw new HtmlException("Internal error: node parenting is wrong. Attribute: " + att.Name);
            }
        }

        if (HasChildNodes)
        {
            foreach (var node in ChildNodes)
            {
                if (node.ParentNode != this)
                    throw new HtmlException("Internal error: node parenting is wrong. Node: " + node.Clone(HtmlCloneOptions.Attributes).OuterHtml);

                node.CheckParenting();
            }
        }
    }

    public virtual HtmlNode CreateNew()
    {
        if (OwnerDocument == null)
            throw new InvalidOperationException();

        switch (NodeType)
        {
            case HtmlNodeType.Attribute:
                if (LocalName == null)
                    throw new InvalidOperationException("Cannot create a new attribute node when LocalName is null.");

                return OwnerDocument.CreateAttribute(Prefix ?? string.Empty, LocalName, NamespaceURI);

            case HtmlNodeType.Comment:
                return OwnerDocument.CreateComment();

            case HtmlNodeType.Document:
                return OwnerDocument.CreateDocument();

            case HtmlNodeType.Element:
            case HtmlNodeType.ProcessingInstruction:
            case HtmlNodeType.DocumentType:
                if (LocalName == null)
                    throw new InvalidOperationException("Cannot create a new element node when LocalName is null.");

                return OwnerDocument.CreateElement(Prefix ?? string.Empty, LocalName, NamespaceURI);

            case HtmlNodeType.Text:
                return OwnerDocument.CreateText();

            case HtmlNodeType.XPathResult:
                var result = (HtmlXPathResult)this;
                return new HtmlXPathResult(OwnerDocument, result.Result);

            default:
                throw new NotSupportedException();
        }
    }

    protected virtual void AddNamespacesInScope(XmlNamespaceScope scope, IDictionary<string, string> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (!string.IsNullOrWhiteSpace(NamespaceURI))
        {
            if (Prefix != null && (scope != XmlNamespaceScope.ExcludeXml || !string.Equals(NamespaceURI, XmlnsNamespaceURI, StringComparison.Ordinal)))
            {
                dictionary[Prefix] = NamespaceURI;
            }
        }

        if (ParentNode != null && scope != XmlNamespaceScope.Local)
        {
            ParentNode.AddNamespacesInScope(scope, dictionary);
        }
    }

    public virtual IXmlNamespaceResolver ParentNamespaceResolver
    {
        get
        {
            if (OwnerDocument != null)
                return OwnerDocument;

            if (ParentNode != null)
                return ParentNode.ParentNamespaceResolver;

            return this;
        }
    }

    public virtual IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
    {
        var dic = new Dictionary<string, string>();
        AddNamespacesInScope(scope, dic);
        return dic;
    }

    string IXmlNamespaceResolver.LookupNamespace(string prefix) => GetNamespaceOfPrefix(prefix) ?? string.Empty;
    string IXmlNamespaceResolver.LookupPrefix(string namespaceName) => GetPrefixOfNamespace(namespaceName) ?? string.Empty;

    public XmlNode ImportAsXml(XmlDocument owner) => ImportAsXml(owner, true);
    public virtual XmlNode ImportAsXml(XmlDocument owner, bool deep)
    {
        ArgumentNullException.ThrowIfNull(owner);

        // we don't use CreateStringWriter because we don't want custom escaping
        using var sw = new StringWriter(CultureInfo.InvariantCulture);
        using var writer = new XmlTextWriter(sw);
        WriteTo(writer);
        var nodeDoc = new XmlDocument();
        nodeDoc.Load(new StringReader(sw.ToString()));
        if (nodeDoc.DocumentElement == null)
            throw new InvalidOperationException("Failed to import node as XML. The generated XML is invalid.");

        return owner.ImportNode(nodeDoc.DocumentElement, deep);
    }
}
