using EmployeeIntegration.Application;
using EmployeeIntegration.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var clientOptions = new Employee360ClientOptions();
var baseUrlConfig = builder.Configuration["Employee360:BaseUrl"] ?? builder.Configuration["Employee360__BaseUrl"];
if (!string.IsNullOrWhiteSpace(baseUrlConfig))
{
    clientOptions.BaseUrl = baseUrlConfig;
}

if (int.TryParse(builder.Configuration["Employee360:TimeoutSeconds"], out var timeoutSec) && timeoutSec > 0)
{
    clientOptions.Timeout = TimeSpan.FromSeconds(timeoutSec);
}

if (int.TryParse(builder.Configuration["Employee360:MaxRetries"], out var maxRetries) && maxRetries > 0)
{
    clientOptions.MaxRetries = maxRetries;
}

builder.Services.AddSingleton(clientOptions);

builder.Services.AddHttpClient<IEmployee360Client, Employee360Client>((sp, client) =>
{
    var opts = sp.GetRequiredService<Employee360ClientOptions>();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = opts.Timeout;
});

builder.Services.AddSingleton<IEventProcessingStore, InMemoryEventProcessingStore>();
builder.Services.AddTransient<IEmployeeEventProcessor, EmployeeEventProcessor>();

builder.Build().Run();