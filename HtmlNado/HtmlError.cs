namespace HtmlNado;

[DebuggerDisplay("{Line}x{Column}x{Offset} {ErrorType}")]
public class HtmlError
{
    public HtmlError(HtmlReaderState state, HtmlErrorType errorType)
    {
        Line = state.Line;
        Column = state.Column;
        Offset = state.Offset;
        ErrorType = errorType;
    }

    public HtmlError(int line, int column, int offset, HtmlErrorType errorType)
    {
        Line = line;
        Column = column;
        Offset = offset;
        ErrorType = errorType;
    }

    public virtual HtmlNode Node { get; internal set; }
    public virtual HtmlErrorType ErrorType { get; }
    public virtual int Offset { get; }
    public virtual int Line { get; }
    public virtual int Column { get; }
}
