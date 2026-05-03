namespace HtmlNado;

public class HtmlDocumentWithCode : HtmlXPathDocument
{
    public HtmlDocumentWithCode()
    {
        CodeStartDelimiter = '<';
        CodeEndDelimiter = '>';
        CodeStartToken = '%';
        CodeEndToken = '%';
        DirectiveToken = '@';
    }

    public char CodeStartDelimiter { get; set; }
    public char CodeEndDelimiter { get; set; }
    public char CodeStartToken { get; set; }
    public char CodeEndToken { get; set; }
    public char DirectiveToken { get; set; }
    public bool HasPrematureEnd { get; protected internal set; }

    public override bool IsXhtml { get => false; set { } }

    public override HtmlDocument CreateDocument() => new HtmlDocumentWithCode();

    public override HtmlAttribute CreateAttribute(string prefix, string localName, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        if (prefix.IndexOf(':') >= 0)
            throw new ArgumentException(null, nameof(prefix));

        ArgumentNullException.ThrowIfNull(localName);

        return new HtmlAttributeWithCode(prefix, localName, namespaceURI, this);
    }

    protected override void OnParsed(object sender, HtmlDocumentParseEventArgs e)
    {
        if (e.Reader.State.FragmentType == HtmlFragmentType.TagEnd)
        {
            var startDirective = CodeStartToken + DirectiveToken.ToString(CultureInfo.InvariantCulture);
            if (string.Equals(e.Reader.State.Value, startDirective, StringComparison.OrdinalIgnoreCase))
            {
                if (e.CurrentNode is HtmlElement && e.CurrentNode.HasAttributes)
                {
                    var name = e.CurrentNode.Attributes[0].Name;
                    if (name.EndsWith(CodeEndToken.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                    {
                        name = name.Substring(0, name.Length - 1);
                    }

                    e.CurrentNode.Name += " " + name;
                    e.CurrentNode.Attributes.RemoveAt(0);
                    var element = (HtmlElement)e.CurrentNode;
                    element.CloseChar = CodeEndToken;
                    element.IsEmpty = true;
                    e.CurrentNode.RemoveAttribute(CodeStartToken.ToString(CultureInfo.InvariantCulture));
                    e.CurrentNode.RemoveAttribute(CodeEndToken.ToString(CultureInfo.InvariantCulture));
                    e.CurrentNode = e.CurrentNode.ParentNode;
                }
            }
            else if (e.Reader.State.Value.StartsWith(startDirective, StringComparison.OrdinalIgnoreCase) && e.CurrentNode is HtmlElement)
            {
                var name = e.CurrentNode.Name.Substring(2);
                if (name.EndsWith(CodeEndToken.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - 1);
                }

                e.CurrentNode.Name = startDirective + " " + name;
                var element = (HtmlElement)e.CurrentNode;
                element.CloseChar = CodeEndToken;
                element.IsEmpty = true;
                e.CurrentNode.RemoveAttribute(CodeStartToken.ToString(CultureInfo.InvariantCulture));
                e.CurrentNode.RemoveAttribute(CodeEndToken.ToString(CultureInfo.InvariantCulture));
                e.CurrentNode = e.CurrentNode.ParentNode;
            }
            else if (e.Reader.State.Value.StartsWith(CodeStartToken.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) && e.CurrentNode is HtmlElement)
            {
                var element = (HtmlElement)e.CurrentNode;
                element.DontCloseIfEmpty = true;
                element.IsEmpty = true;
                e.CurrentNode = e.CurrentNode.ParentNode;
            }
        }
        else if (e.Reader.State.FragmentType == HtmlFragmentType.Text && HasPrematureEnd)
        {
            var element = e.CurrentNode as HtmlElement;
            Unclose(element);
        }
        else
        {
            base.OnParsed(sender, e);
        }
    }

    protected override void ParseName(string name, out string prefix, out string localName)
    {
        if (name != null && name.Length > 2 &&
            name.StartsWith(CodeStartToken.ToString(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith(CodeEndToken.ToString(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase))
        {
            prefix = string.Empty;
            localName = name;
            return;
        }

        base.ParseName(name, out prefix, out localName);
    }

    private static void Unclose(HtmlElement element)
    {
        if (element == null)
            return;

        element.IsClosed = false;
        Unclose(element.ParentNode as HtmlElement);
    }

    public override HtmlReader CreateReader(TextReader reader) => new HtmlReaderWithCode(this, reader, Options);

    public virtual void RemoveServerCode()
    {
        foreach (var node in SelectNodes("//*[starts-with(name(),'" + CodeStartToken + DirectiveToken + "')]").ToArray())
        {
            node.Remove();
        }
    }
}
