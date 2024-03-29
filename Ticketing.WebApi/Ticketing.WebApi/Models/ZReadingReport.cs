public class ZReadingReport
{
    public string ReportDate { get; set; }
    public string ReportTime { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public string BeginningSINo { get; set; }
    public string EndingSINo { get; set; }
    public string BeginningVoidNo { get; set; }
    public string EndingVoidNo { get; set; }
    public string BeginningReturnNo { get; set; }
    public string EndingReturnNo { get; set; }
    public int ResetCount { get; set; }
    public int ZCount { get; set; }
    public decimal PreviousAccumulatedSales { get; set; }
    public decimal PresentAccumulatedSales { get; set; }
    public decimal TodaySales { get; set; }
    public decimal VatableSales { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatExemptSales { get; set; }
    public decimal ZeroRatedSales { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal LessDiscount { get; set; }
    public decimal LessReturn { get; set; }
    public decimal LessVoid { get; set; }
    public decimal LessVatAdjustment { get; set; }
    public decimal NetAmount { get; set; }
    public decimal SCDiscount { get; set; }
    public decimal PWDDiscount { get; set; }
    public decimal NAACDiscount { get; set; }
    public decimal SoloDiscount { get; set; }
    public decimal OtherDiscount { get; set; }
    public decimal VoidAdjustment { get; set; }
    public decimal ReturnAdjustment { get; set; }
    public decimal SCVatAdjustment { get; set; }
    public decimal PWDVatAdjustment { get; set; }
    public decimal RegularDiscountVatAdjustment { get; set; }
    public decimal VatOnReturnVatAdjustment { get; set; }
    public decimal OtherVatAdjustment { get; set; }
    public decimal CashInDrawer { get; set; }
    public decimal Cashless { get; set; }
    public decimal Cheque { get; set; }
    public decimal CreditCard { get; set; }
    public decimal GiftCertificate { get; set; }
    public decimal OpeningFund { get; set; }
    public decimal LessWithdrawal { get; set; }
    public decimal PaymentReceived { get; set; }
    public decimal ShortOver { get; set; }
}