using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.Filpride.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IBS.Utility.Helpers;
using System.ComponentModel.DataAnnotations;

namespace IBSWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userList = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userList.Add(new
                    {
                        id = user.Id,
                        username = user.UserName,
                        name = user.Name,
                        department = user.Department,
                        role = string.Join(", ", roles),
                        stationAccess = user.StationAccess ?? "N/A",
                        isActive = user.IsActive,
                        createdDate = user.CreatedDate.ToString("MMM dd, yyyy"),
                        modifiedDate = user.ModifiedDate?.ToString("MMM dd, yyyy") ?? "N/A",
                        modifiedBy = user.ModifiedBy ?? "N/A"
                    });
                }

                return Json(new { data = userList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users list. Error: {ErrorMessage}", ex.Message);
                return Json(new { data = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return Json(new { success = false, message = "Invalid user id" });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userData = new
                {
                    id = user.Id,
                    username = user.UserName,
                    name = user.Name,
                    department = user.Department,
                    role = roles.FirstOrDefault(),
                    stationAccess = user.StationAccess,
                    isActive = user.IsActive
                };

                return Json(new { success = true, data = userData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user. Error: {ErrorMessage}", ex.Message);
                return Json(new { success = false, message = "Error retrieving user data" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert([FromBody] UserUpsertModel model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request payload" });

            if (string.IsNullOrWhiteSpace(model.Username) ||
                string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Department) ||
                string.IsNullOrWhiteSpace(model.Role))
            {
                return Json(new { success = false, message = "Missing required fields" });
            }

            if (string.IsNullOrEmpty(model.Id) && string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { success = false, message = "Password is required for new users" });
            }

            try
            {
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "System";
                var company = User.FindFirstValue("Company") ?? "System";

                if (string.IsNullOrEmpty(model.Id))
                {
                    // CREATE NEW USER
                    var newUser = new ApplicationUser
                    {
                        UserName = model.Username,
                        Name = model.Name.ToUpper(),
                        Department = model.Department,
                        StationAccess = model.StationAccess,
                        IsActive = model.IsActive,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                    };

                    var result = await _userManager.CreateAsync(newUser, model.Password!);

                    if (result.Succeeded)
                    {
                        var addRoleResult = await _userManager.AddToRoleAsync(newUser, model.Role);
                        if (!addRoleResult.Succeeded)
                        {
                            // If role assignment failed, remove the newly created user and return error
                            await _userManager.DeleteAsync(newUser);
                            var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                            return Json(new { success = false, message = errors, errors = addRoleResult.Errors.Select(e => e.Description) });
                        }

                        // Audit Trail
                        await LogAuditTrail(
                            currentUser,
                            $"Created new user: {model.Username} with role {model.Role}",
                            "User Management",
                            company
                        );

                        var safeUsername = (model.Username ?? string.Empty)
                            .Replace("\r", string.Empty)
                            .Replace("\n", string.Empty);

                        _logger.LogInformation("User {Username} created successfully by {CurrentUser}", safeUsername, currentUser);
                        return Json(new { success = true, message = "User created successfully" });
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = errors });
                    }
                }
                else
                {
                    // UPDATE EXISTING USER
                    var user = await _userManager.FindByIdAsync(model.Id);
                    if (user == null)
                    {
                        return Json(new { success = false, message = "User not found" });
                    }

                    // Track changes for audit
                    var changes = new List<string>();
                    if (user.Name != model.Name) changes.Add($"Name: {user.Name} → {model.Name}");
                    if (user.Department != model.Department) changes.Add($"Department: {user.Department} → {model.Department}");
                    if (user.StationAccess != model.StationAccess) changes.Add($"Station: {user.StationAccess ?? "None"} → {model.StationAccess ?? "None"}");
                    if (user.IsActive != model.IsActive) changes.Add($"Status: {(user.IsActive ? "Active" : "Inactive")} → {(model.IsActive ? "Active" : "Inactive")}");

                    // Update role if changed
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (!currentRoles.Contains(model.Role))
                    {
                        // Remove existing roles
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded)
                        {
                            var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                            return Json(new { success = false, message = errors, errors = removeResult.Errors.Select(e => e.Description) });
                        }

                        // Add new role
                        var addRoleResult = await _userManager.AddToRoleAsync(user, model.Role);
                        if (!addRoleResult.Succeeded)
                        {
                            // Try to restore previous roles if possible
                            if (currentRoles.Any())
                            {
                                var restoreResult = await _userManager.AddToRolesAsync(user, currentRoles);
                                if (!restoreResult.Succeeded)
                                {
                                    _logger.LogError("Failed to restore roles for user {Username} after AddToRole failure: {Errors}", user.UserName, string.Join(", ", restoreResult.Errors.Select(e => e.Description)));
                                }
                            }

                            var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                            return Json(new { success = false, message = errors, errors = addRoleResult.Errors.Select(e => e.Description) });
                        }

                        changes.Add($"Role: {currentRoles.FirstOrDefault()} → {model.Role}");
                    }

                    // Update user properties
                    user.Name = model.Name.ToUpper();
                    user.Department = model.Department;
                    user.StationAccess = model.StationAccess;
                    user.IsActive = model.IsActive;
                    user.ModifiedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    user.ModifiedBy = currentUser;

                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        if (changes.Any())
                        {
                            await LogAuditTrail(
                                currentUser,
                                $"Updated user {model.Username}: {string.Join("; ", changes)}",
                                "User Management",
                                company
                            );
                        }
                        var safeUsername = (model.Username ?? string.Empty)
                            .Replace("\r", string.Empty)
                            .Replace("\n", string.Empty);

                        _logger.LogInformation("User {safeUsername} updated successfully by {CurrentUser}", safeUsername, currentUser);
                        return Json(new { success = true, message = "User updated successfully" });
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = errors });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user. Error: {ErrorMessage}", ex.Message);
                return Json(new { success = false, message = "An error occurred while saving the user" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return Json(new { success = false, message = "Invalid user id" });
                }
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "System";
                var company = User.FindFirstValue("Company") ?? "System";
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Prevent admin from deactivating themselves
                if (string.Equals(user.UserName, currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "You cannot deactivate your own account" });
                }
                user.IsActive = !user.IsActive;
                user.ModifiedDate = DateTimeHelper.GetCurrentPhilippineTime();
                user.ModifiedBy = currentUser;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    var action = user.IsActive ? "activated" : "deactivated";
                    await LogAuditTrail(
                        currentUser,
                        $"User {user.UserName} {action}",
                        "User Management",
                        company
                    );

                    var safeUsername = (user.UserName ?? string.Empty)
                        .Replace("\r", string.Empty)
                        .Replace("\n", string.Empty);
                        
                    _logger.LogInformation("User {safeUsername} {Action} by {CurrentUser}", safeUsername, action, currentUser);
                    return Json(new { success = true, message = $"User {action} successfully" });
                }

                return Json(new { success = false, message = "Failed to update user status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status. Error: {ErrorMessage}", ex.Message);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetModel model)
        {
            try
            {
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "System";
                var company = User.FindFirstValue("Company") ?? "System";
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Remove old password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    user.ModifiedDate = DateTime.Now;
                    user.ModifiedBy = currentUser;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to update audit fields for user {Username} after password reset", user.UserName);
                    }

                    await LogAuditTrail(
                        currentUser,
                        $"Password reset for user {user.UserName}",
                        "User Management",
                        company
                    );

                    var safeUsername = (user.UserName ?? string.Empty)
                        .Replace("\r", string.Empty)
                        .Replace("\n", string.Empty);

                    _logger.LogInformation("Password reset for user {Username} by {CurrentUser}", safeUsername, currentUser);
                    return Json(new { success = true, message = "Password reset successfully" });
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password. Error: {ErrorMessage}", ex.Message);
                return Json(new { success = false, message = "An error occurred while resetting password" });
            }
        }

        #endregion

        #region HELPER METHODS

        private async Task LogAuditTrail(string username, string activity, string documentType, string company)
        {
            var auditTrail = new FilprideAuditTrail(username, activity, documentType, company);
            await _dbContext.FilprideAuditTrails.AddAsync(auditTrail);
            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region VIEW DATA

        [HttpGet]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles
                // .Where(r => r.Name != "Admin") // Exclude Admin role
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                })
                .ToList();

            return Json(roles);
        }

        [HttpGet]
        public async Task<IActionResult> GetStations()
        {
            var stations = await _dbContext.MobilityStations
                .Where(s => s.IsActive)
                .OrderBy(s => s.StationId)
                .Select(s => new SelectListItem
                {
                    Value = s.StationCode,
                    Text = $"{s.StationCode} {s.StationName}"
                })
                .ToListAsync();

            return Json(stations);
        }

        #endregion
    }

    #region MODELS

    public class UserUpsertModel
    {
        public string? Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Department { get; set; }
        public string? StationAccess { get; set; }
        [Required]
        public string Role { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; }
    }

    public class PasswordResetModel
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }

    #endregion
}