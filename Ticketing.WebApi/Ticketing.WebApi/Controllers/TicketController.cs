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
            var useEncryption = ConfigurationManager.AppSettings["UseEncryption"].ToString();
            services.UseEncryption = useEncryption.Equals("1");
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
            ticketInfo += $"[C]{ticket.Company}\n";
            ticketInfo += $"[C]{ticket.Address1}\n";
            ticketInfo += $"[C]{ticket.Address2}\n";
            ticketInfo += $"[C]{ticket.Address3}\n";
            ticketInfo += $"[C]================================\n";
            ticketInfo += $"[L]TIN      : {ticket.TIN}\n";
            ticketInfo += $"[L]PLATENO  : {ticket.PlateNo}\n";
            ticketInfo += $"[L]TICKETNO : {ticket.TicketNo}\n";
            ticketInfo += $"[L]LOCATION : {ticket.Location}\n";
            ticketInfo += $"[L]TIME IN  : {ticket.TimeIn}\n";
            ticketInfo += $"[L]TERMINAL : {ticket.Terminal}\n";
            ticketInfo += $"[C]================================\n";
            ticketInfo += $"[C] <qrcode size='20'>{ticket.TicketNo}</qrcode>\n";
            ticketInfo += $"[C]================================\n";
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
                Duration = result.Rows[0]["Duration"].ToString(),
                IsCompleted = result.Rows[0]["IsCompleted"].ToString().Equals("1"),
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
        [Route("api/ticket/checkifvatable")]
        public async Task<IHttpActionResult> CheckIfVatable(int rateid)
        {
            var result = await services.CheckIfNonVat(rateid);
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
        public async Task<IHttpActionResult> ComputeTransaction(int transitid,string gate,string parkertype,string tenderamount,string change,string totalamount,string userid, int discountid = 0, string discountamount ="", int cashlesstype = 0, string cashlessreference = "")
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
        public async Task<IHttpActionResult> GetOfficialReceiptInfo(string ticketno, int transitid, string gate, string parkertype, string tenderamount, string change, string totalamount, string userid, int discountid = 0, string discountamount = "", int cashlesstype = 0, string cashlessreference = "")
        {
            var compute = await services.ComputeTransaction(transitid, gate, parkertype, tenderamount, change, totalamount, userid,discountid,discountamount,cashlesstype,cashlessreference);
            if (compute.Contains("success"))
            {
                var result = await services.GetOfficialReceipt(transitid);
                var orinfo = string.Empty;
                orinfo += $"[C]{result.Company}\n";
                orinfo += $"[C]{result.Address1}\n";
                orinfo += $"[C]{result.Address2}\n";
                orinfo += $"[C]{result.Address3}\n";
                orinfo += $"[C]VAT REG TIN :\n";
                orinfo += $"[C]{result.TIN} :\n";
                orinfo += $"[C]ACCREDITATION NO :\n";
                orinfo += $"[C]{result.AccreditationNo} :\n";
                //orinfo += $"[C]VALID UNTIL :{result.AccreditationValidUntil} \n";
                //orinfo += $"[C]DATE ISSUED :{result.AccreditationDate} \n";
                //orinfo += $"[C]PTU NO :\n";
                //orinfo += $"[C]{result.PTUNo} \n";
                //orinfo += $"[C]DATE ISSUED :{result.PTUDateIssued} \n\n";
                orinfo += $"[C]<b>OFFICIAL RECEIPT</b>\n\n";
                orinfo += $"[C]<b>{result.RateName}</b>\n\n";
                if(decimal.Parse(result.Discount) > 0)
                {
                    orinfo += $"[C]<b>DISCOUNT : {result.DiscountName}</b>\n\n";
                }
                orinfo += $"[L]OR NO    : {result.OrNumber}\n";
                orinfo += $"[L]TICKET NO: {result.TicketNo}\n";
                orinfo += $"[L]PLATE NO : {result.PlateNo}\n";
                orinfo += $"[L]LOCATION : {result.Location}\n";
                orinfo += $"[L]TERMNIAL : {result.Terminal}\n";
                orinfo += $"[L]CASHIER  : {result.CashierName}\n";
                orinfo += $"[L]TIME IN  : {result.TimeIn}\n";
                orinfo += $"[L]TIME OUT : {result.TimeOut}\n";
                orinfo += $"[L]DURATION : {result.Duration}\n";
                orinfo += $"[C]================================\n";
                orinfo += $"[L]TOTAL W/ VAT  :P {result.TotalWithVaT}\n";
                orinfo += $"[L]VAT           :P {result.Vat}\n";
                orinfo += $"[L]SUBTOTAL      :P {result.Subtotal}\n";
                orinfo += $"[L]DISCOUNT      :P {result.Discount}\n";
                orinfo += $"[C]================================\n";
                orinfo += $"[L]TENDER TYPE   :P {result.TenderType}\n";
                orinfo += $"[L]TOTAL AMT DUE :P {result.TotalAmountDue}\n";
                orinfo += $"[L]AMT TENDERED  :P {result.AmountTendered}\n";
                orinfo += $"[L]CHANGE        :P {result.Change}\n";
                orinfo += $"[C]================================\n";
                //orinfo += $"[L]VATable Sales    :P {result.VatableSales}\n";
                //orinfo += $"[L]VAT Amount       :P {result.VatAmount}\n";
                //orinfo += $"[L]VAT Exempt Sales :P {result.VatExempt}\n";
                //orinfo += $"[L]Zero Rated Sales :P {result.ZeroRated}\n";
                //orinfo += $"[C]================================\n";
                orinfo += $"[L]PARKER INFORMATION\n";
                orinfo += $"[L]NAME : _____________________\n";
                orinfo += $"[L]ADDRESS : __________________\n";
                orinfo += $"[L]TIN: _______________________\n";
                orinfo += $"[L]SC/PWD ID: _________________\n";
                orinfo += $"[L]SIGNATURE: _________________\n\n";
                orinfo += $"[C]================================\n";
                orinfo += $"[C]SMARTBAS (PHILS.) CORP.\n";
                orinfo += $"[C]Unit 3106, East Tower, Phil.\n";
                orinfo += $"[C]Stock Exchange Center\n";
                orinfo += $"[C]Exchange Road,Ortigas Center,\n";
                orinfo += $"[C]Pasig City 1605\n";
                orinfo += $"[C]VAT REG TIN :\n";
                orinfo += $"[C]010-364-544-000\n";
                orinfo += $"[C]ACCREDITATION NO :\n";
                orinfo += $"[C]{result.AccreditationNo} \n";
                orinfo += $"[C]VALID UNTIL :{result.AccreditationValidUntil} \n";
                orinfo += $"[C]DATE ISSUED :{result.AccreditationDate} \n";
                orinfo += $"[C]PTU NO :\n";
                orinfo += $"[C]{result.PTUNo} \n";
                orinfo += $"[C]DATE ISSUED :{result.PTUDateIssued} \n\n";
                orinfo += $"[C]THANK YOU!\n";
                orinfo += $"[C]================================\n";
                result.Printable = orinfo;
                return Ok(result);
            }
            else
            {
                return NotFound();
            }

        }
        [HttpGet]
        [Route("api/ticket/reprintofficialreceipt")]
        public async Task<IHttpActionResult> ReprintOfficialReceipt(int transitid)
        {
            var result = await services.GetOfficialReceipt(transitid);
            var reprint = await services.GetReprintCount(result.OrNumber, ReprintType.OfficialReceipt);
            var orinfo = string.Empty;
            orinfo += $"[C]{result.Company}\n";
            orinfo += $"[C]{result.Address1}\n";
            orinfo += $"[C]{result.Address2}\n";
            orinfo += $"[C]{result.Address3}\n";
            orinfo += $"[C]VAT REG TIN :\n";
            orinfo += $"[C]{result.TIN} :\n";
            orinfo += $"[C]ACCREDITATION NO :\n";
            orinfo += $"[C]{result.AccreditationNo} :\n";
            //orinfo += $"[C]VALID UNTIL :{result.AccreditationValidUntil} \n";
            //orinfo += $"[C]DATE ISSUED :{result.AccreditationDate} \n";
            //orinfo += $"[C]PTU NO :\n";
            //orinfo += $"[C]{result.PTUNo} \n";
            //orinfo += $"[C]DATE ISSUED :{result.PTUDateIssued} \n\n";
            orinfo += $"[C]<b>OFFICIAL RECEIPT</b>\n\n";
            if(reprint > 0)
            {
                orinfo += $"[C]<b>REPRINT : {reprint}</b>\n\n";
            }
            if (decimal.Parse(result.Discount) > 0)
            {
                orinfo += $"[C]<b>DISCOUNT : {result.DiscountName}</b>\n\n";
            }
            orinfo += $"[C]<b>{result.RateName}</b>\n\n";
            orinfo += $"[L]OR NO    : {result.OrNumber}\n";
            orinfo += $"[L]TICKET NO: {result.TicketNo}\n";
            orinfo += $"[L]PLATE NO : {result.PlateNo}\n";
            orinfo += $"[L]LOCATION : {result.Location}\n";
            orinfo += $"[L]TERMNIAL : {result.Terminal}\n";
            orinfo += $"[L]CASHIER  : {result.CashierName}\n";
            orinfo += $"[L]TIME IN  : {result.TimeIn}\n";
            orinfo += $"[L]TIME OUT : {result.TimeOut}\n";
            orinfo += $"[L]DURATION : {result.Duration}\n";
            orinfo += $"[C]================================\n";
            orinfo += $"[L]TOTAL W/ VAT  :P {result.TotalWithVaT}\n";
            orinfo += $"[L]VAT           :P {result.Vat}\n";
            orinfo += $"[L]SUBTOTAL      :P {result.Subtotal}\n";
            orinfo += $"[L]DISCOUNT      :P {result.Discount}\n";
            orinfo += $"[C]================================\n";
            orinfo += $"[L]TENDER TYPE   :P {result.TenderType}\n";
            orinfo += $"[L]TOTAL AMT DUE :P {result.TotalAmountDue}\n";
            orinfo += $"[L]AMT TENDERED  :P {result.AmountTendered}\n";
            orinfo += $"[L]CHANGE        :P {result.Change}\n";
            //orinfo += $"[C]================================\n";
            //orinfo += $"[L]VATable Sales    :P {result.VatableSales}\n";
            //orinfo += $"[L]VAT Amount       :P {result.VatAmount}\n";
            //orinfo += $"[L]VAT Exempt Sales :P {result.VatExempt}\n";
            //orinfo += $"[L]Zero Rated Sales :P {result.ZeroRated}\n";
            orinfo += $"[C]================================\n";
            orinfo += $"[L]PARKER INFORMATION\n";
            orinfo += $"[L]NAME : _____________________\n";
            orinfo += $"[L]ADDRESS : __________________\n";
            orinfo += $"[L]TIN: _______________________\n";
            orinfo += $"[L]SC/PWD ID: _________________\n";
            orinfo += $"[L]SIGNATURE: _________________\n\n";
            orinfo += $"[C]================================\n";
            orinfo += $"[C]SMARTBAS (PHILS.) CORP.\n";
            orinfo += $"[C]Unit 3106, East Tower, Phil.\n";
            orinfo += $"[C]Stock Exchange Center\n";
            orinfo += $"[C]Exchange Road,Ortigas Center,\n";
            orinfo += $"[C]Pasig City 1605\n";
            orinfo += $"[C]VAT REG TIN :\n";
            orinfo += $"[C]010-364-544-000\n";
            orinfo += $"[C]ACCREDITATION NO :\n";
            orinfo += $"[C]{result.AccreditationNo} \n";
            orinfo += $"[C]VALID UNTIL :{result.AccreditationValidUntil} \n";
            orinfo += $"[C]DATE ISSUED :{result.AccreditationDate} \n";
            orinfo += $"[C]PTU NO :\n";
            orinfo += $"[C]{result.PTUNo} \n";
            orinfo += $"[C]DATE ISSUED :{result.PTUDateIssued} \n\n";
            orinfo += $"[C]THANK YOU!\n";
            orinfo += $"[C]================================\n";
            result.Printable = orinfo;
            return Ok(result);
        }

        [HttpGet]
        [Route("api/ticket/searchor")]
        public async Task<IHttpActionResult> SearchOfficialReceipt(int gateid, string keyword = "")
        {
            var items = await services.OfficialReceiptSearch(gateid, keyword);
            return Ok(items);
        }

        [HttpGet]
        [Route("api/ticket/isvaliduser")]
        public async Task<IHttpActionResult> IsValidUser(string username, string password,string gateid)
        {
            var result = await services.IsValidUser(username, password,gateid);
            var item = new LoginViewModel
            {
                Key = Security.EncryptToBase64(result.ToString()),
                IsValid = result.IsValid,
                Id = result.UserId,
                Name = result.Name
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
        [HttpGet]
        [Route("api/ticket/forxreading")]
        public async Task<IHttpActionResult> GetForXReading(int gateid)
        {
            var result = await services.GetForXReading(gateid);
            return Ok(result);
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
            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetXReadingAsync(srno);
            //            orinfo += $"[C]<b>OFFICIAL RECEIPT</b>\n\n";
            bodyString += $"[C]<b>X READING</b>\n";
            bodyString += $"[L]CASHIER  :{body.FirstOrDefault().CashierName}\n";
            bodyString += $"[L]LOCATION :{body.FirstOrDefault().Location}\n";
            bodyString += $"[L]TERMINAL :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"[L]SR NO    :{srno}\n";
            bodyString += $"[L]TIME IN  :{body.FirstOrDefault().TimeIn.ToUpper()}\n";
            bodyString += $"[L]TIME OUT :{body.FirstOrDefault().TimeOut.ToUpper()}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE       IN    OUT   REMAINING\n";
            bodyString += $"[L]PARKER     [L]{body.FirstOrDefault().ParkerIn}  [L]{body.FirstOrDefault().ParkerOut} [R] \n";     
            bodyString += $"[L]RESERVED   [L]{body.FirstOrDefault().ReservedIn}  [L]{body.FirstOrDefault().ReservedIn} [R] \n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]RATE TYPE        COUNT   AMOUNT\n";
            bodyString += $"[C]================================\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[C]PENALTY COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"[L]OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[C]CASHLESS COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            var cashless = await services.GetCashlessForXReading(srno);
            foreach(var bodyItem in cashless)
            {
                bodyString += $"[L]{bodyItem.RateType} [C]{bodyItem.Count}   [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TOTAL TRANSACTION :[R]{body.FirstOrDefault().TotalTransaction}\n";
            bodyString += $"[L]TOTAL PARTIAL     :[R]{body.FirstOrDefault().TotalPartial}\n";
            bodyString += $"[L]TOTAL TENDERED    :[R]{body.FirstOrDefault().TotalTendered}\n";
            bodyString += $"[L]VARIANCE          :[R]{body.FirstOrDefault().TotalVariance}\n";
            bodyString += $"[C]================================\n";
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

            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetYReadingAsync(srno);
            bodyString += $"[C]<b>Y READING</b>\n";
            bodyString += $"[L]LOCATION    :{body.FirstOrDefault().Location}\n";
            bodyString += $"[L]TERMINAL    :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"[L]SR NO       :{srno}\n";
            bodyString += $"[L]TIME IN     :{body.FirstOrDefault().TimeIn.ToUpper()}\n";
            bodyString += $"[L]TIME OUT    :{body.FirstOrDefault().TimeOut.ToUpper()}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE       IN    OUT   REMAINING\n";
            bodyString += $"[L]PARKER     [L]{body.FirstOrDefault().ParkerIn}  [L]{body.FirstOrDefault().ParkerOut} [R] \n";
            bodyString += $"[L]RESERVED   [L]{body.FirstOrDefault().ReservedIn}  [L]{body.FirstOrDefault().ReservedIn} [R] \n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES COUNTER\n";
            bodyString += $"[L]---------------------------------------\n";
            bodyString += $"[L]RATE TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]PENALTY COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOST CARD   [R]{body.FirstOrDefault().LostCardCount}     [R]{body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"[L]OVER NIGHT  [R]{body.FirstOrDefault().OvernightCount}     [R]{body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHLESS COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            var cashless = await services.GetCashlessForXReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHIER COLLECTION SUMMARY\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHIERS SALES\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]NAME        START    NO   AMOUNT\n";
            bodyString += $"[C]================================\n";

            var summary = await services.GetCashierSummaryForYReading(srno);

            foreach(var i in summary)
            {
                bodyString += $"[L]{i.Cashier}[R]{i.TimeIn}[R]{i.Count}[R]{i.Amount}\n";
            }

            bodyString += $"[C]================================\n";
            bodyString += $"[L]TOTAL TRANSACTION :[R]{body.FirstOrDefault().TotalTransaction}\n";
            bodyString += $"[L]TOTAL PARTIAL     :[R]{body.FirstOrDefault().TotalPartial}\n";
            bodyString += $"[L]TOTAL TENDERED    :[R]{body.FirstOrDefault().TotalTendered}\n";
            bodyString += $"[L]VARIANCE          :[R]{body.FirstOrDefault().TotalVariance}\n";
            bodyString += $"[C]================================\n";
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

            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetZReadingAsync(srno,userid);
            bodyString += $"[C]<b>Z READING</b>\n";
            bodyString += $"[L]LOCATION    :{body.FirstOrDefault().Location}\n";
            bodyString += $"[L]TERMINAL    :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"[L]SR NO       :{srno}\n";
            bodyString += $"[L]TIME IN     :{body.FirstOrDefault().TimeIn.ToUpper()}\n";
            bodyString += $"[L]TIME OUT    :{body.FirstOrDefault().TimeOut.ToUpper()}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TICKET COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]NEW R-TICKET NO : {body.FirstOrDefault().NewRT}\n";
            bodyString += $"[L]OLD R-TICKET NO : {body.FirstOrDefault().OldRT}\n";
            bodyString += $"[L]NEW F-TICKET NO : {body.FirstOrDefault().NewFT}\n";
            bodyString += $"[L]OLD F-TICKET NO : {body.FirstOrDefault().OldFT}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]RECEIPT COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]NEW FR NO : {body.FirstOrDefault().NewFR}\n";
            bodyString += $"[L]OLD FR NO : {body.FirstOrDefault().OldFR}\n";
            bodyString += $"[L]NEW OR NO : {body.FirstOrDefault().NewOR}\n";
            bodyString += $"[L]OLD OR NO : {body.FirstOrDefault().OldOR}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]OLD GRAND SALES : {body.FirstOrDefault().OldGrandSales}\n";
            bodyString += $"[L]TODAY SALES     : {body.FirstOrDefault().TodaySales}\n";
            bodyString += $"[L]NEW GRAND SALES : {body.FirstOrDefault().NewGrandSales}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]RATE TYPE        COUNT    AMOUNT\n";
            bodyString += $"[C]================================\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]PENALTY COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"[L]OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHLESS COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            var cashless = await services.GetCashlessForZReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]VAT BREAKDOWN\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]VATABLE SALES : [R]{body.FirstOrDefault().VatableSales}\n";
            bodyString += $"[L]VAT AMOUNT    : [R]{body.FirstOrDefault().VatAmount}\n";
            bodyString += $"[L]VAT EXEPMT    : [R]0.00\n";
            bodyString += $"[L]ZERO RATE     : [R]0.00\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]Z-COUNT [R]{body.FirstOrDefault().ZCount}\n";
            bodyString += $"[L]RESET COUNTER [R]{body.FirstOrDefault().Reset}\n";
            bodyString += $"[L]TOTAL ACCUMULATED SALES [R]{body.FirstOrDefault().Total}\n";
            bodyString += $"[L]PREPARED BY : [R]{body.FirstOrDefault().PreparedBy}\n";
            bodyString += $"[C]================================\n";
            item.Body = bodyString;
            return Ok(item);
        }


        #region  CHANGE FUND AND TENDER DECLARATION

        [HttpGet]
        [Route("api/ticket/checkchangefund")]
        public async Task<IHttpActionResult> CheckChangeFund(int userid, int gateid)
        {
            var result = await services.CheckChangeFund(userid, gateid);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/setchangefund")]
        public async Task<IHttpActionResult> SetChangeFund(int id, decimal fund)
        {
            var result = await services.SetChangeFund(id, fund);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/ticket/checktenderdeclaration")]
        public async Task<IHttpActionResult> CheckTenderDeclaration(int id, int gateid)
        {
            var result = await services.CheckTenderDeclaration(id,gateid);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/ticket/settenderdeclaration")]
        public async Task<IHttpActionResult> SetTenderDeclaration(int id, int v1000,int v500,int v200, int v100, int v50, int v20, int v10, int v5, int v1, int vcent, string comment)
        {
            var item = new TenderDeclarationValue
            {
                Id = id,
                ValueFor1000 = v1000,
                ValueFor500 = v500,
                ValueFor200 = v200,
                ValueFor100 = v100,
                ValueFor50 = v50,
                ValueFor20 = v20,
                ValueFor10 = v10,
                ValueFor5 = v5,
                ValueFor1 = v1,
                ValueForCent = vcent,
                Comment = comment,
            };
            var result = await services.SetTenderDeclaration(item);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/gettenderdeclaration")]
        public async Task<IHttpActionResult> GetTenderDeclaration(int gateid,int id)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();

            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetTenderDeclaration(id);
            bodyString += $"[C]================================\n";
            bodyString += $"[C]<b>TENDER DECLARATION</b>\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOCATION  :{body.Location}\n";
            bodyString += $"[L]GATE      :{body.Gate}\n";
            bodyString += $"[L]SR NO     :{body.SrNoString}\n";
            bodyString += $"[L]CASHIER   :{body.Cashier}\n";
            bodyString += $"[L]SHIFT IN  :{body.ShiftIn}\n";
            bodyString += $"[L]SHIFT OUT :{body.ShiftOut}\n";
            bodyString += $"[C]================================\n";
            bodyString += $" COUNT      CASH      AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]{body.PHP100}       1000 PHP    {(decimal.Parse(body.PHP1000) * 1000).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP500}       500  PHP    {(decimal.Parse(body.PHP500) * 500).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP200}       200  PHP    {(decimal.Parse(body.PHP200) * 200).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP100}       100  PHP    {(decimal.Parse(body.PHP100) * 100).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP50}       50   PHP    {(decimal.Parse(body.PHP50) * 50).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP20}       20   PHP    {(decimal.Parse(body.PHP20) * 20).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP10}       10   PHP    {(decimal.Parse(body.PHP10) * 10).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP5}       5    PHP    {(decimal.Parse(body.PHP5) * 5).ToString("N2")}\n";
            bodyString += $"[L]{body.PHP1}       1    PHP    {(decimal.Parse(body.PHP1) * 1).ToString("N2")}\n";
            bodyString += $"[L]{body.CENT1}       1    CENT   {(decimal.Parse(body.CENT1)/ 100).ToString("N2")}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TOTAL CASH         :{decimal.Parse(body.TotalConfirmed).ToString("N2")} PHP\n";
            bodyString += $"[L]TOTAL PARTIAL      :{decimal.Parse(body.Partial).ToString("N2")} PHP\n";
            bodyString += $"[C]================================\n";
            item.Body = bodyString;
            return Ok(item);
        }

        [HttpGet]
        [Route("api/ticket/getchangefundprint")]
        public async Task<IHttpActionResult> GetChangeFund(int gateid, int id)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();
            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetChangeFund(id);
            bodyString += $"[C]================================\n";
            bodyString += $"[C]<b>USER LOG IN</b>\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOCATION :{body.ParkingName}\n";
            bodyString += $"[L]TERMINAL :{body.GateName}\n";
            bodyString += $"[L]SR NO    :{body.SrNoString}\n";
            bodyString += $"[L]CASHIER  :{body.Username}\n";
            bodyString += $"[L]SHIFT IN :{body.TimeIn}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[C]<b>CHANGE FUND</b>\n";
            bodyString += $"[C]<b>P {decimal.Parse(body.StartAmount).ToString("N2")}</b>\n";
            bodyString += $"[C]================================\n\n\n";
            item.Body = bodyString;
            return Ok(item);
        }

        #endregion
        [HttpGet]
        [Route("api/ticket/getdiscounttypes")]
        public async Task<IHttpActionResult> GetDiscountTypes()
        {
            var items = await services.GetDiscountTypes();
            return Ok(items);
        }
        [HttpGet]
        [Route("api/ticket/gettransactiontypes")]
        public async Task<IHttpActionResult> GetTransactionTypes()
        {
            var items = await services.TransactionTypes();
            return Ok(items);
        }
        [HttpGet]
        [Route("api/ticket/resetcounter")]
        public async Task<IHttpActionResult> ResetCounter(int gateId)
        {
            var items = await services.ResetCounter(gateId);
            return Ok(items);
        }
        [HttpGet]
        [Route("api/ticket/ticketlist")]
        public async Task<IHttpActionResult> GetTicketList(int gateId,string keyword)
        {
            var items = await services.GetTicketList(gateId,keyword);
            return Ok(items);
        }
        [HttpGet]
        [Route("api/ticket/testconnection")]
        public async Task<IHttpActionResult> TestConnection()
        {
            var success = true;
            return Ok(success);
        }
        [HttpGet]
        [Route("api/ticket/reprintticket")]
        public async Task<IHttpActionResult> ReprintTicket(string ticketNo)
        {
            var sql = $"EXEC [dbo].[spGetTicketForReprint] @TicketNo = '{ticketNo}'";
            var dt = await SCObjects.LoadDataTableAsync(sql, UserConnectionString);
            var reprintCount = 0;
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
            reprintCount = int.Parse(dt.Rows[0]["ReprintCount"].ToString());
            var ticketInfo = string.Empty;
            ticketInfo += $"[C]{ticket.Company}\n";
            ticketInfo += $"[C]{ticket.Address1}\n";
            ticketInfo += $"[C]{ticket.Address2}\n";
            ticketInfo += $"[C]{ticket.Address3}\n";
            ticketInfo += $"[C]================================\n";
            ticketInfo += $"[C]REPRINT COUNT: {reprintCount}\n";
            ticketInfo += $"[C]================================\n";
            ticketInfo += $"[L]TIN      : {ticket.TIN}\n";
            ticketInfo += $"[L]PLATENO  : {ticket.PlateNo}\n";
            ticketInfo += $"[L]TICKETNO : {ticket.TicketNo}\n";
            ticketInfo += $"[L]LOCATION : {ticket.Location}\n";
            ticketInfo += $"[L]TIME IN  : {ticket.TimeIn}\n";
            ticketInfo += $"[L]TERMINAL : {ticket.Terminal}\n";
            ticketInfo += $"[C]================================\n";
            ticketInfo += $"[C] <qrcode size='20'>{ticket.TicketNo}</qrcode>\n";
            ticketInfo += $"[C]================================\n";
            ticket.Printable = ticketInfo;
            return Ok(ticket);
        }

        [HttpGet]
        [Route("api/ticket/auditlogs")]
        public async Task<IHttpActionResult> SetAuditLogs(string description,int gateId, int userId)
        {
            var result = await services.SetAuditLogs(description, gateId, userId);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/ticket/readingitems")]
        public async Task<IHttpActionResult> GetReadingItems(int gateId, string type, string keyword)
        {
            var result = await services.GetReadingItems(gateId, type, keyword);
            return Ok(result);
        }
        [HttpGet]
        [Route("api/ticket/gateinformation")]
        public async Task<IHttpActionResult> GetInformation(int gateId)
        {
            var result = await services.GateInformation(gateId);
            return Ok(result);
        }
        #region Readings Reprint
        [HttpGet]
        [Route("api/ticket/xreadingreprint")]
        public async Task<IHttpActionResult> ReprintXReading(int gateid, string srno)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();
            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetXReadingAsync(srno);
            bodyString += $"[C]<b>X READING</b>\n";
            var reprint = await services.GetReprintCount(srno, ReprintType.XReading);
            if(reprint > 0)
            {
                bodyString += $"[L]REPRINT  :{reprint}\n";
            }
            bodyString += $"[L]CASHIER  :{body.FirstOrDefault().CashierName}\n";
            bodyString += $"[L]LOCATION :{body.FirstOrDefault().Location}\n";
            bodyString += $"[L]TERMINAL :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"[L]SR NO    :{srno}\n";
            bodyString += $"[L]TIME IN  :{body.FirstOrDefault().TimeIn.ToUpper()}\n";
            bodyString += $"[L]TIME OUT :{body.FirstOrDefault().TimeOut.ToUpper()}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE       IN    OUT   REMAINING\n";
            bodyString += $"[L]PARKER     [L]{body.FirstOrDefault().ParkerIn}  [L]{body.FirstOrDefault().ParkerOut} [R] \n";
            bodyString += $"[L]RESERVED   [L]{body.FirstOrDefault().ReservedIn}  [L]{body.FirstOrDefault().ReservedIn} [R] \n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]RATE TYPE        COUNT   AMOUNT\n";
            bodyString += $"[C]================================\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[C]PENALTY COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"[L]OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[C]CASHLESS COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            var cashless = await services.GetCashlessForXReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"[L]{bodyItem.RateType} [C]{bodyItem.Count}   [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TOTAL TRANSACTION :[R]{body.FirstOrDefault().TotalTransaction}\n";
            bodyString += $"[L]TOTAL PARTIAL     :[R]{body.FirstOrDefault().TotalPartial}\n";
            bodyString += $"[L]TOTAL TENDERED    :[R]{body.FirstOrDefault().TotalTendered}\n";
            bodyString += $"[L]VARIANCE          :[R]{body.FirstOrDefault().TotalVariance}\n";
            bodyString += $"[C]================================\n";
            item.Body = bodyString;
            return Ok(item);
        }

        [HttpGet]
        [Route("api/ticket/yreadingreprint")]
        public async Task<IHttpActionResult> ReprintYReading(int gateid, string srno)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();

            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetYReadingAsync(srno);
            bodyString += $"[C]<b>Y READING</b>\n";
            var reprint = await services.GetReprintCount(srno, ReprintType.YReading);
            if(reprint > 0)
            {
                bodyString += $"[L]REPRINT    :{reprint}\n";
            }
            bodyString += $"[L]LOCATION    :{body.FirstOrDefault().Location}\n";
            bodyString += $"[L]TERMINAL    :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"[L]SR NO       :{srno}\n";
            bodyString += $"[L]TIME IN     :{body.FirstOrDefault().TimeIn.ToUpper()}\n";
            bodyString += $"[L]TIME OUT    :{body.FirstOrDefault().TimeOut.ToUpper()}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE       IN    OUT   REMAINING\n";
            bodyString += $"[L]PARKER     [L]{body.FirstOrDefault().ParkerIn}  [L]{body.FirstOrDefault().ParkerOut} [R] \n";
            bodyString += $"[L]RESERVED   [L]{body.FirstOrDefault().ReservedIn}  [L]{body.FirstOrDefault().ReservedIn} [R] \n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES COUNTER\n";
            bodyString += $"[L]---------------------------------------\n";
            bodyString += $"[L]RATE TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]PENALTY COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOST CARD   [R]{body.FirstOrDefault().LostCardCount}     [R]{body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"[L]OVER NIGHT  [R]{body.FirstOrDefault().OvernightCount}     [R]{body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHLESS COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            var cashless = await services.GetCashlessForXReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHIER COLLECTION SUMMARY\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHIERS SALES\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]NAME        START    NO   AMOUNT\n";
            bodyString += $"[C]================================\n";

            var summary = await services.GetCashierSummaryForYReading(srno);

            foreach (var i in summary)
            {
                bodyString += $"[L]{i.Cashier}[R]{i.TimeIn}[R]{i.Count}[R]{i.Amount}\n";
            }

            bodyString += $"[C]================================\n";
            bodyString += $"[L]TOTAL TRANSACTION :[R]{body.FirstOrDefault().TotalTransaction}\n";
            bodyString += $"[L]TOTAL PARTIAL     :[R]{body.FirstOrDefault().TotalPartial}\n";
            bodyString += $"[L]TOTAL TENDERED    :[R]{body.FirstOrDefault().TotalTendered}\n";
            bodyString += $"[L]VARIANCE          :[R]{body.FirstOrDefault().TotalVariance}\n";
            bodyString += $"[C]================================\n";
            item.Body = bodyString;
            return Ok(item);
        }

        [HttpGet]
        [Route("api/ticket/zreadingreprint")]
        public async Task<IHttpActionResult> ReprintZReading(int gateid, string srno, int userid)
        {
            var header = await services.GetReportHeaderAsync(gateid);
            var item = new ReadingResponse();

            var bodyString = string.Empty;
            bodyString += $"[C]{header.Company}\n";
            bodyString += $"[C]{header.Address1}\n";
            bodyString += $"[C]{header.Address2}\n";
            bodyString += $"[C]{header.Address3}\n";
            bodyString += $"[C]VAT REG TIN :\n";
            bodyString += $"[C]{header.TIN} :\n";
            bodyString += $"[C]ACCREDITATION NO :\n";
            bodyString += $"[C]{header.AccreditationNo} :\n";
            //bodyString += $"[C]VALID UNTIL :{header.AccreditationValidUntil} \n";
            //bodyString += $"[C]DATE ISSUED :{header.AccreditationDate} \n";
            //bodyString += $"[C]PTU NO :\n";
            //bodyString += $"[C]{header.PTUNo} \n";
            //bodyString += $"[C]DATE ISSUED :{header.PTUDateIssued} \n\n";
            var body = await services.GetZReadingAsync(srno, userid);
            bodyString += $"[C]<b>Z READING</b>\n";
            var reprint = await services.GetReprintCount(srno, ReprintType.ZReading);
            if(reprint > 0)
            {
                bodyString += $"[L]REPRINT    :{reprint}\n";
            }
            bodyString += $"[L]LOCATION    :{body.FirstOrDefault().Location}\n";
            bodyString += $"[L]TERMINAL    :{body.FirstOrDefault().Terminal}\n";
            bodyString += $"[L]SR NO       :{srno}\n";
            bodyString += $"[L]TIME IN     :{body.FirstOrDefault().TimeIn.ToUpper()}\n";
            bodyString += $"[L]TIME OUT    :{body.FirstOrDefault().TimeOut.ToUpper()}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TICKET COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]NEW R-TICKET NO : {body.FirstOrDefault().NewRT}\n";
            bodyString += $"[L]OLD R-TICKET NO : {body.FirstOrDefault().OldRT}\n";
            bodyString += $"[L]NEW F-TICKET NO : {body.FirstOrDefault().NewFT}\n";
            bodyString += $"[L]OLD F-TICKET NO : {body.FirstOrDefault().OldFT}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]RECEIPT COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]NEW FR NO : {body.FirstOrDefault().NewFR}\n";
            bodyString += $"[L]OLD FR NO : {body.FirstOrDefault().OldFR}\n";
            bodyString += $"[L]NEW OR NO : {body.FirstOrDefault().NewOR}\n";
            bodyString += $"[L]OLD OR NO : {body.FirstOrDefault().OldOR}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]OLD GRAND SALES : {body.FirstOrDefault().OldGrandSales}\n";
            bodyString += $"[L]TODAY SALES     : {body.FirstOrDefault().TodaySales}\n";
            bodyString += $"[L]NEW GRAND SALES : {body.FirstOrDefault().NewGrandSales}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]SALES COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]RATE TYPE        COUNT    AMOUNT\n";
            bodyString += $"[C]================================\n";
            foreach (var bodyItem in body)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]PENALTY COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]LOST CARD   {body.FirstOrDefault().LostCardCount}     {body.FirstOrDefault().LostCardAmount}\n";
            bodyString += $"[L]OVER NIGHT  {body.FirstOrDefault().OvernightCount}     {body.FirstOrDefault().OvernightAmount}\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]CASHLESS COUNTER\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]TYPE        COUNT     AMOUNT\n";
            bodyString += $"[C]================================\n";
            var cashless = await services.GetCashlessForZReading(srno);
            foreach (var bodyItem in cashless)
            {
                bodyString += $"{bodyItem.RateType}[R]{bodyItem.Count}  [R]{bodyItem.Amount}\n";
            }
            bodyString += $"[C]================================\n";
            bodyString += $"[L]VAT BREAKDOWN\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]VATABLE SALES : [R]{body.FirstOrDefault().VatableSales}\n";
            bodyString += $"[L]VAT AMOUNT    : [R]{body.FirstOrDefault().VatAmount}\n";
            bodyString += $"[L]VAT EXEPMT    : [R]0.00\n";
            bodyString += $"[L]ZERO RATE     : [R]0.00\n";
            bodyString += $"[C]================================\n";
            bodyString += $"[L]Z-COUNT [R]{body.FirstOrDefault().ZCount}\n";
            bodyString += $"[L]RESET COUNTER [R]{body.FirstOrDefault().Reset}\n";
            bodyString += $"[L]TOTAL ACCUMULATED SALES [R]{body.FirstOrDefault().Total}\n";
            bodyString += $"[L]PREPARED BY : [R]{body.FirstOrDefault().PreparedBy}\n";
            bodyString += $"[C]================================\n";
            item.Body = bodyString;
            return Ok(item);
        }
        #endregion
    }
}

