using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;
using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
        : IUserService
    {
        public async Task<IEnumerable<object>> GetAllUsersAsync(CancellationToken cancellationToken)
        {
            var users = await userManager.Users.ToListAsync(cancellationToken);
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    id = user.Id,
                    username = user.UserName,
                    name = user.Name,
                    department = user.Department,
                    role = string.Join(", ", roles),
                    isActive = user.IsActive,
                    createdDate = user.CreatedDate.ToString("MMM dd, yyyy"),
                    modifiedDate = user.ModifiedDate?.ToString("MMM dd, yyyy") ?? "N/A",
                    modifiedBy = user.ModifiedBy ?? "N/A"
                });
            }

            return userList;
        }

        public async Task<ServiceResult<object>> GetUserByIdAsync(string id, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult<object>.Failure("User not found.", ServiceResultStatus.NotFound);
            }

            var roles = await userManager.GetRolesAsync(user);
            var userData = new
            {
                id = user.Id,
                username = user.UserName,
                name = user.Name,
                department = user.Department,
                role = roles.FirstOrDefault(),
                isActive = user.IsActive
            };

            return ServiceResult<object>.Success(userData);
        }

        public async Task<ServiceResult> UpsertUserAsync(UserUpsertDto model, string currentUsername, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Id))
                {
                    if (string.IsNullOrWhiteSpace(model.Password))
                    {
                        return ServiceResult.Failure("Password is required for new users.");
                    }

                    var newUser = new ApplicationUser
                    {
                        UserName = model.Username,
                        Name = model.Name.ToUpper(),
                        Department = model.Department,
                        IsActive = model.IsActive,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                    };

                    var result = await userManager.CreateAsync(newUser, model.Password);
                    if (!result.Succeeded)
                    {
                        return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
                    }

                    var roleResult = await userManager.AddToRoleAsync(newUser, model.Role);
                    if (!roleResult.Succeeded)
                    {
                        await userManager.DeleteAsync(newUser);
                        return ServiceResult.Failure(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }

                    await RecordAuditAsync(currentUsername, $"Created new user: {model.Username} with role {model.Role}", cancellationToken);
                    return ServiceResult.Success("User created successfully.");
                }
                else
                {
                    var user = await userManager.FindByIdAsync(model.Id);
                    if (user == null)
                    {
                        return ServiceResult.Failure("User not found.", ServiceResultStatus.NotFound);
                    }

                    var changes = new List<string>();
                    if (user.Name != model.Name)
                    {
                        changes.Add($"Name: {user.Name} → {model.Name}");
                    }

                    if (user.Department != model.Department)
                    {
                        changes.Add($"Department: {user.Department} → {model.Department}");
                    }

                    if (user.IsActive != model.IsActive)
                    {
                        changes.Add($"Status: {(user.IsActive ? "Active" : "Inactive")} → {(model.IsActive ? "Active" : "Inactive")}");
                    }

                    var currentRoles = await userManager.GetRolesAsync(user);
                    if (!currentRoles.Contains(model.Role))
                    {
                        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded)
                        {
                            return ServiceResult.Failure(string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                        }

                        var addResult = await userManager.AddToRoleAsync(user, model.Role);
                        if (!addResult.Succeeded)
                        {
                            if (currentRoles.Any())
                            {
                                await userManager.AddToRolesAsync(user, currentRoles);
                            }

                            return ServiceResult.Failure(string.Join(", ", addResult.Errors.Select(e => e.Description)));
                        }
                        changes.Add($"Role: {currentRoles.FirstOrDefault()} → {model.Role}");
                    }

                    user.Name = model.Name.ToUpper();
                    user.Department = model.Department;
                    user.IsActive = model.IsActive;
                    user.ModifiedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    user.ModifiedBy = currentUsername;

                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        return ServiceResult.Failure(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }

                    if (changes.Any())
                    {
                        await RecordAuditAsync(currentUsername, $"Updated user {model.Username}: {string.Join("; ", changes)}", cancellationToken);
                    }

                    return ServiceResult.Success("User updated successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error upserting user.");
                return ServiceResult.Failure($"Failed to save user: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> ToggleUserStatusAsync(string id, string currentUsername, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.Failure("User not found.", ServiceResultStatus.NotFound);
            }

            if (string.Equals(user.UserName, currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Failure("You cannot deactivate your own account.");
            }

            user.IsActive = !user.IsActive;
            user.ModifiedDate = DateTimeHelper.GetCurrentPhilippineTime();
            user.ModifiedBy = currentUsername;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure("Failed to update user status.");
            }

            var action = user.IsActive ? "activated" : "deactivated";
            await RecordAuditAsync(currentUsername, $"User {user.UserName} {action}", cancellationToken);

            return ServiceResult.Success($"User {action} successfully.");
        }

        public async Task<ServiceResult> ResetPasswordAsync(PasswordResetDto model, string currentUsername, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return ServiceResult.Failure("User not found.", ServiceResultStatus.NotFound);
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            user.ModifiedDate = DateTimeHelper.GetCurrentPhilippineTime();
            user.ModifiedBy = currentUsername;
            await userManager.UpdateAsync(user);

            await RecordAuditAsync(currentUsername, $"Password reset for user {user.UserName}", cancellationToken);

            return ServiceResult.Success("Password reset successfully.");
        }

        public async Task<IEnumerable<object>> GetRolesAsync(CancellationToken cancellationToken)
        {
            return await roleManager.Roles
                .Select(r => new { text = r.Name, value = r.Name })
                .ToListAsync(cancellationToken);
        }

        private async Task RecordAuditAsync(string username, string activity, CancellationToken cancellationToken)
        {
            var audit = new AuditTrail(username, activity, "User Management");
            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
        }
    }
}
