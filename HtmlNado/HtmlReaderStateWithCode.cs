namespace HtmlNado;

public class HtmlReaderStateWithCode(HtmlReaderWithCode reader, HtmlParserState rawParserState, string rawValue) : HtmlReaderState(reader, rawParserState, rawValue)
{
    public new HtmlReaderWithCode Reader => (HtmlReaderWithCode)base.Reader;

    public override string Value
    {
        get
        {
            if (RawValue != null && (RawParserState == HtmlParserState.AttValue || RawParserState == HtmlParserState.AttName) &&
                RawValue.Contains(Reader.Document.CodeStartDelimiter.ToString(CultureInfo.InvariantCulture) + Reader.Document.CodeStartToken, StringComparison.OrdinalIgnoreCase) &&
                ((RawValue.StartsWith('\'') && RawValue.EndsWith('\'')) ||
                (RawValue.StartsWith('"') && RawValue.EndsWith('"'))))
                return RawValue[1..^1];

            return base.Value;
        }
    }
}
