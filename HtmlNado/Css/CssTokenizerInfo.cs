namespace HtmlNado.Css;

public sealed class CssTokenizerInfo
{
    public CssTokenizerInfo(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }
    public int Column { get; }

    public override string ToString() => Line + ":" + Column;
}
