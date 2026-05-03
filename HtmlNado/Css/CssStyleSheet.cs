namespace HtmlNado.Css;

public class CssStyleSheet : ICssFormattable
{
    private readonly List<CssRule> _rules = [];
    public virtual IList<CssRule> Rules => _rules;

    public override string ToString()
    {
        using var writer = new StringWriter();
        Write(new CssFormatter(writer));
        return writer.ToString();
    }

    public virtual void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (i > 0)
            {
                for (int j = 0; j < formatter.LineCountBetweenRules; j++)
                {
                    formatter.Write(Environment.NewLine);
                }
            }

            rule.Write(formatter);
        }
    }
}
