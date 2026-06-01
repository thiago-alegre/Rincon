namespace Rincon.Models.ViewModels
{
    public class PersonalAccountDetailVM
    {
        public PersonalAccount Account { get; set; } = new();
        public IEnumerable<Sale> Sales { get; set; } = new List<Sale>();
        public decimal CurrentDebt { get; set; }
        public DateTime? DebtSince { get; set; }
    }
}
