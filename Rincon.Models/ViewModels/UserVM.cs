using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Rincon.Models.ViewModels
{
    public class UserVM
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [Display(Name = "Nombre completo")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [Display(Name = "DNI")]
        public string DNI { get; set; }

        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol")]
        public string Role { get; set; }

        public IEnumerable<SelectListItem>? RoleList { get; set; }

        public bool CanChangePassword { get; set; } = true;

        public string? PasswordProtectionMessage { get; set; }
    }
}
