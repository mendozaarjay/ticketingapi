using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Security;
using Ticketing.WebApi.Models;
using WebGrease.Css.Visitor;

namespace Ticketing.WebApi.Services
{
    public class DataServices
    {
        public string UserConnection { get; set; }
        public bool UseEncryption { get; set; }

        public async Task<List<Rates>> GetRates(string rate)
        {
            var sql = @"SELECT [rl].[RateName],
                   ROW_NUMBER() OVER (ORDER BY [rd].[ApplyIndex]) AS [Id],
                   [rd].[NumberOfMinutes],
                   [rd].[Amount],
                   [rd].[RepeatCount]
            FROM [dbo].[RatesDetails] [rd]
                INNER JOIN [dbo].[RatesList] [rl]
                    ON [rl].[RateID] = [rd].[RateID]
            WHERE [rl].[RateID] = " + rate;
            var result = await SCObjects.LoadDataTableAsync(sql, UserConnection);
            var items = new List<Rates>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new Rates
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        Amount = decimal.Parse(dr["Amount"].ToString()),
                        Minutes = int.Parse(dr["NumberOfMinutes"].ToString()),
                        Repeat = int.Parse(dr["RepeatCount"].ToString()),
                        RateName = dr["RateName"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<DateTime> GetEntranceDate(int id)
        {
            var sql = $"SELECT [t].[EntranceDT] FROM [dbo].[Transits] [t] WHERE [t].[TransitID] = {id}";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            var date = DateTime.Parse(result);
            return date;
        }
        public async Task<List<ParkerType>> GetParkerTypes()
        {
            var sql = "EXEC [dbo].[spGetParkerTypes]";
            var result = await SCObjects.LoadDataTableAsync(sql, UserConnection);
            var items = new List<ParkerType>();

            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new ParkerType
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        Name = dr["Name"].ToString(),
                        Vat = decimal.Parse(dr["Vat"].ToString()),
                        IsDefault = dr["IsDefault"].Equals("1"),
                        GracePeriod = int.Parse(dr["GracePeriod"].ToString()),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<string> ComputeTransaction(int transitid, string gate, string parkertype, string tenderamount, string change, string totalamount, string userid, int discountid = 0, string discountamount = "", int cashlesstype = 0, string cashlessreference = "")
        {
            var sql = "[dbo].[spComputeTransaction]";
            var cmd = new SqlCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", transitid);
            cmd.Parameters.AddWithValue("@TimeOut", DateTime.Now);
            cmd.Parameters.AddWithValue("@PaymentGate", gate);
            cmd.Parameters.AddWithValue("@ParkerTypeId", parkertype);
            cmd.Parameters.AddWithValue("@TenderedAmount", tenderamount);
            cmd.Parameters.AddWithValue("@TotalAmount", totalamount);
            cmd.Parameters.AddWithValue("@Change", change);
            cmd.Parameters.AddWithValue("@UserId", userid);
            cmd.Parameters.AddWithValue("@DiscountType", discountid);
            cmd.Parameters.AddWithValue("@DiscountAmount", discountamount);
            cmd.Parameters.AddWithValue("@TransactionType", cashlesstype);
            cmd.Parameters.AddWithValue("@Reference", cashlessreference);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<LoginModel> IsValidUser(string username, string password, string gateid)
        {
            var sql = string.Empty;
            if (this.UseEncryption)
            {
                sql = $"EXEC [dbo].[spMobileCheckUser] @Username = '{username}', @Password = '{Security.Encrypt(password)}'";
            }
            else
            {
                sql = $"EXEC [dbo].[spMobileCheckUser] @Username = '{username}', @Password = '{password}'";
            }
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);

            if (result.Rows.Count <= 0)
            {
                return new LoginModel { IsValid = false };
            }
            var userid = result.Rows[0]["UserId"].ToString();
            var firstname = result.Rows[0]["FirstName"].ToString();
            var lastname = result.Rows[0]["LastName"].ToString();

            var name = firstname + " " + lastname;
            var checker = name.Replace(" ", "");

            if(int.Parse(userid) > 0)
            {
                await CheckFirstSignIn(int.Parse(userid), gateid);
            }

            return new LoginModel { IsValid = true, UserId = int.Parse(userid), UserName = username, Name = checker.Length > 0 ? name : username };
        }
        public async Task<string> CheckFirstSignIn(int userid, string gateid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spMobileSignIn]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@UserId", userid);
            cmd.Parameters.AddWithValue("@GateId", gateid);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<string> SignOut(int userid, int gateid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spMobileSignOut]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@UserId", userid);
            cmd.Parameters.AddWithValue("@GateId", gateid);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<ReadingResult> PerformXReading(int gateid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spPerformXReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateid);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            return new ReadingResult { Success = result.Rows[0]["Success"].ToString(), Returned = result.Rows[0]["Returned"].ToString() };
        }
        public async Task<OfficialReceipt> GetOfficialReceipt(int id)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetOfficialReceiptInformation]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", id);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var item = new OfficialReceipt();
            if (result != null)
            {
                var or = new OfficialReceipt
                {
                    Id = result.Rows[0]["TransitId"].ToString(),
                    Company = result.Rows[0]["CompanyName"].ToString(),
                    Address1 = result.Rows[0]["AddressLine1"].ToString(),
                    Address2 = result.Rows[0]["AddressLine2"].ToString(),
                    Address3 = result.Rows[0]["AddressLine3"].ToString(),
                    TIN = result.Rows[0]["VAT_REG_TIN"].ToString(),
                    AccreditationNo = result.Rows[0]["AccreditationNo"].ToString(),
                    AccreditationDate = result.Rows[0]["AccreditationDateIssued"].ToString(),
                    AccreditationValidUntil = result.Rows[0]["AccreditationValidUntil"].ToString(),
                    PTUNo = result.Rows[0]["PTUNo"].ToString(),
                    PTUDateIssued = result.Rows[0]["PTUDateIssued"].ToString(),
                    OrNumber = result.Rows[0]["ORNumber"].ToString(),
                    TicketNo = result.Rows[0]["TicketNo"].ToString(),
                    PlateNo = result.Rows[0]["PlateNo"].ToString(),
                    Location = result.Rows[0]["Location"].ToString(),
                    Terminal = result.Rows[0]["Terminal"].ToString(),
                    TimeIn = result.Rows[0]["TimeIn"].ToString(),
                    TimeOut = result.Rows[0]["TimeOut"].ToString(),
                    Duration = result.Rows[0]["Duration"].ToString(),
                    TotalWithVaT = result.Rows[0]["TotalWithVat"].ToString(),
                    Vat = result.Rows[0]["Vat"].ToString(),
                    Subtotal = result.Rows[0]["SubTotal"].ToString(),
                    Discount = result.Rows[0]["Discount"].ToString(),
                    DiscountName = result.Rows[0]["DiscountName"].ToString(),
                    TenderType = result.Rows[0]["TenderType"].ToString(),
                    TotalAmountDue = result.Rows[0]["TotalAmountDue"].ToString(),
                    AmountTendered = result.Rows[0]["AmountTendered"].ToString(),
                    Change = result.Rows[0]["Change"].ToString(),
                    VatableSales = result.Rows[0]["VatableSales"].ToString(),
                    VatAmount = result.Rows[0]["VatAmount"].ToString(),
                    VatExempt = result.Rows[0]["VatExempt"].ToString(),
                    ZeroRated = result.Rows[0]["ZeroRated"].ToString(),
                    CashierName = result.Rows[0]["CashierName"].ToString(),
                    RateName = result.Rows[0]["RateName"].ToString(),
                };
                item = or;
            }
            return item;
        }
        public async Task<Header> GetReportHeaderAsync(int gateId)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetReportHeader]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateId);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var item = new Header();
            if (result != null)
            {
                var header = new Header
                {
                    Company = result.Rows[0]["CompanyName"].ToString(),
                    Address1 = result.Rows[0]["AddressLine1"].ToString(),
                    Address2 = result.Rows[0]["AddressLine2"].ToString(),
                    Address3 = result.Rows[0]["AddressLine3"].ToString(),
                    TIN = result.Rows[0]["VAT_REG_TIN"].ToString(),
                    AccreditationNo = result.Rows[0]["AccreditationNo"].ToString(),
                    AccreditationDate = result.Rows[0]["AccreditationDateIssued"].ToString(),
                    AccreditationValidUntil = result.Rows[0]["AccreditationValidUntil"].ToString(),
                    PTUNo = result.Rows[0]["PTUNo"].ToString(),
                    PTUDateIssued = result.Rows[0]["PTUDateIssued"].ToString(),
                };
                item = header;
            }
            return item;
        }

        public async Task<List<XReading>> GetXReadingAsync(string srno)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetXReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<XReading>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new XReading
                    {
                        CashierName = dr["CashierName"].ToString(),
                        Location = dr["Location"].ToString(),
                        Terminal = dr["Terminal"].ToString(),
                        TimeIn = dr["TimeIn"].ToString(),
                        TimeOut = dr["TimeOut"].ToString(),
                        ParkerIn = dr["ParkerIn"].ToString(),
                        ParkerOut = dr["ParkerOut"].ToString(),
                        ReservedIn = dr["ReservedIn"].ToString(),
                        ReservedOut = dr["ReservedOut"].ToString(),
                        RateType = dr["RateType"].ToString(),
                        Count = dr["Count"].ToString(),
                        Amount = dr["Amount"].ToString(),
                        OvernightCount = dr["OvernightCount"].ToString(),
                        OvernightAmount = dr["OvernightAmount"].ToString(),
                        LostCardCount = dr["LostCardCount"].ToString(),
                        LostCardAmount = dr["LostCardAmount"].ToString(),
                        TotalTransaction = dr["TotalTransaction"].ToString(),
                        TotalPartial = dr["TotalPartial"].ToString(),
                        TotalTendered = dr["TotalTendered"].ToString(),
                        TotalVariance = dr["TotalVariance"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<List<CashlessType>> GetCashlessForXReading(string srno)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetCashlessForXReadings]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<CashlessType>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new CashlessType
                    {
                        RateType = dr["RateType"].ToString(),
                        Count = dr["Count"].ToString(),
                        Amount = dr["Amount"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }

        public async Task<string> PerformYReading(int gateid, int userid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spPerformYReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateid);
            cmd.Parameters.AddWithValue("@UserId", userid);
            var result = await SCObjects.ExecuteNonQueryWithReturnAsync(cmd, UserConnection);
            return result;
        }
        public async Task<List<YReading>> GetYReadingAsync(string srno)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetYReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<YReading>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new YReading
                    {
                        Location = dr["Location"].ToString(),
                        Terminal = dr["Terminal"].ToString(),
                        TimeIn = dr["TimeIn"].ToString(),
                        TimeOut = dr["TimeOut"].ToString(),
                        ParkerIn = dr["ParkerIn"].ToString(),
                        ParkerOut = dr["ParkerOut"].ToString(),
                        ReservedIn = dr["ReservedIn"].ToString(),
                        ReservedOut = dr["ReservedOut"].ToString(),
                        RateType = dr["RateType"].ToString(),
                        Count = dr["Count"].ToString(),
                        Amount = dr["Amount"].ToString(),
                        OvernightCount = dr["OvernightCount"].ToString(),
                        OvernightAmount = dr["OvernightAmount"].ToString(),
                        LostCardCount = dr["LostCardCount"].ToString(),
                        LostCardAmount = dr["LostCardAmount"].ToString(),
                        TotalTransaction = dr["TotalTransaction"].ToString(),
                        TotalPartial = dr["TotalPartial"].ToString(),
                        TotalTendered = dr["TotalTendered"].ToString(),
                        TotalVariance = dr["TotalVariance"].ToString(),
                        ParkerRemaining = dr["ParkerRemaining"].ToString(),
                        ReservedRemaining = dr["ReservedRemaining"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<List<CashlessType>> GetCashlessForYReading(string srno)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetCashlessForYReadings]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<CashlessType>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new CashlessType
                    {
                        RateType = dr["RateType"].ToString(),
                        Count = dr["Count"].ToString(),
                        Amount = dr["Amount"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<List<CashierSummary>> GetCashierSummaryForYReading(string srno)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spCashierSummaryForYReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<CashierSummary>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new CashierSummary
                    {
                        Id = dr["Id"].ToString(),
                        UserId = dr["UserId"].ToString(),
                        Count = dr["Count"].ToString(),
                        TimeIn = dr["TimeIn"].ToString(),
                        TimeOut = dr["TimeOut"].ToString(),
                        Amount = dr["Amount"].ToString(),
                        Cashier = dr["Cashier"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<string> GetTodayXReading(int gateid)
        {
            var sql = $"SELECT [dbo].[fnGetXReading]({gateid})";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return result.ToString();
        }
        public async Task<string> GetTodayYReading(int gateid)
        {
            var sql = $"SELECT [dbo].[fnGetYReading]({gateid})";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return result.ToString();
        }
        public async Task<string> GetTodayZReading(int gateid)
        {
            var sql = $"SELECT [dbo].[fnGetZReading]({gateid})";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return result.ToString();
        }

        public async Task<string> PerformZReading(int gateid, int userid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spPerformZReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateid);
            cmd.Parameters.AddWithValue("@UserId", userid);
            var result = await SCObjects.ExecuteNonQueryWithReturnAsync(cmd, UserConnection);
            return result;
        }

        public async Task<List<ZReading>> GetZReadingAsync(string srno, int userid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetZReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            cmd.Parameters.AddWithValue("@UserId", userid);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<ZReading>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new ZReading
                    {
                        Location = dr["Location"].ToString(),
                        Terminal = dr["Terminal"].ToString(),
                        TimeIn = dr["TimeIn"].ToString(),
                        TimeOut = dr["TimeOut"].ToString(),
                        ParkerIn = dr["ParkerIn"].ToString(),
                        ParkerOut = dr["ParkerOut"].ToString(),
                        ReservedIn = dr["ReservedIn"].ToString(),
                        ReservedOut = dr["ReservedOut"].ToString(),
                        RateType = dr["RateType"].ToString(),
                        Count = dr["Count"].ToString(),
                        Amount = dr["Amount"].ToString(),
                        OvernightCount = dr["OvernightCount"].ToString(),
                        OvernightAmount = dr["OvernightAmount"].ToString(),
                        LostCardCount = dr["LostCardCount"].ToString(),
                        LostCardAmount = dr["LostCardAmount"].ToString(),
                        NewFR = dr["NewFR"].ToString(),
                        NewFT = dr["NewFT"].ToString(),
                        NewGrandSales = dr["NewGrandSales"].ToString(),
                        NewOR = dr["NewOR"].ToString(),
                        NewRT = dr["NewRT"].ToString(),
                        OldFR = dr["OldFR"].ToString(),
                        OldFT = dr["OldFT"].ToString(),
                        OldGrandSales = dr["OldGrandSales"].ToString(),
                        OldOR = dr["OldOR"].ToString(),
                        OldRT = dr["OldRT"].ToString(),
                        TodaySales = dr["TodaySales"].ToString(),
                        Total = dr["Total"].ToString(),
                        VatableSales = dr["VatableSales"].ToString(),
                        VatAmount = dr["VatAmount"].ToString(),
                        ZCount = dr["ZCount"].ToString(),
                        PreparedBy = dr["PreparedBy"].ToString(),
                        Reset = dr["Reset"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<List<CashlessType>> GetCashlessForZReading(string srno)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetCashlessForZReadings]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@SrNo", srno);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            var items = new List<CashlessType>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new CashlessType
                    {
                        RateType = dr["RateType"].ToString(),
                        Count = dr["Count"].ToString(),
                        Amount = dr["Amount"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }

        public async Task<ChangeFund> CheckChangeFund(int userId, int gateId)
        {
            var sql = $"SELECT * FROM [dbo].[fnCheckChangeFundForMobilePos]({userId},{gateId}) [fc]";
            var item = new ChangeFund();
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);
            if (result != null)
            {
                if (result.Rows.Count > 0)
                {
                    item.Id = int.Parse(result.Rows[0]["CashierShiftID"].ToString());
                    item.WithChangeFund = result.Rows[0]["WithChangeFund"].ToString().Equals("1");
                }
            }
            return item;
        }
        public async Task<string> SetChangeFund(int id, decimal changeFund)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spSetChangeFund]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ChangeFund", changeFund);
            var result = await SCObjects.ExecNonQueryAsync(cmd, this.UserConnection);
            return result;
        }
        public async Task<TenderDeclaration> CheckTenderDeclaration(int userId, int gateId)
        {
            var sql = $"SELECT * FROM [dbo].[fnCheckTenderDeclarationMobilePos]({userId},{gateId}) [fc]";
            var item = new TenderDeclaration();
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);
            if (result != null)
            {
                if (result.Rows.Count > 0)
                {
                    item.Id = int.Parse(result.Rows[0]["CashierShiftID"].ToString());
                    item.Total = int.Parse(result.Rows[0]["Total"].ToString());
                    item.WithTender = int.Parse(result.Rows[0]["Total"].ToString()) > 0;
                }
            }
            return item;
        }
        public async Task<string> SetTenderDeclaration(TenderDeclarationValue item)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spSetTenderDeclaration]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", item.Id);
            cmd.Parameters.AddWithValue("@ValueFor1000", item.ValueFor1000);
            cmd.Parameters.AddWithValue("@ValueFor500", item.ValueFor500);
            cmd.Parameters.AddWithValue("@ValueFor200", item.ValueFor200);
            cmd.Parameters.AddWithValue("@ValueFor100", item.ValueFor100);
            cmd.Parameters.AddWithValue("@ValueFor50", item.ValueFor50);
            cmd.Parameters.AddWithValue("@ValueFor20", item.ValueFor20);
            cmd.Parameters.AddWithValue("@ValueFor10", item.ValueFor10);
            cmd.Parameters.AddWithValue("@ValueFor5", item.ValueFor5);
            cmd.Parameters.AddWithValue("@ValueFor1", item.ValueFor1);
            cmd.Parameters.AddWithValue("@ValueForCent", item.ValueForCent);
            cmd.Parameters.AddWithValue("@Comment", item.Comment);
            var result = await SCObjects.ExecNonQueryAsync(cmd, this.UserConnection);
            return result;
        }
        public async Task<TenderDeclarationDetails> GetTenderDeclaration(int id)
        {
            var sql = $"EXEC [dbo].[spGenerateTenderDeclaration] @Id = {id}";
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);
            var item = new TenderDeclarationDetails();

            if (result != null)
            {
                if (result.Rows.Count > 0)
                {
                    item.ShiftIn = result.Rows[0]["ShiftIn"].ToString();
                    item.ShiftOut = result.Rows[0]["ShiftOut"].ToString();
                    item.PrintDate = result.Rows[0]["PrintDate"].ToString();
                    item.Location = result.Rows[0]["Location"].ToString();
                    item.SrNoString = result.Rows[0]["SrNoString"].ToString();
                    item.Cashier = result.Rows[0]["Cashier"].ToString();
                    item.Gate = result.Rows[0]["Gate"].ToString();
                    item.TotalDue = result.Rows[0]["TotalDue"].ToString();
                    item.TotalConfirmed = result.Rows[0]["TotalConfirmed"].ToString();
                    item.ChangeFund = result.Rows[0]["ChangeFund"].ToString();
                    item.Partial = result.Rows[0]["Partial"].ToString();
                    item.PHP1000 = result.Rows[0]["PHP1000"].ToString();
                    item.PHP500 = result.Rows[0]["PHP500"].ToString();
                    item.PHP200 = result.Rows[0]["PHP200"].ToString();
                    item.PHP100 = result.Rows[0]["PHP100"].ToString();
                    item.PHP50 = result.Rows[0]["PHP50"].ToString();
                    item.PHP20 = result.Rows[0]["PHP20"].ToString();
                    item.PHP10 = result.Rows[0]["PHP10"].ToString();
                    item.PHP5 = result.Rows[0]["PHP5"].ToString();
                    item.PHP1 = result.Rows[0]["PHP1"].ToString();
                    item.CENT1 = result.Rows[0]["CENT1"].ToString();
                }
            }

            return item;
        }

        public async Task<List<OfficialReceiptItem>> OfficialReceiptSearch(int gateid, string keyword = "")
        {
            var sql = $"EXEC [dbo].[spOfficialReceiptList] @GateId = {gateid}, @Keyword = '{keyword}'";
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);
            var items = new List<OfficialReceiptItem>();
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new OfficialReceiptItem
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        Gate = dr["Gate"].ToString(),
                        ORNumber = dr["ORNumber"].ToString(),
                        PaymentDate = dr["PaymentDate"].ToString(),
                        PlateNo = dr["PlateNo"].ToString(),
                    };
                    items.Add(item);
                }
            }


            return items;
        }
        public async Task<List<DiscountType>> GetDiscountTypes()
        {
            var sql = "SELECT [ID],[Name],[Type],[Amount] FROM [dbo].[DiscountTypes] WHERE [Disable] = 0";
            var items = new List<DiscountType>();
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new DiscountType
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        Type = int.Parse(dr["Type"].ToString()),
                        Amount = int.Parse(dr["Amount"].ToString()),
                        Name = dr["Name"].ToString()
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<List<TransactionType>> TransactionTypes()
        {
            var sql = "SELECT * FROM [dbo].[fnGetTransactionTypes]() [fgtt]";
            var items = new List<TransactionType>();
            var result = await SCObjects.LoadDataTableAsync(sql, this.UserConnection);
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new TransactionType
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        Name = dr["Name"].ToString()
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<string> ResetCounter(int gateId)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spResetCounter]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateId);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<ChangFundItem> GetChangeFund(int id)
        {
            var item = new ChangFundItem();
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spPrintChangeFund]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", id);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);


            if (result != null)
            {
                if (result.Rows.Count > 0)
                {
                    item = new ChangFundItem
                    {
                        GateName = result.Rows[0]["GateName"].ToString(),
                        ParkingName = result.Rows[0]["ParkingName"].ToString(),
                        SrNoString = result.Rows[0]["SrNoString"].ToString(),
                        StartAmount = result.Rows[0]["StartAmount"].ToString(),
                        TimeIn = result.Rows[0]["TimeIn"].ToString(),
                        Username = result.Rows[0]["Username"].ToString(),
                    };
                }
            }
            return item;
        }
        public async Task<List<TicketItem>> GetTicketList(int gateid, string keyword)
        {
            var items = new List<TicketItem>();
            var sql = $"EXEC [dbo].[spGetTicketList] @GateId = {gateid},  @Keyword = '{keyword}'";
            var result = await SCObjects.LoadDataTableAsync(sql, UserConnection);
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new TicketItem
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        PlateNo = dr["PlateNo"].ToString(),
                        Ticket = dr["Ticket"].ToString(),
                        TimeIn = dr["TimeIn"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<List<XReadingItem>> GetForXReading(int gateid)
        {
            var sql = $"EXEC [dbo].[spGetAllForXReading] @GateId = {gateid}";
            var items = new List<XReadingItem>();
            var result = await SCObjects.LoadDataTableAsync(sql, UserConnection);
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new XReadingItem
                    {
                        Id = dr["Id"].ToString(),
                        User = dr["User"].ToString(),
                        Reference = dr["Reference"].ToString(),
                        TimeIn = dr["TimeIn"].ToString(),
                        TimeOut = dr["TimeOut"].ToString(),
                        WithReading = dr["WithReading"].ToString(),
                    };
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<int> GetReprintCount(string referenceNo, string type)
        {
            var sql = $"EXEC [dbo].[spReprintEntry] @ReferenceNo = '{referenceNo}', @Type = '{type}'";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            if (result == null)
            {
                return 0;
            }
            return int.Parse(result.ToString());
        }
        public async Task<string> SetAuditLogs(string description, int gateId, int userId)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spCreateAuditLog]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@GateId", gateId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<List<ReadingItem>> GetReadingItems(int gateId, string type, string keyword)
        {
            var items = new List<ReadingItem>();
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetReadingsItem]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateId);
            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@Keyword", keyword);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);
            if (result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new ReadingItem();
                    item.Id = int.Parse(dr["Id"].ToString());
                    item.StartDate = dr["StartDate"].ToString();
                    item.EndDate = dr["EndDate"].ToString();
                    item.ReferenceNo = dr["ReferenceNo"].ToString();
                    item.Type = dr["Type"].ToString();
                    item.PerformedBy = dr["PerformedBy"].ToString();
                    item.Gate = dr["Gate"].ToString();
                    items.Add(item);
                }
            }
            return items;
        }
        public async Task<GateInformation> GateInformation(int gateId)
        {
            var item = new GateInformation();
            var sql = $"SELECT * FROM [dbo].[GateInformation] [gi] WHERE [gi].[GateId] = {gateId}";
            var result = await SCObjects.LoadDataTableAsync(sql, UserConnection);
            if(result != null)
            {
                if(result.Rows.Count > 0)
                {
                    item.MIN = result.Rows[0]["MIN"].ToString();
                    item.SN = result.Rows[0]["SN"].ToString();
                    item.AccreditationNo = result.Rows[0]["AccreditationNo"].ToString();
                    item.AccreditationDateIssued = result.Rows[0]["AccreditationDateIssued"].ToString();
                }
            }
            return item;
        }
        public async Task<bool> CheckIfNonVat(int rateId)
        {
            var sql = $@"SELECT ISNULL(MAX(1), 0)
                        FROM [dbo].[RatesList] [rl]
                        WHERE [rl].[ApplyVat] = 1
                              AND [rl].[RateID] = {rateId}";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return result.Equals("1");
        }
        public async Task<List<UserAccessMatrix>> GetUserAccessMatrix(int userId)
        {
            var items = new List<UserAccessMatrix>();
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[sp_GetUserAccessForHandHeld]";
            cmd.Parameters.AddWithValue("@UserId", userId);
            var result = await SCObjects.ExecGetDataAsync(cmd, UserConnection);

            if(result != null)
            {
                foreach (DataRow dr in result.Rows)
                {
                    var item = new UserAccessMatrix
                    {
                        Code = dr["Code"].ToString(),
                        Description = dr["Description"].ToString(),
                        CanAdd = dr["CanAdd"].ToString().Equals("1"),
                        CanAccess = dr["CanAccess"].ToString().Equals("1"),
                        CanDelete = dr["CanDelete"].ToString().Equals("1"),
                        CanEdit = dr["CanEdit"].ToString().Equals("1"),
                        CanExport = dr["CanExport"].ToString().Equals("1"),
                        CanPrint = dr["CanPrint"].ToString().Equals("1"),
                        CanSave = dr["CanSave"].ToString().Equals("1"),
                        CanSearch = dr["CanSearch"].ToString().Equals("1"),
                    };
                    items.Add(item);
                }
            }

            return items;
        }
        public async Task<bool> CheckIfWithReadings(int gateid)
        {
            var sql = $"EXEC [dbo].[sp_CheckIfHasReading] @GateId = {gateid}";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return result.Equals("1");
        }
        public async Task<bool> CheckIfWithYReading(int gateid)
        {
            var sql = $"EXEC [dbo].[sp_CheckIfWithYReading] @GateId = {gateid}";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return result.Equals("1");
        }
        public async Task<int> GetGatePerTransit(int transitId)
        {
            var sql = $"SELECT [t].[PaymentGateID] FROM [dbo].[Transits] [t] WHERE [t].[TransitID] = {transitId}";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            return int.Parse(result);
        }
    }
}
