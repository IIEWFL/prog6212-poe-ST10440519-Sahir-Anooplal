using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<List<ApplicationUser>> GetUsersByRoleAsync(string role);
        Task<string?> GetUserRoleAsync(string userId);
    }
}