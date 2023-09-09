public class YReadingReport
{
    public string ReportDate { get; set; }
    public string ReportTime { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public string BeginningOR { get; set; }
    public string EndingOR { get; set; }
    public decimal OpeningFund { get; set; }
    public decimal CashPayment { get; set; }
    public decimal CashlessPayment { get; set; }
    public decimal ChequePayment { get; set; }
    public decimal CreditCardPayment { get; set; }
    public decimal Void { get; set; }
    public decimal Refund { get; set; }
    public decimal Withdrawal { get; set; }
    public decimal CashSummary { get; set; }
    public decimal CashlessSummary { get; set; }
    public decimal ChequeSummary { get; set; }
    public decimal CreditCardSummary { get; set; }
    public decimal OpeningFundSummary { get; set; }
    public decimal LessWithdrawal { get; set; }
    public decimal PaymentReceivedSummary { get; set; }
    public decimal ShortOver { get; set; }
}