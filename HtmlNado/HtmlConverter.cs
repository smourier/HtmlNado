namespace HtmlNado;

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

    private static bool IsTitleTag(string? name) => name.EqualsIgnoreCase("title");

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
        if (node == null || !node.Name.EqualsIgnoreCase("a"))
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
        if (node == null || !node.Name.EqualsIgnoreCase("img"))
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

            if (node.Name.EqualsIgnoreCase("img"))
            {
                AppendImagePlaintText(node, writer);
                return;
            }

            if (node.Name.EqualsIgnoreCase("a"))
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

            if (node.Name.EqualsIgnoreCase("p"))
            {
                WriterWriteLine(writer);
            }

            if (node.Name != null && !_noTextTags.Contains(node.Name))
            {
                AppendPlainText(node, writer);
            }

            if (node.Name.EqualsIgnoreCase("br"))
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
            if (_lastNode != null && _lastNode.Name.EqualsIgnoreCase("p"))
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
        url = url.Nullify();
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
