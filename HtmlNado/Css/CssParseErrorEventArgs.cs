namespace HtmlNado.Css;

public class CssParseErrorEventArgs : EventArgs
{
    public CssParseErrorEventArgs(CssParseError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
    }

    public virtual CssParseError Error { get; }
}
