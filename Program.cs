using Microsoft.EntityFrameworkCore;
using ServiceEventHandler.Data;
using ServiceEventHandler.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEnyimMemcached(options =>
{
	options.Servers.Add(new Enyim.Caching.Configuration.Server
	{
		Address = "localhost",
		Port = 11211
	});
});

builder.Services.AddEasyCaching(options =>
{
    options.UseMemcached(config =>
    {
        config.DBConfig.AddServer("127.0.0.1", 11211);
    }, "memcached");
});

builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapControllers();

InitializeDatabase(app);

app.UseEnyimMemcached();

app.Run();

static void InitializeDatabase(WebApplication app)
{
	using var scope = app.Services.CreateScope();
	var services = scope.ServiceProvider;
	var dbContext = services.GetRequiredService<ApplicationDbContext>();

	if (dbContext.Database.EnsureCreated())
	{
		if (!dbContext.Services.Any())
		{
			var servicesToAdd = GenerateSampleServices(100);
			dbContext.Services.AddRange(servicesToAdd);
			dbContext.SaveChanges();
		}

		if (!dbContext.Logs.Any())
		{
			var logsToAdd = GenerateSampleLogs(dbContext.Services.ToList(), 1000);
			dbContext.Logs.AddRange(logsToAdd);
			dbContext.SaveChanges();
		}
	}
}

static List<Service> GenerateSampleServices(int count)
{
	var services = new List<Service>();

	for (int i = 1; i <= count; i++)
	{
		services.Add(new Service
		{
			Name = $"Service {i}"
		});
	}
	return services;
}

static List<Log> GenerateSampleLogs(List<Service> services, int countPerService)
{
	var logs = new List<Log>();
	var random = new Random();

	foreach (var service in services)
	{
		for (int i = 1; i <= countPerService; i++)
		{
			logs.Add(new Log
			{
				Service = service,
				Message = $"Log message {i} for {service.Name}",
				LogLevel = (InternalLogLevel)random.Next(0, 3),
				Timestamp = DateTime.UtcNow.AddHours(-random.Next(1, 1000))
			});
		}
	}
	return logs;
}