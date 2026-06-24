namespace Rincon.Models.ViewModels
{
    public class CashRegisterVM
    {
        public CashRegisterSession? OpenSession { get; set; }
        public IEnumerable<CashRegisterSession> Sessions { get; set; } = new List<CashRegisterSession>();
        public CashRegisterSummaryVM CurrentSummary { get; set; } = new();
        public bool CanViewAllSessions { get; set; }
    }

    public class CashRegisterSummaryVM
    {
        public decimal CashSales { get; set; }
        public decimal TransferSales { get; set; }
        public decimal PersonalAccountSales { get; set; }
        public decimal PersonalAccountCashPayments { get; set; }
        public decimal PersonalAccountTransferPayments { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CashReturns { get; set; }
        public decimal TransferReturns { get; set; }
        public decimal PersonalAccountReturns { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetTotal { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ExchangeLoss { get; set; }
        public decimal CashExchangeLoss { get; set; }
        public decimal TransferExchangeLoss { get; set; }
        public decimal PersonalAccountExchangeLoss { get; set; }
        public int SalesCount { get; set; }
        public int ReturnsCount { get; set; }
        public int ExchangesCount { get; set; }
    }

    public class CashRegisterOpenVM
    {
        public string? OpeningAmountText { get; set; }
    }

    public class CashRegisterCloseVM
    {
        public int Id { get; set; }
        public string? CountedCashAmountText { get; set; }
        public string? Notes { get; set; }
    }
}
