namespace HtmlNado.Utilities;

public class BufferedStringWriter : BufferedStreamWriter
{
    public BufferedStringWriter(int bufferSize = 1)
        : base(new MemoryStream(), bufferSize, HtmlDocument.UTF8NoBOMEncoding, true)
    {
    }

    public override string ToString()
    {
        Flush();
        var buffer = ((MemoryStream)BaseStream).GetBuffer();
        var s = Encoding.GetString(buffer, 0, (int)BaseStream.Length);
        return s;
    }
}
