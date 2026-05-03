namespace HtmlNado;

public class HtmlAttributeWithCode : HtmlAttribute
{
    protected internal HtmlAttributeWithCode(string prefix, string localName, string namespaceURI, HtmlDocument ownerDocument)
        : base(prefix, localName, namespaceURI, ownerDocument)
    {
    }

    public new HtmlDocumentWithCode? OwnerDocument => (HtmlDocumentWithCode?)base.OwnerDocument;

    protected override char GetQuoteChar()
    {
        var escapeQuoteChar = false;
        var s = base.GetValue(ref escapeQuoteChar);
        if (s != null && s.StartsWith(OwnerDocument?.CodeStartDelimiter.ToString(CultureInfo.CurrentCulture) + OwnerDocument?.CodeStartToken, StringComparison.OrdinalIgnoreCase) &&
            s.EndsWith(OwnerDocument?.CodeEndToken.ToString(CultureInfo.CurrentCulture) + OwnerDocument?.CodeEndDelimiter, StringComparison.OrdinalIgnoreCase))
            return '\'';

        return base.GetQuoteChar();
    }

    protected override string? GetValue(ref bool escapeQuoteChar)
    {
        if (!escapeQuoteChar)
            return base.GetValue(ref escapeQuoteChar);

        escapeQuoteChar = false;
        var s = base.GetValue(ref escapeQuoteChar);
        if (s == null)
            return null;

        var sb = new StringBuilder(s.Length);
        var inCode = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            var next = i < (s.Length - 1) ? s[i + 1] : '\0';
            if (inCode)
            {
                if (c == OwnerDocument?.CodeEndToken && next == OwnerDocument?.CodeEndDelimiter)
                {
                    inCode = false;
                }
            }
            else
            {
                if (c == OwnerDocument?.CodeStartDelimiter && next == OwnerDocument?.CodeStartToken)
                {
                    inCode = true;
                }
                else
                {
                    if (c == QuoteChar)
                    {
                        if (QuoteChar == '"')
                        {
                            sb.Append("&quot;");
                            continue;
                        }

                        if (QuoteChar == '\'')
                        {
                            sb.Append("&apos;");
                            continue;
                        }
                    }
                }
            }
            sb.Append(c);
        }

        var news = sb.ToString();
        return string.Equals(news, s, StringComparison.Ordinal) ? s : news;
    }
}
