using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rincon.Models.ViewModels
{
    public class SalesIndexVM
    {
        public string? UserId { get; set; }
        public DateTime? SaleDate { get; set; }
        public IEnumerable<SelectListItem> UserList { get; set; } = new List<SelectListItem>();
    }
}
