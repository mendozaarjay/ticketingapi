namespace Ticketing.WebApi.Models
{
    public class ParkerTicket
    {
        public string Id { get; set; }
        public string Company { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string TIN { get; set; }
        public string PlateNo { get; set; }
        public string TicketNo { get; set; }
        public string TimeIn { get; set; }
        public string Terminal { get; set; }
        public string Location { get; set; }
        public string Printable { get; set; }
    }
}