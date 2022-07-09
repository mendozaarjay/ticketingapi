namespace Ticketing.WebApi.Models
{
    public class TenderDeclarationValue
    {
        public int Id { get; set; }
        public decimal ValueFor1000 { get; set; }
        public decimal ValueFor500 { get; set; }
        public decimal ValueFor200 { get; set; }
        public decimal ValueFor100 { get; set; }
        public decimal ValueFor50 { get; set; }
        public decimal ValueFor20 { get; set; }
        public decimal ValueFor10 { get; set; }
        public decimal ValueFor5 { get; set; }
        public decimal ValueFor1 { get; set; }
        public decimal ValueForCent { get; set; }
        public string Comment { get; set; }
    }
}