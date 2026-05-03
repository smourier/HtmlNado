namespace HtmlNado;

public class HtmlReaderParseEventArgs : CancelEventArgs
{
    public HtmlReaderParseEventArgs(StringBuilder value, StringBuilder rawValue)
    {
        ArgumentNullException.ThrowIfNull(value);

        ArgumentNullException.ThrowIfNull(rawValue);

        Value = value;
        RawValue = rawValue;
    }

    public StringBuilder Value { get; }
    public StringBuilder RawValue { get; }
    public bool Continue { get; set; }
    public bool Eof { get; set; }
    public string CurrentElement { get; set; }
    public int EatNextCharacters { get; set; }
    public char PreviousCharacter { get; set; }
    public char CurrentCharacter { get; set; }
    public char PeekCharacter { get; set; }
    public HtmlParserState State { get; set; }
}
