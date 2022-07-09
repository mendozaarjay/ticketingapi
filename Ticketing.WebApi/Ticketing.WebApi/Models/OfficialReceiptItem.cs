namespace Ticketing.WebApi.Models
{
    public class OfficialReceiptItem
    {
        public int Id { get; set; }
        public string ORNumber { get; set; }
        public string Gate { get; set; }
        public string PlateNo { get; set; }
        public string PaymentDate { get; set; }
    }
}