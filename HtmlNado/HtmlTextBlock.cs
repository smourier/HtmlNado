namespace HtmlNado;

public class HtmlTextBlock(HtmlNode node, HtmlTextBlockType type)
{
    public HtmlNode Node { get; protected set; } = node;
    public HtmlTextBlockType BlockType { get; protected set; } = type;
}
