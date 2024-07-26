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

    [HttpGet("services-ids")]
    public async Task<ActionResult<List<int>>> GetServicesIds()
    {
        var servicesIds = await _context.Services.AsNoTracking().Select(x => x.Id).ToListAsync();
        return Ok(servicesIds);
    }

    [HttpGet("logs-with-service")]
    public async Task<ActionResult<List<Log>>> GetLogsByServiceIds([FromQuery] List<int> serviceIds)
    {
        var logs = await _context.Logs
            .AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .Select(s => new Log
            {
                Id = s.Id,
                Service = s.Service,
            })
            .ToListAsync();
        return Ok(logs);
    }

    [HttpDelete("/{id}")]
    public async void DeleteService([FromRoute] int id)
    {
        await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
        await Response.WriteAsync("Successfully deleted record");
    }

    [HttpGet("error-logs/count")]
    public async Task<ActionResult<int>> GetErrorLogsCountInefficient()
    {
        var errorLogs = await _context.Logs.ToListAsync();

        int errorLogCount = 0;

        foreach (var errorLog in errorLogs)
        {
            if (errorLog.LogLevel == InternalLogLevel.Error)
            {
                errorLogCount++;
            }
        }

        return Ok(errorLogCount);
    }

}
