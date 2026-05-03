namespace HtmlNado.Css;

public class CssAtRule : CssRule
{
    public CssAtRule(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public string Name { get; }
    public virtual CssSimpleBlock Block { get; internal set; }

    public override void Write(CssFormatter formatter)
    {
        base.Write(formatter);
        if (Block != null)
        {
            Block.Write(formatter);
        }
        else
        {
            formatter.Write(';');
        }
        formatter.Write(Environment.NewLine);
    }
}
