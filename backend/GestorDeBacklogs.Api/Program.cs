using System.Text.Json.Serialization;
using GestorDeBacklogs.Api.Config;
using GestorDeBacklogs.Api.Endpoints;
using GestorDeBacklogs.Api.Security;
using GestorDeBacklogs.Api.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// AuthMode precisa serializar como string ("Pat"/"Sso") pro frontend, não como número.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<AzureDevOpsSettings>(builder.Configuration.GetSection(AzureDevOpsSettings.SectionName));
builder.Services.Configure<AzureAdSettings>(builder.Configuration.GetSection(AzureAdSettings.SectionName));
builder.Services.AddHttpClient("AzureDevOps")
    // PAT inválido faz o Azure DevOps responder com 302 para a tela de login em vez de 401;
    // sem desabilitar o redirect automático, o HttpClient seguiria e veria um 200 (a própria página de login).
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddDataProtection()
    .SetApplicationName("GestorDeBacklogs")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestorBacklogs", "keys")));
builder.Services.AddSingleton<ISecretProtector, SecretProtector>();
builder.Services.AddSingleton<IEntraAuthService, EntraAuthService>();
builder.Services.AddSingleton<IConnectionSettingsStore, ConnectionSettingsStore>();

// MOCK_DATA=true permite rodar a UI localmente com dados fake, sem depender de um Azure DevOps real.
if (builder.Configuration["MOCK_DATA"] == "true")
{
    builder.Services.AddSingleton<IAzureDevOpsClient, MockAzureDevOpsClient>();
}
else
{
    builder.Services.AddScoped<IAzureDevOpsClient, AzureDevOpsClient>();
}

builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapConfigEndpoints();
app.MapAuthEndpoints();
app.MapWorkItemsEndpoints();
app.MapDashboardEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
