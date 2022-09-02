using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using Serilog;
using YamlMockup.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, c) => c.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddSingleton<WeatherForecastService>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

FileExtensionContentTypeProvider provider = new();
provider.Mappings[".res"] = "application/octet-stream";
provider.Mappings[".pexe"] = "application/x-pnacl";
provider.Mappings[".nmf"] = "application/octet-stream";
provider.Mappings[".mem"] = "application/octet-stream";
provider.Mappings[".wasm"] = "application/wasm";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    ContentTypeProvider = provider
});

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
