namespace HtmlNado;

public class HtmlReaderStateWithCode : HtmlReaderState
{
    public HtmlReaderStateWithCode(HtmlReaderWithCode reader, HtmlParserState rawParserState, string rawValue)
        : base(reader, rawParserState, rawValue)
    {
    }

    public new HtmlReaderWithCode Reader => (HtmlReaderWithCode)base.Reader;

    public override string Value
    {
        get
        {
            if (RawValue != null && (RawParserState == HtmlParserState.AttValue || RawParserState == HtmlParserState.AttName) &&
                RawValue.IndexOf(Reader.Document.CodeStartDelimiter.ToString(CultureInfo.InvariantCulture) + Reader.Document.CodeStartToken, StringComparison.OrdinalIgnoreCase) >= 0 &&
                ((RawValue.StartsWith('\'') && RawValue.EndsWith('\'')) ||
                (RawValue.StartsWith('"') && RawValue.EndsWith('"'))))
                return RawValue[1..^1];

            return base.Value;
        }
    }
}
