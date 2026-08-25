using IBS.DataAccess.Data;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;

namespace IBS.Services
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
            var allowsAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (allowsAnonymous)
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                var loginUrl = QueryHelpers.AddQueryString("/Identity/Account/Login", "ReturnUrl", returnUrl);

                if (string.Equals(context.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers.Append("X-Login-Url", loginUrl);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                context.Response.Redirect(loginUrl);
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var isMaintenanceMode = await dbContext.AppSettings
                    .Where(s => s.SettingKey == AppSettingKey.MaintenanceMode)
                    .Select(s => s.Value == "true")
                    .FirstOrDefaultAsync();

                if (isMaintenanceMode && !context.User.IsInRole("Admin") &&
                    !context.Request.Path.StartsWithSegments("/User/Home/Maintenance"))
                {
                    context.Response.Redirect("/User/Home/Maintenance");
                    return;
                }
            }

            await _next(context);
        }
    }
}
