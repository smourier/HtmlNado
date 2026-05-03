namespace HtmlNado;

public class HtmlTextBlock
{
    public HtmlTextBlock(HtmlNode node, HtmlTextBlockType type)
    {
        Node = node;
        BlockType = type;
    }

    public HtmlNode Node { get; protected set; }
    public HtmlTextBlockType BlockType { get; protected set; }
}
