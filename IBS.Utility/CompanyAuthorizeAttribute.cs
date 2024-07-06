using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IBS.Utility
{
    public class CompanyAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string _company;

        public CompanyAuthorizeAttribute(string company)
        {
            _company = company;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var companyClaim = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Company")?.Value;

            if (!string.Equals(companyClaim, _company, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}