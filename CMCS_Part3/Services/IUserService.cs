using CMCS_Part3.Models;

namespace CMCS_Part3.Services
{
    public interface IUserService
    {
        CurrentUser? GetCurrentUser();
        void SetCurrentUser(int lecturerId, string role);
        void ClearCurrentUser();
        List<Lecturer> GetAllLecturers();
        bool CanAccessClaims();
        bool CanAccessApproval();
        bool CanAccessHR();
    }
}