namespace HtmlNado.Css;

public abstract class CssTokenizer
{
    public event EventHandler<CssParseErrorEventArgs> ParseError;

    public abstract CssComponentValue Consume();

    protected virtual void OnParseError(object sender, CssParseErrorEventArgs e) => ParseError?.Invoke(sender, e);
}
