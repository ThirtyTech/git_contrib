using MudBlazor.Services;

public static class ChartServer
{
    public static void DoWork(Options options)
    {

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton(options);

        builder.Services.AddControllers();

        builder.Services
        .AddMudServices()
        .AddRazorComponents()
        .AddInteractiveServerComponents();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStaticFiles(Directory.GetCurrentDirectory());

        app.UseRouting();
        app.MapControllers();
        app.UseAntiforgery();

        app.MapRazorComponents<git_contrib.Components.App>().AddInteractiveServerRenderMode();

        app.Run();
    }
}
