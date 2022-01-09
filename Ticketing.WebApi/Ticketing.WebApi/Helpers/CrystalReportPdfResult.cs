using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ticketing.WebApi
{
    public class CrystalReportPdfResult : ActionResult
    {
        private readonly byte[] _contentBytes;

        public CrystalReportPdfResult(string reportPath, object dataSet, DataTable settings)
        {
            ReportDocument reportDocument = new ReportDocument();
            reportDocument.Load(reportPath);
            reportDocument.DataDefinition.FormulaFields["Company"].Text = FormulaFieldBuilder(settings.Rows[0]["CompanyName"].ToString());
            reportDocument.DataDefinition.FormulaFields["Address1"].Text = FormulaFieldBuilder(settings.Rows[0]["AddressLine1"].ToString());
            reportDocument.DataDefinition.FormulaFields["Address2"].Text = FormulaFieldBuilder(settings.Rows[0]["AddressLine2"].ToString());
            reportDocument.DataDefinition.FormulaFields["Address3"].Text = FormulaFieldBuilder(settings.Rows[0]["AddressLine3"].ToString());
            reportDocument.DataDefinition.FormulaFields["TIN"].Text = FormulaFieldBuilder(settings.Rows[0]["VAT_REG_TIN"].ToString());
            reportDocument.SetDataSource(dataSet);
            _contentBytes = StreamToBytes(reportDocument.ExportToStream(ExportFormatType.PortableDocFormat));
            reportDocument.Dispose();
            reportDocument.Close();
        }

        public override void ExecuteResult(ControllerContext context)
        {

            var response = context.HttpContext.ApplicationInstance.Response;
            response.Clear();
            response.Buffer = false;
            response.ClearContent();
            response.ClearHeaders();
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.ContentType = "application/pdf";

            using (var stream = new MemoryStream(_contentBytes))
            {
                stream.WriteTo(response.OutputStream);
                stream.Flush();
            }
        }

        private static byte[] StreamToBytes(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        private string FormulaFieldBuilder(string value)
        {
            var item = $"'{value}'";
            return item;
        }
    }
}