namespace HtmlNado.Css;

public abstract class CssComponentValue : ICssFormattable
{
    protected CssComponentValue(CssTokenizerInfo info)
    {
        info ??= new CssTokenizerInfo(0, 0);

        Info = info;
    }

    public CssTokenizerInfo Info { get; }

    public override string ToString()
    {
        using var writer = new StringWriter();
        Write(new CssFormatter(writer));
        return writer.ToString();
    }

    public virtual void Write(CssFormatter formatter)
    {
    }

    public static string ToString(IEnumerable<CssComponentValue> values) => ToString(values, null);
    public static string ToString(IEnumerable<CssComponentValue> values, string separator) => values != null ? string.Join(separator, values) : null;
}
