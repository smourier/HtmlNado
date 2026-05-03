namespace HtmlNado;

public class HtmlDocumentParseEventArgs : CancelEventArgs
{
    public HtmlDocumentParseEventArgs(HtmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        Reader = reader;
    }

    public HtmlReader Reader { get; }
    public Encoding DetectedEncoding { get; set; }
    public HtmlNode CurrentNode { get; set; }
    public HtmlAttribute CurrentAttribute { get; set; }
    public bool Continue { get; set; }
}
