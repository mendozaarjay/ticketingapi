namespace Ticketing.WebApi.Models
{
    public class Header
    {
        public string Company { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string TIN { get; set; }
        public string AccreditationNo { get; set; }
        public string AccreditationDate { get; set; }
        public string AccreditationValidUntil { get; set; }
        public string PTUNo { get; set; }
        public string PTUDateIssued { get; set; }
    }
}