using HtmlNado.Utilities;

namespace HtmlNado;

public class HtmlXPathElement : HtmlElement
{
    protected internal HtmlXPathElement(string prefix, string localName, string? namespaceURI, HtmlXPathDocument? ownerDocument)
        : base(prefix, localName, namespaceURI, ownerDocument)
    {
    }

    public new HtmlXPathDocument? OwnerDocument => (HtmlXPathDocument?)base.OwnerDocument;

    private static string GetAttEscapedValue(string? value)
    {
        if (value?.Contains('\'') == true)
            return "=\"" + value.Replace("\"", "&quot;") + "\"";

        return "='" + value + "'";
    }

    private string? GetAttributesXPath()
    {
        if (Attributes.Count == 0)
            return null;

        var sb = new StringBuilder();
        foreach (var att in Attributes)
        {
            if (OwnerDocument == null || OwnerDocument.IsFiltered(att))
                continue;

            if (sb.Length > 0)
            {
                sb.Append(" and ");
            }
            sb.Append('@');
            sb.Append(att.Name);
            sb.Append(GetAttEscapedValue(att.Value));

            GetPrefix(att.NamespaceURI);
        }

        if (sb.ToString().Trim().Length == 0)
            return null;

        return "[" + sb + "]";
    }

    private string? GetDiscriminantAttributeXPath()
    {
        if (OwnerDocument?._discriminantAttributes != null)
        {
            foreach (var att in OwnerDocument._discriminantAttributes)
            {
                var disc = string.IsNullOrEmpty(att.Item2) ? Attributes[att.Item1] : Attributes[att.Item1, att.Item2];
                if (disc != null)
                {
                    disc = Attributes[disc.Value];
                    if (disc != null)
                    {
                        var newPrefix = GetPrefix(NamespaceURI);
                        var name = Name + "[@" + disc.Name + GetAttEscapedValue(disc.Value) + "]";
                        if (newPrefix != null)
                        {
                            name = newPrefix + ":" + name;
                        }
                        return name;
                    }
                }
            }
        }
        return null;
    }

    private string? GetPrefix(string? namespaceURI)
    {
        if (string.IsNullOrEmpty(namespaceURI) || OwnerDocument == null)
            return null;

        var prefix = OwnerDocument.XPathNamespaceManager.LookupPrefix(namespaceURI);
        if (!string.IsNullOrEmpty(prefix))
        {
            OwnerDocument.XPathNamespaceManager.AddNamespace(prefix, namespaceURI);
            return prefix;
        }

        string newPrefix;
        var index = 0;
        do
        {
            newPrefix = "ns" + index;
            if (OwnerDocument.XPathNamespaceManager.LookupNamespace(newPrefix) == null)
                break;

            index++;
        }
        while (true);
        OwnerDocument.XPathNamespaceManager.AddNamespace(newPrefix, namespaceURI);
        return newPrefix;
    }

    private string? GetXPath(HtmlNodeList parentNodes)
    {
        var disc = GetDiscriminantAttributeXPath();
        if (disc != null)
            return disc;

        var name = Name;
        var newPrefix = GetPrefix(NamespaceURI);
        if (newPrefix != null)
        {
            name = newPrefix + ":" + LocalName;
        }

        if (parentNodes.Count == 1)
            return name;

        var sameName = new List<HtmlElement>();
        foreach (var node in parentNodes)
        {
            if (node.NodeType != HtmlNodeType.Element)
                continue;

            if (string.Equals(node.Name, Name, StringComparison.Ordinal))
            {
                sameName.Add((HtmlElement)node);
            }
        }

        if (sameName.Count == 1) // this
            return name;

        string? byIndex = null;
        var sameAtts = new List<HtmlElement>();
        for (var i = 0; i < sameName.Count; i++)
        {
            if (sameName[i] == this)
            {
                byIndex = name + "[" + (i + 1) + "]";
                continue;
            }

            var same = true;
            foreach (var att in Attributes)
            {
                var eatt = sameName[i].Attributes[att.LocalName, att.NamespaceURI];
                if (eatt == null || !eatt.Value.EqualsOrdinalIgnoreCase(att.Value))
                {
                    same = false;
                    break;
                }
            }
            if (same)
            {
                sameAtts.Add(sameName[i]);
            }
        }

        if (sameAtts.Count == 0)
            return name + GetAttributesXPath();

        return byIndex;
    }

    public override string? XPathExpression
    {
        get
        {
            if (field == null)
            {
                if (ParentNode == null)
                {
                    var name = Name;
                    var newPrefix = GetPrefix(NamespaceURI);
                    if (newPrefix != null)
                    {
                        name = newPrefix + ":" + name;
                    }
                    return name;
                }

                field = GetXPath(ParentNode.ChildNodes);
                if (ParentNode is HtmlXPathElement parent)
                {
                    field = parent.XPathExpression + "/" + field;
                }

                if (ParentNode.NodeType == HtmlNodeType.Document)
                {
                    field = "/" + field;
                }
            }
            return field;
        }
    }
}
