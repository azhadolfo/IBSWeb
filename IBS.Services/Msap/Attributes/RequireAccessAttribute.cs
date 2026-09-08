using IBS.Models.Msap.Enums;
using IBS.Services.Msap.AccessControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace IBS.Services.Msap.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public abstract class RequireAccessBaseAttribute(string errorMessage) : Attribute, IAsyncAuthorizationFilter
    {
        protected abstract Task<bool> HasAccessAsync(IAccessControlService accessControl, string userId);

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                Deny(context, "You must be logged in to access this resource.");
                return;
            }

            var accessControl = context.HttpContext.RequestServices.GetRequiredService<IAccessControlService>();
            if (!await HasAccessAsync(accessControl, userIdClaim.Value))
            {
                Deny(context, errorMessage);
            }
        }

        private void Deny(AuthorizationFilterContext context, string message)
        {
            var httpContext = context.HttpContext;
            var isAjax = httpContext.Request.Headers["X-Requested-With"].ToString()
                .Contains("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            var isJsonRequest = httpContext.Request.Headers["Content-Type"].ToString()
                .Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (isAjax || isJsonRequest)
            {
                context.Result = new JsonResult(new { success = false, message });
                return;
            }

            var tempDataFactory = httpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(httpContext);
            tempData["error"] = message;
            tempData.Save();

            var referer = httpContext.Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer)
                && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
                && string.Equals(refererUri.Host, httpContext.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectResult(refererUri.PathAndQuery);
                return;
            }

            context.Result = new RedirectToActionResult("Index", "Home", new { area = "User" });
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequireAccessAttribute(
        ProcedureEnum procedure,
        string errorMessage = "Access denied. You don't have permission to perform this action.")
        : RequireAccessBaseAttribute(errorMessage)
    {
        protected override Task<bool> HasAccessAsync(IAccessControlService accessControl, string userId)
            => accessControl.HasAccessAsync(userId, procedure);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireAnyAccessAttribute : RequireAccessBaseAttribute
    {
        private readonly ProcedureEnum[] _procedures;

        public RequireAnyAccessAttribute(
            params ProcedureEnum[] procedures)
            : base("Access denied. You don't have permission to perform this action.")
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
        }

        public RequireAnyAccessAttribute(
            string errorMessage,
            params ProcedureEnum[] procedures)
            : base(errorMessage)
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
        }

        protected override async Task<bool> HasAccessAsync(IAccessControlService accessControl, string userId)
        {
            foreach (var procedure in _procedures)
            {
                if (await accessControl.HasAccessAsync(userId, procedure))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
