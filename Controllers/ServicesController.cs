using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("services-with-errors")]
    public ActionResult<List<Service>> GetServicesWithErrorLogs()
    {
        var servicesWithErrorLogsCount = _context.Services
            .Where(s => s.Logs
            .Any(l => l.LogLevel.ToString() == InternalLogLevel.Error.ToString())).ToList();

        return Ok(servicesWithErrorLogsCount);
    }
}
