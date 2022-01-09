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
        public async Task<string> ComputeTransaction(int transitid, string timeout, string gate, string parkertype, string tenderamount, string change, string totalamount, string userid)
        {
            var sql = "[dbo].[spComputeTransaction]";
            var cmd = new SqlCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", transitid);
            cmd.Parameters.AddWithValue("@TimeOut", DateTime.Parse(timeout));
            cmd.Parameters.AddWithValue("@PaymentGate", gate);
            cmd.Parameters.AddWithValue("@ParkerTypeId", parkertype);
            cmd.Parameters.AddWithValue("@TenderedAmount", tenderamount);
            cmd.Parameters.AddWithValue("@TotalAmount", change);
            cmd.Parameters.AddWithValue("@Change", totalamount);
            cmd.Parameters.AddWithValue("@UserId", userid);
            var result = await SCObjects.ExecNonQueryAsync(cmd, UserConnection);
            return result;
        }
        public async Task<int> IsValidUser(string username, string password)
        {
            var sql = $"SELECT [dbo].[fnIsValidUser]('{username}','{password}')";
            var result = await SCObjects.ReturnTextAsync(sql, UserConnection);
            var userId = int.Parse(result);
            return userId;
        }
    }
}
