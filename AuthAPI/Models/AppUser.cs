using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Models
{
    public class AppUser : IdentityUser
    {
        // IdentityUser already gives us:
        // Id, UserName, Email, PasswordHash
        // We don't need to add anything extra
    }
}