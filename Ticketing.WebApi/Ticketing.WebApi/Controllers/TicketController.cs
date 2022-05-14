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
        public async Task<IHttpActionResult> PrintTicket(string ticketNo, string gateid, string plateNo = "")
        {
            var sql = $"EXEC [dbo].[spGenerateTicketNumber] @TicketNo = '{ticketNo}', @PlateNo = '{plateNo}',@GateId='{gateid}'";
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
            var ticketInfo = string.Empty;
            ticketInfo += $"{ticket.Company}\n";
            ticketInfo += $"{ticket.Address1}\n";
            ticketInfo += $"{ticket.Address2}\n";
            ticketInfo += $"{ticket.Address3}\n";
            ticketInfo += $"TIN : {ticket.TIN}\n";
            ticketInfo += $"PLATENO  :      {ticket.PlateNo}\n";
            ticketInfo += $"TICKETNO :      {ticket.TicketNo}\n";
            ticketInfo += $"LOCATION :      {ticket.Location}\n";
            ticketInfo += $"TIME IN  :      {ticket.TimeIn}\n";
            ticketInfo += $"TERMINAL :      {ticket.Terminal}\n";
            ticket.Printable = ticketInfo; 
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
        public async Task<IHttpActionResult> ComputeTransaction(int transitid,string gate,string parkertype,string tenderamount,string change,string totalamount,string userid)
        {
            var result = await services.ComputeTransaction(transitid, gate, parkertype, tenderamount, change, totalamount, userid);
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
        [Route("api/ticket/officialreceipt")]
        public async Task<IHttpActionResult> GetOfficialReceiptInfo(string ticketno, int transitid, string gate, string parkertype, string tenderamount, string change, string totalamount, string userid)
        {
            var compute = await services.ComputeTransaction(transitid, gate, parkertype, tenderamount, change, totalamount, userid);
            if (compute.Contains("success"))
            {
                var result = await services.GetOfficialReceipt(transitid);
                var orinfo = string.Empty;
                orinfo += $"{result.Company}\n";
                orinfo += $"{result.Address1}\n";
                orinfo += $"{result.Address2}\n";
                orinfo += $"{result.Address3}\n";
                orinfo += $"VAT REG TIN :\n";
                orinfo += $"{result.TIN} :\n";
                orinfo += $"ACCREDITATION NO :\n";
                orinfo += $"{result.AccreditationNo} :\n";
                orinfo += $"VALID UNTIL :{result.AccreditationValidUntil} \n";
                orinfo += $"DATE ISSUED :{result.AccreditationDate} \n";
                orinfo += $"PTU NO :\n";
                orinfo += $"{result.PTUNo} \n";
                orinfo += $"DATE ISSUED :{result.PTUDateIssued} \n\n";
                orinfo += $"OFFICIAL RECEIPT\n\n";
                orinfo += $"RETAIL\n\n";
                orinfo += $"OR NO : {result.OrNumber}\n";
                orinfo += $"TICKET NO : {result.TicketNo}\n";
                orinfo += $"PLATE NO : {result.PlateNo}\n\n";
                orinfo += $"LOCATION:        :{result.Location}\n";
                orinfo += $"TERMNIAL         :{result.Terminal}\n";
                orinfo += $"CASHIER NAME     :{result.CashierName}\n";
                orinfo += $"DATE/TIME IN     :{result.TimeIn}\n";
                orinfo += $"DATE/TIME OUT    :{result.TimeOut}\n";
                orinfo += $"DURATION OF STAY :{result.Duration}\n";
                orinfo += $"----------------------------\n";
                orinfo += $"TOTAL W/ VAT     :P {result.TotalWithVaT}\n";
                orinfo += $"VAT              :P {result.Vat}\n";
                orinfo += $"SUBTOTAL         :P {result.Subtotal}\n";
                orinfo += $"DISCOUNT         :P {result.Discount}\n";
                orinfo += $"----------------------------\n";
                orinfo += $"TENDER TYPE      :P {result.TenderType}\n";
                orinfo += $"TOTAL AMT DUE    :P {result.TotalAmountDue}\n";
                orinfo += $"AMT TENDERED     :P {result.AmountTendered}\n";
                orinfo += $"CHANGE           :P {result.Change}\n";
                orinfo += $"----------------------------\n";
                orinfo += $"VATable Sales    :P {result.VatableSales}\n";
                orinfo += $"VAT Amount       :P {result.VatAmount}\n";
                orinfo += $"VAT Exempt Sales :P {result.VatExempt}\n";
                orinfo += $"Zero Rated Sales :P {result.ZeroRated}\n";
                orinfo += $"----------------------------\n";
                orinfo += $"PARKER INFORMATION\n";
                orinfo += $"NAME : _____________________\n";
                orinfo += $"ADDRESS : __________________\n";
                orinfo += $"TIN: _______________________\n";
                orinfo += $"SC/PWD ID: _________________\n";
                orinfo += $"SIGNATURE: _________________\n\n";
                orinfo += $"----------------------------\n";
                orinfo += $"SMARTBAS (PHILS.) CORP.\n";
                orinfo += $"Unit 3106, East Tower, Phil.\n";
                orinfo += $"Stock Exchange Center\n";
                orinfo += $"Exchange Road,Ortigas Center,\n";
                orinfo += $"Pasig City 1605\n";
                orinfo += $"VAT REG TIN :\n";
                orinfo += $"010-364-544-000\n";
                orinfo += $"ACCREDITATION NO :\n";
                orinfo += $"{result.AccreditationNo} \n";
                orinfo += $"VALID UNTIL :{result.AccreditationValidUntil} \n";
                orinfo += $"DATE ISSUED :{result.AccreditationDate} \n";
                orinfo += $"PTU NO :\n";
                orinfo += $"{result.PTUNo} \n";
                orinfo += $"DATE ISSUED :{result.PTUDateIssued} \n\n";
                orinfo += $"THANK YOU!\n";
                orinfo += $"THIS RECEIPT SHALL BE VALID\n";
                orinfo += $"FOR FIVE(5) YEARS FROM THE\n";
                orinfo += $"DATE OF THE PERMIT TO USE\n\n";

                result.Printable = orinfo;
                return Ok(result);
            }
            else
            {
                return Ok(Constants.Failed);
            }

        }
        [HttpGet]
        [Route("api/ticket/isvaliduser")]
        public async Task<IHttpActionResult> IsValidUser(string username, string password,string gateid)
        {
            var result = await services.IsValidUser(username, password,gateid);
            var item = new LoginViewModel
            {
                Key = Security.EncryptToBase64(result.ToString()),
                IsValid = result.ToString().Equals("0") ? false : true,
                Id = result,
            };
            return Ok(item);
        }
        [HttpGet]
        [Route("api/ticket/signout")]
        public async Task<IHttpActionResult> Signout(int userid, int gateid)
        {
            var result = await services.SignOut(userid, gateid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/xreading")]
        public async Task<IHttpActionResult> XReading(int gateid)
        {
            var result = await services.PerformXReading(gateid);
            return Ok(result);
        }


        private byte[] ConvertImage(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
        [HttpGet]
        [Route("api/ticket/performxreading")]
        public async Task<IHttpActionResult> PerformXReading(int gateid)
        {
            var result = await services.PerformXReading(gateid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/xreading")]
        public async Task<IHttpActionResult> GetXReadingDetails(int gateid, string srno)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();

            var headerString = string.Empty;
            headerString += $"{header.Company}\n";
            headerString += $"{header.Address1}\n";
            headerString += $"{header.Address2}\n";
            headerString += $"{header.Address3}\n";
            headerString += $"VAT REG TIN :\n";
            headerString += $"{header.TIN} :\n";
            headerString += $"ACCREDITATION NO :\n";
            headerString += $"{header.AccreditationNo} :\n";
            headerString += $"VALID UNTIL :{header.AccreditationValidUntil} \n";
            headerString += $"DATE ISSUED :{header.AccreditationDate} \n";
            headerString += $"PTU NO :\n";
            headerString += $"{header.PTUNo} \n";
            headerString += $"DATE ISSUED :{header.PTUDateIssued} \n\n";
            item.Header = headerString;
            var body = await services.GetXReadingAsync(srno);
            var bodyString = string.Empty;
            bodyString += $"CASHIER NAME :{body.FirstOrDefault().CashierName}\n";
            bodyString += $"LOCATION     :{body.FirstOrDefault().Location}\n";
            bodyString += $"TERMINAL     :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"SR NO        :{srno}\n";
            bodyString += $"TIME IN      :{body.FirstOrDefault().TimeIn}\n";
            bodyString += $"TIME OUT     :{body.FirstOrDefault().TimeOut}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE       IN    OUT   REMAINING\n";
            bodyString += $"PARKER     {body.FirstOrDefault().ParkerIn}    {body.FirstOrDefault().ParkerOut}\n";     
            bodyString += $"RESERVED   {body.FirstOrDefault().ParkerIn}    {body.FirstOrDefault().ParkerOut}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"SALES COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"RATE TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            foreach(var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}        {bodyItem.Count}     {bodyItem.Amount}\n";
            }
            bodyString += $"---------------------------------------\n";
            bodyString += $"PENALTY COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"CASHLESS COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            var cashless = await services.GetCashlessForXReading(srno);
            foreach(var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}        {bodyItem.Count}     {bodyItem.Amount}\n";
            }
            bodyString += $"---------------------------------------\n";
            bodyString += $"TOTAL TRANSACTION :{body.FirstOrDefault().TotalTransaction}\n";
            bodyString += $"TOTAL PARTIAL     :{body.FirstOrDefault().TotalPartial}\n";
            bodyString += $"TOTAL TENDERED    :{body.FirstOrDefault().TotalTendered}\n";
            bodyString += $"VARIANCE          :{body.FirstOrDefault().TotalVariance}\n";
            item.Body = bodyString;
            return Ok(item);
        }
        [HttpGet]
        [Route("api/ticket/performyreading")]
        public async Task<IHttpActionResult> PerformyYReading(int gateid, int userid)
        {
            var result = await services.PerformYReading(gateid,userid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/yreading")]
        public async Task<IHttpActionResult> GetYReadingDetails(int gateid, string srno)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();

            var headerString = string.Empty;
            headerString += $"{header.Company}\n";
            headerString += $"{header.Address1}\n";
            headerString += $"{header.Address2}\n";
            headerString += $"{header.Address3}\n";
            headerString += $"VAT REG TIN :\n";
            headerString += $"{header.TIN} :\n";
            headerString += $"ACCREDITATION NO :\n";
            headerString += $"{header.AccreditationNo} :\n";
            headerString += $"VALID UNTIL :{header.AccreditationValidUntil} \n";
            headerString += $"DATE ISSUED :{header.AccreditationDate} \n";
            headerString += $"PTU NO :\n";
            headerString += $"{header.PTUNo} \n";
            headerString += $"DATE ISSUED :{header.PTUDateIssued} \n\n";
            item.Header = headerString;
            var body = await services.GetYReadingAsync(srno);
            var bodyString = string.Empty;
            bodyString += $"LOCATION     :{body.FirstOrDefault().Location}\n";
            bodyString += $"TERMINAL     :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"SR NO        :{srno}\n";
            bodyString += $"TIME IN      :{body.FirstOrDefault().TimeIn}\n";
            bodyString += $"TIME OUT     :{body.FirstOrDefault().TimeOut}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE       IN    OUT   REMAINING\n";
            bodyString += $"PARKER     {body.FirstOrDefault().ParkerIn}    {body.FirstOrDefault().ParkerOut}\n";
            bodyString += $"RESERVED   {body.FirstOrDefault().ParkerIn}    {body.FirstOrDefault().ParkerOut}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"SALES COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"RATE TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}        {bodyItem.Count}     {bodyItem.Amount}\n";
            }
            bodyString += $"---------------------------------------\n";
            bodyString += $"PENALTY COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"CASHLESS COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            var cashless = await services.GetCashlessForXReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}        {bodyItem.Count}     {bodyItem.Amount}\n";
            }
            bodyString += $"---------------------------------------\n";
            bodyString += $"CASHIER COLLECTION SUMMARY\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"CASHIERS SALES\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"NAME            START  NO  AMOUNT\n";
            bodyString += $"---------------------------------------\n";

            var summary = await services.GetCashierSummaryForYReading(srno);

            foreach(var i in summary)
            {
                bodyString += $"{i.Cashier}    {i.TimeIn}  {i.Count} {i.Amount}\n";
            }

            bodyString += $"---------------------------------------\n";
            bodyString += $"TOTAL TRANSACTION :{body.FirstOrDefault().TotalTransaction}\n";
            bodyString += $"TOTAL PARTIAL     :{body.FirstOrDefault().TotalPartial}\n";
            bodyString += $"TOTAL TENDERED    :{body.FirstOrDefault().TotalTendered}\n";
            bodyString += $"VARIANCE          :{body.FirstOrDefault().TotalVariance}\n";
            item.Body = bodyString;
            return Ok(item);
        }
        [HttpGet]
        [Route("api/ticket/xreadingtoday")]
        public async Task<IHttpActionResult> TodayXReading(int gateid)
        {
            var result = await services.GetTodayXReading(gateid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/yreadingtoday")]
        public async Task<IHttpActionResult> TodayYReading(int gateid)
        {
            var result = await services.GetTodayYReading(gateid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/zreadingtoday")]
        public async Task<IHttpActionResult> TodayZReading(int gateid)
        {
            var result = await services.GetTodayZReading(gateid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/performzreading")]
        public async Task<IHttpActionResult> PerformZReading(int gateid, int userid)
        {
            var result = await services.PerformZReading(gateid, userid);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/ticket/zreading")]
        public async Task<IHttpActionResult> GetZReadingDetails(int gateid, string srno, int userid)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();

            var headerString = string.Empty;
            headerString += $"{header.Company}\n";
            headerString += $"{header.Address1}\n";
            headerString += $"{header.Address2}\n";
            headerString += $"{header.Address3}\n";
            headerString += $"VAT REG TIN :\n";
            headerString += $"{header.TIN} :\n";
            headerString += $"ACCREDITATION NO :\n";
            headerString += $"{header.AccreditationNo} :\n";
            headerString += $"VALID UNTIL :{header.AccreditationValidUntil} \n";
            headerString += $"DATE ISSUED :{header.AccreditationDate} \n";
            headerString += $"PTU NO :\n";
            headerString += $"{header.PTUNo} \n";
            headerString += $"DATE ISSUED :{header.PTUDateIssued} \n\n";
            item.Header = headerString;
            var body = await services.GetZReadingAsync(srno,userid);
            var bodyString = string.Empty;
            bodyString += $"LOCATION     :{body.FirstOrDefault().Location}\n";
            bodyString += $"TERMINAL     :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"SR NO        :{srno}\n";
            bodyString += $"TIME IN      :{body.FirstOrDefault().TimeIn}\n";
            bodyString += $"TIME OUT     :{body.FirstOrDefault().TimeOut}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TICKET COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"NEW R-TICKET NO : {body.FirstOrDefault().NewRT}\n";
            bodyString += $"OLD R-TICKET NO : {body.FirstOrDefault().OldRT}\n";
            bodyString += $"NEW F-TICKET NO : {body.FirstOrDefault().NewFT}\n";
            bodyString += $"OLD F-TICKET NO : {body.FirstOrDefault().OldFT}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"RECEIPT COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"NEW FR NO : {body.FirstOrDefault().NewFR}\n";
            bodyString += $"OLD FR NO : {body.FirstOrDefault().OldFR}\n";
            bodyString += $"NEW OR NO : {body.FirstOrDefault().NewOR}\n";
            bodyString += $"OLD OR NO : {body.FirstOrDefault().OldOR}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"SALES\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"OLD GRAND SALES : {body.FirstOrDefault().OldGrandSales}\n";
            bodyString += $"TODAY SALES     : {body.FirstOrDefault().TodaySales}\n";
            bodyString += $"NEW GRAND SALES : {body.FirstOrDefault().NewGrandSales}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"SALES COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"RATE TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}        {bodyItem.Count}     {bodyItem.Amount}\n";
            }
            bodyString += $"---------------------------------------\n";
            bodyString += $"PENALTY COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"CASHLESS COUNTER\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"---------------------------------------\n";
            var cashless = await services.GetCashlessForZReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}        {bodyItem.Count}     {bodyItem.Amount}\n";
            }
            bodyString += $"---------------------------------------\n";
            bodyString += $"VAT BREAKDOWN\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"VATABLE SALES : {body.FirstOrDefault().VatableSales}\n";
            bodyString += $"VAT AMOUNT    : {body.FirstOrDefault().VatAmount}\n";
            bodyString += $"VAT EXEPMT    : 0.00\n";
            bodyString += $"ZERO RATE     : 0.00\n";
            bodyString += $"---------------------------------------\n";
            bodyString += $"Z-COUNT               {body.FirstOrDefault().ZCount}\n";
            bodyString += $"RESET COUNTER         0\n";
            bodyString += $"TOTAL ACCUMULATED SALES  {body.FirstOrDefault().Total}\n";
            bodyString += $"PREPARED BY :   {body.FirstOrDefault().PreparedBy}\n";
            bodyString += $"---------------------------------------\n";
            item.Body = bodyString;
            return Ok(item);
        }
    }
}
