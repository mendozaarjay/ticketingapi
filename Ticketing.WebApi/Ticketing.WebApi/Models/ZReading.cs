namespace Ticketing.WebApi.Models
{
    public class ZReading
    {
        public string Location { get; set; }
        public string Terminal { get; set; }
        public string TimeIn { get; set; }
        public string TimeOut { get; set; }
        public string ParkerIn { get; set; }
        public string ParkerOut { get; set; }
        public string ReservedIn { get; set; }
        public string ReservedOut { get; set; }
        public string RateType { get; set; }
        public string Count { get; set; }
        public string Amount { get; set; }
        public string OvernightCount { get; set; }
        public string OvernightAmount { get; set; }
        public string LostCardCount { get; set; }
        public string LostCardAmount { get; set; }
        public string NewRT { get; set; }
        public string OldRT { get; set; }
        public string NewFT { get; set; }
        public string OldFT { get; set; }
        public string NewFR { get; set; }
        public string OldFR { get; set; }
        public string NewOR { get; set; }
        public string OldOR { get; set; }
        public string OldGrandSales { get; set; }
        public string TodaySales { get; set; }
        public string NewGrandSales { get; set; }
        public string VatableSales { get; set; }
        public string VatAmount { get; set; }
        public string Total { get; set; }
        public string ZCount { get; set; }
        public string Reset { get; set; }
        public string PreparedBy { get; set; }
    }
}