using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GitBuddy.UI.Services;
using GitBuddy.Core.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<GitBuddy.UI.Components.App>("#app");

builder.Services.AddSingleton<VsCodeBridgeService>();
builder.Services.AddSingleton<GitService>();
builder.Services.AddSingleton<NLCommandService>();
builder.Services.AddSingleton<AutocompleteService>();
builder.Services.AddSingleton<CommandHistoryService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<StateService>();

await builder.Build().RunAsync();
