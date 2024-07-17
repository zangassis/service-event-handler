using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceEventHandler.Data;
using ServiceEventHandler.Models;

namespace ServiceEventHandler.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("error-logs")]
    public ActionResult<List<Log>> GetErrorLogs()
    {
        var logErrors = _context.Logs
          .Where(l => l.LogLevel == InternalLogLevel.Error)
          .ToList();

        return Ok(logErrors);
    }

    [HttpGet("log-services")]
    public ActionResult<IEnumerable<Log>> GetLogsForServices([FromQuery] List<int> serviceIds)
    {
        if (serviceIds == null || serviceIds.Count == 0)
        {
            return BadRequest("Service IDs must be provided.");
        }

        var logs = _context.Logs
            .Include(l => l.Service)
            .Where(l => serviceIds.Contains(l.ServiceId))
            .ToList();

        logs.ForEach(log =>
        {
            if (_context.Services.Any(s => s.Id != log.ServiceId))
            {
                var serviceError = new ServiceIntegrationError
                { 
                    Error = "There are services without integration"
                };

                _context.ServiceIntegrationErrors.Add(serviceError);
                _context.SaveChanges();
            }
        });

        return Ok(logs);
    }
}
