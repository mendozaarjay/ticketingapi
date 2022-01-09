using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Ticketing.WebApi.Helpers;
using Ticketing.WebApi.Models;
using Ticketing.WebApi.Services;

namespace Ticketing.WebApi.Controllers
{
    public class TicketController : ApiController
    {
        public string UserConnectionString { get; }
        private DataServices services = new DataServices();
        public TicketController()
        {
            UserConnectionString = ConfigurationManager.ConnectionStrings["UserConnnectionString"].ConnectionString;
            services.UserConnection = UserConnectionString;
        }
        [HttpGet]
        [Route("api/ticket/printticket")]
        public async Task<IHttpActionResult> PrintTicket(string ticketNo, string plateNo = "")
        {
            var sql = $"EXEC [dbo].[spGenerateTicketNumber] @TicketNo = '{ticketNo}', @PlateNo = '{plateNo}'";
            var dt = await SCObjects.LoadDataTableAsync(sql, UserConnectionString);
            var ticket = new ParkerTicket
            {
               Id = dt.Rows[0]["Id"].ToString(),
               Company = dt.Rows[0]["Company"].ToString(),
               Address1 = dt.Rows[0]["Address1"].ToString(),
               Address2 = dt.Rows[0]["Address2"].ToString(),
               Address3 = dt.Rows[0]["Address3"].ToString(),
               TIN = dt.Rows[0]["TIN"].ToString(),
               PlateNo = dt.Rows[0]["PlateNo"].ToString(),
               TicketNo = dt.Rows[0]["TicketNo"].ToString(),
               TimeIn = dt.Rows[0]["TimeIn"].ToString(),
               Terminal = dt.Rows[0]["Terminal"].ToString(),
               Location = dt.Rows[0]["Location"].ToString(),
            };

            return Ok(ticket);
        }
        [HttpGet]
        [Route("api/ticket/getnextticket")]
        public async Task<IHttpActionResult> GetNextTicket(string gate)
        {
            var sql = $"SELECT [dbo].[fnGetNextTicket]({gate})";
            var ticket = await SCObjects.ReturnTextAsync(sql, UserConnectionString);
            return Ok(ticket);
        }
        [HttpGet]
        [Route("api/ticket/verifyticket")]
        public async Task<IHttpActionResult> VerifyTicket(string ticket, int gate = 0, string plateno = "")
        {
            var sql = $"EXEC [dbo].[spVerifyTicketNo] @TicketNo = '{ticket}', @Gate = {gate},@PlateNo = '{plateno}'";
            var result = await SCObjects.LoadDataTableAsync(sql, UserConnectionString);
            if (result == null)
                return NotFound();
            if (result.Rows.Count <= 0)
                return NotFound();
            var item = new VerifiedTicket
            {
                Id = int.Parse(result.Rows[0]["Id"].ToString()),
                PlateNo = result.Rows[0]["PlateNo"].ToString(),
                TicketNo = result.Rows[0]["TicketNo"].ToString(),
                EntranceDate = DateTime.Parse(result.Rows[0]["EntranceDate"].ToString()).ToString("MM/dd/yyyy hh:mm tt"),
                ExitDate = DateTime.Parse(result.Rows[0]["ExitDate"].ToString()).ToString("MM/dd/yyyy hh:mm tt"),
                Duration = result.Rows[0]["Duration"].ToString()
            };
            return Ok(item);

        }

        [HttpGet]
        [Route("api/ticket/parkertypes")]
        public async Task<IHttpActionResult> GetParkerTypes()
        {
            var result = await services.GetParkerTypes();
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/calculaterate")]
        public async Task<IHttpActionResult> CalculateRate(int transitid, int parkertypeid)
        {
            var rates = await services.GetRates(parkertypeid.ToString());
            var timeIn = await services.GetEntranceDate(transitid);
            var timeOut = DateTime.Now;

            var mins = timeOut - timeIn;
            var condition1 = rates.FirstOrDefault(a => a.Id == 1);
            int remaining = 0;
            var initialAmount = Calculator.ConditionOne(condition1, timeIn, timeOut, ref remaining);
            var condition2 = rates.FirstOrDefault(a => a.Id == 2);
            if (condition2 != null)
            {
                var additionalAmount = Calculator.ConditionTwo(condition2, remaining);
                initialAmount += additionalAmount;
            }
            var rateName = rates.FirstOrDefault().RateName;
            var result = new CalculatedRate
            {
                Rate = rateName,
                Duration = SetDuration((int)mins.TotalMinutes),
                Amount = initialAmount,
            };
            return Ok(result);
        }
        private string SetDuration(int mins)
        {
            if (mins < 60)
                return $"{mins} minutes.";
            int total = mins / 60;
            int remaining = mins % 60;
            return $"{total} hour/s and {remaining} minute/s.";
        }

        [HttpGet]
        [Route("api/ticket/computetransaction")]
        public async Task<IHttpActionResult> ComputeTransaction(int transitid,string timeout,string gate,string parkertype,string tenderamount,string change,string totalamount,string userid)
        {
            var result = await services.ComputeTransaction(transitid, timeout, gate, parkertype, tenderamount, change, totalamount, userid);
            if(result.Contains("success"))
            {
                return Ok(Constants.Success);
            }
            else
            {
                return Ok(Constants.Failed);
            }
        }
        [HttpGet]
        [Route("api/ticket/isvaliduser")]
        public async Task<IHttpActionResult> IsValidUser(string username, string password)
        {
            var result = await services.IsValidUser(username, password);
            var item = new LoginViewModel
            {
                Key = Security.EncryptToBase64(result.ToString()),
                IsValid = result.ToString().Equals("0") ? false : true,
            };
            return Ok(item);
        }

        private byte[] ConvertImage(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
    }
}
