namespace HtmlNado.Utilities;

internal static class Extensions
{
    private static readonly object _void = new();
    public static Task NullTask => Task.FromResult(_void);

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
