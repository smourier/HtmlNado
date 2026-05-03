namespace HtmlNado.Utilities;

public interface ILogger
{
    void Log(TraceLevel level, object value, string methodName);
}
