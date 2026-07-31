using GestorDeBacklogs.Api.Config;
using GestorDeBacklogs.Api.Endpoints;
using GestorDeBacklogs.Api.Security;
using GestorDeBacklogs.Api.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureDevOpsSettings>(builder.Configuration.GetSection(AzureDevOpsSettings.SectionName));
builder.Services.AddHttpClient("AzureDevOps")
    // PAT inválido faz o Azure DevOps responder com 302 para a tela de login em vez de 401;
    // sem desabilitar o redirect automático, o HttpClient seguiria e veria um 200 (a própria página de login).
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddDataProtection()
    .SetApplicationName("GestorDeBacklogs")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestorBacklogs", "keys")));
builder.Services.AddSingleton<ISecretProtector, SecretProtector>();
builder.Services.AddSingleton<IConnectionSettingsStore, ConnectionSettingsStore>();
builder.Services.AddScoped<IAzureDevOpsClient, AzureDevOpsClient>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapConfigEndpoints();
app.MapWorkItemsEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
