using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volumetric.Core.Interfaces;
using Volumetric.Mcp;
using Volumetric.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<IFileIdentityService, FileIdentityService>();
builder.Services.AddSingleton<IDiskScanService>(sp =>
    new DiskScanService(sp.GetRequiredService<IFileIdentityService>()));
builder.Services.AddSingleton<ScanJobManager>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VolumetricTools>();

await builder.Build().RunAsync();
