namespace CMCS_Part3.Models
{
    public class CurrentUser
    {
        public int LecturerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsAuthenticated => LecturerId > 0;

        public bool IsLecturer => Role == UserRole.Lecturer;
        public bool IsApprover => Role == UserRole.ProgrammeCoordinator || Role == UserRole.AcademicManager;
        public bool IsHR => Role == UserRole.HR;
    }
}