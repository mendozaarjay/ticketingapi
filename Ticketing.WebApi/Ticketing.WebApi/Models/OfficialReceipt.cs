namespace Ticketing.WebApi.Models
{
    public class OfficialReceipt
    {
        public string Id { get; set; }
        public string Company { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string TIN { get; set; }
        public string AccreditationNo { get; set; }
        public string AccreditationDate     { get; set; }
        public string AccreditationValidUntil { get; set; }
        public string PTUNo { get; set; }
        public string PTUDateIssued { get; set; }
        public string OrNumber { get; set; }
        public string TicketNo { get; set; }
        public string PlateNo { get; set; }
        public string Location { get; set; }
        public string Terminal { get; set; }
        public string CashierName { get; set; }
        public string TimeIn { get; set; }
        public string TimeOut { get; set; }
        public string Duration { get; set; }
        public string TotalWithVaT { get; set; }
        public string Vat { get; set; }
        public string Subtotal { get; set; }
        public string Discount { get; set; }
        public string TenderType { get; set; }
        public string TotalAmountDue { get; set; }
        public string AmountTendered { get; set; }
        public string Change { get; set; }
        public string VatableSales { get; set; }
        public string VatAmount { get; set; }
        public string VatExempt { get; set; }
        public string ZeroRated { get; set; }
        public string Printable { get; set; }
        public string RateName { get; set; }


    }
}