namespace HtmlNado.Css;

public abstract class CssRule : ICssFormattable
{
    private readonly List<CssComponentValue> _prelude = [];

    protected CssRule()
    {
    }

    public virtual IList<CssComponentValue> Prelude => _prelude;

    public override string ToString()
    {
        using var writer = new StringWriter();
        Write(new CssFormatter(writer));
        return writer.ToString();
    }

    public virtual void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        for (int i = 0; i < _prelude.Count; i++)
        {
            var cv = _prelude[i];
            cv.Write(formatter);
        }
    }
}
