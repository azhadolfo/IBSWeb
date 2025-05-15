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

namespace IBSWeb.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
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

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        public List<SelectListItem> Stations { get; set; }

        public List<SelectListItem> Companies { get; set; }

        public List<SelectListItem> Users { get; set; }
        public List<SelectListItem> StationAccess { get; set; }

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

            [Required]
            [Display(Name = "Company")]
            public string Company { get; set; }

            [Display(Name = "Station")]
            public string StationCode { get; set; }
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
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _signInManager.UserManager.FindByNameAsync(Input.Username);
                    var existingClaims = await _signInManager.UserManager.GetClaimsAsync(user);

                    var existingStationCodeClaim = existingClaims.FirstOrDefault(c => c.Type == "StationCode");

                    if (existingStationCodeClaim != null)
                    {
                        await _signInManager.UserManager.RemoveClaimAsync(user, existingStationCodeClaim);
                    }

                    if (Input.StationCode != null)
                    {
                        var newStationCodeClaim = new Claim("StationCode", Input.StationCode);
                        await _signInManager.UserManager.AddClaimAsync(user, newStationCodeClaim);
                    }

                    var existingCompanyClaim = existingClaims.FirstOrDefault(c => c.Type == "Company");

                    if (existingCompanyClaim != null)
                    {
                        await _signInManager.UserManager.RemoveClaimAsync(user, existingCompanyClaim);
                    }

                    var newCompanyClaim = new Claim("Company", Input.Company);
                    await _signInManager.UserManager.AddClaimAsync(user, newCompanyClaim);

                    await _signInManager.RefreshSignInAsync(user);

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
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    await LoadPageData(returnUrl); // Reload data when the form is redisplayed
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            await LoadPageData(returnUrl); // Reload data when the form is redisplayed
            return Page();
        }

        private async Task LoadPageData(string returnUrl)
        {
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Stations = await _unitOfWork.GetMobilityStationListAsyncByCode();
            Companies = await _unitOfWork.GetCompanyListAsyncByName();
            Users = await _unitOfWork.GetCashierListAsyncByUsernameAsync();
            StationAccess = await _unitOfWork.GetCashierListAsyncByStationAsync();
            ReturnUrl = returnUrl;
        }

    }
}
