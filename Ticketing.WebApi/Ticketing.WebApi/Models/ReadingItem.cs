namespace Ticketing.WebApi.Models
{
    public class ReadingItem
    {
        public int Id { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ReferenceNo { get; set; }
        public string Type { get; set; }
        public string Gate { get; set; }
        public string PerformedBy { get; set; }

    }
}