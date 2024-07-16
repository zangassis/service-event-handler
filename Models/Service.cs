namespace ServiceEventHandler.Models;

public class Service
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Log> Logs { get; set; }
}
