using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ForbiddenPsalmBuilder;
using ForbiddenPsalmBuilder.Core.Services.State;
using ForbiddenPsalmBuilder.Core.Repositories;
using ForbiddenPsalmBuilder.Blazor.Services;
using ForbiddenPsalmBuilder.Data.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient for data loading
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Data services
builder.Services.AddSingleton<IEmbeddedResourceService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<EmbeddedResourceService>>();
    return new EmbeddedResourceService(logger);
});
builder.Services.AddScoped<ForbiddenPsalmBuilder.Core.Services.EquipmentService>(sp =>
{
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    var logger = sp.GetRequiredService<ILogger<ForbiddenPsalmBuilder.Core.Services.EquipmentService>>();
    return new ForbiddenPsalmBuilder.Core.Services.EquipmentService(resourceService, logger);
});
builder.Services.AddScoped<ForbiddenPsalmBuilder.Core.Services.TraderService>(sp =>
{
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    var logger = sp.GetRequiredService<ILogger<ForbiddenPsalmBuilder.Core.Services.TraderService>>();
    return new ForbiddenPsalmBuilder.Core.Services.TraderService(resourceService, logger);
});
builder.Services.AddScoped<ForbiddenPsalmBuilder.Core.Services.FeatFlawService>(sp =>
{
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    return new ForbiddenPsalmBuilder.Core.Services.FeatFlawService(resourceService);
});
builder.Services.AddScoped<ForbiddenPsalmBuilder.Core.Services.InjuryService>(sp =>
{
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    return new ForbiddenPsalmBuilder.Core.Services.InjuryService(resourceService);
});
builder.Services.AddScoped<ForbiddenPsalmBuilder.Core.Services.SpellService>(sp =>
{
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    return new ForbiddenPsalmBuilder.Core.Services.SpellService(resourceService);
});
builder.Services.AddScoped<ForbiddenPsalmBuilder.Core.Services.PetService>(sp =>
{
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    var logger = sp.GetRequiredService<ILogger<ForbiddenPsalmBuilder.Core.Services.PetService>>();
    return new ForbiddenPsalmBuilder.Core.Services.PetService(resourceService, logger);
});

// Storage services
builder.Services.AddScoped<IStateStorageService, LocalStorageService>();
builder.Services.AddScoped<IWarbandRepository, WarbandRepository>();

// Global state management - Singleton to persist across the entire app lifetime
builder.Services.AddSingleton<GlobalGameState>();

// State service - Scoped to ensure proper lifecycle management
builder.Services.AddScoped<IGameStateService>(sp =>
{
    var state = sp.GetRequiredService<GlobalGameState>();
    var repository = sp.GetRequiredService<IWarbandRepository>();
    var resourceService = sp.GetRequiredService<IEmbeddedResourceService>();
    var storageService = sp.GetRequiredService<IStateStorageService>();
    var logger = sp.GetRequiredService<ILogger<GameStateService>>();
    return new GameStateService(state, repository, resourceService, storageService, logger);
});


var app = builder.Build();

// Initialize state on startup
var stateService = app.Services.GetRequiredService<IGameStateService>();
await stateService.LoadStateAsync();
await stateService.LoadGameDataAsync();

await app.RunAsync();
