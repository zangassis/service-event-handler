using EasyCaching.Core;
using Enyim.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ServiceEventHandler.Data;
using ServiceEventHandler.Models;

namespace ServiceEventHandler.Controllers
{
    public class EfficientServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IMemcachedClient _memcachedClient;
        private readonly IEasyCachingProviderFactory _cacheFactory;

        public EfficientServicesController(ApplicationDbContext context, IMemoryCache memoryCache, IMemcachedClient memcachedClient, IEasyCachingProviderFactory cacheFactory)
        {
            _context = context;
            _memoryCache = memoryCache;
            _memcachedClient = memcachedClient;
            _cacheFactory = cacheFactory;
        }

        [HttpGet("log-errors")]
        public async Task<ActionResult<List<Log>>> GetErrorLogs()
        {
            var logErrors = await _context.Logs
              .Where(l => l.LogLevel == InternalLogLevel.Error)
              .ToListAsync();

            return Ok(logErrors);
        }

        [HttpGet("error-logs/{skip}/{take}")]
        public async Task<ActionResult<List<Log>>> GetErrorLogsPaginated([FromRoute] int skip = 0, [FromRoute] int take = 10)
        {
            var logErrors = await _context.Logs
              .Where(l => l.LogLevel == InternalLogLevel.Error)
              .Skip(skip)
              .Take(take)
              .OrderBy(c => c.Id)
              .ToListAsync();

            return Ok(logErrors);
        }

        [HttpGet("logs-with-service")]
        public async Task<ActionResult<List<Log>>> GetAllServicesWithLogs()
        {
            var services = await _context.Services
                .AsNoTracking()
                .ToListAsync();

            var serviceIds = services.Select(s => s.Id).ToList();

            var logs = await _context.Logs
                .AsNoTracking()
                .Where(l => serviceIds.Contains(l.ServiceId))
                .ToListAsync();

            return Ok(logs);
        }


        [HttpDelete("/{id}")]
        public async Task DeleteService([FromRoute] int id)
        {
            await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
            await Response.WriteAsync("Successfully deleted record");
        }

        [HttpGet("error-logs/count")]
        public async Task<ActionResult<int>> GetErrorLogsCount()
        {
            int errorLogsCount = await _context.Logs
                .Where(l => l.LogLevel == InternalLogLevel.Error)
                .CountAsync();

            return Ok(errorLogsCount);
        }

        [HttpGet("log-errors-cache-in-memory")]
        public async Task<ActionResult<List<Log>>> GetErrorLogsWithCacheInMemory()
        {
            const string CacheKey = "logs_with_error";

            if (!_memoryCache.TryGetValue(CacheKey, out List<Log>? logErrors))
            {
                logErrors = await _context.Logs
                    .Where(l => l.LogLevel == InternalLogLevel.Error)
                    .ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                _memoryCache.Set(CacheKey, logErrors, cacheEntryOptions);
            }

            return Ok(logErrors);
        }

        [HttpGet("log-errors-cache-distributed")]
        public async Task<ActionResult<List<Log>>> GetErrorLogsWithCacheDistributed()
        {
            const string CacheKey = "logs_with_error";
            List<Log>? logErrors = await _memcachedClient.GetValueAsync<List<Log>>(CacheKey);

            if (logErrors == null)
            {
                logErrors = await _context.Logs
                    .Where(l => l.LogLevel == InternalLogLevel.Error)
                    .ToListAsync();

                await _memcachedClient.SetAsync(CacheKey, logErrors, TimeSpan.FromMinutes(30));
            }

            return Ok(logErrors);
        }

        [HttpGet("log-errors-cache-distributed-easy")]
        public async Task<ActionResult<List<Log>>> GetErrorLogsWithCacheDistributedEasy()
        {
            const string CacheKey = "logs_with_error";
            var cacheProvider = _cacheFactory.GetCachingProvider("memcached");

            var cachedLogErrors = await cacheProvider.GetAsync<List<Log>>(CacheKey);

            List<Log>? logErrors = null;

            if (cachedLogErrors.HasValue)
            {
                logErrors = cachedLogErrors.Value;
            }
            else
            {
                logErrors = await _context.Logs
                    .Where(l => l.LogLevel == InternalLogLevel.Error)
                    .ToListAsync();

                await cacheProvider.SetAsync(CacheKey, logErrors, TimeSpan.FromMinutes(30));
            }

            return Ok(logErrors);
        }
    }
}
