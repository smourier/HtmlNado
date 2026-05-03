namespace HtmlNado.Css;

public class CssDeclaration : ICssFormattable
{
    private readonly List<CssComponentValue> _values = [];

    public CssDeclaration(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public string Name { get; }
    public bool IsImportant { get; set; }

    public virtual IList<CssComponentValue> Values => _values;

    public override string ToString()
    {
        using var writer = new StringWriter();
        Write(new CssFormatter(writer));
        return writer.ToString();
    }

    public virtual void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        formatter.Write(Name);
        formatter.Write(':');

        // TODO: change this
        formatter.Write(CssComponentValue.ToString(_values));
        if (IsImportant)
        {
            formatter.Write("!important");
        }
        formatter.Write(';');
    }

    public static string ToString(IEnumerable<CssDeclaration> values)
    {
        if (values == null)
            return null;

        return string.Join(null, values.Select(v => v.ToString() + ";"));
    }
}
