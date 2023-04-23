namespace Ticketing.WebApi.Models
{
    public class YReading
    {
        public string Location { get; set; }
        public string Terminal { get; set; }
        public string TimeIn { get; set; }
        public string TimeOut { get; set; }
        public string ParkerIn { get; set; }
        public string ParkerOut { get; set; }
        public string ParkerRemaining { get; set; }
        public string ReservedIn { get; set; }
        public string ReservedOut { get; set; }
        public string ReservedRemaining { get; set; } 
        public string RateType { get; set; }
        public string Count { get; set; }
        public string Amount { get; set; }
        public string OvernightCount { get; set; }
        public string OvernightAmount { get; set; }
        public string LostCardCount { get; set; }
        public string LostCardAmount { get; set; }
        public string TotalTransaction { get; set; }
        public string TotalPartial { get; set; }
        public string TotalTendered { get; set; }
        public string TotalVariance { get; set; }
    }
}