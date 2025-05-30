using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GraphConnectorFunction.Interfaces;
using GraphConnectorFunction.Providers;
using GraphConnectorFunction.Services;
using GraphConnectorFunction.Models;

var builder = FunctionsApplication.CreateBuilder(args);

// Get the configuration from the app settings
var config = builder.Configuration;
var azureFunctionSettings = new AzureFunctionSettings();
config.Bind(azureFunctionSettings);
builder.Services.AddSingleton(options => { return azureFunctionSettings; });

builder.Services.AddHttpClient();
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<IM365RoadmapService, M365RoadmapService>();
builder.Build().Run();
