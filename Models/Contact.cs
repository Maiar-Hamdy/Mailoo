using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mailo.Models
{
    public class Contact
    {
        [Key]
        public int ID { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; }
        public ICollection<EmployeeContact> employeeContacts { get; set; }
    }
}
