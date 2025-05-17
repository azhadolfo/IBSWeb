using IBS.DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IBS.Services.Attributes
{
    public class DepartmentAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _departments;

        public DepartmentAuthorizeAttribute(params string[] departments)
        {
            _departments = departments;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userManager = context.HttpContext.RequestServices.GetService(typeof(UserManager<IdentityUser>)) as UserManager<IdentityUser>;
            var dbContext = context.HttpContext.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;

            if (userManager != null && dbContext != null)
            {
                var user = userManager.GetUserAsync(context.HttpContext.User).Result;

                // Assuming "Department" is a property in your ApplicationUser model
                var userDepartment = dbContext.ApplicationUsers
                    .Where(u => u.Id == user!.Id)
                    .Select(u => u.Department)
                    .FirstOrDefault();

                if (userDepartment == null || !_departments.Contains(userDepartment))
                {
                    context.Result = new ForbidResult();
                }
            }
            else
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
