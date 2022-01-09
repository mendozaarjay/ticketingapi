namespace Ticketing.WebApi.Models
{
    public class ParkerType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Vat { get; set; }
        public bool IsDefault { get; set; }
        public int GracePeriod { get; set; }
    }
}