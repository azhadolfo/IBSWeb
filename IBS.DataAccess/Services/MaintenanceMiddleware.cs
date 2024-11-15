using IBS.DataAccess.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IBS.DataAccess.Services
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Get the DbContext (or use your repository/unit of work)
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check Maintenance Mode (update table/field as necessary)
                var isMaintenanceMode = await dbContext.AppSettings
                    .Where(s => s.SettingKey == "MaintenanceMode")
                    .Select(s => s.Value == "true")
                    .FirstOrDefaultAsync();

                if (isMaintenanceMode && !context.Request.Path.StartsWithSegments("/User/Home/Maintenance"))
                {
                    context.Response.Redirect("/User/Home/Maintenance");
                    return;
                }
            }

            await _next(context);
        }
    }
}