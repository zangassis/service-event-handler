namespace ServiceEventHandler.Models;

public class Log
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; }
    public string Message { get; set; }
    public InternalLogLevel LogLevel { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum InternalLogLevel
{
    Info,
    Warning,
    Error
}