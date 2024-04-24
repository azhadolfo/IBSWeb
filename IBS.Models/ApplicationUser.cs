using Microsoft.AspNetCore.Identity;

namespace IBS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }

        public string Department { get; set; }
    }
}