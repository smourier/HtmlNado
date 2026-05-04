//NOSONAR
global using System;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Collections.Specialized;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.IO.Compression;
global using System.Linq;
global using System.Net;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Xml;
global using System.Xml.XPath;
using HtmlNado.Utilities;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace HtmlNado
{
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
	
	public sealed class HtmlAttributeList : IList<HtmlAttribute>, INotifyCollectionChanged
	{
	    private readonly List<HtmlAttribute> _list = [];
	
	    public event NotifyCollectionChangedEventHandler? CollectionChanged;
	
	    internal HtmlAttributeList(HtmlNode parent)
	    {
	        Parent = parent;
	    }
	
	    public HtmlNode Parent { get; }
	
	    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	    {
	        Parent.ClearCaches();
	        CollectionChanged?.Invoke(this, e);
	    }
	
	    public HtmlAttribute Add(string prefix, string localName, string? namespaceURI, string? value = null)
	    {
	        ArgumentNullException.ThrowIfNull(prefix);
	        ArgumentNullException.ThrowIfNull(localName);
	        var owner = (Parent?.OwnerDocument) ?? throw new InvalidOperationException();
	
	        if (string.IsNullOrWhiteSpace(prefix) && !string.IsNullOrWhiteSpace(namespaceURI))
	        {
	            prefix = owner.GetPrefixOfNamespace(namespaceURI) ?? string.Empty;
	        }
	
	        var att = owner.CreateAttribute(prefix, localName, namespaceURI);
	        att.Value = value;
	        Add(att);
	        return att;
	    }
	
	    public HtmlAttribute Add(string name, string? value)
	    {
	        ArgumentNullException.ThrowIfNull(name);
	
	        var owner = (Parent?.OwnerDocument) ?? throw new InvalidOperationException();
	        var att = owner.CreateAttribute(string.Empty, name, string.Empty);
	        att.Value = value;
	        Add(att);
	        return att;
	    }
	
	    public void Add(HtmlAttribute attribute, bool replace = true)
	    {
	        ArgumentNullException.ThrowIfNull(attribute);
	        if (attribute.ParentNode != null)
	            throw new ArgumentException(null, nameof(attribute));
	
	        var att = this[attribute.LocalName, attribute.NamespaceURI];
	        if (att != null)
	        {
	            if (!replace)
	                throw new ArgumentException("The same attribute (" + att.NamespaceURI + ":" + att.LocalName + ") has has already been added.", nameof(attribute));
	
	            Remove(att);
	        }
	
	        AddNoCheck(attribute);
	    }
	
	    internal void AddNoCheck(HtmlAttribute attribute)
	    {
	        _list.Add(attribute);
	        attribute.ParentNode = Parent;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attribute));
	    }
	
	    public string? GetNamespacePrefixIfDefined(string? namespaceURI)
	    {
	        foreach (var att in _list)
	        {
	            if ((string.Equals(att.Name, HtmlNode.XmlnsPrefix, StringComparison.Ordinal) ||
	                string.Equals(att.Prefix, HtmlNode.XmlnsPrefix, StringComparison.Ordinal)) && string.Equals(att.Value, namespaceURI, StringComparison.Ordinal))
	                return att.LocalName;
	        }
	        return null;
	    }
	
	    public void RemoveAll()
	    {
	        foreach (var att in _list)
	        {
	            if (att.ParentNode != Parent)
	                throw new InvalidOperationException();
	
	            att.ParentNode = null;
	        }
	
	        _list.Clear();
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	    }
	
	    public void Insert(int index, HtmlAttribute attribute)
	    {
	        ArgumentNullException.ThrowIfNull(attribute);
	        if (attribute.ParentNode != null)
	            throw new ArgumentException(null, nameof(attribute));
	
	        _list.Insert(index, attribute);
	        attribute.ParentNode = Parent;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attribute, index));
	    }
	
	    public bool Contains(HtmlAttribute? item) => IndexOf(item) >= 0;
	    public void CopyTo(HtmlAttribute[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
	    public int IndexOf(HtmlAttribute? attribute)
	    {
	        if (attribute == null)
	            return -1;
	
	        return _list.IndexOf(attribute);
	    }
	
	    public int IndexOf(string? name) => _list.FindIndex(a => name.EqualsOrdinalIgnoreCase(a.Name));
	    public int IndexOf(string? localName, string? namespaceURI)
	    {
	        if (localName == null || namespaceURI == null)
	            return -1;
	
	        return _list.FindIndex(a => localName.EqualsOrdinalIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
	    }
	
	    public bool RemoveAt(int index)
	    {
	        if (index < 0 || index >= _list.Count)
	            return false;
	
	        var att = _list[index];
	        if (att.ParentNode != Parent)
	            throw new ArgumentException(null, nameof(index));
	
	        _list.RemoveAt(index);
	        att.ParentNode = null;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, att, index));
	        return true;
	    }
	
	    public void RemoveRange(IEnumerable<HtmlAttribute> attributes)
	    {
	        if (attributes == null)
	            return;
	
	        foreach (var att in attributes)
	        {
	            Remove(att);
	        }
	    }
	
	    public bool RemoveByPrefix(string? prefix, string? localName)
	    {
	        var att = _list.Find(a => localName.EqualsOrdinalIgnoreCase(a.LocalName) && string.Equals(prefix, a.Prefix, StringComparison.Ordinal));
	        if (att == null)
	            return false;
	
	        return Remove(att);
	    }
	
	    public bool Remove(string? localName, string? namespaceURI)
	    {
	        var att = this[localName, namespaceURI];
	        if (att == null)
	            return false;
	
	        return Remove(att);
	    }
	
	    public bool Remove(string? name)
	    {
	        var att = this[name];
	        if (att == null)
	            return false;
	
	        return Remove(att);
	    }
	
	    public bool Remove(HtmlAttribute? attribute)
	    {
	        if (attribute == null)
	            return false;
	
	        if (attribute.ParentNode != Parent)
	            throw new ArgumentException(null, nameof(attribute));
	
	        if (!_list.Remove(attribute))
	            throw new ArgumentException(null, nameof(attribute));
	
	        attribute.ParentNode = null;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, attribute));
	        return true;
	    }
	
	    public HtmlAttribute? this[string? name]
	    {
	        get => _list.Find(a => name.EqualsOrdinalIgnoreCase(a.Name));
	        set
	        {
	            ArgumentNullException.ThrowIfNull(value);
	            if (value.ParentNode != null)
	                throw new ArgumentException(null, nameof(value));
	
	            var index = IndexOf(name);
	            if (index < 0)
	            {
	                AddNoCheck(value);
	            }
	            else
	            {
	                this[index] = value;
	            }
	        }
	    }
	
	    public HtmlAttribute? this[string? localName, string? namespaceURI]
	    {
	        get => _list.Find(a => localName.EqualsOrdinalIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
	        set
	        {
	            ArgumentNullException.ThrowIfNull(value);
	            if (value.ParentNode != null)
	                throw new ArgumentException(null, nameof(value));
	
	            var index = IndexOf(localName, namespaceURI);
	            if (index < 0)
	            {
	                AddNoCheck(value);
	            }
	            else
	            {
	                this[index] = value;
	            }
	        }
	    }
	
	    public HtmlAttribute this[int index]
	    {
	        get => _list[index];
	        set
	        {
	            if (value == _list[index])
	                return;
	
	            var oldItem = _list[index];
	            _list[index] = value;
	            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem));
	        }
	    }
	
	    public int Count => _list.Count;
	    bool ICollection<HtmlAttribute>.IsReadOnly => false;
	    public IEnumerator<HtmlAttribute> GetEnumerator() => _list.GetEnumerator();
	    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	    int IList<HtmlAttribute>.IndexOf(HtmlAttribute item) => IndexOf(item);
	    void IList<HtmlAttribute>.RemoveAt(int index) => RemoveAt(index);
	    void ICollection<HtmlAttribute>.Add(HtmlAttribute item) => Add(item);
	    void ICollection<HtmlAttribute>.Clear() => RemoveAll();
	}
	
	[Flags]
	public enum HtmlCloneOptions
	{
	    None = 0x0,
	    Deep = 0x1,
	    OverwriteAttributes = 0x2,
	    Tag = 0x4,
	    Attributes = 0x8,
	    StreamOrder = 0x10,
	
	    All = Deep | OverwriteAttributes | Tag | Attributes | StreamOrder,
	}
	
	[DebuggerDisplay("'{Value}'")]
	public class HtmlComment : HtmlNode
	{
	    protected internal HtmlComment(HtmlDocument ownerDocument)
	        : base(string.Empty, "#comment", string.Empty, ownerDocument)
	    {
	    }
	
	    public override HtmlNodeType NodeType => HtmlNodeType.Comment;
	
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
	
	        writer.Write("<!--");
	        writer.Write(Value);
	        writer.Write("-->");
	    }
	
	    public override void WriteContentTo(TextWriter writer)
	    {
	    }
	
	    public override void WriteTo(XmlWriter writer)
	    {
	        ArgumentNullException.ThrowIfNull(writer);
	        writer.WriteComment(Value);
	    }
	
	    public override void WriteContentTo(XmlWriter writer)
	    {
	    }
	
	    public override void CopyTo(HtmlNode target, HtmlCloneOptions copyOptions)
	    {
	        base.CopyTo(target, copyOptions);
	        var comment = (HtmlComment)target;
	        comment.Value = Value;
	    }
	}
	
	public partial class HtmlConverter
	{
	    private static readonly HashSet<string> _noTextTags = new(StringComparer.OrdinalIgnoreCase);
	    private static readonly HashSet<string> _paraTextTags = new(StringComparer.OrdinalIgnoreCase);
	    private int _newLineCount;
	    private HtmlNode? _lastNode;
	
	    static HtmlConverter()
	    {
	        _noTextTags.Add("script");
	        _noTextTags.Add("style");
	        _paraTextTags.Add("pre");
	    }
	
	    public virtual HtmlConvertFormat TargetFormat { get; set; }
	    public virtual char UnderlineHeadingsChar { get; set; }
	    public virtual string ImagePlaceHolder { get; set; }
	    public virtual string LinkPlaceHolder { get; set; }
	    public virtual HtmlConverterOptions Options { get; set; }
	    public virtual int MaxSuccessiveNewLinesCount { get; set; }
	    public virtual bool AddRtfHeader { get; set; }
	
	    public HtmlConverter()
	    {
	        UnderlineHeadingsChar = '-';
	        ImagePlaceHolder = "[Image {0}]";
	        LinkPlaceHolder = "{0} [Link {1}]";
	        MaxSuccessiveNewLinesCount = 0;
	        AddRtfHeader = true;
	    }
	
	    public virtual string Convert(HtmlNode node)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        using var writer = new StringWriter();
	        Convert(node, writer);
	        return writer.ToString();
	    }
	
	    public virtual void Convert(HtmlNode node, string filePath)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        ArgumentNullException.ThrowIfNull(filePath);
	
	        using var writer = new StreamWriter(filePath);
	        Convert(node, writer);
	    }
	
	    public virtual void Convert(HtmlNode node, string filePath, Encoding encoding)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        ArgumentNullException.ThrowIfNull(filePath);
	
	        using var writer = new StreamWriter(filePath, false, encoding);
	        Convert(node, writer);
	    }
	
	    public virtual void Convert(HtmlNode node, Stream stream)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        ArgumentNullException.ThrowIfNull(stream);
	
	        using var writer = new StreamWriter(stream);
	        Convert(node, writer);
	    }
	
	    public virtual void Convert(HtmlNode node, Stream stream, Encoding encoding)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        ArgumentNullException.ThrowIfNull(stream);
	
	        using var writer = new StreamWriter(stream, encoding);
	        Convert(node, writer);
	    }
	
	    public virtual void Convert(HtmlNode node, TextWriter writer)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        ArgumentNullException.ThrowIfNull(writer);
	
	        switch (TargetFormat)
	        {
	            case HtmlConvertFormat.RichText:
	                ConvertToRichText(node, writer);
	                break;
	
	            case HtmlConvertFormat.Markdown:
	                ConvertToMarkdown(node, writer);
	                break;
	
	            //case HtmlConvertFormat.PlainText:
	            default:
	                ConvertToPlainText(node, writer);
	                break;
	        }
	    }
	
	    protected virtual void ConvertToPlainText(HtmlNode node, TextWriter writer)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        ArgumentNullException.ThrowIfNull(writer);
	
	        AppendPlainText(node, writer);
	    }
	
	    private static bool IsHeadingTag(string? name)
	    {
	        if (name == null)
	            return false;
	
	        if (name.Length != 2)
	            return false;
	
	        if (name[0] != 'h' && name[0] != 'H')
	            return false;
	
	        return char.IsDigit(name[1]);
	    }
	
	    private static bool IsTitleTag(string? name) => name.EqualsOrdinalIgnoreCase("title");
	
	    private string AppendPlainText(HtmlNode node)
	    {
	        using var writer = new StringWriter();
	        AppendPlainText(node, writer);
	        return writer.ToString();
	    }
	
	    private void WriterWriteDecoded(TextWriter writer, string? s) => WriterWrite(writer, WebUtility.HtmlDecode(s));
	
	    private void WriterWriteLine(TextWriter writer)
	    {
	        if (_newLineCount > MaxSuccessiveNewLinesCount)
	            return;
	
	        writer.WriteLine();
	        _newLineCount++;
	    }
	
	    private void WriterWrite(TextWriter writer, string? s)
	    {
	        while (_newLineCount > MaxSuccessiveNewLinesCount && s != null && s.StartsWith(writer.NewLine, StringComparison.Ordinal))
	        {
	            s = s[writer.NewLine.Length..];
	        }
	
	        writer.Write(s);
	        if (s != null && s.EndsWith(writer.NewLine, StringComparison.Ordinal))
	        {
	            _newLineCount++;
	        }
	        else
	        {
	            _newLineCount = 0;
	        }
	    }
	
	    protected virtual bool WriteLink(string? href)
	    {
	        if (string.IsNullOrWhiteSpace(href))
	            return false;
	
	        return href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
	            href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
	            href.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
	            href.StartsWith("mailto://", StringComparison.OrdinalIgnoreCase);
	    }
	
	    protected virtual void AppendLinkPlainText(HtmlNode? node, TextWriter writer)
	    {
	        if (node == null || !node.Name.EqualsOrdinalIgnoreCase("a"))
	            return;
	
	        var href = node.GetNullifiedAttributeValue("href");
	        if (LinkPlaceHolder == null || !WriteLink(href))
	            return;
	
	        var skipSelfLinks = !Options.HasFlag(HtmlConverterOptions.DontSkipSelfLinks);
	
	        string? lph;
	        if (string.IsNullOrWhiteSpace(node.InnerText))
	        {
	            lph = href;
	        }
	        else if (skipSelfLinks && Uri.TryCreate(href, UriKind.Absolute, out var uri) && (IsSameUrl(uri.ToString(), node.InnerText)))
	        {
	            lph = node.InnerText;
	        }
	        else
	        {
	            lph = string.Format(LinkPlaceHolder, node.InnerText, href);
	        }
	
	        WriterWriteLine(writer);
	        WriterWriteDecoded(writer, lph);
	    }
	
	    protected virtual void AppendImagePlaintText(HtmlNode? node, TextWriter writer)
	    {
	        if (node == null || !node.Name.EqualsOrdinalIgnoreCase("img"))
	            return;
	
	        var src = node.GetNullifiedAttributeValue("src");
	        if (src != null && ImagePlaceHolder != null)
	        {
	            var iph = string.Format(ImagePlaceHolder, src);
	            WriterWriteLine(writer);
	            WriterWriteDecoded(writer, iph);
	        }
	    }
	
	    private void AppendPlainTextNode(HtmlNode node, TextWriter writer)
	    {
	        if (node.NodeType == HtmlNodeType.Text)
	        {
	            var trimmed = node.Value?.Trim();
	            if (trimmed != null && trimmed.Length > 0)
	            {
	                var textToWrite =
	                        (char.IsWhiteSpace(node.Value![0]) ? " " : "") +
	                        trimmed +
	                        (char.IsWhiteSpace(node.Value![^1]) ? " " : "");
	                WriterWriteDecoded(writer, textToWrite);
	            }
	            else if (node.Value?.Length > 0)
	            {
	                WriterWriteDecoded(writer, " ");
	            }
	            return;
	        }
	
	        if (node.NodeType == HtmlNodeType.Document || node.NodeType == HtmlNodeType.Element)
	        {
	            var isTitle = IsTitleTag(node.Name);
	            var isHeading = IsHeadingTag(node.Name);
	            if (isTitle || isHeading)
	            {
	                if (isTitle && !Options.HasFlag(HtmlConverterOptions.IncludeTitle))
	                    return;
	
	                if (isHeading && !Options.HasFlag(HtmlConverterOptions.IncludeHeadings))
	                    return;
	
	                var heading = AppendPlainText(node);
	                if (UnderlineHeadingsChar != '\0' && heading.IndexOfAny(['\r', '\n']) < 0)
	                {
	                    WriterWriteLine(writer);
	                    WriterWriteDecoded(writer, heading);
	                    WriterWrite(writer, new string(UnderlineHeadingsChar, heading.Length));
	                }
	                WriterWriteLine(writer);
	                return;
	            }
	
	            if (node.Name.EqualsOrdinalIgnoreCase("img"))
	            {
	                AppendImagePlaintText(node, writer);
	                return;
	            }
	
	            if (node.Name.EqualsOrdinalIgnoreCase("a"))
	            {
	                AppendLinkPlainText(node, writer);
	                return;
	            }
	
	            if (node.Name != null && _paraTextTags.Contains(node.Name))
	            {
	                WriterWriteLine(writer);
	                AppendPlainText(node, writer);
	                WriterWriteLine(writer);
	                WriterWriteLine(writer); //?
	                return;
	            }
	
	            if (node.Name.EqualsOrdinalIgnoreCase("p"))
	            {
	                WriterWriteLine(writer);
	            }
	
	            if (node.Name != null && !_noTextTags.Contains(node.Name))
	            {
	                AppendPlainText(node, writer);
	            }
	
	            if (node.Name.EqualsOrdinalIgnoreCase("br"))
	            {
	                WriterWriteLine(writer);
	            }
	        }
	    }
	
	    private void AppendPlainText(HtmlNode node, TextWriter writer)
	    {
	        if (!node.HasChildNodes)
	            return;
	
	        foreach (var child in node.ChildNodes)
	        {
	            if (_lastNode != null && _lastNode.Name.EqualsOrdinalIgnoreCase("p"))
	            {
	                WriterWriteLine(writer);
	            }
	            AppendPlainTextNode(child, writer);
	            _lastNode = child;
	        }
	    }
	
	    private void AppendRichText(HtmlNode node, TextWriter writer)
	    {
	        if (!node.HasChildNodes)
	            return;
	
	        foreach (var child in node.ChildNodes)
	        {
	            AppendRichTextNode(child, writer);
	        }
	    }
	
	    private void AppendRichTextNode(HtmlNode child, TextWriter writer)
	    {
	        if (child.NodeType == HtmlNodeType.Text)
	        {
	            if (!string.IsNullOrWhiteSpace(child.Value))
	            {
	                WriterWrite(writer, CleanRtfText(child.Value));
	            }
	            return;
	        }
	
	        if (child.NodeType != HtmlNodeType.Document && child.NodeType != HtmlNodeType.Element)
	            return;
	
	        var name = child.Name?.ToLowerInvariant();
	        switch (name)
	        {
	            case "img":
	                AppendImagePlaintText(child, writer);
	                break;
	
	            case "a":
	                var href = child.GetNullifiedAttributeValue("href");
	                if (href != null || WriteLink(href))
	                {
	                    var content = child.InnerText;
	                    if (string.IsNullOrWhiteSpace(content))
	                    {
	                        content = href;
	                    }
	                    WriterWrite(writer, " {\\field{\\*\\fldinst HYPERLINK \"" + CleanRtfText(href) + "\"}{\\fldrslt \\ul " + CleanRtfText(content) + "\\ul0}} ");
	                }
	                break;
	
	            case "b":
	            case "strong":
	                AppendRichTextTag(child, writer, "b");
	                break;
	
	            case "i":
	                AppendRichTextTag(child, writer, "i");
	                break;
	
	            case "u":
	                AppendRichTextTag(child, writer, "ul");
	                break;
	
	            case "strike":
	                AppendRichTextTag(child, writer, "strike");
	                break;
	
	            case "ul":
	                WriterWrite(writer, "{{\\*\\pn\\pnlvlblt\\pnindent0{\\pntxtb\\bullet}}\\fi-360\\li720 ");
	                foreach (var bullet in child.ChildNodes)
	                {
	                    AppendRichText(bullet, writer);
	                    WriterWrite(writer, "\\par ");
	                    WriterWriteLine(writer);
	                }
	                WriterWrite(writer, "}");
	                break;
	
	            case "ol":
	                var header = "\\*\\pn\\pnlvlbody\\pnindent0{\\pntxta.}";
	                var type = child.GetNullifiedAttributeValue("type");
	                type = type switch
	                {
	                    "a" => "\\pnlcltr",
	                    "A" => "\\pnucltr",
	                    "i" => "\\pnlcrm",
	                    "I" => "\\pnucrm",
	                    _ => "\\pndec",
	                };
	                header += type;
	                header += "\\pnstart" + child.GetAttributeValue("start", 1);
	
	                WriterWrite(writer, "{{" + header + "}\\fi-360\\li720 ");
	                foreach (var bullet in child.ChildNodes)
	                {
	                    AppendRichText(bullet, writer);
	                    WriterWrite(writer, "\\par ");
	                    WriterWriteLine(writer);
	                }
	                WriterWrite(writer, "}");
	                break;
	
	            case "p":
	                AppendRichText(child, writer);
	                WriterWrite(writer, "\\par ");
	                break;
	
	            case "br":
	                if (child.ParentNode != null && child.NextSibling != null)
	                {
	                    WriterWrite(writer, "\\line ");
	                }
	                break;
	
	            default:
	                if (child.Name != null && !_noTextTags.Contains(child.Name))
	                {
	                    AppendRichText(child, writer);
	                }
	                break;
	        }
	    }
	
	    private void AppendRichTextTag(HtmlNode node, TextWriter writer, string tag)
	    {
	        WriterWrite(writer, string.Format("\\{0} ", tag));
	        AppendRichText(node, writer);
	        WriterWrite(writer, string.Format("\\{0}0 ", tag));
	    }
	
	    private static string? CleanRtfText(string? value)
	    {
	        if (value == null)
	            return null;
	
	        value = WebUtility.HtmlDecode(value);
	        value = value.Replace((char)160, ' ');
	        value = CleanRtfRegex().Replace(value, " ");
	        return Utilities.Extensions.EscapeRtf(value);
	    }
	
	    private static string? NormalizeUrl(string? url)
	    {
	        url = url.ToNull();
	        if (url == null)
	            return null;
	
	        if (url.EndsWith('/'))
	        {
	            url = url[..^1];
	        }
	
	        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
	            return url[7..];
	
	        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
	            return url[8..];
	
	        return url;
	    }
	
	    private static bool IsSameUrl(string url1, string url2)
	    {
	        if (string.Equals(url1, url2, StringComparison.OrdinalIgnoreCase))
	            return true;
	
	        return string.Equals(NormalizeUrl(url1), NormalizeUrl(url2), StringComparison.OrdinalIgnoreCase);
	    }
	
	    protected virtual void ConvertToRichText(HtmlNode node, TextWriter writer)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	        ArgumentNullException.ThrowIfNull(writer);
	
	        writer.NewLine = "\n";
	        if (AddRtfHeader)
	        {
	            WriterWrite(writer, "{\\rtf1\\ansi\\deff0\n");
	        }
	        AppendRichText(node, writer);
	        if (AddRtfHeader)
	        {
	            WriterWrite(writer, "}");
	        }
	    }
	
	    protected virtual void ConvertToMarkdown(HtmlNode node, TextWriter writer)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	        ArgumentNullException.ThrowIfNull(writer);
	
	        // UNDONE
	        AppendPlainText(node, writer);
	    }
	
	    [GeneratedRegex("[ ]+", RegexOptions.Multiline)]
	    private static partial Regex CleanRtfRegex();
	}
	
	[Flags]
	public enum HtmlConverterOptions
	{
	    None = 0x0,
	    IncludeTitle = 0x1,
	    IncludeHeadings = 0x2,
	    DontSkipSelfLinks = 0x4,
	}
	
	public enum HtmlConvertFormat
	{
	    PlainText,
	    RichText,
	    Markdown,
	}
	
	[DebuggerDisplay("{Name}")]
	public class HtmlDocument : HtmlNode
	{
	    public static readonly Encoding UTF8NoBOMEncoding = new UTF8Encoding(false, true);
	
	    private readonly Dictionary<string, string> _declaredNamespaces = [];
	    private readonly Dictionary<string, string> _declaredPrefixes = [];
	    private readonly Dictionary<string, HtmlNode> _nodesById = [];
	    private bool? _xhtml;
	    private bool _baseElementSearched;
	
	    public event EventHandler<HtmlDocumentParseEventArgs>? Parsing;
	    public event EventHandler<HtmlDocumentParseEventArgs>? Parsed;
	
	    public HtmlDocument()
	        : base(string.Empty, "#document", string.Empty, null)
	    {
	    }
	
	    public virtual Encoding? StreamEncoding { get; protected set; }
	    public virtual Encoding? DetectedEncoding { get; protected set; }
	    public virtual Uri? BaseAddress { get; set; }
	    public virtual bool ReaderWasRestarted { get; protected set; }
	    public virtual HtmlElement? DocumentType { get; protected set; }
	    public virtual HtmlElement? HtmlElement { get; protected set; }
	    public virtual HtmlElement? BodyElement { get; protected set; }
	    public virtual HtmlElement? HeadElement { get; protected set; }
	
	    public virtual IDictionary<string, HtmlNode> Ids => _nodesById;
	    public IReadOnlyDictionary<string, string> DeclaredNamespaces => _declaredNamespaces;
	    public IReadOnlyDictionary<string, string> DeclaredPrefixes => _declaredPrefixes;
	
	    public virtual string? Title
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
	                        if (html != null)
	                        {
	                            PrependChild(html);
	                        }
	                    }
	
	                    if (html != null)
	                    {
	                        head = CreateElement("head");
	                        if (head != null)
	                        {
	                            html.PrependChild(head);
	                        }
	                    }
	                }
	
	                if (head != null)
	                {
	                    title = CreateElement("title");
	                    if (title != null)
	                    {
	                        head.PrependChild(title);
	                    }
	                }
	            }
	
	            title?.InnerText = value;
	        }
	    }
	
	    public virtual string? FilePath
	    {
	        get => field;
	        protected set
	        {
	            if (field == value)
	                return;
	
	            field = value;
	            if (BaseAddress == null && field != null)
	            {
	                BaseAddress = Path.IsPathRooted(field) ? new Uri(field) : new Uri(Path.GetFullPath(field));
	            }
	        }
	    }
	
	    public virtual HtmlElement? BaseElement
	    {
	        get
	        {
	            if (field == null && !_baseElementSearched)
	            {
	                field = SelectSingleNode("//base") as HtmlElement;
	                _baseElementSearched = true;
	            }
	            return field;
	        }
	        set
	        {
	            if (field == value)
	                return;
	
	            field = value;
	            _baseElementSearched = false;
	        }
	    }
	
	    public virtual HtmlOptions Options
	    {
	        get => field;
	        set
	        {
	            ArgumentNullException.ThrowIfNull(value);
	
	            field = value;
	            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Options)));
	        }
	    } = new();
	
	    internal HtmlAttribute NamespaceXml
	    {
	        get
	        {
	            if (field == null)
	            {
	                field = CreateAttribute(XmlnsPrefix, XmlPrefix, XmlnsNamespaceURI);
	                field.Value = XmlNamespaceURI;
	            }
	            return field;
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
	            throw new HtmlException("0002: Cannot determine document's base address.");
	
	        return new Uri(baseAddress, uri);
	    }
	
	    public void LoadHtml(string html)
	    {
	        ArgumentNullException.ThrowIfNull(html);
	
	        Clear();
	        using var reader = new StringReader(html);
	        StreamEncoding = Encoding.UTF8;
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
	            using var reader = new StreamReader(filePath, Encoding.UTF8, false);
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
	        using (var reader = new StreamReader(filePath, Encoding.UTF8, true))
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
	            using var reader = new StreamReader(stream, Encoding.UTF8, false);
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
	        using (var reader = new StreamReader(stream, Encoding.UTF8))
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
	    protected virtual internal bool ClearId(string? id)
	    {
	        if (id == null)
	            return false;
	
	        return Ids.Remove(id);
	    }
	
	    internal void SetNodeById(HtmlNode? node)
	    {
	        if (node == null)
	            return;
	
	        var id = node.Id;
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
	
	    public HtmlElement? GetElementById(string id) => GetNodeById(id) as HtmlElement;
	    public virtual HtmlNode? GetNodeById(string id)
	    {
	        ArgumentNullException.ThrowIfNull(id);
	
	        Ids.TryGetValue(id, out var node);
	        return node;
	    }
	
	    public virtual void AddNamespace(string? prefix, string? uri)
	    {
	        if (prefix != null)
	        {
	            ArgumentNullException.ThrowIfNull(uri);
	            _declaredPrefixes[prefix] = uri;
	        }
	
	        if (uri != null)
	        {
	            ArgumentNullException.ThrowIfNull(prefix);
	            _declaredNamespaces[uri] = prefix;
	        }
	    }
	
	    public override string? GetNamespaceOfPrefix(string? prefix)
	    {
	        if (_declaredPrefixes == null || prefix == null)
	            return null;
	
	        if (_declaredPrefixes.TryGetValue(prefix, out var namespaceURI))
	            return namespaceURI;
	
	        return null;
	    }
	
	    public override string? GetPrefixOfNamespace(string? namespaceURI)
	    {
	        if (namespaceURI == null)
	            return null;
	
	        if (_declaredNamespaces.TryGetValue(namespaceURI, out var prefix))
	            return prefix;
	
	        return null;
	    }
	
	    protected override void GetNamespaceAttributes(IDictionary<string, string?> namespaces)
	    {
	        base.GetNamespaceAttributes(namespaces);
	        if (_declaredPrefixes != null)
	        {
	            foreach (var kv in _declaredPrefixes)
	            {
	                namespaces[kv.Key] = kv.Value;
	            }
	        }
	
	        foreach (var kv in _declaredNamespaces)
	        {
	            namespaces[kv.Value] = kv.Key;
	        }
	    }
	
	    private HtmlAttribute? CreateAttribute(string name)
	    {
	        ParseName(name, out var prefix, out var localName);
	        if (localName == null)
	            return null;
	
	        return CreateAttribute(prefix ?? string.Empty, localName, null);
	    }
	
	    public virtual HtmlAttribute CreateAttribute(string prefix, string localName, string? namespaceURI)
	    {
	        ArgumentNullException.ThrowIfNull(prefix);
	        ArgumentNullException.ThrowIfNull(localName);
	        if (prefix.Contains(':'))
	            throw new ArgumentException(null, nameof(prefix));
	
	        return new HtmlAttribute(prefix, localName, namespaceURI, this);
	    }
	
	    public virtual HtmlText CreateText() => new(this);
	    public HtmlElement? CreateElement(string name)
	    {
	        ArgumentNullException.ThrowIfNull(name);
	        ParseName(name, out var prefix, out var localName);
	        if (localName == null)
	            return null;
	
	        return CreateElement(prefix ?? string.Empty, localName, null);
	    }
	
	    public virtual HtmlElement CreateElement(string prefix, string localName, string? namespaceURI)
	    {
	        ArgumentNullException.ThrowIfNull(prefix);
	        ArgumentNullException.ThrowIfNull(localName);
	
	        if (prefix.Contains(':'))
	            throw new ArgumentException(null, nameof(prefix));
	
	        return new HtmlElement(prefix, localName, namespaceURI, this);
	    }
	
	    public virtual HtmlDocument CreateDocument() => new();
	    public virtual HtmlComment CreateComment() => new(this);
	
	    private void Clear()
	    {
	        Attributes.RemoveAll();
	        ChildNodes.RemoveAll();
	        ClearCaches();
	        FilePath = null;
	        ClearErrors();
	        BaseElement = null;
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
	    private static string? GetEncodingName(HtmlElement meta)
	    {
	        var name = meta.GetNullifiedAttributeValue("charset");
	        if (name != null)
	            return name;
	
	        var ct = meta.GetNullifiedAttributeValue("http-equiv");
	        if (!ct.EqualsOrdinalIgnoreCase("content-type"))
	            return null;
	
	        return Utilities.Extensions.GetAttributeFromHeader(meta.GetNullifiedAttributeValue("content"), "charset");
	    }
	
	    private bool DetectEncoding(HtmlReader reader, HtmlElement? element, bool firstPass)
	    {
	        if (reader.Options.ReaderDontDetectEncoding)
	            return true;
	
	        if (DetectedEncoding != null)
	            return true;
	
	        if (element == null || !element.Name.EqualsOrdinalIgnoreCase("meta"))
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
	
	            AddError(new HtmlError(reader.State?.Line ?? 0, reader.State?.Column ?? 0, reader.State?.Offset ?? 0, HtmlErrorType.EncodingMismatch));
	            if (Options.ReaderThrowsOnEncodingMismatch)
	                throw new HtmlException(string.Format(CultureInfo.CurrentCulture, "L0004: Html encoding mismatch error. There seems to be mismatch between the stream (HTTP, File, etc.) encoding '{0}' and the declared (HTML META) encoding '{1}'.", StreamEncoding.EncodingName, DetectedEncoding.EncodingName));
	        }
	        return true;
	    }
	
	    protected virtual void OnParsing(object sender, HtmlDocumentParseEventArgs e) => Parsing?.Invoke(sender, e);
	    protected virtual void OnParsed(object sender, HtmlDocumentParseEventArgs e) => Parsed?.Invoke(sender, e);
	
	    private bool OnParsing(HtmlReader reader, ref HtmlNode? currentNode, ref HtmlAttribute? currentAttribute, out bool cont)
	    {
	        var e = new HtmlDocumentParseEventArgs(reader)
	        {
	            DetectedEncoding = DetectedEncoding,
	            CurrentNode = currentNode,
	            CurrentAttribute = currentAttribute
	        };
	
	        OnParsing(this, e);
	        DetectedEncoding = e.DetectedEncoding;
	        currentNode = e.CurrentNode;
	        currentAttribute = e.CurrentAttribute;
	        cont = e.Continue;
	        if (e.Cancel)
	            return false;
	
	        return true;
	    }
	
	    private bool OnParsed(HtmlReader reader, ref HtmlNode? currentNode, ref HtmlAttribute? currentAttribute)
	    {
	        var e = new HtmlDocumentParseEventArgs(reader)
	        {
	            DetectedEncoding = DetectedEncoding,
	            CurrentNode = currentNode,
	            CurrentAttribute = currentAttribute
	        };
	
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
	        HtmlNode? current = this;
	        HtmlAttribute? currentAtt = null;
	        var htmlReader = CreateReader(reader);
	        while (htmlReader.Read())
	        {
	            if (!OnParsing(htmlReader, ref current, ref currentAtt, out bool cont))
	                break;
	
	            if (cont)
	                continue;
	
	            HtmlElement? element = null;
	            HtmlError error;
	            if (htmlReader.State == null)
	                continue;
	
	            switch (htmlReader.State.FragmentType)
	            {
	                case HtmlFragmentType.CDataText:
	                case HtmlFragmentType.Text:
	                    HtmlText text = CreateText();
	                    text.StreamOrder = htmlReader._offset;
	                    text.IsCData = (htmlReader.State.FragmentType == HtmlFragmentType.CDataText);
	                    text.Value = htmlReader.State.Value;
	                    current?.ChildNodes.Add(text);
	                    break;
	
	                case HtmlFragmentType.TagOpen:
	                    string? elementName;
	                    bool processingInstruction;
	                    if (htmlReader.State.Value?.StartsWith('?') == true)
	                    {
	                        elementName = htmlReader.State.Value[1..];
	                        processingInstruction = true;
	                    }
	                    else
	                    {
	                        elementName = htmlReader.State.Value;
	                        processingInstruction = false;
	                    }
	
	                    if (elementName != null)
	                    {
	                        element = CreateElement(elementName);
	                    }
	
	                    element?.StreamOrder = htmlReader._offset;
	
	                    if (DocumentType == null && element?.IsDocumentType == true)
	                    {
	                        DocumentType = element;
	                    }
	                    else if (elementName.EqualsOrdinalIgnoreCase("html"))
	                    {
	                        HtmlElement = element;
	                    }
	                    else if (elementName.EqualsOrdinalIgnoreCase("body"))
	                    {
	                        BodyElement = element;
	                    }
	                    else if (elementName.EqualsOrdinalIgnoreCase("head"))
	                    {
	                        HeadElement = element;
	                    }
	                    else
	                    {
	                        element?.IsProcessingInstruction = processingInstruction;
	                    }
	
	                    if (element != null)
	                    {
	                        current?.ChildNodes.Add(element);
	
	                        current = element;
	                    }
	                    break;
	
	                case HtmlFragmentType.TagEnd:
	                    element = current as HtmlElement;
	                    if (!DetectEncoding(htmlReader, element, firstPass))
	                        return false;
	
	                    if (element != null && (element.Name?.StartsWith('!') == true || element.IsProcessingInstruction == true))
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
	                            var canHaveChild = !htmlReader.Options.GetElementReadOptions(element.Name).HasFlag(HtmlElementReadOptions.NoChild);
	                            if (canHaveChild)
	                            {
	                                current = element;
	                            }
	                            else if (current?.ParentNode != null)
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
	                                if (child is not HtmlElement childElement)
	                                    continue;
	
	                                if (!childElement.IsClosed)
	                                {
	                                    if (!htmlReader.Options.GetElementReadOptions(childElement.Name).HasFlag(HtmlElementReadOptions.AutoClosed))
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
	                    current?.ChildNodes.Add(text);
	                    break;
	
	                case HtmlFragmentType.AttName:
	                    if (string.Equals(htmlReader.State.Value, "?", StringComparison.Ordinal))
	                        break;
	
	                    if (htmlReader.State.Value != null)
	                    {
	                        var att = CreateAttribute(htmlReader.State.Value);
	                        if (att != null)
	                        {
	                            att.StreamOrder = htmlReader._offset;
	                            att.NameQuoteChar = htmlReader.State.QuoteChar;
	
	                            var existingAtt = current?.Attributes[att.Name];
	                            if (existingAtt != null)
	                            {
	                                error = new HtmlError(htmlReader.State, HtmlErrorType.DuplicateAttribute);
	                                AddError(error);
	                            }
	
	                            current?.Attributes.AddNoCheck(att);
	                        }
	                        currentAtt = att;
	                    }
	                    break;
	
	                case HtmlFragmentType.AttValue:
	                    if (currentAtt == null)
	                        break;
	
	                    currentAtt.Value = HtmlAttribute.UnescapeText(htmlReader.State.Value, htmlReader.State.QuoteChar);
	                    currentAtt.QuoteChar = htmlReader.State.QuoteChar;
	
	                    if (currentAtt.Name.EqualsOrdinalIgnoreCase(XmlnsPrefix))
	                    {
	                        element = current as HtmlElement;
	                        if (element != null && currentAtt.Value != null && !Options.EmptyNamespaces.Contains(currentAtt.Value))
	                        {
	                            element.NamespaceURI = currentAtt.Value;
	                        }
	                    }
	                    break;
	
	                case HtmlFragmentType.Comment:
	                    var comment = CreateComment();
	                    comment.StreamOrder = htmlReader._offset;
	                    comment.Value = htmlReader.State.Value;
	                    current?.ChildNodes.Add(comment);
	                    break;
	            }
	
	            if (!OnParsed(htmlReader, ref current, ref currentAtt))
	                break;
	        }
	
	        if (htmlReader.FirstEncodingErrorOffset >= 0)
	        {
	            AddError(new HtmlError(htmlReader.State?.Line ?? 0, htmlReader.State?.Column ?? 0, htmlReader.State?.Offset ?? 0, HtmlErrorType.EncodingError));
	            if (DetectedEncoding == null)
	            {
	                if (htmlReader.Options.ReaderThrowsOnEncodingMismatch)
	                    throw new HtmlException(string.Format(CultureInfo.CurrentCulture, "0003: Html text encoding error. There seems to be a mismatch between the encoding '{0}', used to read the input Html text, or to open the input Html file, and the real detected text encoding, which cannot be determined at that time. If you do not want to see this exception thrown, please configure the ThrowOnEncodingError HtmlReader option. Offset of the first detected text encoding mismatch is {1}.", StreamEncoding?.EncodingName, htmlReader.FirstEncodingErrorOffset));
	            }
	        }
	        return true;
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
	
	    public override string? Name
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
	
	    public static TextWriter CreateStringWriter(HtmlDocument? document, IFormatProvider? provider = null)
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
	
	    protected virtual TextWriter CreateStringWriter(IFormatProvider? provider = null) => provider != null ? new StringWriter(provider) : new StringWriter();
	    protected virtual StreamWriter CreateStreamWriter(string filePath, bool append = false, Encoding? encoding = null)
	    {
	        ArgumentNullException.ThrowIfNull(filePath);
	        var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
	        return CreateStreamWriter(stream, encoding);
	    }
	
	    protected virtual StreamWriter CreateStreamWriter(Stream stream, Encoding? encoding = null)
	    {
	        ArgumentNullException.ThrowIfNull(stream);
	        encoding ??= UTF8NoBOMEncoding;
	        return new StreamWriter(stream, encoding);
	    }
	
	    public virtual void Save(string filePath)
	    {
	        ArgumentNullException.ThrowIfNull(filePath);
	
	        if (Path.GetExtension(filePath).EqualsOrdinalIgnoreCase(".xml"))
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
	
	        if (Path.GetExtension(filePath).EqualsOrdinalIgnoreCase(".xml"))
	        {
	            encoding ??= Encoding.UTF8;
	
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
	
	        var name = DocumentType.Attributes.Count > 0 ? DocumentType.Attributes[0].Name : null;
	        name ??= "html";
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
	                        if (!node.Name.EqualsOrdinalIgnoreCase("html") || node != HtmlElement)
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
	            foreach (var kv in _declaredNamespaces)
	            {
	                dictionary[kv.Value] = kv.Key;
	            }
	
	            foreach (var kv in _declaredPrefixes)
	            {
	                dictionary[kv.Key] = kv.Value;
	            }
	        }
	        base.AddNamespacesInScope(scope, dictionary);
	    }
	
	    internal static void RemoveIntrinsicElement(HtmlDocument? doc, HtmlElement? element)
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
	}
	
	public class HtmlDocumentParseEventArgs : CancelEventArgs
	{
	    public HtmlDocumentParseEventArgs(HtmlReader reader)
	    {
	        ArgumentNullException.ThrowIfNull(reader);
	        Reader = reader;
	    }
	
	    public HtmlReader Reader { get; }
	    public virtual Encoding? DetectedEncoding { get; set; }
	    public virtual HtmlNode? CurrentNode { get; set; }
	    public virtual HtmlAttribute? CurrentAttribute { get; set; }
	    public virtual bool Continue { get; set; }
	}
	
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
	
	[Flags]
	public enum HtmlElementReadOptions
	{
	    None = 0x0,
	    InnerRaw = 0x1,
	    AutoClosed = 0x2,
	    NoChild = 0x4,
	}
	
	[Flags]
	public enum HtmlElementWriteOptions
	{
	    None = 0x0,
	    DontCloseIfEmpty = 0x1,
	    AlwaysClose = 0x2,
	    NoChild = 0x4,
	}
	
	[DebuggerDisplay("{Line}x{Column}x{Offset} {ErrorType}")]
	public class HtmlError
	{
	    public HtmlError(HtmlReaderState state, HtmlErrorType errorType)
	    {
	        Line = state.Line;
	        Column = state.Column;
	        Offset = state.Offset;
	        ErrorType = errorType;
	    }
	
	    public HtmlError(int line, int column, int offset, HtmlErrorType errorType)
	    {
	        Line = line;
	        Column = column;
	        Offset = offset;
	        ErrorType = errorType;
	    }
	
	    public HtmlNode? Node { get; internal set; }
	    public HtmlErrorType ErrorType { get; }
	    public int Offset { get; }
	    public int Line { get; }
	    public int Column { get; }
	}
	
	public enum HtmlErrorType
	{
	    TagNotClosed,
	    TagNotOpened,
	    EncodingError,
	    EncodingMismatch,
	    NamespaceNotDeclared,
	    DuplicateAttribute,
	}
	
	[Serializable]
	public class HtmlException : Exception
	{
	    public const string Prefix = "HTM";
	
	    public HtmlException()
	        : base(Prefix + "0001: HtmlNado exception.")
	    {
	    }
	
	    public HtmlException(string message)
	        : base(Prefix + ":" + message)
	    {
	    }
	
	    public HtmlException(Exception innerException)
	        : base(null, innerException)
	    {
	    }
	
	    public HtmlException(string message, Exception innerException)
	        : base(Prefix + ":" + message, innerException)
	    {
	    }
	
	    public int Code => GetCode(Message);
	
	    public static int GetCode(string message)
	    {
	        if (message == null)
	            return -1;
	
	        if (!message.StartsWith(Prefix, StringComparison.Ordinal))
	            return -1;
	
	        var pos = message.IndexOf(':', Prefix.Length);
	        if (pos < 0)
	            return -1;
	
	        if (int.TryParse(message[Prefix.Length..pos], NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
	            return i;
	
	        return -1;
	    }
	}
	
	public enum HtmlFragmentType
	{
	    Text,
	    TagOpen,    // <
	    TagEnd,     // -> TagEnd
	    TagEndClose,    // />
	    TagClose,   // </body
	    AttName,
	    AttValue,
	    Comment,
	    CDataText,
	}
	
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
	
	    public IEnumerable<HtmlNode> AllChildNodes
	    {
	        get
	        {
	            if (HasChildNodes)
	            {
	                foreach (var node in ChildNodes)
	                {
	                    yield return node;
	                    foreach (var child in node.AllChildNodes)
	                    {
	                        yield return child;
	                    }
	                }
	            }
	        }
	    }
	
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
	            throw new HtmlException("0005: Maximum recursion depth (" + _maxRecursion + ") exceeded. This may be caused by a recursive XSLT.");
	
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
	
	        prefix = name[..pos].ToNull();
	        localName = name[(pos + 1)..].ToNull();
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
	        return Attributes[name]?.Value.ToNull();
	    }
	
	    public string? GetNullifiedAttributeValue(string localName, string namespaceURI)
	    {
	        ArgumentNullException.ThrowIfNull(localName);
	        ArgumentNullException.ThrowIfNull(namespaceURI);
	        return Attributes[localName, namespaceURI]?.Value.ToNull();
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
	
	        if (prefix.EqualsOrdinalIgnoreCase(Prefix) && DeclaredNamespaceURI != null)
	            return DeclaredNamespaceURI;
	
	        foreach (var att in Attributes)
	        {
	            if (att.Prefix.EqualsOrdinalIgnoreCase(XmlnsPrefix) && att.LocalName.EqualsOrdinalIgnoreCase(prefix))
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
	        if (namespaceURI.EqualsOrdinalIgnoreCase(NamespaceURI))
	            return Prefix;
	
	        foreach (var att in Attributes)
	        {
	            if (att.Prefix.EqualsOrdinalIgnoreCase(XmlnsPrefix) && att.Value.EqualsOrdinalIgnoreCase(namespaceURI))
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
	            if (att.Prefix.EqualsOrdinalIgnoreCase(XmlnsPrefix))
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
	
	        if (options.HasFlag(HtmlCloneOptions.StreamOrder))
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
	
	        if (options.HasFlag(HtmlCloneOptions.Attributes))
	        {
	            foreach (var att in Attributes)
	            {
	#if DEBUG_HTML_ID
	                if (att.Name == HtmlElement.DebugIdAttributeName)
	                    continue;
	#endif
	                var cloneAtt = (HtmlAttribute)att.Clone(options);
	
	                if (options.HasFlag(HtmlCloneOptions.OverwriteAttributes))
	                {
	                    target.Attributes[cloneAtt.Name] = cloneAtt;
	                }
	                else
	                {
	                    target.Attributes.Add(cloneAtt);
	                }
	            }
	        }
	
	        if (options.HasFlag(HtmlCloneOptions.Deep))
	        {
	            foreach (var node in ChildNodes)
	            {
	                var cloneNode = node.Clone(options);
	                target.AppendChild(cloneNode);
	            }
	        }
	
	        if (options.HasFlag(HtmlCloneOptions.Tag))
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
	                    throw new HtmlException("0003: Internal error: node parenting is wrong. Attribute: " + att.Name);
	            }
	        }
	
	        if (HasChildNodes)
	        {
	            foreach (var node in ChildNodes)
	            {
	                if (node.ParentNode != this)
	                    throw new HtmlException("0004: Internal error: node parenting is wrong. Node: " + node.Clone(HtmlCloneOptions.Attributes).OuterHtml);
	
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
	
	public class HtmlNodeDepthComparer : IComparer<HtmlNode>
	{
	    public virtual ListSortDirection Direction { get; set; }
	
	    public virtual int Compare(HtmlNode? x, HtmlNode? y)
	    {
	        ArgumentNullException.ThrowIfNull(x);
	        ArgumentNullException.ThrowIfNull(y);
	
	        if (ReferenceEquals(x, y))
	            return 0;
	
	        var comp = x.Depth.CompareTo(y.Depth);
	        return Direction == ListSortDirection.Ascending ? comp : -comp;
	    }
	}
	
	public sealed class HtmlNodeList : IList<HtmlNode>, INotifyCollectionChanged
	{
	    private readonly List<HtmlNode> _list = [];
	    private readonly HtmlNode _parent;
	
	    public event NotifyCollectionChangedEventHandler? CollectionChanged;
	
	    internal HtmlNodeList(HtmlNode parent)
	    {
	        _parent = parent;
	    }
	
	    public int Count => _list.Count;
	    public HtmlNode? this[string? name] => _list.Find(n => n.Name.EqualsOrdinalIgnoreCase(name));
	    public HtmlNode? this[string? localName, string? namespaceURI] => _list.Find(a => localName.EqualsOrdinalIgnoreCase(a.LocalName) && a.NamespaceURI != null && string.Equals(namespaceURI, a.NamespaceURI, StringComparison.Ordinal));
	    public HtmlNode this[int index]
	    {
	        get => _list[index];
	        set
	        {
	            if (value == _list[index])
	                return;
	
	            var oldItem = _list[index];
	            _list[index] = value;
	            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem));
	        }
	    }
	
	    public void Replace(HtmlNode newChild, HtmlNode oldChild)
	    {
	        ArgumentNullException.ThrowIfNull(newChild);
	        ArgumentNullException.ThrowIfNull(oldChild);
	
	        if (newChild.ParentNode != null)
	            throw new ArgumentException(null, nameof(newChild));
	
	        var index = _list.IndexOf(oldChild);
	        if (index >= 0)
	        {
	            if (oldChild.ParentNode != _parent)
	                throw new ArgumentException(null, nameof(oldChild));
	
	            _list.RemoveAt(index);
	            oldChild.OwnerDocument?.ClearId(oldChild);
	            HtmlDocument.RemoveIntrinsicElement(oldChild.OwnerDocument, oldChild as HtmlElement);
	            oldChild.ParentNode = null;
	        }
	        else
	            throw new ArgumentException(null, nameof(oldChild));
	
	        _list.Insert(index, newChild);
	        newChild.OwnerDocument?.SetNodeById(newChild);
	        newChild.ParentNode = _parent;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newChild, oldChild));
	    }
	
	    public void RemoveAll()
	    {
	        foreach (var node in _list)
	        {
	            HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
	            if (node.ParentNode != _parent)
	                throw new InvalidOperationException();
	
	            node.ParentNode = null;
	        }
	        RemoveAllNoCheck();
	    }
	
	    public void Insert(int index, HtmlNode node)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	        if (node.ParentNode != null)
	            throw new ArgumentException(null, nameof(node));
	
	        HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
	        _list.Insert(index, node);
	        node.OwnerDocument?.SetNodeById(node);
	        node.ParentNode = _parent;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index));
	    }
	
	    public void AddRange(IEnumerable<HtmlNode> nodes)
	    {
	        if (nodes == null)
	            return;
	
	        foreach (var node in nodes)
	        {
	            Add(node);
	        }
	    }
	
	    internal void AddNoCheck(HtmlNode node)
	    {
	        _list.Add(node);
	        node.OwnerDocument?.SetNodeById(node);
	        node.ParentNode = _parent;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node));
	    }
	
	    public void Add(HtmlNode node)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	        if (node.ParentNode != null)
	            throw new ArgumentException(null, nameof(node));
	
	        AddNoCheck(node);
	    }
	
	    public bool RemoveAt(int index)
	    {
	        if (index < 0 || index >= _list.Count)
	            return false;
	
	        var node = _list[index];
	        if (node.ParentNode != _parent)
	            throw new ArgumentException(null, nameof(index));
	
	        _list.RemoveAt(index);
	        node.OwnerDocument?.ClearId(node);
	        HtmlDocument.RemoveIntrinsicElement(node.OwnerDocument, node as HtmlElement);
	        node.ParentNode = null;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node, index));
	        return true;
	    }
	
	    public bool Remove(HtmlNode? node)
	    {
	        if (node == null)
	            return false;
	
	        var index = _list.IndexOf(node);
	        if (index < 0)
	            return false;
	
	        var existing = _list[index];
	        if (existing.ParentNode != _parent)
	            throw new ArgumentException(null, nameof(node));
	
	        _list.RemoveAt(index);
	        node?.OwnerDocument?.ClearId(node);
	        HtmlDocument.RemoveIntrinsicElement(node?.OwnerDocument, node as HtmlElement);
	        node?.ParentNode = null;
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node));
	        return true;
	    }
	
	    internal void RemoveAllNoCheck()
	    {
	        _parent.OwnerDocument?.ClearIds(_list);
	        _list.Clear();
	        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	    }
	
	    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	    {
	        _parent.ClearCaches();
	        CollectionChanged?.Invoke(this, e);
	    }
	
	    bool ICollection<HtmlNode>.IsReadOnly => false;
	    public int IndexOf(HtmlNode item) => _list.IndexOf(item);
	    public bool Contains(HtmlNode item) => IndexOf(item) >= 0;
	    public void CopyTo(HtmlNode[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
	    public IEnumerator<HtmlNode> GetEnumerator() => _list.GetEnumerator();
	    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	    void IList<HtmlNode>.RemoveAt(int index) => RemoveAt(index);
	    void ICollection<HtmlNode>.Clear() => RemoveAll();
	}
	
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
	
	[Flags]
	public enum HtmlNodeNavigatorOptions
	{
	    None = 0,
	    UppercasedNames = 0x1,
	    LowercasedNames = 0x2,
	    UppercasedPrefixes = 0x4,
	    LowercasedPrefixes = 0x8,
	    UppercasedNamespaceURIs = 0x10,
	    LowercasedNamespaceURIs = 0x20,
	    UppercasedValues = 0x40,
	    LowercasedValues = 0x80,
	    Dynamic = 0x100,
	    RootNode = 0x200,
	    DepthFirst = 0x400,
	
	    UppercasedAll = UppercasedNames | UppercasedPrefixes | UppercasedNamespaceURIs | UppercasedValues,
	    LowercasedAll = LowercasedNames | LowercasedPrefixes | LowercasedNamespaceURIs | LowercasedValues,
	}
	
	public enum HtmlNodeType
	{
	    Attribute,
	    Comment,
	    Document,
	    Element,
	    EndElement,
	    Text,
	    None,
	    ProcessingInstruction,
	    DocumentType,
	    XPathResult,
	}
	
	public class HtmlOptions
	{
	    private readonly Dictionary<string, HtmlElementReadOptions> _readOptions = new(StringComparer.OrdinalIgnoreCase);
	    private readonly Dictionary<string, HtmlElementWriteOptions> _writeOptions = new(StringComparer.OrdinalIgnoreCase);
	    private readonly HashSet<string> _emptyNamespacesForXPath = [];
	    private readonly HashSet<string> _emptyNamespaces = [];
	    private readonly HashSet<string> _parsedScriptTypes = [];
	
	    public HtmlOptions()
	    {
	        ReaderThrowsOnEncodingMismatch = true;
	        ReaderRestartsOnEncodingDetected = true;
	
	        // check http://dev.w3.org/html5/html-author/#conforming-elements
	        _readOptions["area"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["base"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["basefont"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["bgsound"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["br"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["col"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["command"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["embed"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["frame"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["hr"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["img"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["input"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["isindex"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["keygen"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["link"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["meta"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["p"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["param"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["script"] = HtmlElementReadOptions.InnerRaw;
	        _readOptions["spacer"] = HtmlElementReadOptions.AutoClosed;
	        _readOptions["source"] = HtmlElementReadOptions.AutoClosed | HtmlElementReadOptions.NoChild;
	        _readOptions["style"] = HtmlElementReadOptions.InnerRaw;
	        _readOptions["wbr"] = HtmlElementReadOptions.AutoClosed;
	
	        // NOTE: This "NOXHTML" element is not defined in specs and is specific to us
	        // It may just be used by the caller if he wants to make sure what is inside will never be changed.
	        _readOptions["noxhtml"] = HtmlElementReadOptions.InnerRaw;
	
	        _writeOptions["a"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["abbr"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["address"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["area"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["article"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["aside"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["audio"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["b"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["base"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["bb"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["bdo"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["br"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["blockquote"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["button"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["canvas"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["caption"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["cite"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["code"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["col"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["command"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["datagrid"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["datalist"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["del"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["details"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["dfn"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["dialog"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["div"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["dl"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["em"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["embed"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["fieldset"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["figure"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["footer"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["form"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["h1"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["h2"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["h3"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["h4"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["h5"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["h6"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["header"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["header"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["hr"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["i"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["iframe"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["i"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["img"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["input"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["ins"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["kbd"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["label"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["legend"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["ins"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["link"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["map"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["mark"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["menu"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["meta"] = HtmlElementWriteOptions.DontCloseIfEmpty | HtmlElementWriteOptions.NoChild;
	        _writeOptions["meter"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["nav"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["noscript"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["object"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["ol"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["output"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["param"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["pre"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["progress"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["q"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["rp"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["rt"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["ruby"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["samp"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["script"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["section"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["select"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["small"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["source"] = HtmlElementWriteOptions.NoChild;
	        _writeOptions["span"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["strong"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["style"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["sub"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["sup"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["table"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["textarea"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["time"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["title"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["ul"] = HtmlElementWriteOptions.AlwaysClose;
	        _writeOptions["video"] = HtmlElementWriteOptions.AlwaysClose;
	
	        // avoids using xhtml for all HTML xpath queries
	        _emptyNamespacesForXPath.Add(HtmlNode.XhtmlNamespaceURI);
	    }
	
	    public IReadOnlySet<string> ParsedScriptTypes => _parsedScriptTypes;
	    public IReadOnlySet<string> EmptyNamespaces => _emptyNamespaces;
	    public IReadOnlySet<string> EmptyNamespacesForXPath => _emptyNamespacesForXPath;
	    public virtual bool ReaderThrowsOnEncodingMismatch { get; set; }
	    public virtual bool ReaderRestartsOnEncodingDetected { get; set; }
	    public virtual bool ReaderThrowsOnUnknownDetectedEncoding { get; set; }
	    public virtual bool ReaderDontDetectEncoding { get; set; }
	    public virtual bool DontBuildIdDictionary { get; set; }
	
	    public virtual HtmlElementWriteOptions GetElementWriteOptions(string? name)
	    {
	        if (name == null)
	            return HtmlElementWriteOptions.None;
	
	        _writeOptions.TryGetValue(name, out var options);
	        return options;
	    }
	
	    public virtual void SetElementWriteOptions(string name, HtmlElementWriteOptions options)
	    {
	        ArgumentNullException.ThrowIfNull(name);
	        _writeOptions[name] = options;
	    }
	
	    public virtual HtmlElementReadOptions GetElementReadOptions(string? name)
	    {
	        if (name == null)
	            return HtmlElementReadOptions.None;
	
	        _readOptions.TryGetValue(name, out var options);
	        return options;
	    }
	
	    public virtual void SetElementReadOptions(string name, HtmlElementReadOptions options)
	    {
	        ArgumentNullException.ThrowIfNull(name);
	        _readOptions[name] = options;
	    }
	
	    internal bool ParseScriptType(string? type)
	    {
	        if (type == null)
	            return false;
	
	        return ParsedScriptTypes.Contains(type);
	    }
	}
	
	public enum HtmlParserState
	{
	    Text,
	    TagOpen,    // <
	    TagEnd,     // -> TagEnd
	    TagEndClose,    // />
	    TagClose,   // </body
	    AttName,
	    AttValue,
	    CommentClose,
	    CDataText,
	
	    CommentOpen,
	    TagStart,   // <body
	    AttAssign,
	    Atts,
	    RawText, // SCRIPT and STYLE special handling
	    CData,
	}
	
	public class HtmlReader
	{
	    private readonly StringBuilder _rawValue = new();
	    private string? _currentElement;
	    private string? _typeAttribute; // only for <script type=""...> parsing
	    private bool _attIsScriptType; // only for <script type=""...> parsing
	    private int _eatNext;
	    private bool _eof;
	
	    internal char _quoteChar;
	    internal int _line = 1;
	    internal int _column = 1;
	    internal int _offset = -1;
	
	    public event EventHandler<HtmlReaderParseEventArgs>? Parsing;
	
	    public HtmlReader(TextReader reader, HtmlOptions? options = null)
	    {
	        ArgumentNullException.ThrowIfNull(reader);
	
	        ParserState = HtmlParserState.Text;
	        Value = new StringBuilder();
	        options ??= new HtmlOptions();
	
	        FirstEncodingErrorOffset = -1;
	        Errors = [];
	        Options = options;
	        TextReader = reader;
	    }
	
	    public TextReader TextReader { get; }
	    public HtmlOptions Options { get; }
	    public virtual ICollection<HtmlError> Errors { get; }
	    public virtual HtmlReaderState? State { get; protected set; }
	    public virtual int FirstEncodingErrorOffset { get; protected set; }
	    public virtual HtmlParserState ParserState { get; protected set; }
	    public StringBuilder Value { get; private set; }
	
	    protected Queue<HtmlReaderState> ParserStatesQueue { get; } = new Queue<HtmlReaderState>();
	
	    public virtual bool IsRestartable
	    {
	        get
	        {
	            if (TextReader is not StreamReader sr)
	                return false;
	
	            return sr.BaseStream != null && sr.BaseStream.CanSeek;
	        }
	    }
	
	    public virtual bool Restart()
	    {
	        if (!IsRestartable)
	            throw new InvalidOperationException();
	
	        if (TextReader is not StreamReader sr || sr.BaseStream == null || !sr.BaseStream.CanSeek)
	            return false;
	
	        return sr.BaseStream.Seek(0, SeekOrigin.Begin) == 0;
	    }
	
	    protected virtual void OnParsing(object sender, HtmlReaderParseEventArgs e) => Parsing?.Invoke(sender, e);
	
	    private void SetCurrentElement(string? tag)
	    {
	        if (!string.Equals(_currentElement, tag, StringComparison.Ordinal))
	        {
	            _currentElement = tag;
	            _typeAttribute = null;
	        }
	    }
	
	    private bool OnParsing(ref char c, ref char prev, ref char peek, out bool cont)
	    {
	        var e = new HtmlReaderParseEventArgs(Value, _rawValue)
	        {
	            Eof = _eof,
	            CurrentElement = _currentElement,
	            CurrentCharacter = c,
	            PreviousCharacter = prev,
	            PeekCharacter = peek,
	            EatNextCharacters = _eatNext,
	            State = ParserState
	        };
	        OnParsing(this, e);
	        cont = e.Continue;
	        _eof = e.Eof;
	        prev = e.PreviousCharacter;
	        c = e.CurrentCharacter;
	        SetCurrentElement(e.CurrentElement);
	        peek = e.PeekCharacter;
	        _eatNext = e.EatNextCharacters;
	        ParserState = e.State;
	        if (e.Cancel)
	            return false;
	
	        return true;
	    }
	
	    public virtual bool IsAnyQuote(int character) => character == '"' || character == '\'';
	    public virtual bool IsWhiteSpace(int character) => character == 10 || character == 13 || character == 32 || character == 9;
	    public virtual HtmlReaderState CreateState(HtmlParserState rawParserState, string? rawValue) => new(this, rawParserState, rawValue);
	    protected virtual void PushCurrentState(HtmlParserState fragmentType, string? value) => PushState(CreateState(fragmentType, value));
	
	    protected virtual void PushState(HtmlReaderState state)
	    {
	        ArgumentNullException.ThrowIfNull(state);
	        if (state.ParserState == HtmlParserState.AttName)
	        {
	            _attIsScriptType = state.Value != null && _currentElement != null && state.Value.EqualsOrdinalIgnoreCase("type") && _currentElement.EqualsOrdinalIgnoreCase("script");
	        }
	        else if (_attIsScriptType && state.ParserState == HtmlParserState.AttValue && state.Value != null)
	        {
	            _typeAttribute = state.Value;
	        }
	        ParserStatesQueue.Enqueue(state);
	    }
	
	    protected virtual void PushCurrentState() => PushState(CreateState(ParserState, Value?.ToString()));
	    protected virtual void AddError(HtmlErrorType type) => Errors.Add(new HtmlError(State?.Line ?? 0, State?.Column ?? 0, State?.Offset ?? 0, type));
	
	    public virtual bool Read()
	    {
	        if (ParserStatesQueue.Count > 0)
	        {
	            State = ParserStatesQueue.Dequeue();
	            return true;
	        }
	
	        if (_eof)
	            return false;
	
	        DoRead();
	
	        if (ParserStatesQueue.Count > 0)
	        {
	            State = ParserStatesQueue.Dequeue();
	            return true;
	        }
	
	        return false;
	    }
	
	    protected virtual void PushEndOfFile()
	    {
	        switch (ParserState)
	        {
	            case HtmlParserState.CDataText:
	            case HtmlParserState.Text:
	                if (_rawValue.Length > 0)
	                {
	                    PushCurrentState();
	                }
	                break;
	
	            case HtmlParserState.TagOpen:
	            case HtmlParserState.CommentOpen:
	                PushCurrentState(HtmlParserState.Text, "<" + _rawValue);
	                break;
	
	            case HtmlParserState.Atts:
	                PushCurrentState(HtmlParserState.Text, _rawValue.ToString());
	                break;
	
	            case HtmlParserState.AttName:
	                if (string.Equals(_rawValue.ToString().Trim(), ">", StringComparison.Ordinal))
	                    break;
	
	                PushCurrentState();
	                PushCurrentState(HtmlParserState.AttValue, null);
	                break;
	
	            case HtmlParserState.AttValue:
	                PushCurrentState();
	                break;
	
	            case HtmlParserState.TagStart:
	                PushCurrentState(HtmlParserState.Text, "<");
	                break;
	        }
	    }
	
	    protected virtual void DoRead()
	    {
	        _rawValue.Length = 0;
	        Value.Length = 0;
	        var c = char.MaxValue;
	        do
	        {
	            var prev = c;
	            c = (char)TextReader.Read();
	
	            if (_eatNext > 0)
	            {
	                _eatNext--;
	                continue;
	            }
	
	            var peek = (char)TextReader.Peek();
	
	            if (!OnParsing(ref c, ref prev, ref peek, out bool cont))
	                return;
	
	            if (cont)
	                continue;
	
	            if (c == char.MaxValue)
	            {
	                _eof = true;
	                PushEndOfFile();
	                return;
	            }
	
	            _rawValue.Append(c);
	            _offset++;
	            if (c == 65533)
	            {
	                FirstEncodingErrorOffset = _offset;
	                _column++;
	                continue;
	            }
	
	            if (c == 10)
	            {
	                _line++;
	                _column = 1;
	            }
	            else
	            {
	                if (c != 13)
	                {
	                    _column++;
	                }
	            }
	
	            switch (ParserState)
	            {
	                case HtmlParserState.Text:
	                    if (c == '<')
	                    {
	                        if (Value.Length == 0)
	                        {
	                            if (peek == '>')
	                            {
	                                PushCurrentState(HtmlParserState.Text, _rawValue.ToString());
	                                return;
	                            }
	                            ParserState = HtmlParserState.TagStart;
	                        }
	                        else
	                        {
	                            PushCurrentState();
	                            ParserState = HtmlParserState.TagStart;
	                            return;
	                        }
	                    }
	                    else
	                    {
	                        Value.Append(c);
	                    }
	                    break;
	
	                case HtmlParserState.RawText:
	                    if (((c == '>') || (IsWhiteSpace(c))) && (Value.Length >= (_currentElement?.Length + 2)) &&
	                        (Value[Value.Length - (_currentElement?.Length ?? 0) - 2] == '<') &&
	                        (Value[Value.Length - (_currentElement?.Length ?? 0) - 1] == '/') &&
	                        (Value.ToString(Value.Length - (_currentElement?.Length ?? 0), _currentElement?.Length ?? 0).EqualsOrdinalIgnoreCase(_currentElement)))
	                    {
	                        var rawText = Value.ToString(0, Value.Length - (_currentElement?.Length ?? 0) - 2);
	                        PushCurrentState(HtmlParserState.Text, rawText);
	                        if (c == '>')
	                        {
	                            PushCurrentState(HtmlParserState.TagClose, _currentElement);
	                            ParserState = HtmlParserState.Text;
	                            return;
	                        }
	                        ParserState = HtmlParserState.Atts;
	                        return;
	                    }
	
	                    Value.Append(c);
	                    break;
	
	                case HtmlParserState.CData:
	                    if (c == '>' && Value.Length >= 2 && Value[^2] == ']' && Value[^1] == ']')
	                    {
	                        var rawText = Value.ToString(0, Value.Length - 2);
	                        PushCurrentState(HtmlParserState.CDataText, rawText);
	                        ParserState = HtmlParserState.Text;
	                        return;
	                    }
	
	                    Value.Append(c);
	                    break;
	
	                case HtmlParserState.TagStart:
	                    if (c == '<')
	                    {
	                        AddError(HtmlErrorType.TagNotClosed);
	                        Value = new StringBuilder(_rawValue.ToString());
	                        ParserState = HtmlParserState.Text;
	                        continue;
	                    }
	
	                    if (IsWhiteSpace(c))
	                    {
	                        Value = new StringBuilder(_rawValue.ToString());
	                        ParserState = HtmlParserState.Text;
	                        continue;
	                    }
	
	                    ParserState = HtmlParserState.TagOpen;
	                    Value.Append(c);
	                    break;
	
	                case HtmlParserState.TagOpen:
	                    if (c == '<')
	                    {
	                        AddError(HtmlErrorType.TagNotClosed);
	                        Value = new StringBuilder(c + _rawValue.ToString());
	                        ParserState = HtmlParserState.Text;
	                        continue;
	                    }
	
	                    if (c == '>')
	                    {
	                        SetCurrentElement(Value.ToString());
	                        PushCurrentState();
	                        PushCurrentState(HtmlParserState.TagEnd, _currentElement);
	                        if (Options.GetElementReadOptions(_currentElement).HasFlag(HtmlElementReadOptions.InnerRaw))
	                        {
	                            // no need to check for <script type='' ..> here
	                            ParserState = HtmlParserState.RawText;
	                        }
	                        else
	                        {
	                            ParserState = HtmlParserState.Text;
	                        }
	                        return;
	                    }
	
	                    if (c == '/' && peek == '>')
	                    {
	                        SetCurrentElement(Value.ToString());
	                        PushCurrentState();
	                        PushCurrentState(HtmlParserState.TagEndClose, _currentElement);
	                        ParserState = HtmlParserState.Text;
	                        _eatNext = 1;
	                        return;
	                    }
	
	                    if (IsWhiteSpace(c))
	                    {
	                        SetCurrentElement(Value.ToString());
	                        PushCurrentState();
	                        ParserState = HtmlParserState.Atts;
	                        return;
	                    }
	
	                    Value.Append(c);
	
	                    if (string.Equals(Value.ToString(), "!--", StringComparison.OrdinalIgnoreCase))
	                    {
	                        Value.Length = 0;
	                        ParserState = HtmlParserState.CommentOpen;
	                    }
	                    else if (string.Equals(Value.ToString(), "![CDATA[", StringComparison.OrdinalIgnoreCase))
	                    {
	                        Value.Length = 0;
	                        ParserState = HtmlParserState.CData;
	                    }
	                    break;
	
	                case HtmlParserState.CommentOpen:
	                    if (c == '>' && Value.Length >= 2 && Value[^1] == '-' && Value[^2] == '-')
	                    {
	                        PushCurrentState(HtmlParserState.CommentClose, Value.Remove(Value.Length - 2, 2).ToString());
	                        ParserState = HtmlParserState.Text;
	                        return;
	                    }
	
	                    Value.Append(c);
	                    break;
	
	                case HtmlParserState.Atts:
	                    if (c == '>')
	                    {
	                        PushCurrentState(HtmlParserState.TagEnd, _currentElement);
	                        if (Options.GetElementReadOptions(_currentElement).HasFlag(HtmlElementReadOptions.InnerRaw))
	                        {
	                            if (Options.ParseScriptType(_typeAttribute))
	                            {
	                                ParserState = HtmlParserState.Text;
	                            }
	                            else
	                            {
	                                ParserState = HtmlParserState.RawText;
	                            }
	                        }
	                        else
	                        {
	                            ParserState = HtmlParserState.Text;
	                        }
	                        return;
	                    }
	
	                    if (c == '/' && peek == '>')
	                    {
	                        PushCurrentState(HtmlParserState.TagEndClose, _currentElement);
	                        ParserState = HtmlParserState.Text;
	                        _eatNext = 1;
	                        return;
	                    }
	
	                    if (!IsWhiteSpace(c))
	                    {
	                        Value.Length = 0;
	                        Value.Append(c);
	                        ParserState = HtmlParserState.AttName;
	                        break;
	                    }
	                    break;
	
	                case HtmlParserState.AttName:
	                    // quoted named are essentially useful for !DOCTYPE tags
	                    if (Value.Length == 1) // first char?
	                    {
	                        if (IsAnyQuote(Value[0]))
	                        {
	                            // quoted
	                            _quoteChar = Value[0];
	                        }
	                        else
	                        {
	                            // not quoted
	                            _quoteChar = '\0';
	                        }
	                    }
	
	                    // quoted name?
	                    if (IsAnyQuote(_quoteChar))
	                    {
	                        Value.Append(c);
	                        // check escaped quote
	                        if (c == _quoteChar && peek != _quoteChar && prev != _quoteChar)
	                        {
	                            PushCurrentState();
	                            ParserState = HtmlParserState.Atts;
	                            return;
	                        }
	                    }
	                    else
	                    {
	                        if (c == '=')
	                        {
	                            PushCurrentState();
	                            ParserState = HtmlParserState.AttValue;
	                            return;
	                        }
	
	                        if (c == '>')
	                        {
	                            PushCurrentState();
	                            PushCurrentState(HtmlParserState.AttValue, null);
	                            PushCurrentState(HtmlParserState.TagEnd, _currentElement);
	                            if (Options.GetElementReadOptions(_currentElement).HasFlag(HtmlElementReadOptions.InnerRaw))
	                            {
	                                if (Options.ParseScriptType(_typeAttribute))
	                                {
	                                    ParserState = HtmlParserState.Text;
	                                }
	                                else
	                                {
	                                    ParserState = HtmlParserState.RawText;
	                                }
	                            }
	                            else
	                            {
	                                ParserState = HtmlParserState.Text;
	                            }
	                            return;
	                        }
	
	                        if (c == '/' && peek == '>')
	                        {
	                            PushCurrentState();
	                            PushCurrentState(HtmlParserState.AttValue, null);
	                            PushCurrentState(HtmlParserState.TagEndClose, _currentElement);
	                            ParserState = HtmlParserState.Text;
	                            _eatNext = 1;
	                            return;
	                        }
	
	                        if (IsWhiteSpace(c))
	                        {
	                            PushCurrentState();
	                            ParserState = HtmlParserState.AttAssign;
	                            return;
	                        }
	                        Value.Append(c);
	                    }
	                    break;
	
	                case HtmlParserState.AttAssign:
	                    if (c == '=')
	                    {
	                        ParserState = HtmlParserState.AttValue;
	                        break;
	                    }
	
	                    if (c == '>')
	                    {
	                        PushCurrentState();
	                        PushCurrentState(HtmlParserState.AttValue, null);
	                        PushCurrentState(HtmlParserState.TagEnd, _currentElement);
	                        if (Options.GetElementReadOptions(_currentElement).HasFlag(HtmlElementReadOptions.InnerRaw))
	                        {
	                            if (Options.ParseScriptType(_typeAttribute))
	                            {
	                                ParserState = HtmlParserState.Text;
	                            }
	                            else
	                            {
	                                ParserState = HtmlParserState.RawText;
	                            }
	                        }
	                        else
	                        {
	                            ParserState = HtmlParserState.Text;
	                        }
	                        return;
	                    }
	
	                    if (c == '/' && peek == '>')
	                    {
	                        PushCurrentState();
	                        PushCurrentState(HtmlParserState.AttValue, null);
	                        PushCurrentState(HtmlParserState.TagEndClose, _currentElement);
	                        ParserState = HtmlParserState.Text;
	                        _eatNext = 1;
	                        return;
	                    }
	
	                    if (!IsWhiteSpace(c))
	                    {
	                        // send a null attribute
	                        PushCurrentState(HtmlParserState.AttValue, null);
	
	                        ParserState = HtmlParserState.AttName;
	                        Value.Append(c);
	                        break;
	                    }
	                    break;
	
	                case HtmlParserState.AttValue:
	                    if (Value.Length == 0) // first char?
	                    {
	                        if (!IsWhiteSpace(c))
	                        {
	                            if (IsAnyQuote(c))
	                            {
	                                // quoted
	                                _quoteChar = c;
	                            }
	                            else
	                            {
	                                // not quoted
	                                _quoteChar = '\0';
	                            }
	                            Value.Append(c);
	                        }
	                        // else skip whitespaces
	                    }
	                    else
	                    {
	                        // quoted value?
	                        if (IsAnyQuote(_quoteChar))
	                        {
	                            Value.Append(c);
	                            // check escaped quote
	                            if (c == _quoteChar && peek != _quoteChar && (prev != _quoteChar || Value.Length == 2)) // test "" or ''
	                            {
	                                PushCurrentState();
	                                ParserState = HtmlParserState.Atts;
	                                return;
	                            }
	                        }
	                        else
	                        {
	                            if (c == '>')
	                            {
	                                PushCurrentState();
	                                PushCurrentState(HtmlParserState.TagEnd, _currentElement);
	                                if (Options.GetElementReadOptions(_currentElement).HasFlag(HtmlElementReadOptions.InnerRaw))
	                                {
	                                    if (Options.ParseScriptType(_typeAttribute))
	                                    {
	                                        ParserState = HtmlParserState.Text;
	                                    }
	                                    else
	                                    {
	                                        ParserState = HtmlParserState.RawText;
	                                    }
	                                }
	                                else
	                                {
	                                    ParserState = HtmlParserState.Text;
	                                }
	                                return;
	                            }
	
	                            if (c == '/' && peek == '>')
	                            {
	                                PushCurrentState();
	                                PushCurrentState(HtmlParserState.TagEndClose, _currentElement);
	                                ParserState = HtmlParserState.Text;
	                                _eatNext = 1;
	                                return;
	                            }
	
	                            if (IsWhiteSpace(c))
	                            {
	                                PushCurrentState();
	                                ParserState = HtmlParserState.Atts;
	                                return;
	                            }
	                            Value.Append(c);
	                        }
	                    }
	                    break;
	            }
	        }
	        while (true);
	    }
	}
	
	public class HtmlReaderParseEventArgs : CancelEventArgs
	{
	    public HtmlReaderParseEventArgs(StringBuilder value, StringBuilder rawValue)
	    {
	        ArgumentNullException.ThrowIfNull(value);
	        ArgumentNullException.ThrowIfNull(rawValue);
	
	        Value = value;
	        RawValue = rawValue;
	    }
	
	    public StringBuilder Value { get; }
	    public StringBuilder RawValue { get; }
	    public virtual bool Continue { get; set; }
	    public virtual bool Eof { get; set; }
	    public virtual string? CurrentElement { get; set; }
	    public virtual int EatNextCharacters { get; set; }
	    public virtual char PreviousCharacter { get; set; }
	    public virtual char CurrentCharacter { get; set; }
	    public virtual char PeekCharacter { get; set; }
	    public virtual HtmlParserState State { get; set; }
	}
	
	[DebuggerDisplay("{Line}x{Column}x{Offset} {ParserState} '{RawValue}'")]
	public class HtmlReaderState
	{
	    public HtmlReaderState(HtmlReader reader, HtmlParserState? rawParserState, string? rawValue)
	    {
	        ArgumentNullException.ThrowIfNull(reader);
	
	        Reader = reader;
	        Line = reader._line;
	        Column = reader._column;
	        Offset = reader._offset;
	        RawValue = rawValue;
	        RawParserState = rawParserState;
	        QuoteChar = reader._quoteChar;
	    }
	
	    public HtmlReader Reader { get; }
	    public virtual char QuoteChar { get; protected set; }
	    public virtual int Offset { get; protected set; }
	    public virtual int Line { get; protected set; }
	    public virtual int Column { get; protected set; }
	    public virtual string? RawValue { get; protected set; }
	    public virtual HtmlParserState? RawParserState { get; protected set; }
	
	    public virtual HtmlFragmentType? FragmentType => (HtmlFragmentType?)(int?)ParserState;
	
	    public virtual HtmlParserState? ParserState
	    {
	        get
	        {
	            if (RawParserState == HtmlParserState.TagOpen && RawValue != null && RawValue.StartsWith('/'))
	                return HtmlParserState.TagClose;
	
	            return RawParserState;
	        }
	    }
	
	    public virtual string? Value
	    {
	        get
	        {
	            if (RawParserState == HtmlParserState.TagOpen && RawValue != null && RawValue.StartsWith('/'))
	                return RawValue[1..];
	
	            if (RawValue != null && (RawParserState == HtmlParserState.AttValue || RawParserState == HtmlParserState.AttName) &&
	                ((RawValue.StartsWith('\'') && RawValue.EndsWith('\'')) ||
	                (RawValue.StartsWith('"') && RawValue.EndsWith('"'))))
	            {
	                var quote = RawValue[0];
	                return RawValue[1..^1].Replace(quote + quote.ToString(CultureInfo.InvariantCulture), quote.ToString(CultureInfo.InvariantCulture));
	            }
	
	            return RawValue;
	        }
	    }
	}
	
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
	
	public class HtmlTextBlock(HtmlNode node, HtmlTextBlockType type)
	{
	    public HtmlNode Node { get; protected set; } = node;
	    public HtmlTextBlockType BlockType { get; protected set; } = type;
	}
	
	public class HtmlTextBlockReader
	{
	    private readonly Queue<HtmlTextBlock> _queue = new();
	    private bool _eof;
	
	    public HtmlTextBlockReader(HtmlNode node)
	    {
	        ArgumentNullException.ThrowIfNull(node);
	
	        Node = node;
	        CurrentNode = node;
	    }
	
	    public HtmlNode Node { get; }
	    public virtual HtmlNode CurrentNode { get; protected set; }
	    public virtual HtmlTextBlock? Value { get; protected set; }
	
	    public virtual bool Read()
	    {
	        if (_queue.Count > 0)
	        {
	            Value = _queue.Dequeue();
	            return true;
	        }
	
	        if (_eof)
	            return false;
	
	        DoRead();
	
	        if (_queue.Count > 0)
	        {
	            Value = _queue.Dequeue();
	            return true;
	        }
	
	        return false;
	    }
	
	    protected virtual void DoRead()
	    {
	        var current = CurrentNode;
	        if (current == null || current.ChildNodes == null)
	        {
	            _eof = true;
	            return;
	        }
	
	        HtmlTextBlock block;
	        var bt = IsHeadingOrTitleTag(current.Name);
	        if (bt.HasValue)
	        {
	            block = new HtmlTextBlock(current, bt.Value);
	            _queue.Enqueue(block);
	            return;
	        }
	    }
	
	    private static HtmlTextBlockType? IsHeadingOrTitleTag(string? name)
	    {
	        if (name == null)
	            return null;
	
	        if (name.EqualsOrdinalIgnoreCase("title"))
	            return HtmlTextBlockType.Title;
	
	        if (name.Length != 2)
	            return null;
	
	        if (name[0] != 'h' && name[0] != 'H')
	            return null;
	
	        if (char.IsDigit(name[1]))
	            return HtmlTextBlockType.Heading;
	
	        return null;
	    }
	}
	
	public enum HtmlTextBlockType
	{
	    Unspecified,
	    Title,
	    Heading,
	}
	
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
	
	public class HtmlXPathDocument : HtmlDocument
	{
	    public const string HtmlTransformNamespaceUri = "http://www.simonmourier.com/htf/2026/1";
	    public const string HtmlTransformNamespacePrefix = "htf";
	
	    private readonly List<Tuple<string, string>> _filteredAttributes = [];
	    internal List<Tuple<string, string>> _discriminantAttributes = [];
	
	    static HtmlXPathDocument()
	    {
	        HtmlTransformNamespaceManager = new XmlNamespaceManager(new NameTable());
	        HtmlTransformNamespaceManager.AddNamespace(HtmlTransformNamespacePrefix, HtmlTransformNamespaceUri);
	    }
	
	    public static XmlNamespaceManager HtmlTransformNamespaceManager { get; }
	
	    private static bool IsNamespaceAttribute(HtmlAttribute attribute) => attribute != null && string.Equals(attribute.NamespaceURI, XmlnsNamespaceURI, StringComparison.Ordinal) && string.Equals(attribute.Prefix, XmlnsPrefix, StringComparison.Ordinal);
	
	    protected internal HtmlXPathDocument()
	    {
	        AddDiscriminantAttribute("key", HtmlTransformNamespaceUri);
	        XPathNamespaceManager = new XmlNamespaceManager(new NameTable());
	    }
	
	    public virtual XmlNamespaceManager XPathNamespaceManager { get; }
	
	    private void RemoveHtmlTransformNamespace(HtmlElement? element)
	    {
	        if (element == null)
	            return;
	
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
	                        var importNode = parentNode.OwnerDocument?.ImportNode(element);
	                        if (importNode == null)
	                            continue;
	
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
	
	                            if (att.LocalName?.StartsWith(replaceToken, StringComparison.OrdinalIgnoreCase) == true)
	                            {
	                                var replaceName = att.LocalName[replaceToken.Length..];
	                                if (!string.IsNullOrEmpty(replaceName))
	                                {
	                                    string? ns = null;
	                                    var nsAtt = element.Attributes[replaceTokenNs + replaceName, HtmlTransformNamespaceUri];
	                                    if (nsAtt != null)
	                                    {
	                                        ns = nsAtt.Value.ToNull();
	                                    }
	
	                                    HtmlAttribute? replaceAtt;
	                                    if (ns != null)
	                                    {
	                                        replaceAtt = element.Attributes[replaceName, ns];
	                                        if (replaceAtt != null)
	                                        {
	                                            otherElement.SetAttribute(replaceName, ns, replaceAtt?.Value);
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
	                            else if (att.LocalName?.StartsWith(removeToken, StringComparison.OrdinalIgnoreCase) == true)
	                            {
	                                var removeName = att.LocalName[removeToken.Length..];
	                                if (!string.IsNullOrEmpty(removeName))
	                                {
	                                    string? ns = null;
	                                    var nsAtt = element.Attributes[removeTokenNs + removeName, HtmlTransformNamespaceUri];
	                                    if (nsAtt != null)
	                                    {
	                                        ns = nsAtt.Value.ToNull();
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
	            if (element.LocalName == null)
	                continue;
	
	            var targetElement = target.CreateElement(element.Prefix ?? string.Empty, element.LocalName, element.NamespaceURI);
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
	
	                if (att.LocalName == null)
	                    continue;
	
	                targetElement.SetAttribute(att.LocalName, att.NamespaceURI ?? string.Empty, att.Value);
	            }
	        }
	        return changed;
	    }
	
	    private static HtmlNode EnsureTargetParent(HtmlXPathElement element, HtmlXPathDocument target, out bool changed)
	    {
	        changed = false;
	        if (element.ParentNode is HtmlXPathElement parent && parent.LocalName != null)
	        {
	            if (target.SelectSingleNode(parent.XPathExpression, element.OwnerDocument?.XPathNamespaceManager) is HtmlElement targetElement)
	                return targetElement;
	
	            var parentElement = EnsureTargetParent(parent, target, out _);
	            targetElement = target.CreateElement(parent.Prefix ?? string.Empty, parent.LocalName, parent.NamespaceURI);
	
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
	
	    public override HtmlElement CreateElement(string prefix, string localName, string? namespaceURI) => new HtmlXPathElement(prefix, localName, namespaceURI, this);
	    public override HtmlDocument CreateDocument() => new HtmlXPathDocument();
	
	    public virtual void AddDiscriminantAttribute(string name, string namespaceURI)
	    {
	        ArgumentNullException.ThrowIfNull(name);
	        _discriminantAttributes.Add(new Tuple<string, string>(name, namespaceURI));
	    }
	
	    public virtual void AddFilteredAttribute(string name, string namespaceURI)
	    {
	        ArgumentNullException.ThrowIfNull(name);
	        _filteredAttributes.Add(new Tuple<string, string>(name, namespaceURI));
	    }
	
	    public virtual bool IsDiscriminant(HtmlAttribute attribute)
	    {
	        ArgumentNullException.ThrowIfNull(attribute);
	
	        foreach (var pair in _discriminantAttributes)
	        {
	            var ns = attribute.NamespaceURI.ToNull();
	            var dns = pair.Item2.ToNull();
	            if (string.Equals(ns, dns, StringComparison.Ordinal) && string.Equals(pair.Item1, attribute.LocalName, StringComparison.Ordinal))
	                return true;
	        }
	        return false;
	    }
	
	    public virtual bool IsFiltered(HtmlAttribute attribute)
	    {
	        ArgumentNullException.ThrowIfNull(attribute);
	
	        if (IsNamespaceAttribute(attribute))
	            return true;
	
	        foreach (var pair in _filteredAttributes)
	        {
	            var ns = attribute.NamespaceURI.ToNull();
	            var dns = pair.Item2.ToNull();
	            if (string.Equals(ns, dns, StringComparison.Ordinal) && string.Equals(pair.Item1, attribute.LocalName, StringComparison.Ordinal))
	                return true;
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
	
	public class HtmlXPathResult : HtmlNode
	{
	    protected internal HtmlXPathResult(HtmlDocument? ownerDocument, object? result)
	        : base(string.Empty, "#result", string.Empty, ownerDocument)
	    {
	        Result = result;
	    }
	
	    public virtual object? Result { get; protected set; }
	
	    public override string? Value
	    {
	        get
	        {
	            if (Result == null)
	                return null;
	
	            return string.Format(CultureInfo.InvariantCulture, "{0}", Result);
	        }
	        set => Result = value;
	    }
	
	    public override HtmlNodeType NodeType => HtmlNodeType.XPathResult;
	
	    public override void WriteTo(TextWriter writer)
	    {
	        if (writer != null && Result != null)
	        {
	            writer.Write(Result);
	        }
	    }
	
	    public override void WriteContentTo(TextWriter writer)
	    {
	        if (writer != null && Result != null)
	        {
	            writer.Write(Result);
	        }
	    }
	
	    public override void WriteTo(XmlWriter writer)
	    {
	        if (writer != null && Result != null)
	        {
	            writer.WriteValue(Result);
	        }
	    }
	
	    public override void WriteContentTo(XmlWriter writer)
	    {
	        if (writer != null && Result != null)
	        {
	            writer.WriteValue(Result);
	        }
	    }
	}
}

namespace HtmlNado.Utilities
{
	internal static class Extensions
	{
	    public static string? ToNull(this string? text)
	    {
	        if (text == null)
	            return null;
	
	        if (string.IsNullOrWhiteSpace(text))
	            return null;
	
	        var t = text.Trim();
	        return t.Length == 0 ? null : t;
	    }
	
	    public static bool EqualsOrdinalIgnoreCase(this string? thisString, string? text, bool trim = true)
	    {
	        if (trim)
	        {
	            thisString = thisString.ToNull();
	            text = text.ToNull();
	        }
	
	        if (thisString == null)
	            return text == null;
	
	        if (text == null)
	            return false;
	
	        if (thisString.Length != text.Length)
	            return false;
	
	        return string.Compare(thisString, text, StringComparison.OrdinalIgnoreCase) == 0;
	    }
	
	    public static void EscapeRtf(TextWriter writer, string? text) => EscapeRtf(writer, null, text);
	    public static void EscapeRtf(TextWriter writer, Encoding? escapeEncoding, string? text)
	    {
	        ArgumentNullException.ThrowIfNull(writer);
	        if (text == null)
	            return;
	
	        var uc0 = false;
	        for (var i = 0; i < text.Length; i++)
	        {
	            var c = text[i];
	            if (IsRtfSpec(c))
	            {
	                writer.Write('\\');
	                writer.Write(c);
	                uc0 = false;
	            }
	            else if (c > 0xFF)
	            {
	                if (!uc0)
	                {
	                    writer.Write(@"\uc0");
	                    uc0 = true;
	                }
	                writer.Write(@"\u");
	                var ic = (int)c;
	                if (ic > 32767)
	                {
	                    ic -= 65536;
	                }
	                writer.Write(ic);
	            }
	            else if ((c >= 0 && c < 0x20) || (c >= 0x80 && c <= 0xFF))
	            {
	                uc0 = false;
	                writer.Write(@"\'");
	                if (escapeEncoding == null)
	                {
	                    int ic = c;
	                    writer.Write(ic.ToString("x2", CultureInfo.InvariantCulture));
	                }
	                else
	                {
	                    foreach (var b in escapeEncoding.GetBytes([c]))
	                    {
	                        writer.Write(b.ToString("x2", CultureInfo.InvariantCulture));
	                    }
	                }
	            }
	            else
	            {
	                uc0 = false;
	                writer.Write(c);
	            }
	        }
	    }
	
	    private static bool IsRtfSpec(char c) => c == '{' || c == '}' || c == '\\';
	    public static string? EscapeRtf(string text)
	    {
	        if (text == null)
	            return null;
	
	        using var writer = new StringWriter(new StringBuilder(text.Length));
	        EscapeRtf(writer, text);
	        return writer.ToString();
	    }
	
	    public static string GetValidXmlName(string text)
	    {
	        ArgumentNullException.ThrowIfNull(text);
	        if (text.Length == 0)
	            throw new ArgumentException(null, nameof(text));
	
	        var sb = new StringBuilder(text.Length);
	        if (IsValidXmlNameStart(text[0]))
	        {
	            sb.Append(text[0]);
	        }
	        else
	        {
	            sb.Append(GetXmlNameEscape(text[0]));
	        }
	
	        for (int i = 1; i < text.Length; i++)
	        {
	            if (IsValidXmlNamePart(text[i]))
	            {
	                sb.Append(text[i]);
	            }
	            else
	            {
	                sb.Append(GetXmlNameEscape(text[i]));
	            }
	        }
	        return sb.ToString();
	    }
	
	    private static string GetXmlNameEscape(char c) => "_x" + ((int)c).ToString("x4", CultureInfo.InvariantCulture) + "_";
	
	    // http://www.w3.org/TR/REC-xml/#NT-Letter
	    // valids are Lu, Ll, Lt, Lo, Nl
	    private static bool IsValidXmlNameStart(char c)
	    {
	        if (c == '_')
	            return true;
	
	        if (c == 0x20DD || c == 0x20E0)
	            return false;
	
	        if (c > 0xF900 && c < 0xFFFE)
	            return false;
	
	        var category = CharUnicodeInfo.GetUnicodeCategory(c);
	        return category switch
	        {
	            //Lu
	            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.LetterNumber or UnicodeCategory.OtherLetter => true,
	            _ => false,
	        };
	    }
	
	    // valids are Lu, Ll, Lt, Lo, Nl, Mc, Me, Mn, Lm, or Nd
	    private static bool IsValidXmlNamePart(char c)
	    {
	        if (c == '_' || c == '.' || c == '-')
	            return true;
	
	        if (c == 0x0387)
	            return true;
	
	        if (c == 0x20DD || c == 0x20E0)
	            return false;
	
	        if (c > 0xF900 && c < 0xFFFE)
	            return false;
	
	        var category = CharUnicodeInfo.GetUnicodeCategory(c);
	        return category switch
	        {
	            //Lu
	            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.LetterNumber or UnicodeCategory.OtherLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark or UnicodeCategory.DecimalDigitNumber => true,
	            _ => false,
	        };
	    }
	
	    // helper methods to parse content-disposition
	    public static string? UnencodeUTF8(string? header) => header == null ? null : Encoding.UTF8.GetString(Encoding.Default.GetBytes(header));
	    public static string? GetAttributeFromHeader(string? header, string name)
	    {
	        int index;
	        if (header == null)
	            return null;
	
	        var startIndex = 1;
	        while (startIndex < header.Length)
	        {
	            startIndex = CultureInfo.InvariantCulture.CompareInfo.IndexOf(header, name, startIndex, CompareOptions.IgnoreCase);
	            if (startIndex < 0 || (startIndex + name.Length) >= header.Length)
	                break;
	
	            var c1 = header[startIndex - 1];
	            var c2 = header[startIndex + name.Length];
	            if ((c1 == ';' || c1 == ',' || char.IsWhiteSpace(c1)) && (c2 == '=' || char.IsWhiteSpace(c2)))
	                break;
	
	            startIndex += name.Length;
	        }
	
	        if (startIndex < 0 || startIndex >= header.Length)
	            return null;
	
	        startIndex += name.Length;
	        while (startIndex < header.Length && char.IsWhiteSpace(header[startIndex]))
	        {
	            startIndex++;
	        }
	
	        if (startIndex >= header.Length || header[startIndex] != '=')
	            return null;
	
	        startIndex++;
	        while (startIndex < header.Length && char.IsWhiteSpace(header[startIndex]))
	        {
	            startIndex++;
	        }
	
	        if (startIndex >= header.Length)
	            return null;
	
	        if (startIndex < header.Length && header[startIndex] == '"')
	        {
	            if (startIndex == (header.Length - 1))
	                return null;
	
	            index = header.IndexOf('"', startIndex + 1);
	            if (index < 0 || index == (startIndex + 1))
	                return null;
	
	            return header.Substring(startIndex + 1, (index - startIndex) - 1).Trim();
	        }
	
	        index = startIndex;
	        while (index < header.Length)
	        {
	            if (header[index] == ' ' || header[index] == ',')
	                break;
	
	            index++;
	        }
	
	        if (index == startIndex)
	            return null;
	
	        return header[startIndex..index].Trim();
	    }
	}
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore IDE0079 // Remove unnecessary suppression
