using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rincon.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingrese un nombre para la categoría")]
        [Display(Name = "Nombre de la categoría")]
        public string Name { get; set; }

        [Display(Name = "Fecha de creación")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Estado")]
        public Boolean isActive { get; set; } = true;

    }
}
