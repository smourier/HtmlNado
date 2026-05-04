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
    public virtual bool Continue { get; set; }
    public virtual bool Eof { get; set; }
    public virtual string? CurrentElement { get; set; }
    public virtual int EatNextCharacters { get; set; }
    public virtual char PreviousCharacter { get; set; }
    public virtual char CurrentCharacter { get; set; }
    public virtual char PeekCharacter { get; set; }
    public virtual HtmlParserState State { get; set; }
}
