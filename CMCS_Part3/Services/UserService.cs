using CMCS_Part3.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS_Part3.Services
{
    public class UserService : IUserService
    {
        private readonly CMCSDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionUserKey = "CurrentUser";

        public UserService(CMCSDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public CurrentUser? GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session.GetString(SessionUserKey) is string userData)
            {
                return System.Text.Json.JsonSerializer.Deserialize<CurrentUser>(userData);
            }
            return null;
        }

        public void SetCurrentUser(int lecturerId, string role)
        {
            var lecturer = _context.Lecturers.FirstOrDefault(l => l.Id == lecturerId);
            if (lecturer != null)
            {
                var currentUser = new CurrentUser
                {
                    LecturerId = lecturerId,
                    Name = lecturer.Name,
                    Email = lecturer.Email,
                    Role = role
                };

                var userData = System.Text.Json.JsonSerializer.Serialize(currentUser);
                _httpContextAccessor.HttpContext?.Session.SetString(SessionUserKey, userData);
            }
        }

        public void ClearCurrentUser()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(SessionUserKey);
        }

        public List<Lecturer> GetAllLecturers()
        {
            return _context.Lecturers.Where(l => l.IsActive).OrderBy(l => l.Name).ToList();
        }

        public bool CanAccessClaims()
        {
            var user = GetCurrentUser();
            return user?.IsLecturer == true;
        }

        public bool CanAccessApproval()
        {
            var user = GetCurrentUser();
            return user?.IsApprover == true;
        }

        public bool CanAccessHR()
        {
            var user = GetCurrentUser();
            return user?.IsHR == true;
        }
    }
}