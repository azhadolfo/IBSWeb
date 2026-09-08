using System.ComponentModel.DataAnnotations;

namespace IBS.DTOs.Msap
{
    public class UserUpsertDto
    {
        public string? Id { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Department { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;

        public string? Password { get; set; }

        public bool IsActive { get; set; }
    }

    public class PasswordResetDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string NewPassword { get; set; } = null!;
    }
}
