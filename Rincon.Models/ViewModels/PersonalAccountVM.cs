using Rincon.Utilities.Enums;

namespace Rincon.Models.ViewModels
{
    public class PersonalAccountDetailVM
    {
        public PersonalAccount Account { get; set; } = new();
        public IEnumerable<Sale> Sales { get; set; } = new List<Sale>();
        public IEnumerable<PersonalAccountPayment> Payments { get; set; } = new List<PersonalAccountPayment>();
        public decimal CurrentDebt { get; set; }
        public DateTime? DebtSince { get; set; }
    }

    public class PersonalAccountSettleVM
    {
        public int Id { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? AmountText { get; set; }
        public string? Notes { get; set; }
    }
}
