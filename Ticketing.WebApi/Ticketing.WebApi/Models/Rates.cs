namespace Ticketing.WebApi.Models
{
    public class Rates
    {
        public int Id { get; set; }
        public string RateName { get; set; }
        public int Minutes { get; set; }
        public decimal Amount { get; set; }
        public int Repeat { get; set; }
    }
}