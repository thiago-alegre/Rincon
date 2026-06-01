using System.ComponentModel.DataAnnotations;

namespace Rincon.Models
{
    public class PersonalAccount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre completo")]
        [Display(Name = "Nombre completo")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Ingrese el DNI")]
        [Display(Name = "DNI")]
        public string DNI { get; set; }

        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        [Display(Name = "Teléfono")]
        public string? Phone { get; set; }

        [Display(Name = "Fecha de alta")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Estado")]
        public bool isActive { get; set; } = true;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
