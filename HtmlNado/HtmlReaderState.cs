namespace HtmlNado;

[DebuggerDisplay("{Line}x{Column}x{Offset} {ParserState} '{RawValue}'")]
public class HtmlReaderState
{
    public HtmlReaderState(HtmlReader reader, HtmlParserState? rawParserState, string? rawValue)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Reader = reader;
        Line = reader._line;
        Column = reader._column;
        Offset = reader._offset;
        RawValue = rawValue;
        RawParserState = rawParserState;
        QuoteChar = reader._quoteChar;
    }

    public HtmlReader Reader { get; }
    public virtual char QuoteChar { get; protected set; }
    public virtual int Offset { get; protected set; }
    public virtual int Line { get; protected set; }
    public virtual int Column { get; protected set; }
    public virtual string? RawValue { get; protected set; }
    public virtual HtmlParserState? RawParserState { get; protected set; }

    public virtual HtmlFragmentType? FragmentType => (HtmlFragmentType?)(int?)ParserState;

    public virtual HtmlParserState? ParserState
    {
        get
        {
            if (RawParserState == HtmlParserState.TagOpen && RawValue != null && RawValue.StartsWith('/'))
                return HtmlParserState.TagClose;

            return RawParserState;
        }
    }

    public virtual string? Value
    {
        get
        {
            if (RawParserState == HtmlParserState.TagOpen && RawValue != null && RawValue.StartsWith('/'))
                return RawValue[1..];

            if (RawValue != null && (RawParserState == HtmlParserState.AttValue || RawParserState == HtmlParserState.AttName) &&
                ((RawValue.StartsWith('\'') && RawValue.EndsWith('\'')) ||
                (RawValue.StartsWith('"') && RawValue.EndsWith('"'))))
            {
                var quote = RawValue[0];
                return RawValue[1..^1].Replace(quote + quote.ToString(CultureInfo.InvariantCulture), quote.ToString(CultureInfo.InvariantCulture));
            }

            return RawValue;
        }
    }
}
