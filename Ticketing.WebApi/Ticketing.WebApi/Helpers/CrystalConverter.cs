using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Data;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Ticketing.WebApi
{
    public static class CrystalConverter
    {
        public static byte[] Convert(string reportPath, object dataSet, DataTable settings)
        {
            ReportDocument reportDocument = new ReportDocument();
            reportDocument.Load(reportPath);
            reportDocument.DataDefinition.FormulaFields["Company"].Text = FormulaFieldBuilder(settings.Rows[0]["CompanyName"].ToString());
            reportDocument.DataDefinition.FormulaFields["Address1"].Text = FormulaFieldBuilder(settings.Rows[0]["AddressLine1"].ToString());
            reportDocument.DataDefinition.FormulaFields["Address2"].Text = FormulaFieldBuilder(settings.Rows[0]["AddressLine2"].ToString());
            reportDocument.DataDefinition.FormulaFields["Address3"].Text = FormulaFieldBuilder(settings.Rows[0]["AddressLine3"].ToString());
            reportDocument.DataDefinition.FormulaFields["TIN"].Text = FormulaFieldBuilder(settings.Rows[0]["VAT_REG_TIN"].ToString());
            reportDocument.SetDataSource(dataSet);
            var data = StreamToBytes(reportDocument.ExportToStream(ExportFormatType.HTML40));

            reportDocument.Dispose();
            reportDocument.Close();
            return data;
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
        private static string FormulaFieldBuilder(string value)
        {
            var item = $"'{value}'";
            return item;
        }
    }
}