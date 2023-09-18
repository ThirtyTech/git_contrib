using MudBlazor.Services;

public static class ChartServer
{
    public static void DoWork()
    {

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();

        builder.Services
        .AddMudServices()
        .AddRazorComponents()
        .AddServerComponents();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();
        app.MapControllers();

        app.MapRazorComponents<git_contrib.Components.App>()
            .AddServerRenderMode();

        app.Run();
    }
}
