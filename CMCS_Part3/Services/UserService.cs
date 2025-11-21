using CMCS_Part3.Models;
using Microsoft.AspNetCore.Identity;

namespace CMCS_Part3.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager; //[2]

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager; //[2]
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId); //[2]
        }

        public async Task<List<ApplicationUser>> GetUsersByRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role); //[2]
            return users.ToList(); //[1]
        }

        public async Task<string?> GetUserRoleAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId); //[2]
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user); //[2]
                return roles.FirstOrDefault(); //[1]
            }
            return null;
        }
    }
}

/*
[1] Microsoft Docs. "C# Programming Guide." https://learn.microsoft.com/en-us/dotnet/csharp/
[2] Microsoft Docs. "ASP.NET Core Identity." https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
*/