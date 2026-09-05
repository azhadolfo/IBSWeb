// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IBS.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;

namespace IBSWeb.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        public List<CompanyPortalLink> CompanyPortalLinks { get; private set; } = [];

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            public string Username { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public class CompanyPortalLink
        {
            public string Name { get; set; }

            public string Url { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("/");
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            await LoadPageData(returnUrl);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Check if user exists and is active BEFORE attempting password sign in
                var user = await _signInManager.UserManager.FindByNameAsync(Input.Username);

                if (user != null && !user.IsActive)
                {
                    _logger.LogWarning("Deactivated user attempted login: {Username}", Input.Username);
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator.");
                    await LoadPageData(returnUrl);
                    return Page();
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    await LoadPageData(returnUrl);
                    return Page();
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // User is guaranteed to exist and be active at this point
                    user = await _signInManager.UserManager.FindByNameAsync(Input.Username);

                    var existingClaims = await _signInManager.UserManager.GetClaimsAsync(user);

                    if (!existingClaims.Any(claim => claim.Type == "Company"))
                    {
                        await _signInManager.UserManager.AddClaimAsync(user, new Claim("Company", "Filpride"));
                    }

                    await _signInManager.SignInAsync(user, Input.RememberMe);

                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            await LoadPageData(returnUrl);
            return Page();
        }



        private async Task LoadPageData(string returnUrl)
        {
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
            CompanyPortalLinks = _configuration
                .GetSection("CompanyPortalLinks")
                .Get<List<CompanyPortalLink>>()?
                .Where(link =>
                    !string.IsNullOrWhiteSpace(link.Name) &&
                    Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .ToList() ?? [];
        }

    }
}
