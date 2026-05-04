namespace HtmlNado;

public class HtmlReaderWithCode(HtmlDocumentWithCode document, TextReader reader, HtmlOptions options) : HtmlReader(reader, options)
{
    public HtmlDocumentWithCode Document { get; private set; } = document;
    protected bool IsInCode { get; set; }

    protected virtual void HandleEof(HtmlReaderParseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        e.Value.Insert(0, Document.CodeStartDelimiter);
        e.State = HtmlParserState.Text;
        Document.HasPrematureEnd = true;
    }

    public override HtmlReaderState CreateState(HtmlParserState rawParserState, string rawValue) => new HtmlReaderStateWithCode(this, rawParserState, rawValue);

    protected override void OnParsing(object sender, HtmlReaderParseEventArgs e)
    {
        if (IsInCode)
        {
            if (e.CurrentCharacter == Document.CodeEndToken && e.PeekCharacter == Document.CodeEndDelimiter)
            {
                IsInCode = false;
                return;
            }

            if (e.CurrentCharacter != char.MaxValue)
            {
                e.Value.Append(e.CurrentCharacter);
                e.Continue = true;
            }
            else
            {
                HandleEof(e);
            }
            return;
        }

        var startCode = (e.State == HtmlParserState.TagStart && e.CurrentCharacter == Document.CodeStartToken && e.PeekCharacter != Document.DirectiveToken) ||
            (e.State == HtmlParserState.AttValue && e.CurrentCharacter == Document.CodeStartDelimiter && e.PeekCharacter == Document.CodeStartToken);

        if (startCode)
        {
            IsInCode = true;
            e.Continue = true;
            e.Value.Append(e.CurrentCharacter);
            return;
        }

        base.OnParsing(sender, e);
    }
}
