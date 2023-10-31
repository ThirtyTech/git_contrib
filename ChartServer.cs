using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;

public static class ChartServer
{
    public static void DoWork(Options options)
    {

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton(options);

        builder.Services
        .AddMudServices()
        .AddRazorComponents()
        .AddInteractiveServerComponents();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }

        // app.UseStaticFiles(Directory.GetCurrentDirectory());
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new EmbeddedFileProvider(assembly: typeof(ChartServer).Assembly, baseNamespace: "git_contrib.wwwroot"),
        });

        app.UseAntiforgery();

        app.MapRazorComponents<git_contrib.Components.App>().AddInteractiveServerRenderMode();

        app.Run();
    }
}
