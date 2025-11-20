namespace CMCS_Part3.Models
{
    public static class UserRole
    {
        public const string Lecturer = "Lecturer";
        public const string ProgrammeCoordinator = "ProgrammeCoordinator";
        public const string AcademicManager = "AcademicManager";
        public const string HR = "HR";

        public static List<string> GetAllRoles()
        {
            return new List<string> { Lecturer, ProgrammeCoordinator, AcademicManager, HR };
        }
    }
}