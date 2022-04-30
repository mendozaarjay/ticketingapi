namespace Ticketing.WebApi.Models
{
    public class CashierSummary
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Cashier { get; set; }
        public string Count { get; set; }
        public string TimeIn { get; set; }
        public string TimeOut { get; set; }
        public string Amount { get; set; }
    }
}