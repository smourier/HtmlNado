namespace HtmlNado;

public class HtmlXmlWriter : XmlWriter
{
    private WriteState _writeState;

    public HtmlXmlWriter(HtmlNode parent)
    {
        parent ??= new HtmlDocument();
        Parent = parent;

        if (Parent.OwnerDocument == null)
            throw new ArgumentException(null, nameof(parent));

        Current = Parent;
        _writeState = WriteState.Start;
    }

    public HtmlDocument? OwnerDocument => Parent.OwnerDocument;
    public HtmlNode Parent { get; }
    public HtmlNode Current { get; private set; }

    public override WriteState WriteState => _writeState;

    public override void Flush()
    {
    }

    public override void WriteCData(string? text)
    {
        if (text == null)
            return;

        if (Current is HtmlAttribute att)
        {
            att.Value = text;
            return;
        }

        var node = Parent.OwnerDocument?.CreateText();
        if (node == null)
            return;

        node.Value = text;
        Current.AppendChild(node);
    }

    public override void WriteComment(string? text)
    {
        if (text == null)
            return;

        var node = Parent.OwnerDocument?.CreateComment();
        if (node == null)
            return;

        node.Value = text;
        Current.AppendChild(node);
    }

    public override void WriteDocType(string? name, string? pubid, string? sysid, string? subset)
    {
        var text = "<!DOCTYPE " + name;
        if (pubid != null)
        {
            text += " PUBLIC \"" + pubid + "\" \"" + sysid + "\"";
        }
        else if (sysid != null)
        {
            text += " SYSTEM \"" + sysid + "\"";
        }

        if (subset != null)
        {
            text += "[" + subset + "]";
        }
        text += ">";
        WriteCData(text);
    }

    private HtmlElement GetCurrentElement()
    {
        if (Current is not HtmlElement element)
            throw new InvalidOperationException($"Current node is not an element but is of '{Current.GetType().FullName}' type.");

        return element;
    }

    public override void WriteEndElement()
    {
        Current = GetCurrentElement().ParentNode ?? throw new InvalidOperationException($"Current node does not have a parent node.");
        _writeState = WriteState.Element;
    }

    public override void WriteStartAttribute(string? prefix, string? localName, string? ns)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(localName);
        var current = GetCurrentElement();
        var att = current.Attributes.Add(prefix, localName, ns);
        Current = att;
        _writeState = WriteState.Attribute;
    }

    public override void WriteEndAttribute()
    {
        if (Current is not HtmlAttribute att)
            throw new InvalidOperationException($"Current node is not an attribute but is of '{Current.GetType().FullName}' type.");

        Current = att.ParentNode ?? throw new InvalidOperationException($"Current node does not have a parent node.");
        _writeState = WriteState.Element;
    }

    public override void WriteStartElement(string? prefix, string? localName, string? ns)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(localName);
        var element = OwnerDocument?.CreateElement(prefix, localName, ns);
        if (element == null)
            return;

        Current.AppendChild(element);
        Current = element;
        _writeState = WriteState.Element;
    }

    public override void WriteSurrogateCharEntity(char lowChar, char highChar)
    {
        var c = CombineSurrogateChar(lowChar, highChar);
        WriteCData("&#x" + c.ToString("X", NumberFormatInfo.InvariantInfo) + ";");
    }

    public override void WriteEntityRef(string? name) => WriteCData("&" + name + ";");
    public override void WriteFullEndElement() => WriteEndElement();
    public override void WriteProcessingInstruction(string? name, string? text) => WriteCData("<?" + name + " " + text + "?>");
    public override void WriteRaw(string? data) => WriteCData(data);
    public override void WriteRaw(char[] buffer, int index, int count) => throw new NotImplementedException();
    public override void WriteWhitespace(string? ws) => WriteCData(ws);
    public override void WriteStartDocument() => WriteStartDocument(false);
    public override void WriteStartDocument(bool standalone) => throw new NotImplementedException();
    public override void WriteEndDocument() => throw new NotImplementedException();
    public override void WriteString(string? text) => WriteCData(text);
    public override void WriteCharEntity(char ch) => WriteCData("&#x" + ((int)ch).ToString("X", NumberFormatInfo.InvariantInfo) + ";");
    public override void WriteChars(char[] buffer, int index, int count) => WriteRaw(buffer, index, count);
    public override string? LookupPrefix(string? ns) => Parent.OwnerDocument?.GetPrefixOfNamespace(ns);
    public override void WriteBase64(byte[] buffer, int index, int count) => throw new NotImplementedException();

    private static int CombineSurrogateChar(int lowChar, int highChar) => (lowChar - 0xdc00) | (((highChar - 0xd800) << 10) + 0x10000);
}
