using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Rincon.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Nombre completo")]
        public string FullName { get; set; }

        [Display(Name = "DNI")]
        public string DNI { get; set; }

        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        [Display(Name = "Fecha de alta")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;
    }
}