namespace HtmlNado;

public static class Slug
{
    public static string Create(string text, SlugOptions options = null)
    {
        if (text == null)
            return null;

        options = options ?? new SlugOptions();
        string normalized;
        if (options.EarlyTruncate && options.MaximumLength > 0 && text.Length > options.MaximumLength)
        {
            normalized = text.Substring(0, options.MaximumLength).Normalize(NormalizationForm.FormD);
        }
        else
        {
            normalized = text.Normalize(NormalizationForm.FormD);
        }

        var max = options.MaximumLength > 0 ? Math.Min(normalized.Length, options.MaximumLength) : normalized.Length;
        var sb = new StringBuilder(max);
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            var uc = char.GetUnicodeCategory(c);
            if (options.AllowedUnicodeCategories.Contains(uc) && options.IsAllowed(c))
            {
                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:
                        if (options.ToLower)
                        {
                            c = options.Culture != null ? char.ToLower(c, options.Culture) : char.ToLowerInvariant(c);
                        }
                        sb.Append(options.Replace(c));
                        break;

                    case UnicodeCategory.LowercaseLetter:
                        if (options.ToUpper)
                        {
                            c = options.Culture != null ? char.ToUpper(c, options.Culture) : char.ToUpperInvariant(c);
                        }
                        sb.Append(options.Replace(c));
                        break;

                    default:
                        sb.Append(options.Replace(c));
                        break;
                }
            }
            else if (uc == UnicodeCategory.NonSpacingMark)
            {
                // don't add a separator
            }
            else
            {
                if (options.Separator != null && !EndsWith(sb, options.Separator))
                {
                    sb.Append(options.Separator);
                }
            }

            if (options.MaximumLength > 0 && sb.Length >= options.MaximumLength)
                break;
        }

        var result = sb.ToString();
        if (options.MaximumLength > 0 && result.Length > options.MaximumLength)
        {
            result = result.Substring(0, options.MaximumLength);
        }

        if (!options.CanEndWithSeparator && options.Separator != null && result.EndsWith(options.Separator, StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(0, result.Length - options.Separator.Length);
        }

        return result.Normalize(NormalizationForm.FormC);
    }

    private static bool EndsWith(StringBuilder sb, string text)
    {
        if (sb.Length < text.Length)
            return false;

        for (var i = 0; i < text.Length; i++)
        {
            if (sb[sb.Length - 1 - i] != text[text.Length - 1 - i])
                return false;
        }
        return true;
    }
}
