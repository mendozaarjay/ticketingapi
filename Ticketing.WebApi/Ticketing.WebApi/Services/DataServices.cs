using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Ticketing.WebApi.Models;

namespace Ticketing.WebApi.Services
{
    public class DataServices
    {
        public string UserConnection { get; set; }
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
            var result =  await SCObjects.LoadDataTableAsync(sql, UserConnection);
            var items = new List<ParkerType>();

            if(result != null)
            {
                foreach(DataRow dr in result.Rows)
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
        public async Task<string> ComputeTransaction(int transitid, string gate, string parkertype, string tenderamount, string change, string totalamount, string userid)
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
            cmd.Parameters.AddWithValue("@TotalAmount", change);
            cmd.Parameters.AddWithValue("@Change", totalamount);
            cmd.Parameters.AddWithValue("@UserId", userid);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<int> IsValidUser(string username, string password, string gateid)
        {
            var sql = $"SELECT [dbo].[fnIsValidUser]('{username}','{password}')";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            var userId = int.Parse(result);
            if(userId > 0)
            {
                var signin = await this.CheckFirstSignIn(userId, gateid);
            }
            return userId;
        }
        public async Task<string> CheckFirstSignIn(int userid, string gateid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spMobileSignIn]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@UserId",userid);
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
        public async Task<string> PerformXReading(int gateid)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spPerformXReading]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@GateId", gateid);
            var result = await SCObjects.ExecuteNonQueryWithReturnAsync(cmd, UserConnection);
            return result;
        }
        public async Task<OfficialReceipt> GetOfficialReceipt(int id)
        {
            var cmd = new SqlCommand();
            cmd.CommandText = "[dbo].[spGetOfficialReceipInformation]";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", id);
            var result = await SCObjects.ExecGetDataAsync(cmd,UserConnection);
            var item = new OfficialReceipt();
            if(result != null)
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
                    TenderType = result.Rows[0]["TenderType"].ToString(),
                    TotalAmountDue = result.Rows[0]["TotalAmountDue"].ToString(),
                    AmountTendered = result.Rows[0]["AmountTendered"].ToString(),
                    Change = result.Rows[0]["Change"].ToString(),
                    VatableSales = result.Rows[0]["VatableSales"].ToString(),
                    VatAmount = result.Rows[0]["VatAmount"].ToString(),
                    VatExempt = result.Rows[0]["VatExempt"].ToString(),
                    ZeroRated = result.Rows[0]["ZeroRated"].ToString(),
                    CashierName = result.Rows[0]["CashierName"].ToString(),
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

        public async Task<string> PerformYReading(int gateid,int userid)
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
    }
}
