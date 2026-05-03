namespace HtmlNado;

public class HtmlXPathDocument : HtmlDocument
{
    public const string HtmlTransformNamespaceUri = "http://www.simonmourier.com/htf/2026/1";
    public const string HtmlTransformNamespacePrefix = "htf";

    private List<Tuple<string, string>> _filteredAttributes;
    internal List<Tuple<string, string>> _discriminantAttributes;

    static HtmlXPathDocument()
    {
        HtmlTransformNamespaceManager = new XmlNamespaceManager(new NameTable());
        HtmlTransformNamespaceManager.AddNamespace(HtmlTransformNamespacePrefix, HtmlTransformNamespaceUri);
    }

    public static XmlNamespaceManager HtmlTransformNamespaceManager { get; private set; }

    private static bool IsNamespaceAttribute(HtmlAttribute attribute) => attribute != null && string.Equals(attribute.NamespaceURI, XmlnsNamespaceURI, StringComparison.Ordinal) && string.Equals(attribute.Prefix, XmlnsPrefix, StringComparison.Ordinal);

    private void RemoveHtmlTransformNamespace(HtmlElement element)
    {
        var remove = new List<HtmlAttribute>();
        foreach (var att in element.Attributes)
        {
            if (IsDiscriminant(att) || IsNamespaceAttribute(att) || string.Equals(att.NamespaceURI, HtmlTransformNamespaceUri, StringComparison.Ordinal))
            {
                remove.Add(att);
            }
        }

        foreach (var att in remove)
        {
            element.Attributes.Remove(att);
        }

        foreach (var node in element.ChildNodes)
        {
            if (node is HtmlElement child)
            {
                RemoveHtmlTransformNamespace(child);
            }
        }
    }

    public virtual bool InjectXml(HtmlXPathDocument target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var changed = false;
        foreach (var node in SelectNodes("//*"))
        {
            if (node is not HtmlXPathElement element)
                continue;

            var other = target.SelectSingleNode(element.XPathExpression, XPathNamespaceManager);
            if (other != null)
            {
                if (element.GetAttributeValue("replace", HtmlTransformNamespaceUri, false))
                {
                    var parentNode = other.ParentNode;
                    if (parentNode != null)
                    {
                        parentNode.RemoveChild(other);
                        var importNode = parentNode.OwnerDocument.ImportNode(element);
                        RemoveHtmlTransformNamespace((HtmlElement)importNode);
                        parentNode.AppendChild(importNode);
                    }
                    continue;
                }

                var newTextAtt = element.Attributes["replaceText", HtmlTransformNamespaceUri];
                if (newTextAtt != null)
                {
                    other.InnerText = newTextAtt.Value;
                }

                if (other is HtmlElement otherElement)
                {
                    foreach (var att in element.Attributes)
                    {
                        if (string.Equals(att.NamespaceURI, HtmlTransformNamespaceUri, StringComparison.Ordinal))
                        {
                            const string replaceToken = "replace-";
                            const string replaceTokenNs = "replaceNamespace-";
                            const string removeToken = "remove-";
                            const string removeTokenNs = "removeNamespace-";

                            if (att.LocalName.StartsWith(replaceToken, StringComparison.OrdinalIgnoreCase))
                            {
                                var replaceName = att.LocalName[replaceToken.Length..];
                                if (!string.IsNullOrEmpty(replaceName))
                                {
                                    string ns = null;
                                    var nsAtt = element.Attributes[replaceTokenNs + replaceName, HtmlTransformNamespaceUri];
                                    if (nsAtt != null)
                                    {
                                        ns = nsAtt.Value.Nullify();
                                    }

                                    HtmlAttribute replaceAtt;
                                    if (ns != null)
                                    {
                                        replaceAtt = element.Attributes[replaceName, ns];
                                        if (replaceAtt != null)
                                        {
                                            otherElement.SetAttribute(replaceName, ns, replaceAtt.Value);
                                        }
                                    }
                                    else
                                    {
                                        replaceAtt = element.Attributes[replaceName];
                                        if (replaceAtt != null)
                                        {
                                            otherElement.SetAttribute(replaceName, replaceAtt.Value);
                                        }
                                    }
                                }
                            }
                            else if (att.LocalName.StartsWith(removeToken, StringComparison.OrdinalIgnoreCase))
                            {
                                var removeName = att.LocalName[removeToken.Length..];
                                if (!string.IsNullOrEmpty(removeName))
                                {
                                    string ns = null;
                                    var nsAtt = element.Attributes[removeTokenNs + removeName, HtmlTransformNamespaceUri];
                                    if (nsAtt != null)
                                    {
                                        ns = nsAtt.Value.Nullify();
                                    }

                                    var removeAtt = ns != null ? other.Attributes[removeName, ns] : other.Attributes[removeName];
                                    if (removeAtt != null)
                                    {
                                        other.Attributes.Remove(removeAtt);
                                    }
                                }
                            }

                        }
                    }
                    continue;
                }
            }

            var parent = EnsureTargetParent(element, target, out changed);
            var targetElement = target.CreateElement(element.Prefix, element.LocalName, element.NamespaceURI);
            changed = true;
            var prepend = element.GetAttributeValue("prepend", HtmlTransformNamespaceUri, false);
            if (parent == null)
            {
                if (prepend)
                {
                    target.PrependChild(targetElement);
                }
                else
                {
                    target.AppendChild(targetElement);
                }
            }
            else
            {
                if (prepend)
                {
                    parent.PrependChild(targetElement);
                }
                else
                {
                    parent.AppendChild(targetElement);
                }
            }

            foreach (var att in element.Attributes)
            {
                if (IsDiscriminant(att) || IsNamespaceAttribute(att) | string.Equals(att.NamespaceURI, HtmlTransformNamespaceUri, StringComparison.Ordinal))
                    continue;

                targetElement.SetAttribute(att.LocalName, att.NamespaceURI, att.Value);
            }
        }
        return changed;
    }

    private static HtmlNode EnsureTargetParent(HtmlXPathElement element, HtmlXPathDocument target, out bool changed)
    {
        changed = false;
        if (element.ParentNode is HtmlXPathElement parent)
        {
            if (target.SelectSingleNode(parent.XPathExpression, element.OwnerDocument.XPathNamespaceManager) is HtmlElement targetElement)
                return targetElement;

            var parentElement = EnsureTargetParent(parent, target, out _);
            targetElement = target.CreateElement(parent.Prefix, parent.LocalName, parent.NamespaceURI);

            bool prepend = parent.GetAttributeValue("prepend", HtmlTransformNamespaceUri, false);
            if (prepend)
            {
                parentElement.PrependChild(targetElement);
            }
            else
            {
                parentElement.AppendChild(targetElement);
            }
            changed = true;
            return targetElement;
        }
        return target;
    }

    protected internal HtmlXPathDocument()
    {
        AddDiscriminantAttribute("key", HtmlTransformNamespaceUri);
        XPathNamespaceManager = new XmlNamespaceManager(new NameTable());
    }

    public virtual XmlNamespaceManager XPathNamespaceManager { get; private set; }

    public override HtmlElement CreateElement(string prefix, string localName, string namespaceURI) => new HtmlXPathElement(prefix, localName, namespaceURI, this);

    public override HtmlDocument CreateDocument() => new HtmlXPathDocument();

    public virtual void AddDiscriminantAttribute(string name, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(name);

        _discriminantAttributes ??= [];
        _discriminantAttributes.Add(new Tuple<string, string>(name, namespaceURI));
    }

    public virtual void AddFilteredAttribute(string name, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(name);

        _filteredAttributes ??= [];
        _filteredAttributes.Add(new Tuple<string, string>(name, namespaceURI));
    }

    public virtual bool IsDiscriminant(HtmlAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (_discriminantAttributes != null)
        {
            foreach (var pair in _discriminantAttributes)
            {
                string ns = attribute.NamespaceURI.Nullify();
                string dns = pair.Item2.Nullify();
                if (string.Equals(ns, dns, StringComparison.Ordinal) && string.Equals(pair.Item1, attribute.LocalName, StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    public virtual bool IsFiltered(HtmlAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (IsNamespaceAttribute(attribute))
            return true;

        if (_filteredAttributes != null)
        {
            foreach (var pair in _filteredAttributes)
            {
                var ns = attribute.NamespaceURI.Nullify();
                var dns = pair.Item2.Nullify();
                if (string.Equals(ns, dns, StringComparison.Ordinal) && string.Equals(pair.Item1, attribute.LocalName, StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    public virtual void FindDifferentAttributes(HtmlNode node1, HtmlNode node2, IList<HtmlAttribute> onlyInNode1, IList<HtmlAttribute> onlyInNode2, IList<HtmlAttribute> different)
    {
        ArgumentNullException.ThrowIfNull(node1);

        ArgumentNullException.ThrowIfNull(node2);

        ArgumentNullException.ThrowIfNull(onlyInNode1);

        ArgumentNullException.ThrowIfNull(onlyInNode2);

        foreach (var att in node1.Attributes)
        {
            if (IsFiltered(att))
                continue;

            if (node2.Attributes[att.Name] == null)
            {
                onlyInNode1.Add(att);
            }
        }

        foreach (var att in node2.Attributes)
        {
            if (IsFiltered(att))
                continue;

            if (node1.Attributes[att.Name] == null)
            {
                onlyInNode2.Add(att);
            }
        }

        foreach (var att in node1.Attributes)
        {
            if (IsFiltered(att))
                continue;

            var a = node2.Attributes[att.Name];
            if (a == null)
                continue;

            if (!string.Equals(a.Value, att.Value, StringComparison.Ordinal))
            {
                different.Add(att);
            }
        }
    }
}
