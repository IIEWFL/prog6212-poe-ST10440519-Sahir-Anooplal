using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CMCS_Part3.Models
{
    public class Lecturer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Lecturer name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        public List<Claim> Claims { get; set; } = new List<Claim>();
    }
}