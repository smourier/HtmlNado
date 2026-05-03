namespace HtmlNado.Css;

public class CssParseError
{
    public CssParseError(CssTokenizerInfo info)
    {
        info ??= new CssTokenizerInfo(0, 0);

        Info = info;
    }

    public CssTokenizerInfo Info { get; private set; }
    public virtual CssComponentValue Value { get; set; }
    public virtual CssParseErrorType Type { get; set; }

    public override string ToString()
    {
        string s = "Error at line: " + Info.Line + " column: " + Info.Column + " Type: " + Type;
        if (Value != null)
        {
            s += " Value: " + Value + " (" + Value.GetType().Name + ")";
        }
        return s;
    }
}
