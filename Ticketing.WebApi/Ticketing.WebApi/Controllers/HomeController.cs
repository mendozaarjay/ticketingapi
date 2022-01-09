using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Ticketing.WebApi.Controllers
{
    public class HomeController : Controller
    {
        public string UserConnectionString { get; }
        public HomeController()
        {
            UserConnectionString = ConfigurationManager.ConnectionStrings["UserConnnectionString"].ConnectionString;
        }
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }
        //public async Task<ActionResult> PrintTicket(string ticketNo, string plateNo ="")
        //{
        //    var sql = $"EXEC [dbo].[spGetTicketInfo] @TicketNo = '{ticketNo}'";
        //    var dt = await SCObjects.LoadDataTableAsync(sql, UserConnectionString);
        //    Zen.Barcode.CodeQrBarcodeDraw qrCode = Zen.Barcode.BarcodeDrawFactory.CodeQr;
        //    var qrresult = qrCode.Draw(ticketNo, 50);
        //    var bytes = ConvertImage(qrresult);
        //    dt.Columns["QRCode"].DataType = typeof(byte[]);
        //    dt.Rows[0]["QRCode"] = bytes;
        //    dt.AcceptChanges();
        //    var path = System.Web.Hosting.HostingEnvironment.MapPath("~/Crystal/ParkerTicket.rpt");
        //    return new CrystalReportPdfResult(path, dt);
        //}
        //public async Task<ActionResult> PrintOR(string ticketNo)
        //{
        //    var sql = $"EXEC [dbo].[spGetOfficialReceiptInformation] @TicketNo = '{ticketNo}'";
        //    var dt = await SCObjects.LoadDataTableAsync(sql, UserConnectionString);
        //    var path = System.Web.Hosting.HostingEnvironment.MapPath("~/Crystal/OfficialReceipt.rpt");
        //    return new CrystalReportPdfResult(path, dt);
        //}

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
