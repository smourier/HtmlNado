namespace HtmlNado.Css;

public class CssQualifiedRule : CssRule
{
    public CssQualifiedRule()
    {
        Declarations = new CssDeclarationList(new LiteralToken('{'));
    }

    public virtual CssDeclarationList Declarations { get; internal set; }

    public override void Write(CssFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        base.Write(formatter);
        if (Declarations.Count > 0)
        {
            if (formatter.LastWrittenCharacter != ' ')
            {
                formatter.Write(' ');
            }

            Declarations.Write(formatter);
        }
        formatter.Write(Environment.NewLine);
    }
}
