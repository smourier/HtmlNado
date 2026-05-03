namespace HtmlNado;

[DebuggerDisplay("{Name}")]
public class HtmlDocument : HtmlNode
{
    public static readonly Encoding UTF8NoBOMEncoding = new UTF8Encoding(false, true);

    private HtmlOptions _options = new();
    private string _filePath;
    private HtmlElement _baseElement;
    private bool _baseElementSearched;
    private bool? _xhtml;
    private HtmlAttribute _namespaceXml;
    private Dictionary<string, string> _declaredNamespaces;
    private Dictionary<string, string> _declaredPrefixes;
    private Dictionary<string, HtmlNode> _nodesById;

    public event EventHandler<HtmlDocumentParseEventArgs> Parsing;
    public event EventHandler<HtmlDocumentParseEventArgs> Parsed;

    public HtmlDocument()
        : base(string.Empty, "#document", string.Empty, null)
    {
    }

    public virtual Encoding StreamEncoding { get; private set; }
    public virtual Encoding DetectedEncoding { get; private set; }
    public virtual Uri BaseAddress { get; set; }
    public virtual bool ReaderWasRestarted { get; private set; }
    public virtual HtmlElement DocumentType { get; private set; }
    public virtual HtmlElement HtmlElement { get; private set; }
    public virtual HtmlElement BodyElement { get; private set; }
    public virtual HtmlElement HeadElement { get; private set; }

    internal static T ChangeType<T>(object value, T defaultValue) => Conversions.ChangeType(value, defaultValue);

    internal static void RemoveIntrinsicElement(HtmlDocument doc, HtmlElement element)
    {
        if (doc == null || element == null)
            return;

        if (element == doc.HtmlElement)
        {
            doc.HtmlElement = null;
            return;
        }

        if (element == doc.BodyElement)
        {
            doc.BodyElement = null;
            return;
        }

        if (element == doc.HeadElement)
        {
            doc.HeadElement = null;
            return;
        }

        if (element == doc.DocumentType)
        {
            doc.DocumentType = null;
            return;
        }
    }

    public virtual string Title
    {
        get => SelectSingleNode("//title", HtmlNodeNavigatorOptions.LowercasedNames)?.InnerText;
        set
        {
            var title = SelectSingleNode("//title", HtmlNodeNavigatorOptions.LowercasedNames);
            if (title == null)
            {
                var head = HeadElement;
                if (head == null)
                {
                    var html = HtmlElement;
                    if (html == null)
                    {
                        html = CreateElement("html");
                        PrependChild(html);
                    }

                    head = CreateElement("head");
                    html.PrependChild(head);
                }

                title = CreateElement("title");
                head.PrependChild(title);
            }
            title.InnerText = value;
        }
    }

    public virtual string FilePath
    {
        get => _filePath;
        protected set
        {
            _filePath = value;
            if (BaseAddress == null)
            {
                BaseAddress = Path.IsPathRooted(_filePath) ? new Uri(_filePath) : new Uri(Path.GetFullPath(_filePath));
            }
        }
    }

    public virtual HtmlElement BaseElement
    {
        get
        {
            if (_baseElement == null && !_baseElementSearched)
            {
                _baseElement = SelectSingleNode("//base") as HtmlElement;
                _baseElementSearched = true;
            }
            return _baseElement;
        }
        set
        {
            _baseElement = value;
            _baseElementSearched = false;
        }
    }

    public virtual HtmlOptions Options
    {
        get => _options;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _options = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Options)));
        }
    }

    public string MakeAbsoluteAddress(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return MakeAbsoluteAddress(new Uri(url, UriKind.RelativeOrAbsolute)).ToString();
    }

    public Uri MakeAbsoluteAddress(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (uri.IsAbsoluteUri)
            return uri;

        var baseAddress = BaseAddress;
        var baseElement = BaseElement;
        if (baseElement != null)
        {
            var href = baseElement.GetNullifiedAttributeValue("href");
            if (href != null)
            {
                var address = new Uri(href, UriKind.RelativeOrAbsolute);
                if (address.IsAbsoluteUri)
                {
                    baseAddress = address;
                }
            }
        }

        if (baseAddress == null)
            throw new HtmlException("HTML0002: Cannot determine document's base address.");

        return new Uri(baseAddress, uri);
    }

    public void LoadHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        Clear();
        using var reader = new StringReader(html);
        StreamEncoding = Encoding.Default; // This is arguable, but it's better for saves
        InternalLoad(reader, false);
    }

    public void Load(string filePath, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        Clear();
        FilePath = filePath;
        using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks, bufferSize))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(filePath, streamEncoding, false, bufferSize))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(string filePath, bool detectEncodingFromByteOrderMarks)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        Clear();
        FilePath = filePath;
        if (detectEncodingFromByteOrderMarks)
        {
            using var reader = new StreamReader(filePath, true);
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }
        else
        {
            // use ansi as the default encoding
            using var reader = new StreamReader(filePath, Encoding.Default, false);
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(filePath, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(string filePath, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        Clear();
        FilePath = filePath;
        using (var reader = new StreamReader(filePath, encoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(filePath, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(string filePath, Encoding encoding, bool detectEncodingFromByteOrderMarks)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        Clear();
        FilePath = filePath;
        using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(filePath, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        Clear();
        FilePath = filePath;
        using (var reader = new StreamReader(filePath, Encoding.Default, true))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(filePath, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(Stream stream, bool detectEncodingFromByteOrderMarks)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Clear();
        if (detectEncodingFromByteOrderMarks)
        {
            using var reader = new StreamReader(stream, true);
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }
        else
        {
            using var reader = new StreamReader(stream, Encoding.Default, false);
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(stream, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(Stream stream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Clear();
        using (var reader = new StreamReader(stream, encoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(stream, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Clear();
        using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(stream, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Clear();
        using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(stream, streamEncoding, false, bufferSize))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Clear();
        using (var reader = new StreamReader(stream, Encoding.Default))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            if (InternalLoad(reader, true))
                return;
        }

        var streamEncoding = DetectedEncoding;
        Restart();
        using (var reader = new StreamReader(stream, streamEncoding))
        {
            reader.Peek();
            StreamEncoding = reader.CurrentEncoding;
            InternalLoad(reader, false);
        }
    }

    public void Load(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Clear();
        InternalLoad(reader, false);
    }

    public virtual IDictionary<string, HtmlNode> Ids
    {
        get
        {
            _nodesById = _nodesById ?? new Dictionary<string, HtmlNode>(StringComparer.OrdinalIgnoreCase);
            return _nodesById;
        }
    }

    protected virtual void ClearAllIds() => Ids.Clear();
    internal void ClearIds(IEnumerable<HtmlNode> nodes)
    {
        _ = Ids;
        foreach (var node in nodes)
        {
            ClearId(node?.Id);
        }
    }

    internal void ClearId(HtmlNode node) => ClearId(node.Id);
    protected virtual internal bool ClearId(string id)
    {
        if (id == null)
            return false;

        return Ids.Remove(id);
    }

    internal void SetNodeById(HtmlNode node)
    {
        var id = node?.Id;
        if (id == null)
            return;

        SetNodeById(id, node);
    }

    protected virtual void SetNodeById(string id, HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(id);

        ArgumentNullException.ThrowIfNull(node);

        if (Options.DontBuildIdDictionary)
            return;

        Ids[id] = node;
    }

    public HtmlElement GetElementById(string id) => GetNodeById(id) as HtmlElement;
    public virtual HtmlNode GetNodeById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        Ids.TryGetValue(id, out var node);
        return node;
    }

    public virtual void AddNamespace(string prefix, string uri)
    {
        if (prefix == null)
        {
            if (_declaredPrefixes != null)
            {
                _declaredPrefixes.Remove(prefix);
            }
        }
        else
        {
            ArgumentNullException.ThrowIfNull(uri);

            if (_declaredPrefixes == null)
            {
                _declaredPrefixes = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            _declaredPrefixes[prefix] = uri;
        }

        if (uri == null)
        {
            if (_declaredNamespaces != null)
            {
                _declaredNamespaces.Remove(uri);
            }
        }
        else
        {
            ArgumentNullException.ThrowIfNull(prefix);

            if (_declaredNamespaces == null)
            {
                _declaredNamespaces = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            _declaredNamespaces[uri] = prefix;
        }
    }

    public override string GetNamespaceOfPrefix(string prefix)
    {
        if (_declaredPrefixes == null)
            return string.Empty;

        if (_declaredPrefixes.TryGetValue(prefix, out string namespaceURI))
            return namespaceURI;

        return string.Empty;
    }

    public override string GetPrefixOfNamespace(string namespaceURI)
    {
        if (_declaredNamespaces == null)
            return string.Empty;

        if (_declaredNamespaces.TryGetValue(namespaceURI, out string prefix))
            return prefix;

        return string.Empty;
    }

    protected override void GetNamespaceAttributes(IDictionary<string, string> namespaces)
    {
        base.GetNamespaceAttributes(namespaces);
        if (_declaredPrefixes != null)
        {
            foreach (var kv in _declaredPrefixes)
            {
                namespaces[kv.Key] = kv.Value;
            }
        }

        if (_declaredNamespaces != null)
        {
            foreach (var kv in _declaredNamespaces)
            {
                namespaces[kv.Value] = kv.Key;
            }
        }
    }

    public IReadOnlyDictionary<string, string> DeclaredNamespaces
    {
        get
        {
            if (_declaredNamespaces == null)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            return _declaredNamespaces;
        }
    }

    public IReadOnlyDictionary<string, string> DeclaredPrefixes
    {
        get
        {
            if (_declaredPrefixes == null)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            return _declaredPrefixes;
        }
    }

    private HtmlAttribute CreateAttribute(string name)
    {
        ParseName(name, out string prefix, out string localName);
        return CreateAttribute(prefix, localName, null);
    }

    public virtual HtmlAttribute CreateAttribute(string prefix, string localName, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        if (prefix.IndexOf(':') >= 0)
            throw new ArgumentException(null, nameof(prefix));

        ArgumentNullException.ThrowIfNull(localName);

        return new HtmlAttribute(prefix, localName, namespaceURI, this);
    }

    public virtual HtmlText CreateText() => new(this);

    public HtmlElement CreateElement(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        ParseName(name, out string prefix, out string localName);
        return CreateElement(prefix, localName, null);
    }

    public virtual HtmlElement CreateElement(string prefix, string localName, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        if (prefix.IndexOf(':') >= 0)
            throw new ArgumentException(null, nameof(prefix));

        ArgumentNullException.ThrowIfNull(localName);

        return new HtmlElement(prefix, localName, namespaceURI, this);
    }

    public virtual HtmlDocument CreateDocument() => new();
    public virtual HtmlComment CreateComment() => new(this);

    private void Clear()
    {
        Attributes.RemoveAll();
        ChildNodes.RemoveAll();
        ClearCaches();
        _filePath = null;
        ClearErrors();
        _baseElement = null;
        _baseElementSearched = false;
        _xhtml = null;
        HtmlElement = null;
        BodyElement = null;
        HeadElement = null;
        BaseAddress = null;
        DocumentType = null;
        DetectedEncoding = null;
        StreamEncoding = null;
        ReaderWasRestarted = false;
    }

    private void Restart()
    {
        Clear();
        ReaderWasRestarted = true;
    }

    // see http://stackoverflow.com/questions/4696499/meta-charset-utf-8-vs-meta-http-equiv-content-type
    private static string GetEncodingName(HtmlElement meta)
    {
        var name = meta.GetAttributeValue<string>("charset", null).Nullify();
        if (name != null)
            return name;

        var ct = meta.GetAttributeValue<string>("http-equiv", null);
        if (!ct.EqualsIgnoreCase("content-type"))
            return null;

        return Utilities.Extensions.GetAttributeFromHeader(meta.GetAttributeValue<string>("content", null).Nullify(), "charset");
    }

    private bool DetectEncoding(HtmlReader reader, HtmlElement element, bool firstPass)
    {
        if (DetectedEncoding != null)
            return true;

        if (element == null || !element.Name.EqualsIgnoreCase("meta"))
            return true;

        var encodingName = GetEncodingName(element);
        if (encodingName == null)
            return true;

        if (Options.ReaderThrowsOnUnknownDetectedEncoding)
        {
            DetectedEncoding = Encoding.GetEncoding(encodingName);
        }
        else
        {
            try
            {
                DetectedEncoding = Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return true;
            }
        }

        // update stream encoding
        if (reader.TextReader is StreamReader sr)
        {
            StreamEncoding = sr.CurrentEncoding;
        }

        if (DetectedEncoding != null && StreamEncoding != null && !string.Equals(DetectedEncoding.EncodingName, StreamEncoding.EncodingName, StringComparison.OrdinalIgnoreCase))
        {
            if (firstPass && Options.ReaderRestartsOnEncodingDetected && reader.IsRestartable)
            {
                if (reader.Restart())
                    return false;
            }

            AddError(new HtmlError(reader.State.Line, reader.State.Column, reader.State.Offset, HtmlErrorType.EncodingMismatch));
            if (Options.ReaderThrowsOnEncodingMismatch)
                throw new HtmlException(string.Format(CultureInfo.CurrentCulture, "HTML0004: Html encoding mismatch error. There seems to be mismatch between the stream (HTTP, File, etc.) encoding '{0}' and the declared (HTML META) encoding '{1}'.", StreamEncoding.EncodingName, DetectedEncoding.EncodingName));
        }
        return true;
    }

    protected virtual void OnParsing(object sender, HtmlDocumentParseEventArgs e) => Parsing?.Invoke(sender, e);
    protected virtual void OnParsed(object sender, HtmlDocumentParseEventArgs e) => Parsed?.Invoke(sender, e);

    private bool OnParsing(HtmlReader reader, ref HtmlNode currentNode, ref HtmlAttribute currentAttribute, out bool cont)
    {
        var e = new HtmlDocumentParseEventArgs(reader);
        e.DetectedEncoding = DetectedEncoding;
        e.CurrentNode = currentNode;
        e.CurrentAttribute = currentAttribute;
        OnParsing(this, e);
        DetectedEncoding = e.DetectedEncoding;
        currentNode = e.CurrentNode;
        currentAttribute = e.CurrentAttribute;
        cont = e.Continue;
        if (e.Cancel)
            return false;

        return true;
    }

    private bool OnParsed(HtmlReader reader, ref HtmlNode currentNode, ref HtmlAttribute currentAttribute)
    {
        var e = new HtmlDocumentParseEventArgs(reader);
        e.DetectedEncoding = DetectedEncoding;
        e.CurrentNode = currentNode;
        e.CurrentAttribute = currentAttribute;
        OnParsed(this, e);
        DetectedEncoding = e.DetectedEncoding;
        currentNode = e.CurrentNode;
        currentAttribute = e.CurrentAttribute;
        if (e.Cancel)
            return false;

        return true;
    }

    public virtual HtmlReader CreateReader(TextReader reader) => new(reader, Options);

    private bool InternalLoad(TextReader reader, bool firstPass)
    {
        HtmlNode current = this;
        HtmlAttribute currentAtt = null;
        var htmlReader = CreateReader(reader);
        while (htmlReader.Read())
        {
            if (!OnParsing(htmlReader, ref current, ref currentAtt, out bool cont))
                break;

            if (cont)
                continue;

            HtmlElement element;
            HtmlError error;
            switch (htmlReader.State.FragmentType)
            {
                case HtmlFragmentType.CDataText:
                case HtmlFragmentType.Text:
                    HtmlText text = CreateText();
                    text.StreamOrder = htmlReader._offset;
                    text.IsCData = (htmlReader.State.FragmentType == HtmlFragmentType.CDataText);
                    text.Value = htmlReader.State.Value;
                    if (current != null)
                    {
                        current.ChildNodes.Add(text);
                    }
                    break;

                case HtmlFragmentType.TagOpen:
                    string elementName;
                    bool processingInstruction;
                    if (htmlReader.State.Value.StartsWith("?", StringComparison.Ordinal))
                    {
                        elementName = htmlReader.State.Value.Substring(1);
                        processingInstruction = true;
                    }
                    else
                    {
                        elementName = htmlReader.State.Value;
                        processingInstruction = false;
                    }

                    element = CreateElement(elementName);
                    element.StreamOrder = htmlReader._offset;

                    if (DocumentType == null && element.IsDocumentType)
                    {
                        DocumentType = element;
                    }
                    else if (elementName.EqualsIgnoreCase("html"))
                    {
                        HtmlElement = element;
                    }
                    else if (elementName.EqualsIgnoreCase("body"))
                    {
                        BodyElement = element;
                    }
                    else if (elementName.EqualsIgnoreCase("head"))
                    {
                        HeadElement = element;
                    }
                    else
                    {
                        element.IsProcessingInstruction = processingInstruction;
                    }

                    if (current != null)
                    {
                        current.ChildNodes.Add(element);
                    }

                    current = element;
                    break;

                case HtmlFragmentType.TagEnd:
                    element = current as HtmlElement;
                    if (!DetectEncoding(htmlReader, element, firstPass))
                        return false;

                    if (element != null && (element.Name.StartsWith("!", StringComparison.Ordinal) || element.IsProcessingInstruction))
                    {
                        element.IsEmpty = true;
                        if (current != null && current.ParentNode != null)
                        {
                            current = current.ParentNode;
                        }
                    }
                    else
                    {
                        if (element != null)
                        {
                            var canHaveChild = (htmlReader.Options.GetElementReadOptions(element.Name) & HtmlElementReadOptions.NoChild) != HtmlElementReadOptions.NoChild;
                            if (canHaveChild)
                            {
                                current = element;
                            }
                            else if (current.ParentNode != null)
                            {
                                current = current.ParentNode;
                            }
                        }
                    }
                    break;

                case HtmlFragmentType.TagEndClose:
                case HtmlFragmentType.TagClose:
                    element = current as HtmlElement;
                    if (!DetectEncoding(htmlReader, element, firstPass))
                        return false;

                    if (element != null)
                    {
                        if (htmlReader.State.FragmentType == HtmlFragmentType.TagClose)
                        {
                            element.IsEmpty = false;
                        }

                        var parent = element.GetParentToClose(0, htmlReader.State.Value);
                        if (parent != null)
                        {
                            parent.IsClosed = true;
                            if (parent.ParentNode != null)
                            {
                                current = parent.ParentNode;
                            }

                            // check children closure
                            foreach (var child in parent.ChildNodes)
                            {
                                if (!(child is HtmlElement childElement))
                                    continue;

                                if (!childElement.IsClosed)
                                {
                                    if ((htmlReader.Options.GetElementReadOptions(childElement.Name) & HtmlElementReadOptions.AutoClosed) != HtmlElementReadOptions.AutoClosed)
                                    {
                                        error = new HtmlError(htmlReader.State, HtmlErrorType.TagNotClosed);
                                        AddError(error);
                                        error.Node = childElement;
                                        childElement.AddError(error);
                                    }
                                    else
                                    {
                                        childElement.IsClosed = true;
                                    }
                                }
                            }
                            break;
                        }
                    }

                    error = new HtmlError(htmlReader.State, HtmlErrorType.TagNotOpened);
                    AddError(error);

                    // add a text node to keep the maximum compatibility
                    text = CreateText();
                    text.StreamOrder = htmlReader._offset;
                    error.Node = text;
                    text.Value = "</" + htmlReader.State.Value + ">";
                    text.AddError(error);
                    if (current != null)
                    {
                        current.ChildNodes.Add(text);
                    }
                    break;

                case HtmlFragmentType.AttName:
                    if (string.Equals(htmlReader.State.Value, "?", StringComparison.Ordinal))
                        break;

                    var att = CreateAttribute(htmlReader.State.Value);
                    att.StreamOrder = htmlReader._offset;
                    att.NameQuoteChar = htmlReader.State.QuoteChar;

                    var existingAtt = current?.Attributes[att.Name];
                    if (existingAtt != null)
                    {
                        error = new HtmlError(htmlReader.State, HtmlErrorType.DuplicateAttribute);
                        AddError(error);
                    }

                    if (current != null)
                    {
                        current.Attributes.AddNoCheck(att);
                    }
                    currentAtt = att;
                    break;

                case HtmlFragmentType.AttValue:
                    if (currentAtt == null)
                        break;

                    currentAtt.Value = HtmlAttribute.UnescapeText(htmlReader.State.Value, htmlReader.State.QuoteChar);
                    currentAtt.QuoteChar = htmlReader.State.QuoteChar;

                    if (currentAtt.Name.EqualsIgnoreCase(XmlnsPrefix))
                    {
                        element = current as HtmlElement;
                        if (element != null && !Options.EmptyNamespaces.Contains(currentAtt.Value))
                        {
                            element.NamespaceURI = currentAtt.Value;
                        }
                    }
                    break;

                case HtmlFragmentType.Comment:
                    var comment = CreateComment();
                    comment.StreamOrder = htmlReader._offset;
                    comment.Value = htmlReader.State.Value;
                    if (current != null)
                    {
                        current.ChildNodes.Add(comment);
                    }
                    break;
            }

            if (!OnParsed(htmlReader, ref current, ref currentAtt))
                break;
        }

        if (htmlReader.FirstEncodingErrorOffset >= 0)
        {
            AddError(new HtmlError(htmlReader.State.Line, htmlReader.State.Column, htmlReader.State.Offset, HtmlErrorType.EncodingError));
            if (DetectedEncoding == null)
            {
                if (htmlReader.Options.ReaderThrowsOnEncodingMismatch)
                    throw new HtmlException(string.Format(CultureInfo.CurrentCulture, "HTML0003: Html text encoding error. There seems to be a mismatch between the encoding '{0}', used to read the input Html text, or to open the input Html file, and the real detected text encoding, which cannot be determined at that time. If you do not want to see this exception thrown, please configure the ThrowOnEncodingError HtmlReader option. Offset of the first detected text encoding mismatch is {1}.", StreamEncoding.EncodingName, htmlReader.FirstEncodingErrorOffset));
            }
        }
        return true;
    }

    public override string InnerHtml
    {
        get => base.InnerHtml;
        set
        {
            if (!string.Equals(value, base.InnerHtml, StringComparison.Ordinal))
            {
                RemoveAll();
                if (value != null)
                {
                    var doc = CreateDocument();
                    if (value != null)
                    {
                        doc.LoadHtml(value);
                    }

                    foreach (var node in doc.ChildNodes)
                    {
                        ChildNodes.AddNoCheck(node);
                    }
                }
                ClearCaches();
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(InnerHtml)));
            }
        }
    }

    public override HtmlNodeType NodeType => HtmlNodeType.Document;

    public override string Name
    {
        get => base.Name;
        set
        {
            // do nothing
        }
    }

    public virtual void Save(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        //#if DEBUG
        //            CheckParenting();
        //#endif

        WriteTo(writer);
    }

    public virtual void Save(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        //#if DEBUG
        //            CheckParenting();
        //#endif
        WriteTo(writer);
    }

    public static TextWriter CreateStringWriter(HtmlDocument document, IFormatProvider provider = null)
    {
        if (document != null)
        {
            var writer = document.CreateStringWriter(provider);
            if (writer != null)
                return writer;
        }

        if (provider == null)
            return new StringWriter();

        return new StringWriter(provider);
    }

    protected virtual TextWriter CreateStringWriter(IFormatProvider provider = null) => provider != null ? new StringWriter(provider) : new StringWriter();
    protected virtual StreamWriter CreateStreamWriter(string filePath, bool append = false, Encoding encoding = null)
    {
        var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        return CreateStreamWriter(stream, encoding);
    }

    protected virtual StreamWriter CreateStreamWriter(Stream stream, Encoding encoding = null)
    {
        encoding = encoding ?? UTF8NoBOMEncoding;
        return new StreamWriter(stream, encoding);
    }

    public virtual void Save(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (Path.GetExtension(filePath).EqualsIgnoreCase(".xml"))
        {
            using var writer = new XmlTextWriter(filePath, Encoding.UTF8);
            Save(writer);
            return;
        }

        if (StreamEncoding != null)
        {
            using var writer = CreateStreamWriter(filePath, false, StreamEncoding);
            Save(writer);
        }
        else
        {
            using var writer = CreateStreamWriter(filePath);
            Save(writer);
        }
    }

    public virtual void Save(string filePath, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (Path.GetExtension(filePath).EqualsIgnoreCase(".xml"))
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            using var writer = new XmlTextWriter(filePath, encoding);
            Save(writer);
            return;
        }

        using (var writer = CreateStreamWriter(filePath, false, encoding))
        {
            Save(writer);
        }
    }

    public virtual void Save(Stream outStream)
    {
        ArgumentNullException.ThrowIfNull(outStream);

        if (StreamEncoding != null)
        {
            using var writer = CreateStreamWriter(outStream, StreamEncoding);
            Save(writer);
        }
        else
        {
            using var writer = CreateStreamWriter(outStream);
            Save(writer);
        }
    }

    public virtual void Save(Stream outStream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(outStream);

        using var writer = CreateStreamWriter(outStream, encoding);
        Save(writer);
    }

    public override void WriteTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        WriteContentTo(writer);
    }

    public override void WriteContentTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var node in ChildNodes)
        {
            node.WriteTo(writer);
        }
    }

    public override void WriteTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        WriteContentTo(writer);
    }

    public virtual void WriteDocType(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (DocumentType == null)
            return;

        var name = DocumentType.Attributes.Count > 0 ? DocumentType.Attributes[0].Name : "html";
        var att = DocumentType.Attributes["public"];
        var pubid = att?.NextSibling?.Name;
        var sysid = att?.NextSibling?.NextSibling?.Name;
        writer.WriteDocType(name, pubid, sysid, null);
    }

    public virtual bool IsXhtml
    {
        get
        {
            if (_xhtml.HasValue)
                return _xhtml.Value;

            return HtmlElement != null && HtmlElement.Attributes.GetNamespacePrefixIfDefined(XhtmlNamespaceURI) != null;
        }
        set => _xhtml = value;
    }

    public bool IsValidXmlDocument
    {
        get
        {
            foreach (var node in ChildNodes)
            {
                switch (node.NodeType)
                {
                    case HtmlNodeType.Comment:
                    case HtmlNodeType.ProcessingInstruction:
                        break;

                    case HtmlNodeType.Text:
                        if (!((HtmlText)node).IsWhitespace)
                            return false;
                        break;

                    case HtmlNodeType.DocumentType:
                        if (node != DocumentType)
                            return false;
                        break;

                    case HtmlNodeType.Element:
                        if (!node.Name.EqualsIgnoreCase("html") || node != HtmlElement)
                            return false;

                        break;

                    default:
                        return false;
                }
            }
            return true;
        }
    }

    public override void WriteContentTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!IsValidXmlDocument)
        {
            var oneElementWritten = false;
            foreach (var node in ChildNodes)
            {
                if (node == HtmlElement)
                {
                    oneElementWritten = true;
                }

                if (node == HtmlElement || node == DocumentType ||
                    node.NodeType == HtmlNodeType.Comment ||
                    node.NodeType == HtmlNodeType.ProcessingInstruction ||
                    (node.NodeType == HtmlNodeType.Text && ((HtmlText)node).IsWhitespace))
                {
                    node.WriteTo(writer);
                }
            }

            if (!oneElementWritten)
            {
                foreach (var node in ChildNodes)
                {
                    if (node.NodeType == HtmlNodeType.Element)
                    {
                        node.WriteTo(writer);
                        break;
                    }
                }
            }
            return;
        }

        foreach (var node in ChildNodes)
        {
            node.WriteTo(writer);
        }
    }

    internal HtmlAttribute NamespaceXml
    {
        get
        {
            if (_namespaceXml == null)
            {
                _namespaceXml = CreateAttribute(XmlnsPrefix, XmlPrefix, XmlnsNamespaceURI);
                _namespaceXml.Value = XmlNamespaceURI;
            }
            return _namespaceXml;
        }
    }

    public HtmlNode ImportNode(HtmlNode node) => ImportNode(node, HtmlCloneOptions.All);

    public virtual HtmlNode ImportNode(HtmlNode node, HtmlCloneOptions cloneOptions)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.Clone(cloneOptions);
    }

    protected override void AddNamespacesInScope(XmlNamespaceScope scope, IDictionary<string, string> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (scope != XmlNamespaceScope.Local)
        {
            if (_declaredNamespaces != null)
            {
                foreach (var kv in _declaredNamespaces)
                {
                    dictionary[kv.Value] = kv.Key;
                }
            }

            if (_declaredPrefixes != null)
            {
                foreach (var kv in _declaredPrefixes)
                {
                    dictionary[kv.Key] = kv.Value;
                }
            }
        }
        base.AddNamespacesInScope(scope, dictionary);
    }
}
